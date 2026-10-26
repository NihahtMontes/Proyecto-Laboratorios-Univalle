using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;

namespace Laboratorios.Tests;

/// <summary>
/// Aforo y choque de docente del paso 1 (Planificacion): el aforo excedido bloquea siempre (mismo
/// texto que la reprogramación), el choque de docente bloquea salvo aceptación explícita con la
/// huella vigente (queda constancia en las observaciones); la asignación de horario (O10) aplica las
/// mismas reglas. Además, los helpers puros de PlanningAssignmentRules.
/// </summary>
public class PlanificacionCapacityTeacherTests
{
    private const int SubjectId = 500;       // Química General (carrera 10, semestre 3)
    private const int PracticeId = 700;
    private const int OccupiedPlanningId = 60;   // ya existe: miércoles P3–P4 en LAB-A con Teacher1
    private const int AssignPlanningId = 70;     // aprobada sin periodo, 25 estudiantes, LAB-A

    private static readonly DateTime Wednesday = TestDb.Week1Monday.AddDays(2);   // miércoles 07/10/2026

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static void SeedScenario(TestDb db)
    {
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.Context.Interns.Add(new Intern
        {
            Id = TestDb.Teacher1,
            Name = "Ana Pérez",
            Category = PersonCategory.Docente,
            Status = GeneralStatus.Activo,
            InternStatus = GeneralStatus.Activo
        });
        db.Context.Interns.Add(new Intern
        {
            Id = TestDb.Teacher2,
            Name = "Luis Rojas",
            Category = PersonCategory.Docente,
            Status = GeneralStatus.Activo,
            InternStatus = GeneralStatus.Activo
        });
        db.Context.Practices.Add(new Practice
        {
            Id = PracticeId,
            Code = "PRC-00001",
            Name = "Prueba de llama",
            SubjectId = SubjectId,
            Status = GeneralStatus.Activo
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    /// <summary>Planificación aprobada sin periodo asignado (O10) con programación de 25 estudiantes.</summary>
    private static void AddApprovedWithoutPeriod(TestDb db, int id, DateTime date, int laboratoryId, int students)
    {
        db.Context.Plannings.Add(new Planning
        {
            Id = id,
            ManagementId = TestDb.ManagementId,
            LaboratoryId = laboratoryId,
            TeacherPersonId = TestDb.Teacher2,
            PracticeName = $"Práctica {id}",
            GroupName = "A",
            Status = PlanningStatus.Approved,
            AcademicWeekId = TestDb.Week1Id,
            RequestDate = date,
            ScheduledDate = date + new TimeSpan(7, 0, 0),
            StartTime = new TimeSpan(7, 0, 0),
            EndTime = new TimeSpan(8, 50, 0),
            PracticeSchedule = new PracticeSchedule { Id = id * 10, Code = $"PP-{id}", StudentCount = students }
        });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    private static PlanificacionModel BuildModel(TestDb db) => new(
        db.Context,
        new AcademicManagementService(db.Context),
        NullLogger<PlanificacionModel>.Instance,
        db.Occupancy(),
        new NoOpPlanningLock(),
        db.GroupSchedule());

    /// <summary>Modelo con PageContext (usuario "qa_test" para la constancia) y TempData en memoria.</summary>
    private static PlanificacionModel BuildPage(TestDb db)
    {
        var model = BuildModel(db);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "qa_test")], "qa_test"))
            },
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
        model.TempData = new TempDataDictionary(model.HttpContext, new EmptyTempDataProvider());
        return model;
    }

    private static PlanificacionModel.InputModel Input(int startOrder, int count, int laboratoryId = TestDb.LabA,
        int teacherPersonId = TestDb.Teacher1, int? students = null) => new()
    {
        LaboratoryId = laboratoryId,
        TeacherPersonId = teacherPersonId,
        SubjectId = SubjectId,
        PracticeId = PracticeId,
        GroupName = "A",
        StudentCount = students,
        Date = Wednesday.ToString("yyyy-MM-dd"),
        StartPeriodId = startOrder,   // catálogo sembrado: Id = Order
        PeriodCount = count
    };

    // ==================== Aforo (bloqueo duro) ====================

    [Fact]
    public async Task OnPost_StudentsAboveLabCapacity_IsBlocked()
    {
        using var db = new TestDb();
        SeedScenario(db);

        var model = BuildPage(db);
        model.ManagementId = TestDb.ManagementId;
        model.Input = Input(startOrder: 7, count: 2, students: 25);   // LAB-A admite 20

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        var error = Assert.Single(model.ModelState["Input.StudentCount"]!.Errors);
        Assert.Equal("El grupo tiene 25 estudiantes y el laboratorio LAB-A admite 20.", error.ErrorMessage);
        Assert.Empty(db.Context.Plannings.ToList());
    }

    [Fact]
    public async Task OnPost_StudentsAboveCapacity_WithoutLabCapacity_Saves()
    {
        using var db = new TestDb();
        SeedScenario(db);

        var model = BuildPage(db);
        model.ManagementId = TestDb.ManagementId;
        model.Input = Input(startOrder: 7, count: 2, laboratoryId: TestDb.LabB, students: 25);   // LAB-B sin capacidad

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Requerimientos", redirect.PageName);
        Assert.True(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPost_EditWithoutSlotOrTeacherChange_PreexistingTeacherConflict_SavesWithoutNote()
    {
        using var db = new TestDb();
        SeedScenario(db);
        // Choque que ya existía (p. ej. importado): Teacher1 en LAB-A y en LAB-B a la misma hora.
        db.AddPlanning(OccupiedPlanningId, Wednesday, 3, 2, laboratoryId: TestDb.LabA, teacherPersonId: TestDb.Teacher1);
        db.AddPlanning(80, Wednesday, 3, 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher1);

        var model = BuildPage(db);
        model.ManagementId = TestDb.ManagementId;
        model.Id = 80;
        model.Input = Input(startOrder: 3, count: 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher1);
        model.Input.Observations = "Solo corrijo la observación";

        var result = await model.OnPostAsync();

        // Sin cambio de horario ni de docente no se vuelve a exigir la aceptación ni se suma constancia.
        Assert.IsType<RedirectToPageResult>(result);
        var saved = db.Context.Plannings.AsNoTracking().Single(p => p.Id == 80);
        Assert.Equal("Solo corrijo la observación", saved.Observations);
        Assert.Empty(model.TeacherConflicts);
    }

    // ==================== Choque de docente (bloquear + aceptar) ====================

    [Fact]
    public async Task OnPost_TeacherBusyInSamePeriods_IsBlockedWithConflictList()
    {
        using var db = new TestDb();
        SeedScenario(db);
        db.AddPlanning(OccupiedPlanningId, Wednesday, 3, 2, laboratoryId: TestDb.LabA, teacherPersonId: TestDb.Teacher1);

        var model = BuildPage(db);
        model.ManagementId = TestDb.ManagementId;
        model.Input = Input(startOrder: 3, count: 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher1);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Contains("El docente ya tiene otra práctica en esos periodos", error.ErrorMessage);
        Assert.NotEmpty(model.TeacherConflicts);
        Assert.Equal("60", model.TeacherConflictFingerprint);
        // No se guardó ninguna planificación nueva.
        Assert.Single(db.Context.Plannings.ToList(), p => p.Id == OccupiedPlanningId);
    }

    [Fact]
    public async Task OnPost_TeacherConflict_AcceptedWithFingerprint_SavesWithNote()
    {
        using var db = new TestDb();
        SeedScenario(db);
        db.AddPlanning(OccupiedPlanningId, Wednesday, 3, 2, laboratoryId: TestDb.LabA, teacherPersonId: TestDb.Teacher1);

        var model = BuildPage(db);
        model.ManagementId = TestDb.ManagementId;
        model.Input = Input(startOrder: 3, count: 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher1);
        model.Input.AcceptTeacherConflict = true;
        model.Input.TeacherConflictFingerprint = "60";

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Requerimientos", redirect.PageName);
        var saved = db.Context.Plannings.AsNoTracking().Single(p => p.Id != OccupiedPlanningId);
        Assert.NotNull(saved.Observations);
        Assert.Contains("Choque de docente aceptado por qa_test el", saved.Observations);
        Assert.True(saved.Observations!.Length <= 1000);
        Assert.NotNull(model.TempData["WarningMessage"]);
    }

    [Fact]
    public async Task OnPost_TeacherConflict_WrongFingerprint_IsBlocked()
    {
        using var db = new TestDb();
        SeedScenario(db);
        db.AddPlanning(OccupiedPlanningId, Wednesday, 3, 2, laboratoryId: TestDb.LabA, teacherPersonId: TestDb.Teacher1);

        var model = BuildPage(db);
        model.ManagementId = TestDb.ManagementId;
        model.Input = Input(startOrder: 3, count: 2, laboratoryId: TestDb.LabB, teacherPersonId: TestDb.Teacher1);
        model.Input.AcceptTeacherConflict = true;
        model.Input.TeacherConflictFingerprint = "999";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains("El docente ya tiene otra práctica en esos periodos",
            model.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
        Assert.Single(db.Context.Plannings.ToList(), p => p.Id == OccupiedPlanningId);
    }

    // ==================== Asignación de horario (O10): aforo ====================

    [Fact]
    public async Task OnPostAssignPeriod_CapacityExceeded_DoesNotAssign()
    {
        using var db = new TestDb();
        SeedScenario(db);
        AddApprovedWithoutPeriod(db, AssignPlanningId, Wednesday, TestDb.LabA, students: 25);

        var model = BuildPage(db);
        model.Id = AssignPlanningId;
        var result = await model.OnPostAssignPeriodAsync(startPeriodId: 3, periodCount: 2);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(db.Context.Plannings.AsNoTracking().Single(p => p.Id == AssignPlanningId).StartPeriodId);
        Assert.Contains("El grupo tiene 25 estudiantes y el laboratorio LAB-A admite 20.",
            Assert.IsType<string>(model.TempData["ErrorMessage"]));
    }

    // ==================== Helpers puros (PlanningAssignmentRules) ====================

    public static TheoryData<string?, string> NoteCases => new()
    {
        { null, "Nota corta" },
        { "Observación previa", "Nota corta" },
        { new string('x', 1500), "Nota que siempre debe quedar al final" },
        { null, new string('n', 1200) }
    };

    [Theory]
    [MemberData(nameof(NoteCases))]
    public void AppendTeacherConflictNote_KeepsTheNoteAndMaxSize(string? observations, string note)
    {
        var result = PlanningAssignmentRules.AppendTeacherConflictNote(observations, note, 1000);

        Assert.True(result.Length <= 1000);
        var expectedTail = note.Length > 1000 ? note[..1000] : note;
        Assert.EndsWith(expectedTail, result);
    }

    [Theory]
    [InlineData(25, 20, true)]
    [InlineData(15, 20, false)]
    [InlineData(null, 20, false)]
    [InlineData(25, null, false)]
    public void CapacityError_OnlyBlocksWhenBothPresentAndExceeded(int? students, int? capacity, bool expected)
    {
        var error = PlanningAssignmentRules.CapacityError(students, capacity, "LAB-A");
        if (expected)
            Assert.Equal("El grupo tiene 25 estudiantes y el laboratorio LAB-A admite 20.", error);
        else
            Assert.Null(error);
    }

    [Fact]
    public void Fingerprint_IsDistinctSortedAndJoined()
    {
        Assert.Equal("3,5", PlanningAssignmentRules.Fingerprint([5, null, 3, 5]));
        Assert.Equal("1,2,50", PlanningAssignmentRules.Fingerprint([50, 2, 1]));
        Assert.Equal(string.Empty, PlanningAssignmentRules.Fingerprint([]));
        Assert.Equal(string.Empty, PlanningAssignmentRules.Fingerprint([null, null]));
    }
}
