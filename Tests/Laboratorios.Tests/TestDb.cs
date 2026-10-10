using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;

namespace Laboratorios.Tests;

internal sealed class FakeCurrentUser : ICurrentUserService
{
    public int? UserId => null;
}

/// <summary>
/// Base EF InMemory con el mismo comportamiento de consultas que la app (NoTracking global).
/// La siembra usa el SaveChanges SINCRÓNICO a propósito: la app solo sobrescribe SaveChangesAsync
/// (auditoría, sincronización de mantenimientos/históricos e incidentes L5), lógica ajena a estas pruebas.
/// EnableNullChecks(false) evita completar propiedades obligatorias irrelevantes en cada entidad sembrada.
/// EnsureCreated siembra el catálogo de periodos (HasData, Id = Order = 1..12).
/// </summary>
internal sealed class TestDb : IDisposable
{
    public const int ManagementId = 1;
    public const int Week1Id = 1;
    public const int Week2Id = 2;

    /// <summary>Semana 1: lunes 05/10/2026 a sábado 10/10/2026. Semana 2: 12/10 a 17/10 (miércoles 14 suspendido).</summary>
    public static readonly DateTime Week1Monday = new(2026, 10, 5);
    public static readonly DateTime Week2Monday = new(2026, 10, 12);
    public static readonly DateTime SuspendedDay = new(2026, 10, 14);
    public const string SuspendedReason = "Feriado departamental";

    public const int LabA = 1;          // capacidad 20
    public const int LabB = 2;          // sin capacidad registrada
    public const int LabInactive = 3;

    public const int Teacher1 = 100;
    public const int Teacher2 = 101;

    public ApplicationDbContext Context { get; }

    public TestDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        Context = new ApplicationDbContext(options, new FakeCurrentUser());
        Context.Database.EnsureCreated();
        SeedCalendarAndLabs();
    }

    private void SeedCalendarAndLabs()
    {
        Context.Managements.Add(new Management { Id = ManagementId, Code = "2026-2", Year = 2026, Semester = 2 });
        AddWeek(Week1Id, 1, Week1Monday);
        AddWeek(Week2Id, 2, Week2Monday);
        Context.Laboratories.AddRange(
            new Laboratory { Id = LabA, Code = "LAB-A", Name = "Laboratorio A", Capacity = 20 },
            new Laboratory { Id = LabB, Code = "LAB-B", Name = "Laboratorio B", Capacity = null },
            new Laboratory { Id = LabInactive, Code = "LAB-X", Name = "Laboratorio inactivo", Capacity = 30, Status = GeneralStatus.Inactivo });
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    private void AddWeek(int id, int number, DateTime monday)
    {
        var week = new AcademicWeek { Id = id, ManagementId = ManagementId, Number = number, StartDate = monday, EndDate = monday.AddDays(5) };
        for (var i = 0; i < 6; i++)
        {
            var date = monday.AddDays(i);
            var suspended = date == SuspendedDay;
            week.Days.Add(new AcademicDay
            {
                Date = date,
                Status = suspended ? AcademicDayStatus.Suspended : AcademicDayStatus.Enabled,
                Reason = suspended ? SuspendedReason : null
            });
        }
        Context.AcademicWeeks.Add(week);
    }

    /// <summary>Agrega una planificación con horario derivado por PlanningSlot.Apply (periodo Id = Order).</summary>
    public Planning AddPlanning(int id, DateTime date, int startOrder, int count, int laboratoryId = LabA, int teacherPersonId = Teacher1,
        string status = PlanningStatus.Planned, int? weekId = Week1Id, int? students = 15, string subject = "Química", int? courseGroupId = null)
    {
        var planning = new Planning
        {
            Id = id,
            ManagementId = ManagementId,
            LaboratoryId = laboratoryId,
            TeacherPersonId = teacherPersonId,
            PracticeName = $"Práctica {id}",
            GroupName = "A",
            Status = status,
            AcademicWeekId = weekId,
            RequestDate = date,
            CourseGroupId = courseGroupId,
            Subject = new Subject { Id = id * 10, Name = subject },
            PracticeSchedule = students.HasValue ? new PracticeSchedule { Id = id * 10, Code = $"PP-{id}", StudentCount = students } : null
        };
        PlanningSlot.Apply(planning, date, startOrder, count, Periods());
        Context.Plannings.Add(planning);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        return planning;
    }

    /// <summary>Planificación legada: sin periodo, solo con horas.</summary>
    public void AddLegacyPlanning(int id, DateTime date, TimeSpan start, TimeSpan end, int laboratoryId = LabA, int teacherPersonId = Teacher2)
    {
        Context.Plannings.Add(new Planning
        {
            Id = id,
            ManagementId = ManagementId,
            LaboratoryId = laboratoryId,
            TeacherPersonId = teacherPersonId,
            PracticeName = $"Legada {id}",
            Status = PlanningStatus.Approved,
            RequestDate = date,
            ScheduledDate = date + start,
            StartTime = start,
            EndTime = end
        });
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public void AddArticleWithStock(int articleId, string code, string name, decimal? stock)
    {
        Context.Articles.Add(new Article { Id = articleId, Code = code, Name = name });
        if (stock.HasValue)
            Context.ArticleStocks.Add(new ArticleStock { ArticleId = articleId, LaboratoryId = LabA, AvailableQuantity = stock.Value });
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public void AddRequirement(int planningId, int articleId, decimal quantity)
    {
        Context.PlanningRequirements.Add(new PlanningRequirement { PlanningId = planningId, ArticleId = articleId, RequestedQuantity = quantity });
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public void AddDeparture(int planningId, LoanStatus status)
    {
        Context.Departures.Add(new Departure { ManagementId = ManagementId, PlanningId = planningId, Status = status });
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public IReadOnlyCollection<ClassPeriod> Periods() => Context.ClassPeriods.AsNoTracking().ToList();

    public IOccupancyService Occupancy() => new OccupancyService([new PlanningOccupancySource(Context)]);

    public SlotValidationService Validation() => new(
        Context,
        Occupancy(),
        new ArticleAvailabilityService(Context, new L3CircuitService(Context, NullLogger<L3CircuitService>.Instance)),
        new GroupScheduleService(Context));

    public GroupScheduleService GroupSchedule() => new(Context);

    // ==================== Siembra académica: carrera, materia, grupos y horarios ====================

    public const int CareerId = 10;
    public const int Management2Id = 2;

    public void AddManagement(int id, string code, int year = 2026, int semester = 2)
    {
        Context.Managements.Add(new Management { Id = id, Code = code, Year = year, Semester = semester });
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public Intern AddIntern(int id, string name)
    {
        var intern = new Intern { Id = id, Name = name };
        Context.Interns.Add(intern);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        return intern;
    }

    public Subject AddSubject(int id, string name, int? careerId = null, int? semester = null, GeneralStatus status = GeneralStatus.Activo)
    {
        var subject = new Subject { Id = id, Name = name, CareerId = careerId, Semester = semester, Status = status };
        Context.Subjects.Add(subject);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        return subject;
    }

    public CourseGroup AddGroup(int id, string code, string name, int subjectId, int teacherPersonId = Teacher1,
        int managementId = ManagementId, GeneralStatus status = GeneralStatus.Activo)
    {
        var group = new CourseGroup { Id = id, Code = code, Name = name, SubjectId = subjectId, TeacherPersonId = teacherPersonId, ManagementId = managementId, Status = status };
        Context.CourseGroups.Add(group);
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
        return group;
    }

    /// <summary>Agrega celdas de horario semanal al grupo (periodo Id = Order, como en el catálogo sembrado).</summary>
    public void AddGroupSlots(int groupId, params (int Weekday, int PeriodOrder)[] cells)
    {
        foreach (var (weekday, periodOrder) in cells)
            Context.CourseGroupScheduleSlots.Add(new CourseGroupScheduleSlot
            {
                CourseGroupId = groupId,
                Weekday = weekday,
                ClassPeriodId = periodOrder   // el catálogo sembrado usa Id = Order (1..12)
            });
        Context.SaveChanges();
        Context.ChangeTracker.Clear();
    }

    public void Dispose() => Context.Dispose();
}
