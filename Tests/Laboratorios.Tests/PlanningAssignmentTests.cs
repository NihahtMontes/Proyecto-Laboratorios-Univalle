using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Laboratorios.Tests;

/// <summary>
/// Fase 2 (V13): asignación de origen por línea al aprobar, salida L-3 desde varios orígenes y artículos consumibles.
/// </summary>
public class PlanningAssignmentTests
{
    private const int Bowl = 600;
    private const int Film = 601;
    private const int ReviewerUserId = 900;
    private const int Econo = TestDb.EconomatoId;

    private static TeacherRequestService Service(TestDb db)
    {
        var circuit = new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance);
        return new TeacherRequestService(db.Context, new AcademicManagementService(db.Context), db.Occupancy(), db.GroupSchedule(),
            new ArticleAvailabilityService(db.Context, circuit), new AlwaysFreeLock(),
            Options.Create(new TeacherRequestOptions()), NullLogger<TeacherRequestService>.Instance);
    }

    private static L3CircuitService Circuit(TestDb db) => new(db.Context, NullLogger<L3CircuitService>.Instance);

    /// <summary>Economato con 3 bowls y Laboratorio A con 5; film transparente (consumible) con 10 en el Economato.</summary>
    private static void Seed(TestDb db)
    {
        db.AddEconomato();
        db.Context.Users.Add(new User { Id = ReviewerUserId, UserName = "revisor", FirstName = "Ana", LastName = "R", Role = UserRole.Administrador });
        db.Context.Articles.AddRange(
            new Article { Id = Bowl, Code = "BOW", Name = "Bowl" },
            new Article { Id = Film, Code = "FIL", Name = "Papel film", ArticleType = ArticleType.Consumable });
        db.Context.ArticleStocks.AddRange(
            new ArticleStock { ArticleId = Bowl, LaboratoryId = Econo, AvailableQuantity = 3 },
            new ArticleStock { ArticleId = Bowl, LaboratoryId = TestDb.LabA, AvailableQuantity = 5 },
            new ArticleStock { ArticleId = Film, LaboratoryId = Econo, AvailableQuantity = 10 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.AddSubject(70, "Cocina");
        db.AddGroup(80, "G-80", "A", 70, teacherPersonId: TestDb.Teacher1);
    }

    private static int RequirementId(TestDb db, int planningId, int articleId) =>
        db.Context.PlanningRequirements.AsNoTracking().Single(r => r.PlanningId == planningId && r.ArticleId == articleId).Id;

    // ───────── Sugerencia y aprobación ─────────

    [Fact]
    public async Task La_sugerencia_toma_primero_el_Economato_y_completa_con_otro_laboratorio()
    {
        using var db = new TestDb();
        Seed(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddRequirement(1, Bowl, 6);

        var line = Assert.Single(await Service(db).SuggestAssignmentsAsync(1));

        Assert.Equal(Econo, line.Origins[0].LaboratoryId);
        Assert.Equal(3m, line.Suggested.Single(s => s.LaboratoryId == Econo).Quantity);
        Assert.Equal(3m, line.Suggested.Single(s => s.LaboratoryId == TestDb.LabA).Quantity);
    }

    [Fact]
    public async Task Aprobar_sin_reparto_guarda_el_sugerido()
    {
        using var db = new TestDb();
        Seed(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddRequirement(1, Bowl, 6);

        var result = await Service(db).ApproveAsync(ReviewerUserId, 1, null, null);

        Assert.True(result.Ok, result.Message);
        var saved = await db.Context.PlanningAssignments.AsNoTracking().OrderBy(a => a.LaboratoryId).ToListAsync();
        Assert.Equal([(TestDb.LabA, 3m), (Econo, 3m)], saved.Select(a => (a.LaboratoryId, a.Quantity)).OrderBy(x => x.LaboratoryId).ToList());
    }

    [Fact]
    public async Task Un_reparto_que_no_suma_la_cantidad_no_se_aprueba()
    {
        using var db = new TestDb();
        Seed(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddRequirement(1, Bowl, 6);

        var result = await Service(db).ApproveAsync(ReviewerUserId, 1, null, null, assignments: [new AssignmentInput(Bowl, Econo, 3)]);

        Assert.False(result.Ok);
        Assert.Contains("suma 3", result.Message);
    }

    [Fact]
    public async Task Un_origen_sin_libre_bloquea_salvo_aprobar_igual_con_faltantes()
    {
        using var db = new TestDb();
        Seed(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddRequirement(1, Bowl, 6);
        AssignmentInput[] todoDelEconomato = [new AssignmentInput(Bowl, Econo, 6)];

        var blocked = await Service(db).ApproveAsync(ReviewerUserId, 1, null, null, assignments: todoDelEconomato);
        db.Context.ChangeTracker.Clear();
        var forced = await Service(db).ApproveAsync(ReviewerUserId, 1, null, null, allowShortage: true, assignments: todoDelEconomato);

        Assert.False(blocked.Ok);
        Assert.Contains("origen ECONO", blocked.Message);
        Assert.True(forced.Ok, forced.Message);
    }

    [Fact]
    public async Task Lo_asignado_a_otra_practica_que_se_cruza_reduce_el_libre_de_ese_origen()
    {
        using var db = new TestDb();
        Seed(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabA, status: PlanningStatus.Approved);
        db.AddRequirement(1, Bowl, 3);
        db.Context.PlanningAssignments.Add(new PlanningAssignment { PlanningRequirementId = RequirementId(db, 1, Bowl), LaboratoryId = Econo, Quantity = 3 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.AddPlanning(2, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddRequirement(2, Bowl, 2);

        var line = Assert.Single(await Service(db).SuggestAssignmentsAsync(2));

        Assert.Equal(0m, line.Origins.Single(o => o.LaboratoryId == Econo).Free);
        Assert.Equal(2m, Assert.Single(line.Suggested, s => s.LaboratoryId == TestDb.LabA).Quantity);
    }

    // ───────── Salida desde varios orígenes ─────────

    [Fact]
    public async Task La_salida_con_asignaciones_crea_un_item_por_origen()
    {
        using var db = new TestDb();
        Seed(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Approved);
        db.AddRequirement(1, Bowl, 6);
        var requirementId = RequirementId(db, 1, Bowl);
        db.Context.PlanningAssignments.AddRange(
            new PlanningAssignment { PlanningRequirementId = requirementId, LaboratoryId = Econo, Quantity = 3 },
            new PlanningAssignment { PlanningRequirementId = requirementId, LaboratoryId = TestDb.LabA, Quantity = 3 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var result = await Circuit(db).CreateDepartureAsync(1, Econo, [new DepartureLine(requirementId, 6)], null);

        Assert.True(result.Ok, result.Message);
        var items = await db.Context.DepartureItems.AsNoTracking().OrderBy(i => i.OriginLaboratoryId).ToListAsync();
        Assert.Equal([(TestDb.LabA, 3m), (Econo, 3m)], items.Select(i => (i.OriginLaboratoryId!.Value, i.Quantity!.Value)).OrderBy(x => x.Item1).ToList());
        // Reutilizable: la existencia de ambos orígenes no cambia al entregar.
        Assert.Equal(8m, await db.Context.ArticleStocks.AsNoTracking().Where(s => s.ArticleId == Bowl).SumAsync(s => s.AvailableQuantity));
    }

    [Fact]
    public async Task La_salida_se_bloquea_si_un_origen_asignado_no_tiene_libre()
    {
        using var db = new TestDb();
        Seed(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Approved);
        db.AddRequirement(1, Bowl, 4);
        var requirementId = RequirementId(db, 1, Bowl);
        db.Context.PlanningAssignments.Add(new PlanningAssignment { PlanningRequirementId = requirementId, LaboratoryId = Econo, Quantity = 4 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var result = await Circuit(db).CreateDepartureAsync(1, Econo, [new DepartureLine(requirementId, 4)], null);

        Assert.False(result.Ok);
        Assert.Contains("en ECONO: libre ahora 3, solicitado 4", result.Message);
        Assert.Empty(await db.Context.Departures.AsNoTracking().ToListAsync());
    }

    // ───────── Consumibles ─────────

    [Fact]
    public async Task El_consumible_descuenta_al_entregar_no_se_devuelve_y_no_queda_fuera()
    {
        using var db = new TestDb();
        Seed(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Approved);
        db.AddRequirement(1, Film, 4);
        db.AddRequirement(1, Bowl, 2);
        var filmLine = RequirementId(db, 1, Film);
        var bowlLine = RequirementId(db, 1, Bowl);

        var departure = await Circuit(db).CreateDepartureAsync(1, Econo, [new DepartureLine(filmLine, 4), new DepartureLine(bowlLine, 2)], null);
        Assert.True(departure.Ok, departure.Message);
        db.Context.ChangeTracker.Clear();

        Assert.Equal(6m, (await db.Context.ArticleStocks.AsNoTracking().SingleAsync(s => s.ArticleId == Film)).AvailableQuantity);
        var outNow = await StockRules.OutstandingAsync(db.Context, Econo, [Film, Bowl]);
        Assert.False(outNow.ContainsKey(Film));
        Assert.Equal(2m, outNow[Bowl]);

        var items = await db.Context.DepartureItems.AsNoTracking().ToDictionaryAsync(i => i.ArticleId!.Value, i => i.Id);
        db.AddIntern(300, "Tec. Ruth");
        var returnFilm = await Circuit(db).RegisterReturnAsync(departure.EntityId!.Value, 300, TestDb.Week1Monday, [new ReturnLine(items[Film], 1)], null);
        Assert.False(returnFilm.Ok);
        Assert.Contains("consumibles no se devuelven", returnFilm.Message);

        // Devolver solo los bowls cierra la salida: el consumible quedó saldado al entregarse.
        var returnBowls = await Circuit(db).RegisterReturnAsync(departure.EntityId!.Value, 300, TestDb.Week1Monday, [new ReturnLine(items[Bowl], 2)], null);
        Assert.True(returnBowls.Ok, returnBowls.Message);
        Assert.Equal(LoanStatus.Returned, (await db.Context.Departures.AsNoTracking().SingleAsync()).Status);
    }
}
