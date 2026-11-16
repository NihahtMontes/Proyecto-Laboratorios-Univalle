using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.ManagementPlans;

namespace Laboratorios.Tests;

/// <summary>
/// Reconstrucción de planes L-48 desde registros existentes: candidatos por par gestión-equipo,
/// enlaces al registro más reciente, regla de fase/estado, idempotencia y lote de importación.
/// </summary>
public class ManagementPlanBackfillServiceTests
{
    private static readonly DateTime September = new(2026, 9, 1);

    private static ManagementPlanBackfillService Service(TestDb db) =>
        new(db.Context,
            new ManagementContextService(db.Context, new MemoryCache(new MemoryCacheOptions())),
            NullLogger<ManagementPlanBackfillService>.Instance);

    /// <summary>Dos equipos operativos en la gestión 1 (creada por TestDb).</summary>
    private static TestDb Seed(int unitId = 100, int secondUnitId = 101)
    {
        var db = new TestDb();
        db.AddEquipmentWithUnit(10, unitId, $"INV-{unitId}", 200, $"ART-{unitId}", "Balanza analítica");
        db.AddEquipmentWithUnit(11, secondUnitId, $"INV-{secondUnitId}", 201, $"ART-{secondUnitId}", "Multímetro");
        return db;
    }

    private static TestDb SeedSingle(int unitId, EquipmentStatus status = EquipmentStatus.Operational)
    {
        var db = new TestDb();
        db.AddEquipmentWithUnit(10, unitId, $"INV-{unitId}", 200, $"ART-{unitId}", "Balanza analítica", status);
        return db;
    }

    private static void Save(TestDb db, params object[] entities)
    {
        db.Context.AddRange(entities);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    // ───────────────────────────── (a) verificación sin falla

    [Fact]
    public async Task VerificacionSinFalla_CreaPlanConVerificationYEstadoVerifiedGood()
    {
        using var db = Seed();
        Save(db, new Verification
        {
            EquipmentUnitId = 100, ManagementId = TestDb.ManagementId,
            Date = September, Status = VerificationStatus.Completed, ResponsiblePersonId = 55
        });

        var result = await Service(db).ApplyAsync(TestDb.ManagementId, 7);

        Assert.True(result.Ok);
        Assert.Equal(1, result.CreatedPlans);
        var plan = await db.Context.ManagementPlans.AsNoTracking().SingleAsync(p => p.ManagementId == TestDb.ManagementId);
        Assert.Equal(100, plan.EquipmentUnitId);
        Assert.NotNull(plan.VerificationId);
        Assert.Null(plan.RequestId);
        Assert.Null(plan.MaintenanceId);
        Assert.Equal(WizardPhase.Verification, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.VerifiedGood, plan.CurrentState);
        Assert.Equal(ManagementPlanStatus.InProgress, plan.PlanStatus);
        Assert.Equal(September.Date, plan.PlannedDate!.Value.Date);
        Assert.Equal(55, plan.ResponsiblePersonId);
        Assert.False(plan.IsDraft);
        // CreatedById lo estampa ApplicationDbContext (ICurrentUserService): con el FakeCurrentUser de TestDb queda null.
        Assert.Null(plan.CreatedById);
    }

    // ───────────────────────────── (b) par con plan existente

    [Fact]
    public async Task ParConPlanExistente_SeOmite()
    {
        using var db = Seed();
        Save(db,
            new Verification { EquipmentUnitId = 100, ManagementId = TestDb.ManagementId, Date = September, Status = VerificationStatus.Completed },
            new ManagementPlan
            {
                ManagementId = TestDb.ManagementId, EquipmentUnitId = 100,
                CurrentPhase = WizardPhase.Verification, CurrentState = WizardEquipmentState.VerifiedGood,
                PlanStatus = ManagementPlanStatus.InProgress
            });

        var result = await Service(db).ApplyAsync(null, null);

        Assert.True(result.Ok);
        Assert.Equal(0, result.CreatedPlans);
        Assert.Null(result.ImportBatchId);
        Assert.Single(await db.Context.ManagementPlans.AsNoTracking().ToListAsync());
    }

    // ───────────────────────────── (c) salida con PlanningId se ignora

    [Fact]
    public async Task SalidaConPlanningId_SeIgnora()
    {
        using var db = Seed();
        Save(db, new Departure
        {
            ManagementId = TestDb.ManagementId, EquipmentUnitId = 100, PlanningId = 999,
            Type = DepartureType.InternalLoan, Status = LoanStatus.Active, DepartureDate = September
        });

        var preview = await Service(db).PreviewAsync(null);

        Assert.Equal(0, preview.TotalNewPlans);
        Assert.Equal(0, preview.TotalCandidateUnits);
    }

    // ───────────────────────────── (d) cadena con falla + solicitud + mantenimiento terminado

    [Fact]
    public async Task CadenaFallaSolicitudMantenimiento_EnlazaLosTresYEstadoSegunRegla()
    {
        using var db = Seed();
        Save(db,
            new Verification
            {
                EquipmentUnitId = 100, ManagementId = TestDb.ManagementId, Date = September,
                Status = VerificationStatus.WithObservations,
                Faults = [new VerificationFault { Description = "No enciende la pantalla" }]
            },
            new Request
            {
                ManagementId = TestDb.ManagementId, EquipmentUnitId = 100, Type = RequestType.Technical,
                Status = RequestStatus.Approved, RequestDate = September.AddDays(2), Description = "Cambio de pantalla"
            });
        var request = await db.Context.Requests.AsNoTracking().SingleAsync();
        Save(db, new Maintenance
        {
            EquipmentUnitId = 100, ManagementId = TestDb.ManagementId, RequestId = request.Id,
            Status = MaintenanceStatus.Completed, ScheduledDate = September.AddDays(3),
            StartDate = September.AddDays(4), EndDate = September.AddDays(5)
        });
        var maintenance = await db.Context.Maintenances.AsNoTracking().SingleAsync();
        var verification = await db.Context.Verifications.AsNoTracking().SingleAsync();

        var result = await Service(db).ApplyAsync(TestDb.ManagementId, null);

        Assert.True(result.Ok);
        Assert.Equal(1, result.CreatedPlans);
        var plan = await db.Context.ManagementPlans.AsNoTracking().SingleAsync(p => p.ManagementId == TestDb.ManagementId);
        Assert.Equal(verification.Id, plan.VerificationId);
        Assert.Equal(request.Id, plan.RequestId);
        Assert.Equal(maintenance.Id, plan.MaintenanceId);
        Assert.Null(plan.DepartureId);
        // Regla: L-8 completado → fase Salida, esperando L-3, plan completado (como la importación histórica).
        Assert.Equal(WizardPhase.Exit, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingDeparture, plan.CurrentState);
        Assert.Equal(ManagementPlanStatus.Completed, plan.PlanStatus);
        Assert.Equal(September.Date, plan.PlannedDate!.Value.Date);
    }

    // ───────────────────────────── (e) dos ejecuciones seguidas

    [Fact]
    public async Task DobleEjecucion_LaSegundaNoCreaNada()
    {
        using var db = Seed();
        Save(db, new Verification { EquipmentUnitId = 100, ManagementId = TestDb.ManagementId, Date = September, Status = VerificationStatus.Completed });

        var service = Service(db);
        var first = await service.ApplyAsync(null, null);
        var second = await service.ApplyAsync(null, null);

        Assert.True(first.Ok);
        Assert.Equal(1, first.CreatedPlans);
        Assert.True(second.Ok);
        Assert.Equal(0, second.CreatedPlans);
        Assert.Null(second.ImportBatchId);
        Assert.Single(await db.Context.ManagementPlans.AsNoTracking().ToListAsync());
    }

    // ───────────────────────────── (f) registros cancelados no generan plan

    [Fact]
    public async Task RegistrosCancelados_NoGeneranPlan()
    {
        using var db = Seed();
        Save(db,
            new Verification { EquipmentUnitId = 100, ManagementId = TestDb.ManagementId, Date = September, Status = VerificationStatus.Annulled },
            new Request
            {
                ManagementId = TestDb.ManagementId, EquipmentUnitId = 100, Type = RequestType.Technical,
                Status = RequestStatus.Cancelled, RequestDate = September, Description = "Anulada"
            },
            new Maintenance { EquipmentUnitId = 100, ManagementId = TestDb.ManagementId, Status = MaintenanceStatus.Cancelled },
            new Departure
            {
                ManagementId = TestDb.ManagementId, EquipmentUnitId = 100, PlanningId = null,
                Type = DepartureType.InternalMaintenance, Status = LoanStatus.Cancelled, DepartureDate = September
            });

        var preview = await Service(db).PreviewAsync(null);
        var result = await Service(db).ApplyAsync(null, null);

        Assert.Equal(0, preview.TotalNewPlans);
        Assert.Equal(0, preview.TotalCandidateUnits);
        Assert.True(result.Ok);
        Assert.Equal(0, result.CreatedPlans);
        Assert.Empty(await db.Context.ManagementPlans.AsNoTracking().ToListAsync());
    }

    // ───────────────────────────── (g) la vista previa cuenta lo que crea Apply

    [Fact]
    public async Task PreviewCuentaIgualQueApply()
    {
        using var db = Seed();
        var request = new Request
        {
            ManagementId = TestDb.ManagementId, EquipmentUnitId = null, Type = RequestType.Technical,
            Status = RequestStatus.Pending, RequestDate = September.AddDays(1), Description = "Revisión general"
        };
        Save(db, request);
        Save(db,
            new RequestEquipmentUnit { RequestId = request.Id, EquipmentUnitId = 100, IsActive = true },
            new Maintenance { EquipmentUnitId = 101, ManagementId = TestDb.ManagementId, Status = MaintenanceStatus.InProgress, StartDate = September });

        var preview = await Service(db).PreviewAsync(TestDb.ManagementId);
        var result = await Service(db).ApplyAsync(TestDb.ManagementId, 7);

        Assert.True(result.Ok);
        Assert.Equal(preview.TotalNewPlans, result.CreatedPlans);
        Assert.Equal(1, preview.ManagementsCount);
        Assert.Equal(2, preview.TotalCandidateUnits);
        var row = preview.Managements.Single();
        Assert.Equal(2, row.NewPlans);
        Assert.Equal(1, row.NewPlansByState[WizardEquipmentState.AwaitingMaintenance]);
        Assert.Equal(1, row.NewPlansByState[WizardEquipmentState.InMaintenance]);
    }

    // ───────────────────────────── (h) ImportBatch y trazabilidad

    [Fact]
    public async Task CreaImportBatch_YLosPlanesLlevanSuId()
    {
        using var db = Seed();
        Save(db, new Verification { EquipmentUnitId = 100, ManagementId = TestDb.ManagementId, Date = September, Status = VerificationStatus.Completed });

        var result = await Service(db).ApplyAsync(null, 7);

        Assert.True(result.Ok);
        Assert.NotNull(result.ImportBatchId);
        Assert.StartsWith("PLANES-", result.ImportBatchCode);
        var batch = await db.Context.ImportBatches.AsNoTracking().SingleAsync();
        Assert.Equal(result.ImportBatchId, batch.Id);
        Assert.Equal("ReconstruccionPlanes", batch.SourceType);
        Assert.Equal("Reconstrucción de planes desde registros", batch.SourceName);
        Assert.Equal(ImportBatchStatus.Official, batch.Status);
        Assert.Equal(64, batch.SourceSha256.Length);
        Assert.Contains("usuario #7", batch.Notes);
        var plan = await db.Context.ManagementPlans.AsNoTracking().SingleAsync();
        Assert.Equal(batch.Id, plan.ImportBatchId);
    }

    // ───────────────────────────── extras: regla y fuentes de candidatos

    [Fact]
    public async Task SolicitudPurchasing_SoloAlimentaAcquisitionRequestId_YCompletaElPlan()
    {
        using var db = Seed();
        Save(db, new Request
        {
            ManagementId = TestDb.ManagementId, EquipmentUnitId = 100, Type = RequestType.Purchasing,
            Status = RequestStatus.Completed, RequestDate = September, Description = "Compra de repuestos"
        });

        var result = await Service(db).ApplyAsync(TestDb.ManagementId, null);

        Assert.Equal(1, result.CreatedPlans);
        var plan = await db.Context.ManagementPlans.AsNoTracking().SingleAsync();
        Assert.Null(plan.RequestId);
        Assert.NotNull(plan.AcquisitionRequestId);
        Assert.Equal(WizardPhase.Disbursement, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.Completed, plan.CurrentState);
        Assert.Equal(ManagementPlanStatus.Completed, plan.PlanStatus);
    }

    [Fact]
    public async Task SolicitudConUnidadesVinculadas_CreaUnPlanPorUnidadActiva()
    {
        using var db = Seed();
        var request = new Request
        {
            ManagementId = TestDb.ManagementId, EquipmentUnitId = null, Type = RequestType.Technical,
            Status = RequestStatus.Pending, RequestDate = September, Description = "Revisión por laboratorio"
        };
        Save(db, request);
        Save(db,
            new RequestEquipmentUnit { RequestId = request.Id, EquipmentUnitId = 100, IsActive = true },
            new RequestEquipmentUnit { RequestId = request.Id, EquipmentUnitId = 101, IsActive = true },
            new RequestEquipmentUnit { RequestId = request.Id, EquipmentUnitId = 102, IsActive = false, DeactivatedDate = September });

        var result = await Service(db).ApplyAsync(null, null);

        Assert.Equal(2, result.CreatedPlans);
        var plans = await db.Context.ManagementPlans.AsNoTracking().ToListAsync();
        Assert.All(plans, p => Assert.Equal(WizardEquipmentState.AwaitingMaintenance, p.CurrentState));
        Assert.DoesNotContain(plans, p => p.EquipmentUnitId == 102);
    }

    [Fact]
    public async Task EquipoDeBaja_SeExcluye()
    {
        using var db = SeedSingle(100, EquipmentStatus.Decommissioned);
        Save(db, new Verification { EquipmentUnitId = 100, ManagementId = TestDb.ManagementId, Date = September, Status = VerificationStatus.Completed });

        var preview = await Service(db).PreviewAsync(null);

        Assert.Equal(0, preview.TotalNewPlans);
        Assert.Equal(0, preview.TotalCandidateUnits);
    }

    [Fact]
    public async Task SalidaPorMantenimiento_AvanzaAKardex_YLaMasRecientePrevalece()
    {
        using var db = Seed();
        Save(db,
            new Departure
            {
                ManagementId = TestDb.ManagementId, EquipmentUnitId = 100, PlanningId = null,
                Type = DepartureType.InternalMaintenance, Status = LoanStatus.Active, DepartureDate = September
            },
            new Departure
            {
                ManagementId = TestDb.ManagementId, EquipmentUnitId = 100, PlanningId = null,
                Type = DepartureType.ExternalMaintenance, Status = LoanStatus.Returned, DepartureDate = September.AddDays(10)
            });

        var result = await Service(db).ApplyAsync(null, null);

        var plan = await db.Context.ManagementPlans.AsNoTracking().SingleAsync();
        var later = await db.Context.Departures.AsNoTracking().OrderByDescending(d => d.DepartureDate).FirstAsync();
        Assert.Equal(later.Id, plan.DepartureId);
        Assert.Equal(WizardPhase.Kardex, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingKardex, plan.CurrentState);
        Assert.Equal(ManagementPlanStatus.InProgress, plan.PlanStatus);
    }

    [Fact]
    public async Task VerificacionConFalla_SinSolicitud_QuedaEsperandoL7()
    {
        using var db = Seed();
        Save(db, new Verification
        {
            EquipmentUnitId = 100, ManagementId = TestDb.ManagementId, Date = September,
            Status = VerificationStatus.Completed, Observations = "Vidrio trizado"
        });

        await Service(db).ApplyAsync(null, null);

        var plan = await db.Context.ManagementPlans.AsNoTracking().SingleAsync();
        Assert.Equal(WizardPhase.TechnicalRequest, plan.CurrentPhase);
        Assert.Equal(WizardEquipmentState.AwaitingRequest, plan.CurrentState);
        Assert.Equal(ManagementPlanStatus.InProgress, plan.PlanStatus);
    }
}
