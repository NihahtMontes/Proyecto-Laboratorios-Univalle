using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Controllers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Laboratorios.Tests;

/// <summary>
/// Pruebas del rollback del wizard (POST /api/WizardRollback/redo).
/// El controlador se instancia directamente con el contexto de TestDb y un NullLogger:
/// [Authorize]/[ValidateAntiForgeryToken] son filtros de MVC y no se ejecutan en la
/// llamada directa al método, por lo que no hace falta HttpContext ni antiforgery.
/// EF InMemory no aplica índices únicos ni CHECKs: las invariantes de SQL Server
/// (IX_Maintenances_RequestId, CK_EquipmentStateHistories_Dates) se verifican sobre
/// los valores persistidos (RequestId liberado, EndDate &gt;= StartDate). Los registros
/// cancelados se consultan con IgnoreQueryFilters porque el contexto oculta
/// Maintenance/Request/Verification/Departure cancelados/anulados por filtro global.
/// </summary>
public class WizardRollbackTests
{
    private const int UnitId = 500;
    private const int VerificationId = 510;
    private const int RequestId = 520;
    private const int MaintenanceId = 530;
    private const int DepartureId = 540;
    private const int KardexHistoryId = 550;
    private const int PlanId = 560;

    private static readonly DateTime VerificationDate = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RequestDate = new(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MaintenanceStart = new(2026, 10, 6, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DepartureDate = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime KardexStart = new(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc);

    private static WizardRollbackController Controller(TestDb db) =>
        new(db.Context, NullLogger<WizardRollbackController>.Instance);

    /// <summary>Siembra un plan completo en fase Desembolso: L-6, L-7, L-8, L-3 y Kardex cerrados.</summary>
    private static void SeedCompletedPlan(TestDb db, DateTime? kardexStart = null)
    {
        var kardexStartDate = kardexStart ?? KardexStart;

        db.Context.EquipmentUnits.Add(new EquipmentUnit
        {
            Id = UnitId,
            InventoryNumber = "EQ-500",
            CurrentStatus = EquipmentStatus.Operational
        });
        db.Context.Verifications.Add(new Verification
        {
            Id = VerificationId,
            EquipmentUnitId = UnitId,
            ManagementId = TestDb.ManagementId,
            Date = VerificationDate,
            Status = VerificationStatus.Completed
        });
        db.Context.Requests.Add(new Request
        {
            Id = RequestId,
            ManagementId = TestDb.ManagementId,
            EquipmentUnitId = UnitId,
            RequestDate = RequestDate,
            Status = RequestStatus.Approved,
            Type = RequestType.Technical,
            Description = "Solicitud técnica de prueba"
        });
        db.Context.Maintenances.Add(new Maintenance
        {
            Id = MaintenanceId,
            EquipmentUnitId = UnitId,
            ManagementId = TestDb.ManagementId,
            RequestId = RequestId,
            Status = MaintenanceStatus.Completed,
            StartDate = MaintenanceStart,
            EndDate = MaintenanceStart.AddHours(4)
        });
        db.Context.Departures.Add(new Departure
        {
            Id = DepartureId,
            ManagementId = TestDb.ManagementId,
            DepartureDate = DepartureDate,
            Status = LoanStatus.Returned,
            ActualReturnDate = kardexStartDate
        });
        db.Context.EquipmentStateHistories.Add(new EquipmentStateHistory
        {
            Id = KardexHistoryId,
            EquipmentUnitId = UnitId,
            Status = EquipmentStatus.Operational,
            StartDate = kardexStartDate,
            EndDate = null
        });
        db.Context.ManagementPlans.Add(new ManagementPlan
        {
            Id = PlanId,
            ManagementId = TestDb.ManagementId,
            EquipmentUnitId = UnitId,
            VerificationId = VerificationId,
            RequestId = RequestId,
            MaintenanceId = MaintenanceId,
            DepartureId = DepartureId,
            KardexHistoryId = KardexHistoryId,
            CurrentPhase = WizardPhase.Disbursement,
            CurrentState = WizardEquipmentState.AwaitingDisbursement,
            PlanStatus = ManagementPlanStatus.InProgress
        });

        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task RedoPhase_Mantenimiento_ReposicionaPlanYCancelaPosterioresSinBorrar()
    {
        using var db = new TestDb();
        SeedCompletedPlan(db);

        var result = await Controller(db).RedoPhase(
            new RollbackRequestDto { PlanId = PlanId, TargetPhase = WizardPhase.Maintenance });

        Assert.IsType<OkObjectResult>(result);

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Maintenance, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingMaintenance, plan.CurrentState);
        Assert.Equal(ManagementPlanStatus.Pending, plan.PlanStatus);
        Assert.Null(plan.MaintenanceId);
        Assert.Null(plan.DepartureId);
        Assert.Null(plan.KardexHistoryId);
        // Rehacer L-8 NO cancela la solicitud L-7 (fase anterior a la objetivo).
        Assert.Equal(RequestId, plan.RequestId);
        Assert.Equal(VerificationId, plan.VerificationId);

        // El L-8 cancelado se conserva (sin hard-delete), con el vínculo liberado
        // para que IX_Maintenances_RequestId permita registrar un L-8 nuevo.
        // IgnoreQueryFilters: el filtro global oculta los registros cancelados.
        var maintenance = db.Context.Maintenances.IgnoreQueryFilters().AsNoTracking()
            .Single(m => m.Id == MaintenanceId);
        Assert.Equal(MaintenanceStatus.Cancelled, maintenance.Status);
        Assert.Null(maintenance.RequestId);
        Assert.NotNull(maintenance.Observations);
        Assert.Contains("Anulado al rehacer la fase L-8 Mantenimiento del plan", maintenance.Observations);

        var departure = db.Context.Departures.IgnoreQueryFilters().AsNoTracking()
            .Single(d => d.Id == DepartureId);
        Assert.Equal(LoanStatus.Cancelled, departure.Status);

        // El historial de Kardex anterior queda cerrado, nunca eliminado.
        var kardex = db.Context.EquipmentStateHistories.AsNoTracking().Single(h => h.Id == KardexHistoryId);
        Assert.NotNull(kardex.EndDate);
        Assert.True(kardex.EndDate >= kardex.StartDate);
    }

    [Fact]
    public async Task RedoPhase_Mantenimiento_RestauraHistorialConInicioCoherenteYTextoEnEspanol()
    {
        using var db = new TestDb();
        SeedCompletedPlan(db);

        var result = await Controller(db).RedoPhase(
            new RollbackRequestDto { PlanId = PlanId, TargetPhase = WizardPhase.Maintenance });

        Assert.IsType<OkObjectResult>(result);

        var unit = db.Context.EquipmentUnits.AsNoTracking().Single(u => u.Id == UnitId);
        Assert.Equal(EquipmentStatus.UnderMaintenance, unit.CurrentStatus);

        var restored = db.Context.EquipmentStateHistories.AsNoTracking()
            .Single(h => h.Id != KardexHistoryId && h.EquipmentUnitId == UnitId);
        Assert.Equal(EquipmentStatus.UnderMaintenance, restored.Status);
        Assert.Null(restored.EndDate);
        // Fecha coherente: anclada al inicio real del L-8, no a la fecha del rollback.
        Assert.Equal(MaintenanceStart, restored.StartDate);
        // Texto visible en español, sin el nombre inglés del enum.
        Assert.Contains("L-8 Mantenimiento", restored.Reason);
        Assert.DoesNotContain("Maintenance", restored.Reason);
        Assert.DoesNotContain("TechnicalRequest", restored.Reason);
    }

    [Fact]
    public async Task RedoPhase_Kardex_ReposicionaPlanYRestauraHistorialAncladoAlKardexAnterior()
    {
        using var db = new TestDb();
        SeedCompletedPlan(db);

        var result = await Controller(db).RedoPhase(
            new RollbackRequestDto { PlanId = PlanId, TargetPhase = WizardPhase.Kardex });

        Assert.IsType<OkObjectResult>(result);

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Kardex, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingKardex, plan.CurrentState);
        Assert.Null(plan.KardexHistoryId);
        // Rehacer Kardex no toca L-8 ni L-3: siguen vinculados.
        Assert.Equal(MaintenanceId, plan.MaintenanceId);
        Assert.Equal(DepartureId, plan.DepartureId);

        var maintenance = db.Context.Maintenances.AsNoTracking().Single(m => m.Id == MaintenanceId);
        Assert.Equal(MaintenanceStatus.Completed, maintenance.Status);
        Assert.Equal(RequestId, maintenance.RequestId);

        var departure = db.Context.Departures.AsNoTracking().Single(d => d.Id == DepartureId);
        Assert.Equal(LoanStatus.Returned, departure.Status);

        var kardex = db.Context.EquipmentStateHistories.AsNoTracking().Single(h => h.Id == KardexHistoryId);
        Assert.NotNull(kardex.EndDate);

        var restored = db.Context.EquipmentStateHistories.AsNoTracking()
            .Single(h => h.Id != KardexHistoryId && h.EquipmentUnitId == UnitId);
        Assert.Equal(EquipmentStatus.Operational, restored.Status);
        Assert.Null(restored.EndDate);
        // Ancla coherente: el mismo inicio del Kardex anterior, nunca posterior a la
        // devolución real con la que se cerrará el Kardex rehecho.
        Assert.Equal(KardexStart, restored.StartDate);
        Assert.Contains("Kardex", restored.Reason);
        Assert.DoesNotContain("TechnicalRequest", restored.Reason);
    }

    [Fact]
    public async Task RedoPhase_KardexConFechaFutura_CierraHistorialSinRangoDeFechasInvalido()
    {
        using var db = new TestDb();
        var futureStart = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        SeedCompletedPlan(db, kardexStart: futureStart);

        var result = await Controller(db).RedoPhase(
            new RollbackRequestDto { PlanId = PlanId, TargetPhase = WizardPhase.Kardex });

        Assert.IsType<OkObjectResult>(result);

        var kardex = db.Context.EquipmentStateHistories.AsNoTracking().Single(h => h.Id == KardexHistoryId);
        Assert.NotNull(kardex.EndDate);
        // EndDate nunca menor que StartDate (CK_EquipmentStateHistories_Dates):
        // se cierra en su propia fecha de inicio en lugar de lanzar una excepción.
        Assert.Equal(futureStart, kardex.EndDate);
    }

    [Fact]
    public async Task RedoPhase_SolicitudTecnica_AnulaSolicitudYLiberaVinculosSinBorrar()
    {
        using var db = new TestDb();
        SeedCompletedPlan(db);

        var result = await Controller(db).RedoPhase(
            new RollbackRequestDto { PlanId = PlanId, TargetPhase = WizardPhase.TechnicalRequest });

        Assert.IsType<OkObjectResult>(result);

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.TechnicalRequest, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingRequest, plan.CurrentState);
        Assert.Null(plan.RequestId);
        // La verificación L-6 es anterior a la fase objetivo: no se anula.
        Assert.Equal(VerificationId, plan.VerificationId);

        // IgnoreQueryFilters: los registros cancelados siguen en la BD aunque el
        // filtro global los oculte (no hay borrado físico).
        var request = db.Context.Requests.IgnoreQueryFilters().AsNoTracking()
            .Single(r => r.Id == RequestId);
        Assert.Equal(RequestStatus.Cancelled, request.Status);

        var maintenance = db.Context.Maintenances.IgnoreQueryFilters().AsNoTracking()
            .Single(m => m.Id == MaintenanceId);
        Assert.Equal(MaintenanceStatus.Cancelled, maintenance.Status);
        Assert.Null(maintenance.RequestId);

        var restored = db.Context.EquipmentStateHistories.AsNoTracking()
            .Single(h => h.Id != KardexHistoryId && h.EquipmentUnitId == UnitId);
        Assert.Equal(EquipmentStatus.OutOfService, restored.Status);
        Assert.Equal(RequestDate, restored.StartDate);
        Assert.Contains("L-7 Solicitud", restored.Reason);
    }

    [Fact]
    public async Task RedoPhase_CuerpoNulo_RespondeSolicitudInvalidaEnEspanol()
    {
        using var db = new TestDb();

        var result = await Controller(db).RedoPhase(null);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Solicitud inválida.", badRequest.Value);
    }

    [Fact]
    public async Task RedoPhase_FaseNoAnteriorALaActual_RespondeBadRequest()
    {
        using var db = new TestDb();
        SeedCompletedPlan(db);

        var samePhase = await Controller(db).RedoPhase(
            new RollbackRequestDto { PlanId = PlanId, TargetPhase = WizardPhase.Disbursement });
        var badSame = Assert.IsType<BadRequestObjectResult>(samePhase);
        Assert.Equal("Solo se puede rehacer una fase anterior a la fase actual.", badSame.Value);

        var undefinedPhase = await Controller(db).RedoPhase(
            new RollbackRequestDto { PlanId = PlanId, TargetPhase = 0 });
        var badUndefined = Assert.IsType<BadRequestObjectResult>(undefinedPhase);
        Assert.Equal("Solicitud inválida.", badUndefined.Value);
    }

    [Fact]
    public async Task RedoPhase_PlanInexistente_RespondeNotFoundSinAlterarDatos()
    {
        using var db = new TestDb();
        SeedCompletedPlan(db);

        var result = await Controller(db).RedoPhase(
            new RollbackRequestDto { PlanId = 99999, TargetPhase = WizardPhase.Maintenance });

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Plan no encontrado.", notFound.Value);

        var plan = db.Context.ManagementPlans.AsNoTracking().Single(p => p.Id == PlanId);
        Assert.Equal(WizardPhase.Disbursement, plan.CurrentPhase);
        Assert.Equal(MaintenanceId, plan.MaintenanceId);
    }
}
