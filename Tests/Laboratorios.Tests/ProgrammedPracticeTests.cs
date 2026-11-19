using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Laboratorios.Tests;

/// <summary>
/// Fase 3: el docente solicita desde su práctica programada y la bandeja compara previstas (cronograma) con recibidas.
/// </summary>
public class ProgrammedPracticeTests
{
    private static readonly DateTime Monday = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7)).AddDays(14);

    private static TeacherRequestService Service(TestDb db)
    {
        var circuit = new L3CircuitService(db.Context, NullLogger<L3CircuitService>.Instance);
        return new TeacherRequestService(db.Context, new AcademicManagementService(db.Context), db.Occupancy(), db.GroupSchedule(),
            new ArticleAvailabilityService(db.Context, circuit), new AlwaysFreeLock(),
            Options.Create(new TeacherRequestOptions()), NullLogger<TeacherRequestService>.Instance);
    }

    private static RequestWindow Window(DateTime max) =>
        new(TestDb.ManagementId, "2026-2", DateTime.Today.AddDays(2), max, null);

    /// <summary>Tres prácticas programadas del grupo del docente 1 en dos semanas; la segunda ya tiene solicitud.</summary>
    private static void Seed(TestDb db)
    {
        db.AddSubject(70, "Cocina");
        db.AddGroup(80, "G-80", "A", 70, teacherPersonId: TestDb.Teacher1);
        db.AddGroup(81, "G-81", "B", 70, teacherPersonId: TestDb.Teacher2);
        db.Context.Practices.AddRange(
            new Practice { Id = 1, Code = "P1", Name = "Fondos", SubjectId = 70 },
            new Practice { Id = 2, Code = "P2", Name = "Salsas", SubjectId = 70 },
            new Practice { Id = 3, Code = "P3", Name = "Masas", SubjectId = 70 });
        db.Context.PracticeSchedules.AddRange(
            new PracticeSchedule { Id = 501, Code = "PRG-1", ManagementId = TestDb.ManagementId, SubjectId = 70, PracticeId = 1, CourseGroupId = 80, GroupName = "A",
                TeacherPersonId = TestDb.Teacher1, ExecutionDate = Monday, WeekNumber = 12 },
            new PracticeSchedule { Id = 502, Code = "PRG-2", ManagementId = TestDb.ManagementId, SubjectId = 70, PracticeId = 2, CourseGroupId = 80, GroupName = "A",
                TeacherPersonId = TestDb.Teacher1, ExecutionDate = Monday.AddDays(2), WeekNumber = 12 },
            new PracticeSchedule { Id = 503, Code = "PRG-3", ManagementId = TestDb.ManagementId, SubjectId = 70, PracticeId = 3, CourseGroupId = 81, GroupName = "B",
                TeacherPersonId = TestDb.Teacher2, ExecutionDate = Monday.AddDays(8), WeekNumber = 13 });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.AddPlanning(1, Monday.AddDays(2), 2, 2, status: PlanningStatus.Requested, students: null);
        var plan = db.Context.Plannings.Single(p => p.Id == 1);
        plan.PracticeScheduleId = 502;
        db.Context.Plannings.Update(plan);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task El_docente_ve_solo_sus_practicas_programadas_sin_solicitud()
    {
        using var db = new TestDb();
        Seed(db);

        var list = await Service(db).GetProgrammedAsync(TestDb.Teacher1, Window(Monday.AddDays(60)));

        var only = Assert.Single(list);
        Assert.Equal(501, only.ScheduleId);
        Assert.Equal(80, only.CourseGroupId);
        Assert.True(only.InWindow);
    }

    [Fact]
    public async Task Una_solicitud_rechazada_vuelve_a_dejar_la_practica_por_solicitar()
    {
        using var db = new TestDb();
        Seed(db);
        var plan = db.Context.Plannings.Single(p => p.Id == 1);
        plan.Status = PlanningStatus.Rejected;
        db.Context.Plannings.Update(plan);
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();

        var list = await Service(db).GetProgrammedAsync(TestDb.Teacher1, Window(Monday.AddDays(60)));

        Assert.Equal([501, 502], list.Select(p => p.ScheduleId).ToList());
    }

    [Fact]
    public async Task Fuera_del_plazo_se_lista_pero_no_se_puede_solicitar_aun()
    {
        using var db = new TestDb();
        Seed(db);

        var only = Assert.Single(await Service(db).GetProgrammedAsync(TestDb.Teacher1, Window(Monday.AddDays(-1))));

        Assert.False(only.InWindow);
    }

    [Fact]
    public async Task La_bandeja_compara_previstas_y_recibidas_por_semana()
    {
        using var db = new TestDb();
        Seed(db);

        var coverage = await Service(db).GetWeeklyCoverageAsync(Monday, 2);

        Assert.Equal(2, coverage.Count);
        Assert.Equal((2, 1), (coverage[0].Expected, coverage[0].Received));
        Assert.Equal(12, coverage[0].WeekNumber);
        Assert.Contains("Fondos", Assert.Single(coverage[0].Missing));
        Assert.Equal((1, 0), (coverage[1].Expected, coverage[1].Received));
    }
}
