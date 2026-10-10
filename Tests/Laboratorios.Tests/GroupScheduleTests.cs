using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

public class GroupScheduleTests
{
    private const int SubjectId = 500;       // Química General (carrera 10, semestre 3)
    private const int OtherSubjectId = 501;  // Física General (misma carrera y semestre)
    private const int GroupId = 600;         // grupo "A" de Química
    private const int SiblingGroupId = 601;  // grupo "A" de Física

    private static readonly DateTime Wednesday = TestDb.Week1Monday.AddDays(2);   // miércoles 07/10/2026
    private static readonly DateTime Sunday = TestDb.Week1Monday.AddDays(6);      // 11/10/2026

    // ==================== GetSubjectScheduleAsync ====================

    [Fact]
    public async Task GetSubjectSchedule_ReturnsActiveGroupsOrderedWithTeacherAndSummary()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Pérez");
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-B", "B", SubjectId);
        db.AddGroup(SiblingGroupId, "QG-1-A", "A", SubjectId, teacherPersonId: TestDb.Teacher2);
        var inactiveId = 602;
        db.AddGroup(inactiveId, "QG-1-C", "C", SubjectId, status: GeneralStatus.Inactivo);   // excluido
        db.AddGroupSlots(SiblingGroupId, (3, 4));

        var views = await db.GroupSchedule().GetSubjectScheduleAsync(SubjectId, TestDb.ManagementId);

        Assert.Equal(2, views.Count);                       // ordenados por nombre
        Assert.Equal("A", views[0].Name);
        Assert.Equal("B", views[1].Name);
        Assert.Equal($"Docente #{TestDb.Teacher2}", views[0].TeacherName);   // grupo A: docente sin ficha Intern
        Assert.Equal("Ana Pérez", views[1].TeacherName);                     // grupo B: nombre armado desde Intern
        Assert.Equal("Miércoles periodo 4", views[0].Summary);
        Assert.Equal("Sin horario", views[1].Summary);
        var slots = Assert.Single(views[0].Slots);
        Assert.Equal(new WeekSlot(3, 4), slots);
    }

    // ==================== SaveSubjectScheduleAsync ====================

    [Fact]
    public async Task Save_ReplacesSet_IgnoresInvalidAndDuplicates()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroupSlots(GroupId, (1, 1), (2, 2));   // horario previo que debe desaparecer

        var error = await db.GroupSchedule().SaveSubjectScheduleAsync(SubjectId, TestDb.ManagementId,
            new Dictionary<int, IReadOnlyCollection<WeekSlot>>
            {
                [GroupId] = new List<WeekSlot>
                {
                    new(1, 1), new(1, 2), new(1, 3),
                    new(3, 4),
                    new(7, 1),    // día fuera de 1..6: se ignora
                    new(1, 99),   // periodo inexistente: se ignora
                    new(1, 2)     // duplicado: se ignora
                }
            });

        Assert.Null(error);
        var slots = (await db.GroupSchedule().GetGroupSlotsAsync(GroupId)).ToList();
        Assert.Equal(new List<WeekSlot> { new(1, 1), new(1, 2), new(1, 3), new(3, 4) }, slots);

        // Un segundo guardado reemplaza el conjunto completo.
        await db.GroupSchedule().SaveSubjectScheduleAsync(SubjectId, TestDb.ManagementId,
            new Dictionary<int, IReadOnlyCollection<WeekSlot>> { [GroupId] = new List<WeekSlot> { new(2, 5) } });
        slots = (await db.GroupSchedule().GetGroupSlotsAsync(GroupId)).ToList();
        Assert.Equal(new List<WeekSlot> { new(2, 5) }, slots);
    }

    [Fact]
    public async Task Save_CellWhereSiblingHasClass_ReturnsErrorWithoutSaving()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-A", "A", OtherSubjectId);
        db.AddGroupSlots(SiblingGroupId, (2, 3));   // Física A: martes periodo 3

        var error = await db.GroupSchedule().SaveSubjectScheduleAsync(SubjectId, TestDb.ManagementId,
            new Dictionary<int, IReadOnlyCollection<WeekSlot>> { [GroupId] = new List<WeekSlot> { new(2, 2), new(2, 3) } });

        Assert.NotNull(error);
        Assert.Contains("Física General", error);
        Assert.Empty(db.Context.CourseGroupScheduleSlots.AsNoTracking().Where(s => s.CourseGroupId == GroupId));
    }

    [Fact]
    public async Task Save_GroupOfAnotherSubject_ReturnsErrorWithoutSaving()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-A", "A", OtherSubjectId);   // otra materia
        db.AddGroupSlots(GroupId, (1, 1));

        var error = await db.GroupSchedule().SaveSubjectScheduleAsync(SubjectId, TestDb.ManagementId,
            new Dictionary<int, IReadOnlyCollection<WeekSlot>>
            {
                [GroupId] = new List<WeekSlot> { new(2, 2) },
                [SiblingGroupId] = new List<WeekSlot> { new(2, 2) }
            });

        Assert.NotNull(error);
        Assert.Contains("no pertenece a la materia", error);
        var slots = (await db.GroupSchedule().GetGroupSlotsAsync(GroupId)).ToList();
        Assert.Equal(new List<WeekSlot> { new(1, 1) }, slots);   // nada cambió
    }

    [Fact]
    public async Task Save_UnknownSubject_ReturnsError()
    {
        using var db = new TestDb();

        var error = await db.GroupSchedule().SaveSubjectScheduleAsync(999, TestDb.ManagementId,
            new Dictionary<int, IReadOnlyCollection<WeekSlot>>());

        Assert.NotNull(error);
        Assert.Contains("no existe", error);
    }

    // ==================== Summarize ====================

    [Fact]
    public void Summarize_JoinsConsecutivePeriodsPerDay()
    {
        using var db = new TestDb();

        var summary = db.GroupSchedule().Summarize(new List<WeekSlot> { new(1, 1), new(1, 2), new(1, 3), new(3, 4) });

        Assert.Equal("Lunes periodos 1–3 · Miércoles periodo 4", summary);
    }

    [Fact]
    public void Summarize_Empty_ReturnsSinHorario()
    {
        using var db = new TestDb();

        Assert.Equal("Sin horario", db.GroupSchedule().Summarize(Array.Empty<WeekSlot>()));
    }

    // ==================== Regla "mismos estudiantes" (hermanos) ====================

    private static void SeedSiblingScenario(TestDb db, string siblingName = "A")
    {
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-A", siblingName, OtherSubjectId);
    }

    [Fact]
    public async Task Siblings_SameCareerSemesterAndName_OtherSubject_Collide()
    {
        using var db = new TestDb();
        SeedSiblingScenario(db, siblingName: "  a ");   // nombre equivalente sin mayúsculas ni espacios
        db.AddGroupSlots(SiblingGroupId, (3, 4), (3, 5), (4, 2));

        var siblings = await db.GroupSchedule().GetSiblingClassesAsync(GroupId);

        Assert.Equal(3, siblings.Count);
        Assert.All(siblings, s =>
        {
            Assert.Equal("Física General", s.SubjectName);
            Assert.Equal(SiblingGroupId, s.CourseGroupId);
        });
        Assert.Equal(new[] { (3, 4), (3, 5), (4, 2) }, siblings.Select(s => (s.Weekday, s.PeriodOrder)).ToArray());
    }

    [Fact]
    public async Task Siblings_DifferentName_IsNotSibling()
    {
        using var db = new TestDb();
        SeedSiblingScenario(db, siblingName: "B");
        db.AddGroupSlots(SiblingGroupId, (3, 4));

        Assert.Empty(await db.GroupSchedule().GetSiblingClassesAsync(GroupId));
    }

    [Fact]
    public async Task Siblings_OtherManagement_IsNotSibling()
    {
        using var db = new TestDb();
        db.AddManagement(TestDb.Management2Id, "2027-1", year: 2027, semester: 1);
        SeedSiblingScenario(db);
        var otherManagementGroup = 602;
        db.AddGroup(otherManagementGroup, "FG-2-A", "A", OtherSubjectId, managementId: TestDb.Management2Id);
        db.AddGroupSlots(otherManagementGroup, (3, 4));

        Assert.Empty(await db.GroupSchedule().GetSiblingClassesAsync(GroupId));
    }

    [Fact]
    public async Task Siblings_SameSubject_IsNotSibling()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        var sameSubjectGroup = 603;
        db.AddGroup(sameSubjectGroup, "QG-1-B", "A", SubjectId);   // otra materia no: misma materia
        db.AddGroupSlots(sameSubjectGroup, (3, 4));

        Assert.Empty(await db.GroupSchedule().GetSiblingClassesAsync(GroupId));
    }

    [Fact]
    public async Task Siblings_InactiveGroup_IsNotSibling()
    {
        using var db = new TestDb();
        SeedSiblingScenario(db);
        var inactive = db.AddGroup(604, "FG-1-B", "A", OtherSubjectId, status: GeneralStatus.Inactivo);
        db.AddGroupSlots(inactive.Id, (3, 4));

        Assert.Empty(await db.GroupSchedule().GetSiblingClassesAsync(GroupId));
    }

    [Fact]
    public async Task Siblings_GroupSubjectWithoutCareerOrSemester_ReturnsNone()
    {
        using var db = new TestDb();
        // Materia del grupo consultado sin carrera: no hay hermanos aunque existan grupos homónimos.
        db.AddSubject(SubjectId, "Química General", careerId: null, semester: 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-A", "A", OtherSubjectId);
        db.AddGroupSlots(SiblingGroupId, (3, 4));

        Assert.Empty(await db.GroupSchedule().GetSiblingClassesAsync(GroupId));
    }

    // ==================== CheckDestinationAsync ====================

    [Fact]
    public async Task CheckDestination_MixesScheduleCollisionsOwnClassesAndSunday()
    {
        using var db = new TestDb();
        SeedSiblingScenario(db);
        db.AddGroupSlots(GroupId, (3, 6));             // clase propia el miércoles P6
        db.AddGroupSlots(SiblingGroupId, (3, 3), (3, 4));   // hermanos miércoles P3–P4

        var service = db.GroupSchedule();

        var inRange = await service.CheckDestinationAsync(GroupId, Wednesday, 3, 2);   // rango [3,4]
        Assert.True(inRange.HasSchedule);
        Assert.Equal(2, inRange.Collisions.Count);
        Assert.Empty(inRange.OwnClassOrders);

        var ownOnly = await service.CheckDestinationAsync(GroupId, Wednesday, 5, 2);   // rango [5,6]
        Assert.Empty(ownOnly.Collisions);
        Assert.Equal(new List<int> { 6 }, ownOnly.OwnClassOrders.ToList());

        var sunday = await service.CheckDestinationAsync(GroupId, Sunday, 3, 2);       // domingo: sin choques
        Assert.True(sunday.HasSchedule);
        Assert.Empty(sunday.Collisions);
        Assert.Empty(sunday.OwnClassOrders);
    }

    [Fact]
    public async Task CheckDestination_GroupWithoutSchedule_HasScheduleFalse()
    {
        using var db = new TestDb();
        SeedSiblingScenario(db);

        var check = await db.GroupSchedule().CheckDestinationAsync(GroupId, Wednesday, 3, 2);

        Assert.False(check.HasSchedule);
        Assert.Empty(check.Collisions);
        Assert.Empty(check.OwnClassOrders);
    }

    // ==================== Cohortes (horario por semestre-grupo) ====================

    [Fact]
    public void CohortLabel_FormatsSemesterAndGroup()
    {
        Assert.Equal("Sem 3 · Grupo A", GroupScheduleService.CohortLabel(3, " A "));
        Assert.Equal("Grupo A", GroupScheduleService.CohortLabel(null, "A"));
    }

    [Fact]
    public async Task ListCohorts_GroupsByCareerSemesterAndName_CaseInsensitive()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(605, "QG-1-B", "B", SubjectId);                       // otro cohorte (grupo distinto)
        db.AddGroup(SiblingGroupId, "FG-1-A", "  a ", OtherSubjectId);    // mismo cohorte que "A": trim + mayúsculas
        var noCareer = 502;
        db.AddSubject(noCareer, "Electiva", careerId: null, semester: 3);
        db.AddGroup(606, "EL-1-A", "A", noCareer);                        // materia sin carrera: excluido
        db.AddGroup(607, "QG-9-A", "A", SubjectId, managementId: TestDb.Management2Id);   // otra gestión: excluido

        var cohorts = await db.GroupSchedule().ListCohortsAsync(TestDb.ManagementId);

        Assert.Equal(2, cohorts.Count);                     // cohorte "A" (2 materias) y cohorte "B" (1 materia)
        Assert.Equal("Sem 3 · Grupo A", cohorts[0].Label);
        Assert.Equal(TestDb.CareerId, cohorts[0].CareerId);
        Assert.Equal(3, cohorts[0].Semester);
        Assert.Equal("A", cohorts[0].GroupName);
        Assert.Equal(2, cohorts[0].SubjectCount);
        Assert.Equal("Sem 3 · Grupo B", cohorts[1].Label);
        Assert.Equal(1, cohorts[1].SubjectCount);
    }

    [Fact]
    public async Task SaveCohort_ReplacesSet_ForAllCohortGroups()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-A", "A", OtherSubjectId);
        db.AddGroupSlots(GroupId, (1, 1));   // horario previo que debe desaparecer

        var error = await db.GroupSchedule().SaveCohortScheduleAsync(TestDb.ManagementId, TestDb.CareerId, 3, "a",
            new Dictionary<int, IReadOnlyCollection<WeekSlot>>
            {
                [GroupId] = new List<WeekSlot> { new(1, 2), new(1, 3) },
                [SiblingGroupId] = new List<WeekSlot> { new(2, 1), new(7, 1) }   // día fuera de rango: se ignora
            });

        Assert.Null(error);
        var slots = (await db.GroupSchedule().GetGroupSlotsAsync(GroupId)).ToList();
        Assert.Equal(new List<WeekSlot> { new(1, 2), new(1, 3) }, slots);
        var siblingSlots = (await db.GroupSchedule().GetGroupSlotsAsync(SiblingGroupId)).ToList();
        Assert.Equal(new List<WeekSlot> { new(2, 1) }, siblingSlots);
    }

    [Fact]
    public async Task SaveCohort_TwoSubjectsSameCell_ReturnsErrorWithoutSaving()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-A", "A", OtherSubjectId);
        db.AddGroupSlots(GroupId, (1, 1));

        var error = await db.GroupSchedule().SaveCohortScheduleAsync(TestDb.ManagementId, TestDb.CareerId, 3, "A",
            new Dictionary<int, IReadOnlyCollection<WeekSlot>>
            {
                [GroupId] = new List<WeekSlot> { new(3, 4) },
                [SiblingGroupId] = new List<WeekSlot> { new(3, 4) }   // misma celda que Química
            });

        Assert.NotNull(error);
        Assert.Contains("El Miércoles en el periodo 4 hay dos materias", error);
        Assert.Contains("Química General", error);
        Assert.Contains("Física General", error);
        var slots = (await db.GroupSchedule().GetGroupSlotsAsync(GroupId)).ToList();
        Assert.Equal(new List<WeekSlot> { new(1, 1) }, slots);   // nada cambió
    }

    [Fact]
    public async Task SaveCohort_GroupOutsideCohort_ReturnsErrorWithoutSaving()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-B", "B", OtherSubjectId);   // otro grupo: otro cohorte
        db.AddGroupSlots(GroupId, (1, 1));

        var error = await db.GroupSchedule().SaveCohortScheduleAsync(TestDb.ManagementId, TestDb.CareerId, 3, "A",
            new Dictionary<int, IReadOnlyCollection<WeekSlot>>
            {
                [GroupId] = new List<WeekSlot> { new(2, 2) },
                [SiblingGroupId] = new List<WeekSlot> { new(2, 2) }
            });

        Assert.NotNull(error);
        Assert.Contains("no pertenece al cohorte", error);
        var slots = (await db.GroupSchedule().GetGroupSlotsAsync(GroupId)).ToList();
        Assert.Equal(new List<WeekSlot> { new(1, 1) }, slots);   // nada cambió
    }

    [Fact]
    public async Task SaveCohort_TeacherBusyOutsideCohort_ReturnsErrorWithoutSaving()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        var otherSemester = 503;
        db.AddSubject(otherSemester, "Termodinámica", TestDb.CareerId, 5);   // otro semestre: fuera del cohorte
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId, teacherPersonId: TestDb.Teacher1);
        db.AddGroup(608, "TG-1-A", "A", otherSemester, teacherPersonId: TestDb.Teacher1);
        db.AddGroupSlots(608, (1, 2));   // el docente ya da clase el lunes P2

        var error = await db.GroupSchedule().SaveCohortScheduleAsync(TestDb.ManagementId, TestDb.CareerId, 3, "A",
            new Dictionary<int, IReadOnlyCollection<WeekSlot>> { [GroupId] = new List<WeekSlot> { new(1, 2) } });

        Assert.NotNull(error);
        Assert.Contains("docente", error);
        Assert.Contains("Termodinámica", error);
        Assert.Empty(db.Context.CourseGroupScheduleSlots.AsNoTracking().Where(s => s.CourseGroupId == GroupId));
    }

    [Fact]
    public async Task GetCohort_ListsSubjectsWithSlotsAndTeacherBusy()
    {
        using var db = new TestDb();
        db.AddIntern(TestDb.Teacher1, "Ana Pérez");
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId, teacherPersonId: TestDb.Teacher1);
        db.AddGroup(SiblingGroupId, "FG-1-A", "a", OtherSubjectId, teacherPersonId: TestDb.Teacher1);
        db.AddGroupSlots(GroupId, (1, 1));
        db.AddGroupSlots(SiblingGroupId, (1, 2));
        var otherSemester = 503;
        db.AddSubject(otherSemester, "Termodinámica", TestDb.CareerId, 5);
        db.AddGroup(608, "TG-1-A", "A", otherSemester, teacherPersonId: TestDb.Teacher1);
        db.AddGroupSlots(608, (3, 4));   // choque de docente fuera del cohorte

        var view = await db.GroupSchedule().GetCohortScheduleAsync(TestDb.ManagementId, TestDb.CareerId, 3, "A");

        Assert.Equal(2, view.Subjects.Count);
        Assert.Equal("Física General", view.Subjects[0].SubjectName);   // ordenadas por nombre de materia
        Assert.Equal(SiblingGroupId, view.Subjects[0].CourseGroupId);
        Assert.Equal("Química General", view.Subjects[1].SubjectName);
        Assert.Equal("Ana Pérez", view.Subjects[1].TeacherName);
        Assert.Equal("Sem 3 · Grupo A", view.Label);

        var busy = view.TeacherBusy.Where(b => b.CourseGroupId == SiblingGroupId || b.CourseGroupId == GroupId).ToList();
        Assert.Equal(2, busy.Count);   // el docente aparece ocupado para cada materia del cohorte
    }

    [Fact]
    public async Task SaveCohort_CellTakenByUnsentCohortGroup_ReturnsError()
    {
        using var db = new TestDb();
        db.AddSubject(SubjectId, "Química General", TestDb.CareerId, 3);
        db.AddSubject(OtherSubjectId, "Física General", TestDb.CareerId, 3);
        db.AddGroup(GroupId, "QG-1-A", "A", SubjectId);
        db.AddGroup(SiblingGroupId, "FG-1-A", "A", OtherSubjectId);
        db.AddGroupSlots(SiblingGroupId, (2, 3));   // Física ya ocupa martes P3 y no viene en el envío

        var error = await db.GroupSchedule().SaveCohortScheduleAsync(TestDb.ManagementId, TestDb.CareerId, 3, "A",
            new Dictionary<int, IReadOnlyCollection<WeekSlot>> { [GroupId] = new List<WeekSlot> { new(2, 3) } });

        Assert.NotNull(error);
        Assert.Contains("hay dos materias", error);
        Assert.Empty(await db.GroupSchedule().GetGroupSlotsAsync(GroupId));
    }
}
