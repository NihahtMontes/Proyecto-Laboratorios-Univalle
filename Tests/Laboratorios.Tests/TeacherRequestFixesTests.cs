using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Laboratorios.Tests;

/// <summary>
/// Correcciones de la revisión del rol Docente: sobreventa al aprobar, estados bajo bloqueo, programaciones sueltas,
/// material no devuelto y contadores del menú sin base de datos.
/// </summary>
public class TeacherRequestFixesTests
{
    private const int Fixer = 500;
    private const int Bowl = 501;
    private const int ReviewerUserId = 900;
    private const int TeacherUserId = 901;

    private static TeacherRequestService Service(TestDb db)
    {
        var circuit = new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance);
        return new TeacherRequestService(db.Context, new AcademicManagementService(db.Context), db.Occupancy(), db.GroupSchedule(),
            new ArticleAvailabilityService(db.Context, circuit), new AlwaysFreeLock(),
            Options.Create(new TeacherRequestOptions()), NullLogger<TeacherRequestService>.Instance);
    }

    private static void SeedUsers(TestDb db)
    {
        db.Context.Users.AddRange(
            new User { Id = ReviewerUserId, UserName = "revisor", FirstName = "Ana", LastName = "Revisora", Role = UserRole.Administrador },
            new User { Id = TeacherUserId, UserName = "docente", FirstName = "Luis", LastName = "Docente", Role = UserRole.Docente, PersonId = TestDb.Teacher1 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.AddSubject(70, "Cocina");
        db.AddGroup(80, "G-80", "A", 70, teacherPersonId: TestDb.Teacher1);
        db.AddGroup(81, "G-81", "B", 70, teacherPersonId: TestDb.Teacher2);
    }

    private static async Task<Planning> PlanAsync(TestDb db, int id) =>
        await db.Context.Plannings.AsNoTracking().Include(p => p.PracticeSchedule).SingleAsync(p => p.Id == id);

    private static void SetSchedule(TestDb db, int scheduleId, int? createdById, int? importBatchId = null)
    {
        var schedule = db.Context.PracticeSchedules.AsTracking().Single(s => s.Id == scheduleId);
        schedule.CreatedById = createdById;
        schedule.ImportBatchId = importBatchId;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    // ───────── Sobreventa ─────────

    [Fact]
    public async Task Dos_pendientes_con_el_unico_articulo_a_la_misma_hora_la_segunda_no_se_aprueba()
    {
        using var db = new TestDb();
        SeedUsers(db);
        db.AddArticleWithStock(Fixer, "FIX", "Fixer", stock: 1);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabA, teacherPersonId: TestDb.Teacher1, status: PlanningStatus.Requested, courseGroupId: 80);
        db.AddRequirement(1, Fixer, 1);
        db.AddPlanning(2, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2, status: PlanningStatus.Requested, courseGroupId: 81);
        db.AddRequirement(2, Fixer, 1);

        var first = await Service(db).ApproveAsync(ReviewerUserId, 1, "Aprobada en lote: sin choques ni faltantes.", null);
        db.Context.ChangeTracker.Clear();
        var second = await Service(db).ApproveAsync(ReviewerUserId, 2, "Aprobada en lote: sin choques ni faltantes.", null);

        Assert.True(first.Ok, first.Message);
        Assert.False(second.Ok);
        Assert.Contains("no alcanza el material", second.Message);
        Assert.Equal(PlanningStatus.Requested, (await PlanAsync(db, 2)).Status);
    }

    [Fact]
    public async Task Aprobar_igual_con_faltantes_queda_asentado_en_el_historial()
    {
        using var db = new TestDb();
        SeedUsers(db);
        db.AddArticleWithStock(Fixer, "FIX", "Fixer", stock: 1);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabA, status: PlanningStatus.Approved);
        db.AddRequirement(1, Fixer, 1);
        db.AddPlanning(2, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2, status: PlanningStatus.Requested, courseGroupId: 81);
        db.AddRequirement(2, Fixer, 1);

        var result = await Service(db).ApproveAsync(ReviewerUserId, 2, "Se consigue prestado", null, allowShortage: true);

        Assert.True(result.Ok, result.Message);
        Assert.Contains(result.Warnings!, w => w.Contains("faltantes"));
        var review = await db.Context.PlanningReviews.AsNoTracking().SingleAsync(r => r.PlanningId == 2);
        Assert.Contains("Aprobada con faltantes", review.Comment);
        Assert.StartsWith("Se consigue prestado", review.Comment);
    }

    [Fact]
    public async Task Aprobar_con_ajuste_hasta_lo_libre_si_se_aprueba()
    {
        using var db = new TestDb();
        SeedUsers(db);
        db.AddArticleWithStock(Bowl, "BOW", "Bowl", stock: 3);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabA, status: PlanningStatus.Approved);
        db.AddRequirement(1, Bowl, 2);
        db.AddPlanning(2, TestDb.Week1Monday, 2, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher2, status: PlanningStatus.Requested, courseGroupId: 81);
        db.AddRequirement(2, Bowl, 2);

        var tal = await Service(db).ApproveAsync(ReviewerUserId, 2, null, null);
        db.Context.ChangeTracker.Clear();
        var ajustada = await Service(db).ApproveAsync(ReviewerUserId, 2, "Solo hay uno libre", [new RequestLineInput(Bowl, 1)]);

        Assert.False(tal.Ok);
        Assert.True(ajustada.Ok, ajustada.Message);
    }

    // ───────── Estados ─────────

    [Fact]
    public async Task Rechazar_una_ya_rechazada_y_retirar_una_aprobada_fallan()
    {
        using var db = new TestDb();
        SeedUsers(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Requested);
        db.AddPlanning(2, TestDb.Week1Monday, 5, 2, status: PlanningStatus.Approved);
        var teacher = new TeacherProfile(TeacherUserId, TestDb.Teacher1, "Luis Docente");

        var reject = await Service(db).RejectAsync(ReviewerUserId, 1, "No hay ambiente ese día");
        db.Context.ChangeTracker.Clear();
        var again = await Service(db).RejectAsync(ReviewerUserId, 1, "Segundo intento");
        db.Context.ChangeTracker.Clear();
        var withdrawApproved = await Service(db).WithdrawAsync(teacher, 2, "Ya no la necesito");

        Assert.True(reject.Ok, reject.Message);
        Assert.False(again.Ok);
        Assert.False(withdrawApproved.Ok);
        Assert.Equal(PlanningStatus.Approved, (await PlanAsync(db, 2)).Status);
        Assert.Single(await db.Context.PlanningReviews.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Un_docente_no_puede_retirar_la_solicitud_de_otro()
    {
        using var db = new TestDb();
        SeedUsers(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, teacherPersonId: TestDb.Teacher2, status: PlanningStatus.Requested, courseGroupId: 81);
        var otro = new TeacherProfile(TeacherUserId, TestDb.Teacher1, "Luis Docente");

        var result = await Service(db).WithdrawAsync(otro, 1, "intento");

        Assert.False(result.Ok);
        Assert.Equal(PlanningStatus.Requested, (await PlanAsync(db, 1)).Status);
    }

    // ───────── Programaciones sueltas ─────────

    [Fact]
    public async Task Rechazar_cancela_la_programacion_creada_por_el_docente()
    {
        using var db = new TestDb();
        SeedUsers(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Requested);
        SetSchedule(db, 10, createdById: TeacherUserId);

        var result = await Service(db).RejectAsync(ReviewerUserId, 1, "Fuera del cronograma");

        Assert.True(result.Ok, result.Message);
        Assert.Equal(PracticeScheduleStatus.Cancelled, (await PlanAsync(db, 1)).PracticeSchedule!.Status);
    }

    [Fact]
    public async Task Retirar_no_cancela_programaciones_importadas_ni_de_otro_usuario()
    {
        using var db = new TestDb();
        SeedUsers(db);
        var teacher = new TeacherProfile(TeacherUserId, TestDb.Teacher1, "Luis Docente");
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Requested);
        SetSchedule(db, 10, createdById: TeacherUserId, importBatchId: 7);   // importada
        db.AddPlanning(2, TestDb.Week1Monday, 5, 2, status: PlanningStatus.Requested);
        SetSchedule(db, 20, createdById: ReviewerUserId);                    // creada por el administrador

        Assert.True((await Service(db).WithdrawAsync(teacher, 1, null)).Ok);
        db.Context.ChangeTracker.Clear();
        Assert.True((await Service(db).WithdrawAsync(teacher, 2, null)).Ok);

        Assert.Equal(PracticeScheduleStatus.Scheduled, (await PlanAsync(db, 1)).PracticeSchedule!.Status);
        Assert.Equal(PracticeScheduleStatus.Scheduled, (await PlanAsync(db, 2)).PracticeSchedule!.Status);
    }

    [Fact]
    public async Task Rechazar_no_cancela_una_programacion_compartida_con_otra_sesion_vigente()
    {
        using var db = new TestDb();
        SeedUsers(db);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Requested);
        SetSchedule(db, 10, createdById: TeacherUserId);
        db.AddPlanning(2, TestDb.Week2Monday, 2, 2, status: PlanningStatus.Approved, students: null);
        var other = db.Context.Plannings.AsTracking().Single(p => p.Id == 2);
        other.PracticeScheduleId = 10;
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        Assert.True((await Service(db).RejectAsync(ReviewerUserId, 1, "Duplicada")).Ok);

        Assert.Equal(PracticeScheduleStatus.Scheduled, (await PlanAsync(db, 1)).PracticeSchedule!.Status);
    }

    // ───────── Material no devuelto ─────────

    [Fact]
    public async Task Material_no_devuelto_de_una_practica_anterior_reduce_lo_libre()
    {
        using var db = new TestDb();
        db.AddArticleWithStock(Fixer, "FIX", "Fixer", stock: 1);
        db.AddPlanning(1, TestDb.Week1Monday, 2, 2, status: PlanningStatus.Finalized);
        db.Context.Departures.Add(new Departure
        {
            ManagementId = TestDb.ManagementId, PlanningId = 1, OriginLaboratoryId = TestDb.LabA, Status = LoanStatus.Active,
            Items = [new DepartureItem { ArticleId = Fixer, Quantity = 1, ReturnedQuantity = 0 }]
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var semanaSiguiente = await Service(db).CheckMaterialAsync(TestDb.Week2Monday, 2, 2, [new RequestLineInput(Fixer, 1)], null);

        var line = Assert.Single(semanaSiguiente);
        Assert.Equal(MaterialState.Short, line.State);
        Assert.Contains("sin devolver", line.Explanation);
    }

    // ───────── Menú sin base de datos ─────────

    [Fact]
    public async Task Contadores_del_menu_devuelven_cero_si_la_base_no_responde()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=NoExiste;User Id=x;Password=x;Connect Timeout=1;Encrypt=False")
            .Options;
        await using var context = new ApplicationDbContext(options, new FakeCurrentUser());
        var service = new SidebarCountsService(context, NullLogger<SidebarCountsService>.Instance);
        var supervisor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, AuthorizationHelper.RoleSupervisor)], "test"));

        var (pending, observed) = await service.GetAsync(supervisor);

        Assert.Equal(0, pending);
        Assert.Equal(0, observed);
    }
}
