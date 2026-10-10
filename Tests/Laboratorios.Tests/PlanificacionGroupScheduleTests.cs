using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;

namespace Laboratorios.Tests;

/// <summary>
/// El horario semanal del grupo de estudiantes al crear/editar una planificación:
/// el guardado bloquea cuando el destino coincide con clases de OTRAS materias del mismo grupo
/// (CheckDestinationAsync → Collisions), las clases propias solo se sugieren, y el handler
/// GroupSchedule expone ambos conjuntos para marcar las fichas del selector de periodos.
/// </summary>
public class PlanificacionGroupScheduleTests
{
    private const int SubjectId = 500;       // Química General (carrera 10, semestre 3)
    private const int OtherSubjectId = 501;  // Física General (misma carrera y semestre)
    private const int GroupId = 600;         // grupo "A" de Química
    private const int SiblingGroupId = 601;  // grupo "A" de Física
    private const int PracticeId = 700;

    private static readonly DateTime Wednesday = TestDb.Week1Monday.AddDays(2);   // miércoles 07/10/2026

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static void SeedScenario(TestDb db)
    {
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-A", "A", OtherSubjectId);
        db.Context.Interns.Add(new Intern
        {
            Id = TestDb.Teacher1,
            Name = "Ana Pérez",
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

    private static PlanificacionModel BuildModel(TestDb db) => new(
        db.Context,
        new AcademicManagementService(db.Context),
        NullLogger<PlanificacionModel>.Instance,
        db.Occupancy(),
        new NoOpPlanningLock(),
        db.GroupSchedule());

    private static PlanificacionModel WithTempData(PlanificacionModel model)
    {
        model.TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider());
        return model;
    }

    private static PlanificacionModel.InputModel Input(int startOrder, int count) => new()
    {
        LaboratoryId = TestDb.LabA,
        TeacherPersonId = TestDb.Teacher1,
        SubjectId = SubjectId,
        PracticeId = PracticeId,
        GroupName = "A",
        Date = Wednesday.ToString("yyyy-MM-dd"),
        StartPeriodId = startOrder,   // catálogo sembrado: Id = Order
        PeriodCount = count
    };

    // ==================== Guardado: choque con otras materias del grupo ====================

    [Fact]
    public async Task OnPost_GroupHasSiblingClassInDestination_IsBlocked()
    {
        using var db = new TestDb();
        SeedScenario(db);
        db.AddGroupSlots(SiblingGroupId, (3, 4), (3, 5));   // Física: miércoles P4–P5

        var model = BuildModel(db);
        model.ManagementId = TestDb.ManagementId;
        model.Input = Input(startOrder: 4, count: 2);       // destino miércoles P4–P5: choca

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Contains("El grupo A tiene clase de", error.ErrorMessage);
        Assert.Contains("Física General", error.ErrorMessage);
        Assert.Contains("elija otro periodo", error.ErrorMessage);
        // Nada se guardó.
        Assert.Empty(db.Context.Plannings.ToList());
    }

    [Fact]
    public async Task OnPost_GroupHasSiblingClassOutsideDestination_Saves()
    {
        using var db = new TestDb();
        SeedScenario(db);
        db.AddGroupSlots(SiblingGroupId, (3, 4), (3, 5));   // Física: miércoles P4–P5

        var model = WithTempData(BuildModel(db));
        model.ManagementId = TestDb.ManagementId;
        model.Input = Input(startOrder: 7, count: 2);       // destino miércoles P7–P8: libre

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Requerimientos", redirect.PageName);
        Assert.True(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPost_DestinationOnOwnClassSlots_IsSuggestedNotBlocked()
    {
        using var db = new TestDb();
        SeedScenario(db);
        db.AddGroupSlots(GroupId, (3, 4), (3, 5));          // clase PROPIA del grupo: miércoles P4–P5

        var model = WithTempData(BuildModel(db));
        model.ManagementId = TestDb.ManagementId;
        model.Input = Input(startOrder: 4, count: 2);       // coincide con la clase propia: se permite

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Requerimientos", redirect.PageName);
        Assert.True(model.ModelState.IsValid);
    }

    // ==================== Handler GroupSchedule (fichas del selector) ====================

    [Fact]
    public async Task OnGetGroupSchedule_ReturnsOwnSlotsAndSiblingCollisions()
    {
        using var db = new TestDb();
        SeedScenario(db);
        db.AddGroupSlots(GroupId, (3, 6));                  // propia: miércoles P6
        db.AddGroupSlots(SiblingGroupId, (3, 4), (4, 2));   // Física: miércoles P4 y jueves P2

        var model = BuildModel(db);
        var result = await model.OnGetGroupScheduleAsync(SubjectId, "  a ", TestDb.ManagementId);

        var json = Assert.IsType<JsonResult>(result);
        var doc = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
        Assert.True(doc.RootElement.GetProperty("hasSchedule").GetBoolean());

        var own = doc.RootElement.GetProperty("own").EnumerateArray().ToList();
        var ownRow = Assert.Single(own);
        Assert.Equal(3, ownRow.GetProperty("weekday").GetInt32());
        Assert.Equal(6, ownRow.GetProperty("order").GetInt32());

        var busy = doc.RootElement.GetProperty("busy").EnumerateArray().ToList();
        Assert.Equal(2, busy.Count);
        Assert.Contains(busy, s => s.GetProperty("weekday").GetInt32() == 3
            && s.GetProperty("order").GetInt32() == 4 && s.GetProperty("subject").GetString() == "Física General");
        Assert.Contains(busy, s => s.GetProperty("weekday").GetInt32() == 4
            && s.GetProperty("order").GetInt32() == 2);
    }

    [Fact]
    public async Task OnGetGroupSchedule_UnknownGroupOrSubject_ReturnsEmpty()
    {
        using var db = new TestDb();
        SeedScenario(db);
        var model = BuildModel(db);

        var unknownGroup = await model.OnGetGroupScheduleAsync(SubjectId, "ZZZ", TestDb.ManagementId);
        var unknownSubject = await model.OnGetGroupScheduleAsync(999, "A", TestDb.ManagementId);
        var noName = await model.OnGetGroupScheduleAsync(SubjectId, "   ", TestDb.ManagementId);

        foreach (var result in new[] { unknownGroup, unknownSubject, noName })
        {
            var json = Assert.IsType<JsonResult>(result);
            var doc = JsonDocument.Parse(JsonSerializer.Serialize(json.Value));
            Assert.False(doc.RootElement.GetProperty("hasSchedule").GetBoolean());
            Assert.Equal(0, doc.RootElement.GetProperty("own").GetArrayLength());
            Assert.Equal(0, doc.RootElement.GetProperty("busy").GetArrayLength());
        }
    }
}
