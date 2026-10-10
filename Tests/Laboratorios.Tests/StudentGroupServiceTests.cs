using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests;

/// <summary>Grupo de estudiantes = carrera + semestre + letra ("A1"), con sus materias y docentes.</summary>
public class StudentGroupServiceTests
{
    private const int Cocina = 1, Panaderia = 2, Bartending = 3, OtroSemestre = 4;
    private const int Zabala = 200, Gonzales = 201;

    private static (TestDb Db, StudentGroupService Service) Setup()
    {
        var db = new TestDb();
        db.Context.Careers.Add(new Career { Id = TestDb.CareerId, Name = "Gastronomía" });
        db.Context.Interns.AddRange(
            new Intern { Id = Zabala, Name = "ALBERTO ZABALA", Category = PersonCategory.Docente },
            new Intern { Id = Gonzales, Name = "ALVARO GONZALES", Category = PersonCategory.Docente });
        db.Context.SaveChanges();
        db.Context.ChangeTracker.Clear();
        db.AddSubject(Cocina, "Cocina Básica I", TestDb.CareerId, 1);
        db.AddSubject(Panaderia, "Panadería y Pastelería", TestDb.CareerId, 1);
        db.AddSubject(Bartending, "Bartending y Barismo", TestDb.CareerId, 1);
        db.AddSubject(OtroSemestre, "Cocina Internacional", TestDb.CareerId, 3);
        var service = new StudentGroupService(db.Context, new AcademicManagementService(db.Context), new CourseGroupPurgeService(db.Context));
        return (db, service);
    }

    private static StudentGroupInput Input(string letter, params (int Subject, int Teacher)[] subjects) =>
        new(TestDb.ManagementId, TestDb.CareerId, 1, letter, 20, 20, TestDb.LabA, GeneralStatus.Activo,
            subjects.Select(s => new StudentGroupSubjectInput(s.Subject, s.Teacher, null)).ToList());

    [Fact]
    public async Task Create_NamesGroupLetterPlusSemester_AndCreatesOneCourseGroupPerSubject()
    {
        var (db, service) = Setup();
        using var _ = db;

        var result = await service.CreateAsync(Input("a", (Cocina, Zabala), (Panaderia, Gonzales)));

        Assert.Null(result.Error);
        var group = await service.GetAsync(result.GroupId!.Value);
        Assert.NotNull(group);
        Assert.Equal("A1", group.Name);
        Assert.Equal("A", group.Letter);
        Assert.StartsWith("GRU-", group.Code);
        Assert.Equal(3, group.SemesterSubjectCount);
        Assert.Equal(["Cocina Básica I", "Panadería y Pastelería"], group.Subjects.Select(s => s.SubjectName));
        Assert.Equal(["ALBERTO ZABALA", "ALVARO GONZALES"], group.Subjects.Select(s => s.TeacherName));

        var courseGroups = await db.Context.CourseGroups.AsNoTracking().ToListAsync();
        Assert.All(courseGroups, c =>
        {
            Assert.Equal("A1", c.Name);
            Assert.Equal(20, c.StudentCount);
            Assert.Equal(TestDb.LabA, c.LaboratoryId);
            Assert.StartsWith("GRP-", c.Code);
        });
    }

    [Fact]
    public async Task Create_RejectsDuplicateName_OtherSemesterSubject_AndMissingTeacher()
    {
        var (db, service) = Setup();
        using var _ = db;
        Assert.Null((await service.CreateAsync(Input("A", (Cocina, Zabala)))).Error);

        Assert.Contains("Ya existe el grupo A1", (await service.CreateAsync(Input("A", (Panaderia, Zabala)))).Error);
        Assert.Contains("no es una materia activa del 1.º semestre", (await service.CreateAsync(Input("B", (OtroSemestre, Zabala)))).Error);
        Assert.Contains("Asigne el docente", (await service.CreateAsync(Input("B", (Cocina, 0)))).Error);
        Assert.Contains("1 a 3 letras", (await service.CreateAsync(Input("B2", (Cocina, Zabala)))).Error);
        Assert.Contains("al menos una materia", (await service.CreateAsync(Input("B"))).Error);
    }

    [Fact]
    public async Task Update_RenamesGroup_SyncsSubjects_AddsAndRemovesSubjects()
    {
        var (db, service) = Setup();
        using var _ = db;
        var id = (await service.CreateAsync(Input("A", (Cocina, Zabala), (Panaderia, Gonzales)))).GroupId!.Value;
        var panaderiaGroupId = (await service.GetAsync(id))!.Subjects.Single(s => s.SubjectId == Panaderia).CourseGroupId;
        db.AddGroupSlots(panaderiaGroupId, (1, 2));

        // Una planificación guardó el nombre del grupo: debe seguir al nombre nuevo.
        db.AddPlanning(90, TestDb.Week1Monday, 1, 2);
        var planning = await db.Context.Plannings.AsTracking().SingleAsync(p => p.Id == 90);
        planning.SubjectId = Cocina;
        planning.GroupName = "A1";
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var update = Input("B", (Cocina, Gonzales), (Bartending, Zabala)) with { StudentCount = 25 };
        var result = await service.UpdateAsync(id, update);

        Assert.Null(result.Error);
        var group = (await service.GetAsync(id))!;
        Assert.Equal("B1", group.Name);
        Assert.Equal(["Bartending y Barismo", "Cocina Básica I"], group.Subjects.Select(s => s.SubjectName));
        Assert.Equal(Gonzales, group.Subjects.Single(s => s.SubjectId == Cocina).TeacherPersonId);

        var courseGroups = await db.Context.CourseGroups.AsNoTracking().ToListAsync();
        Assert.Equal(2, courseGroups.Count); // Panadería se quitó del grupo
        Assert.All(courseGroups, c => { Assert.Equal("B1", c.Name); Assert.Equal(25, c.StudentCount); });
        Assert.Empty(await db.Context.CourseGroupScheduleSlots.IgnoreQueryFilters().ToListAsync());
        Assert.Equal("B1", (await db.Context.Plannings.AsNoTracking().SingleAsync(p => p.Id == 90)).GroupName);
    }

    [Fact]
    public async Task Delete_RemovesGroupAndItsSubjects()
    {
        var (db, service) = Setup();
        using var _ = db;
        var id = (await service.CreateAsync(Input("A", (Cocina, Zabala), (Panaderia, Gonzales)))).GroupId!.Value;

        Assert.Null(await service.DeleteAsync(id));

        Assert.Empty(await db.Context.StudentGroups.AsNoTracking().ToListAsync());
        Assert.Empty(await db.Context.CourseGroups.IgnoreQueryFilters().AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task List_ReturnsGroupsWithSubjectProgressAndSchedule()
    {
        var (db, service) = Setup();
        using var _ = db;
        var a = (await service.CreateAsync(Input("A", (Cocina, Zabala)))).GroupId!.Value;
        await service.CreateAsync(Input("B", (Cocina, Gonzales), (Panaderia, Zabala)));
        db.AddGroupSlots((await service.GetAsync(a))!.Subjects[0].CourseGroupId, (1, 1), (1, 2));

        var list = await service.ListAsync(TestDb.ManagementId);

        Assert.Equal(["A1", "B1"], list.Select(g => g.Name));
        Assert.Equal(2, list[0].PeriodsPerWeek);
        Assert.Equal(0, list[0].SubjectsWithoutSchedule);
        Assert.Equal(2, list[1].SubjectsWithoutSchedule);
        Assert.Equal("1|A1", list[0].CohortKey);
    }

    [Fact]
    public void ComposeName_LetterPlusSemester_LegacyNamesKept()
    {
        Assert.Equal("A1", StudentGroup.ComposeName(" a ", 1));
        Assert.Equal("AB10", StudentGroup.ComposeName("AB", 10));
        Assert.Equal("G-2", StudentGroup.ComposeName("G-2", 3));
    }
}
