using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Laboratorios.Tests;

internal sealed class AlwaysFreeLock : IPlanningLock
{
    public Task<bool> TryAcquireAsync(IEnumerable<string> resources, CancellationToken ct = default) => Task.FromResult(true);
}

/// <summary>
/// Sprint 3: solicitudes de docentes y stock por horario. Cubre el caso del "fixer" (una unidad usada de 9 a 10
/// y pedida de 11 a 12), que una solicitud pendiente no bloquea, la aprobación con cambios y el historial.
/// </summary>
public class TeacherRequestServiceTests
{
    private const int Fixer = 500;
    private const int Bowl = 501;
    private const int ReviewerUserId = 900;

    private static TeacherRequestService Service(TestDb db)
    {
        var circuit = new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance);
        return new TeacherRequestService(db.Context, new AcademicManagementService(db.Context), db.Occupancy(), db.GroupSchedule(),
            new ArticleAvailabilityService(db.Context, circuit), new AlwaysFreeLock(),
            Options.Create(new TeacherRequestOptions()), NullLogger<TeacherRequestService>.Instance);
    }

    private static void SeedReviewer(TestDb db)
    {
        db.Context.Users.Add(new User { Id = ReviewerUserId, UserName = "revisor", FirstName = "Ana", LastName = "Revisora", Role = UserRole.Administrador });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    [Theory]
    [InlineData("2026-11-09", 2, "2026-11-11")] // lunes + 2 hábiles = miércoles
    [InlineData("2026-11-13", 2, "2026-11-17")] // viernes + 2 hábiles = martes (salta sábado y domingo)
    [InlineData("2026-11-14", 1, "2026-11-16")] // sábado + 1 hábil = lunes
    public void AddBusinessDays_SaltaFinesDeSemana(string from, int days, string expected)
    {
        Assert.Equal(DateTime.Parse(expected), TeacherRequestService.AddBusinessDays(DateTime.Parse(from), days));
    }

    [Fact]
    public async Task Material_FixerUsadoDe9a10_EstaLibreDe11a12()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(Fixer, "FIX", "Fixer", stock: 1);
        // Práctica aprobada periodos 2–3 (08:35–10:15).
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Approved);
        db.AddRequirement(1, Fixer, 1);

        // Pedido periodos 5–6 (11:15–13:05): no se cruza → disponible.
        var later = await Service(db).CheckMaterialAsync(TestDb.Week1Monday, 5, 2, [new RequestLineInput(Fixer, 1)], null);
        Assert.Equal(MaterialState.Ok, Assert.Single(later).State);
        Assert.Equal(1m, later[0].Free);

        // Pedido periodos 3–4: se cruza en el periodo 3 → no alcanza, y explica quién lo usa.
        var overlap = await Service(db).CheckMaterialAsync(TestDb.Week1Monday, 3, 2, [new RequestLineInput(Fixer, 1)], null);
        var line = Assert.Single(overlap);
        Assert.Equal(MaterialState.Short, line.State);
        Assert.Equal(0m, line.Free);
        Assert.Single(line.Uses);
        Assert.Contains("Se libera", line.Explanation);
    }

    [Fact]
    public async Task Material_SolicitudPendiente_NoConsumeStock()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(Fixer, "FIX", "Fixer", stock: 1);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Requested);
        db.AddRequirement(1, Fixer, 1);

        var check = await Service(db).CheckMaterialAsync(TestDb.Week1Monday, 2, 2, [new RequestLineInput(Fixer, 1)], null);
        Assert.Equal(MaterialState.Ok, Assert.Single(check).State);
    }

    [Fact]
    public async Task CheckSlot_Aprobada_BloqueaYPendienteSoloAvisa()
    {
        using var db = new TestDb();
        db.AddSubject(70, "Cocina");
        db.AddGroup(80, "G-80", "A", 70, teacherPersonId: TestDb.Teacher1);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2, status: PlanningStatus.Requested);

        var input = new RequestInput { CourseGroupId = 80, Date = TestDb.Week1Monday, StartPeriodId = 2, PeriodCount = 2, LaboratoryId = TestDb.LabB };
        var pending = await Service(db).CheckSlotAsync(TestDb.Teacher1, input, null, forApproval: true);
        Assert.False(pending.HasBlocks);
        Assert.Contains(pending.Issues, i => i.Code == "PENDING_COMPETITION" && i.Severity == CheckSeverity.Warning);

        db.AddPlanning(2, TestDb.Week1Monday, 3, 1, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2, status: PlanningStatus.Approved);
        var approved = await Service(db).CheckSlotAsync(TestDb.Teacher1, input, null, forApproval: true);
        Assert.Contains(approved.Blocks, i => i.Code == "LAB_OCCUPIED");
    }

    [Fact]
    public async Task Aprobar_ConCambios_RegistraHistorialYLineas()
    {
        using var db = new TestDb();
        SeedReviewer(db);
        db.AddSubject(70, "Cocina");
        db.AddGroup(80, "G-80", "A", 70);
        db.AddArticleWithStock(Bowl, "BOW", "Bowl", stock: 30);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddRequirement(1, Bowl, 12);

        var result = await Service(db).ApproveAsync(ReviewerUserId, 1, "Ajustado", [new RequestLineInput(Bowl, 10)]);
        Assert.True(result.Ok, result.Message);

        var plan = await db.Context.Plannings.AsNoTracking().Include(p => p.Requirements).SingleAsync(p => p.Id == 1);
        Assert.Equal(PlanningStatus.Approved, plan.Status);
        Assert.Equal(10m, Assert.Single(plan.Requirements).RequestedQuantity);

        var review = await db.Context.PlanningReviews.AsNoTracking().Include(r => r.Lines).SingleAsync();
        Assert.Equal(PlanningReviewAction.ApprovedWithChanges, review.Action);
        var line = Assert.Single(review.Lines);
        Assert.Equal(12m, line.PreviousQuantity);
        Assert.Equal(10m, line.NewQuantity);

        // El docente ve lo que pidió (12) frente a lo aprobado (10).
        var view = Assert.Single(await Service(db).GetLinesAsync(1));
        Assert.True(view.ChangedByReviewer);
        Assert.Equal(12m, view.RequestedByTeacher);
    }

    [Fact]
    public async Task Aprobar_ConChoqueDeLaboratorio_SeRechaza()
    {
        using var db = new TestDb();
        SeedReviewer(db);
        db.AddSubject(70, "Cocina");
        db.AddGroup(80, "G-80", "A", 70);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddPlanning(2, TestDb.Week1Monday, 2, 1, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2, status: PlanningStatus.Approved);

        var result = await Service(db).ApproveAsync(ReviewerUserId, 1, null, null);
        Assert.False(result.Ok);
        Assert.Contains("No se puede aprobar", result.Message);
        Assert.Equal(PlanningStatus.Requested, (await db.Context.Plannings.AsNoTracking().SingleAsync(p => p.Id == 1)).Status);
    }

    [Fact]
    public async Task Observar_SinComentario_FallaYConComentarioCambiaEstado()
    {
        using var db = new TestDb();
        SeedReviewer(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Requested);

        Assert.False((await Service(db).ObserveAsync(ReviewerUserId, 1, "  ")).Ok);
        var ok = await Service(db).ObserveAsync(ReviewerUserId, 1, "Cambia al jueves, H-02 está ocupado.");
        Assert.True(ok.Ok, ok.Message);
        Assert.Equal(PlanningStatus.Observed, (await db.Context.Plannings.AsNoTracking().SingleAsync(p => p.Id == 1)).Status);
        Assert.Equal(PlanningReviewAction.Observed, (await db.Context.PlanningReviews.AsNoTracking().SingleAsync()).Action);

        // Una observada ya no se puede aprobar ni rechazar hasta que el docente la reenvíe.
        Assert.False((await Service(db).RejectAsync(ReviewerUserId, 1, "Motivo cualquiera")).Ok);
    }

    [Fact]
    public async Task StockPorHorario_EntregarNoDescuenta_YUnIncidenteL5Si()
    {
        using var db = new TestDb();
        db.AddEconomato();
        db.Context.Articles.Add(new Article { Id = Bowl, Code = "BOW", Name = "Bowl" });
        db.Context.ArticleStocks.Add(new ArticleStock { ArticleId = Bowl, LaboratoryId = TestDb.EconomatoId, AvailableQuantity = 20 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Approved);
        db.AddRequirement(1, Bowl, 10);
        var detailId = await db.Context.PlanningRequirements.AsNoTracking().Where(r => r.PlanningId == 1).Select(r => r.Id).SingleAsync();

        var circuit = new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance);
        var created = await circuit.CreateDepartureAsync(1, TestDb.EconomatoId, [new DepartureLine(detailId, 10)], null);
        Assert.True(created.Ok, created.Message);
        Assert.Equal(20m, (await db.Context.ArticleStocks.AsNoTracking().SingleAsync()).AvailableQuantity);

        // Con 10 fuera, una segunda entrega física de 15 desde el mismo origen no alcanza (libre ahora: 10).
        db.AddPlanning(2, TestDb.Week1Monday, 5, 2, status: PlanningStatus.Approved);
        db.AddRequirement(2, Bowl, 15);
        var detail2 = await db.Context.PlanningRequirements.AsNoTracking().Where(r => r.PlanningId == 2).Select(r => r.Id).SingleAsync();
        var second = await circuit.CreateDepartureAsync(2, TestDb.EconomatoId, [new DepartureLine(detail2, 15)], null);
        Assert.False(second.Ok);
        Assert.Contains("libre ahora 10", second.Message);

        // Un incidente L-5 (rotura de 2) sí reduce la existencia, vía SaveChangesAsync.
        db.Context.L5Incidents.Add(new L5Incident
        {
            PlanningId = 1, ArticleId = Bowl, IncidentType = "Rotura", DamageDescription = "Se cayeron",
            AffectedQuantity = 2, ResponsibleName = "Grupo A", ResponsibleType = "Estudiante", RepositionStatus = "Pendiente"
        });
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();
        Assert.Equal(18m, (await db.Context.ArticleStocks.AsNoTracking().SingleAsync()).AvailableQuantity);
    }

    [Fact]
    public void Estados_DeSolicitud_NoOcupan()
    {
        Assert.False(PlanningStatus.Occupies(PlanningStatus.Requested));
        Assert.False(PlanningStatus.Occupies(PlanningStatus.Observed));
        Assert.False(PlanningStatus.Occupies(PlanningStatus.Rejected));
        Assert.True(PlanningStatus.Occupies(PlanningStatus.Approved));
    }
}
