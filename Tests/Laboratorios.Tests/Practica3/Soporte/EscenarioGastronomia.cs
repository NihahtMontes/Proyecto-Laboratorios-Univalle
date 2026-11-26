using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests.Practica3.Soporte;

/// <summary>
/// Datos de prueba controlados para la Práctica 3 (laboratorios de la Facultad de Gastronomía).
/// Se cargan en la base desechable del servidor de pruebas; todos los nombres de personas son ficticios.
/// </summary>
public static class EscenarioGastronomia
{
    public const string Password = "Practica3#Univalle";
    public const string AdminUser = "admin.p3";          // SuperAdmin: puede reprogramar
    public const string GestorUser = "gestor.p3";        // Administrator: catálogos y laboratorios
    public const string SupervisorUser = "supervisor.p3"; // Supervisor: consulta, sin alta de laboratorios
    public const string CierreUser = "cierre.p3";        // PracticeClosure: solo cierre de prácticas
    public const string BloqueoUser = "bloqueo.p3";      // Supervisor dedicado a la prueba de bloqueo por intentos fallidos

    public const string FacultyName = "Facultad de Gastronomía y Turismo";
    public const string EconomatoCode = "ECONO";
    public const string CocinaFriaCode = "LAB-00001";
    public const string CocinaCalienteCode = "LAB-00002";
    public const string PasteleriaCode = "LAB-00003";

    public const string HarinaCode = "ART-00001";
    public const string HuevoCode = "ART-00002";
    public const string CuchilloCode = "ART-00003";
    public const string MantequillaCode = "ART-00004";
    public const string BowlCode = "ART-00005";
    public const string TablaCode = "ART-00006";

    /// <summary>Práctica de hoy, aprobada, con material por entregar (circuito L-3).</summary>
    public const string PlanHoy = "PRA-P3-001";
    /// <summary>Práctica futura aprobada (origen de la reprogramación).</summary>
    public const string PlanReprogramar = "PRA-P3-002";
    /// <summary>Práctica que ocupa Cocina Caliente (destino ocupado en los escenarios excepcionales).</summary>
    public const string PlanOcupa = "PRA-P3-003";
    /// <summary>Práctica aprobada cuyo material supera la existencia del Economato (recurso no disponible).</summary>
    public const string PlanSinStock = "PRA-P3-004";
    /// <summary>Práctica aprobada reservada para la prueba de falla de la base de datos (Ej. 6).</summary>
    public const string PlanFallaBd = "PRA-P3-005";
    /// <summary>Práctica de hoy con utensilios (proceso completo de aceptación: entrega, devolución, L-5 y cierre).</summary>
    public const string PlanProceso = "PRA-P3-006";

    public static DateTime Today => DateTime.Today;

    /// <summary>Siguiente día hábil (lunes a sábado) a partir de una fecha.</summary>
    public static DateTime NextWorkday(DateTime from, int daysAhead)
    {
        var d = from.Date.AddDays(daysAhead);
        while (d.DayOfWeek == DayOfWeek.Sunday) d = d.AddDays(1);
        return d;
    }

    public static DateTime ReprogramOriginDate => NextWorkday(Today, 5);
    public static DateTime ReprogramTargetDate => NextWorkday(Today, 6);

    public static async Task SeedAsync(IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (await db.Faculties.AnyAsync(f => f.Name == FacultyName)) return;

        // ---------- Roles y usuarios ----------
        var roles = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var role in AuthorizationHelper.ManagedIdentityRoles)
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole<int>(role));

        var users = services.GetRequiredService<UserManager<User>>();
        await CreateUserAsync(users, AdminUser, "Andrea", "Salinas", "7000001", UserRole.SuperAdmin, AuthorizationHelper.RoleSuperAdmin);
        await CreateUserAsync(users, GestorUser, "Rodrigo", "Vargas", "7000002", UserRole.Administrador, AuthorizationHelper.RoleAdministrator);
        await CreateUserAsync(users, SupervisorUser, "Lucía", "Fernández", "7000003", UserRole.Supervisor, AuthorizationHelper.RoleSupervisor);
        await CreateUserAsync(users, CierreUser, "Mario", "Quispe", "7000004", UserRole.PracticeClosure, AuthorizationHelper.RolePracticeClosure);
        await CreateUserAsync(users, BloqueoUser, "Elena", "Rojas", "7000005", UserRole.Supervisor, AuthorizationHelper.RoleSupervisor);

        // ---------- Estructura académica ----------
        var faculty = new Faculty { Name = FacultyName, Code = "FGT", Status = GeneralStatus.Activo };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();
        var career = new Career { Name = "Gastronomía", Code = "GAS", FacultadId = faculty.Id, Status = GeneralStatus.Activo };
        db.Careers.Add(career);
        await db.SaveChangesAsync();

        var economato = new Laboratory { Code = EconomatoCode, Name = "Economato", FacultyId = faculty.Id, Status = GeneralStatus.Activo };
        var cocinaFria = new Laboratory { Code = CocinaFriaCode, Name = "Cocina Fría", FacultyId = faculty.Id, Capacity = 20, Floor = "Planta baja", Status = GeneralStatus.Activo };
        var cocinaCaliente = new Laboratory { Code = CocinaCalienteCode, Name = "Cocina Caliente", FacultyId = faculty.Id, Capacity = 24, Floor = "Planta baja", Status = GeneralStatus.Activo };
        var pasteleria = new Laboratory { Code = PasteleriaCode, Name = "Pastelería", FacultyId = faculty.Id, Capacity = 16, Floor = "Primer piso", Status = GeneralStatus.Activo };
        db.Laboratories.AddRange(economato, cocinaFria, cocinaCaliente, pasteleria);

        var docente1 = new Intern { Name = "Lic. Carla Montaño", Category = PersonCategory.Docente, Status = GeneralStatus.Activo, InternStatus = GeneralStatus.Activo };
        var docente2 = new Intern { Name = "Lic. Javier Céspedes", Category = PersonCategory.Docente, Status = GeneralStatus.Activo, InternStatus = GeneralStatus.Activo };
        var encargado = new Intern { Name = "Tec. Ruth Arnez", Category = PersonCategory.Tecnico, Status = GeneralStatus.Activo, InternStatus = GeneralStatus.Activo };
        db.Interns.AddRange(docente1, docente2, encargado);

        var subject = new Subject { Name = "Cocina Básica I", Code = "GAS-110", CareerId = career.Id, Semester = 1, Status = GeneralStatus.Activo };
        var subject2 = new Subject { Name = "Pastelería Básica", Code = "GAS-120", CareerId = career.Id, Semester = 1, Status = GeneralStatus.Activo };
        db.Subjects.AddRange(subject, subject2);
        await db.SaveChangesAsync();

        // ---------- Gestión 2026-2 y calendario académico ----------
        var management = new Management
        {
            Code = "2026-2",
            Year = 2026,
            Semester = 2,
            Status = ManagementStatus.Active,
            Type = ManagementType.Preventive,
            StartDate = new DateTime(2026, 8, 3),
            PlannedEndDate = new DateTime(2026, 12, 19),
            ClassesStartDate = new DateTime(2026, 8, 3),
            ClassesEndDate = new DateTime(2026, 12, 12),
            Description = "Gestión académica de prueba (Práctica 3)"
        };
        db.Managements.Add(management);
        await db.SaveChangesAsync();
        var calendar = services.GetRequiredService<IAcademicCalendarService>();
        await calendar.GenerateWeeksAsync(management.Id);

        // ---------- Catálogo de artículos y existencias del Economato ----------
        var harina = new Article { Code = HarinaCode, Name = "Harina de trigo", UnitOfMeasure = "KG", Status = GeneralStatus.Activo, ArticleType = ArticleType.Consumable };
        var huevo = new Article { Code = HuevoCode, Name = "Huevo", UnitOfMeasure = "UNIDAD", Status = GeneralStatus.Activo, ArticleType = ArticleType.Consumable };
        var cuchillo = new Article { Code = CuchilloCode, Name = "Cuchillo de chef 8\"", UnitOfMeasure = "UNIDAD", Status = GeneralStatus.Activo };
        var mantequilla = new Article { Code = MantequillaCode, Name = "Mantequilla sin sal", UnitOfMeasure = "KG", Status = GeneralStatus.Activo, ArticleType = ArticleType.Consumable };
        var bowl = new Article { Code = BowlCode, Name = "Bowl de acero inoxidable", UnitOfMeasure = "UNIDAD", Status = GeneralStatus.Activo };
        var tabla = new Article { Code = TablaCode, Name = "Tabla de picar", UnitOfMeasure = "UNIDAD", Status = GeneralStatus.Activo };
        db.Articles.AddRange(harina, huevo, cuchillo, mantequilla, bowl, tabla);
        await db.SaveChangesAsync();
        db.ArticleStocks.AddRange(
            new ArticleStock { LaboratoryId = economato.Id, ArticleId = harina.Id, AvailableQuantity = 50m, UpdatedOn = Today },
            new ArticleStock { LaboratoryId = economato.Id, ArticleId = huevo.Id, AvailableQuantity = 120m, UpdatedOn = Today },
            new ArticleStock { LaboratoryId = economato.Id, ArticleId = cuchillo.Id, AvailableQuantity = 10m, UpdatedOn = Today },
            new ArticleStock { LaboratoryId = economato.Id, ArticleId = mantequilla.Id, AvailableQuantity = 2m, UpdatedOn = Today },
            new ArticleStock { LaboratoryId = economato.Id, ArticleId = bowl.Id, AvailableQuantity = 20m, UpdatedOn = Today },
            new ArticleStock { LaboratoryId = economato.Id, ArticleId = tabla.Id, AvailableQuantity = 15m, UpdatedOn = Today });
        await db.SaveChangesAsync();

        // ---------- Planificaciones ----------
        var periods = await db.ClassPeriods.AsNoTracking().ToListAsync();
        int PeriodId(int order) => periods.First(p => p.Order == order).Id;

        var hoy = await AddPlanningAsync(db, management.Id, PlanHoy, "Mise en place y cortes básicos", subject.Id, docente1.Id, cocinaFria.Id,
            Today.DayOfWeek == DayOfWeek.Sunday ? NextWorkday(Today, 1) : Today, PeriodId(3), 2, PlanningStatus.Approved, 18, periods);
        db.PlanningRequirements.AddRange(
            new PlanningRequirement { PlanningId = hoy.Id, ArticleId = harina.Id, RequestedQuantity = 5m, ExpectedQuantity = 5 },
            new PlanningRequirement { PlanningId = hoy.Id, ArticleId = huevo.Id, RequestedQuantity = 24m, ExpectedQuantity = 24 },
            new PlanningRequirement { PlanningId = hoy.Id, ArticleId = cuchillo.Id, RequestedQuantity = 6m, ExpectedQuantity = 6 });

        var reprogramar = await AddPlanningAsync(db, management.Id, PlanReprogramar, "Fondos y salsas madre", subject.Id, docente1.Id, cocinaFria.Id,
            ReprogramOriginDate, PeriodId(1), 2, PlanningStatus.Approved, 18, periods);
        db.PlanningRequirements.Add(new PlanningRequirement { PlanningId = reprogramar.Id, ArticleId = harina.Id, RequestedQuantity = 2m, ExpectedQuantity = 2 });

        await AddPlanningAsync(db, management.Id, PlanOcupa, "Masas quebradas", subject2.Id, docente2.Id, cocinaCaliente.Id,
            ReprogramTargetDate, PeriodId(5), 2, PlanningStatus.Approved, 15, periods);

        var sinStock = await AddPlanningAsync(db, management.Id, PlanSinStock, "Hojaldre", subject2.Id, docente2.Id, pasteleria.Id,
            Today.DayOfWeek == DayOfWeek.Sunday ? NextWorkday(Today, 1) : Today, PeriodId(7), 2, PlanningStatus.Approved, 14, periods);
        db.PlanningRequirements.Add(new PlanningRequirement { PlanningId = sinStock.Id, ArticleId = mantequilla.Id, RequestedQuantity = 5m, ExpectedQuantity = 5 });

        var proceso = await AddPlanningAsync(db, management.Id, PlanProceso, "Técnicas de corte de vegetales", subject.Id, docente2.Id, cocinaCaliente.Id,
            Today.DayOfWeek == DayOfWeek.Sunday ? NextWorkday(Today, 1) : Today, PeriodId(9), 2, PlanningStatus.Approved, 18, periods);
        db.PlanningRequirements.AddRange(
            new PlanningRequirement { PlanningId = proceso.Id, ArticleId = cuchillo.Id, RequestedQuantity = 4m, ExpectedQuantity = 4 },
            new PlanningRequirement { PlanningId = proceso.Id, ArticleId = bowl.Id, RequestedQuantity = 6m, ExpectedQuantity = 6 },
            new PlanningRequirement { PlanningId = proceso.Id, ArticleId = tabla.Id, RequestedQuantity = 6m, ExpectedQuantity = 6 });

        var fallaBd = await AddPlanningAsync(db, management.Id, PlanFallaBd, "Panes rápidos", subject2.Id, docente2.Id, pasteleria.Id,
            NextWorkday(Today, 2), PeriodId(1), 2, PlanningStatus.Approved, 14, periods);
        db.PlanningRequirements.Add(new PlanningRequirement { PlanningId = fallaBd.Id, ArticleId = harina.Id, RequestedQuantity = 3m, ExpectedQuantity = 3 });
        await db.SaveChangesAsync();
    }

    private static async Task<Planning> AddPlanningAsync(ApplicationDbContext db, int managementId, string code, string practice, int subjectId,
        int teacherId, int laboratoryId, DateTime date, int startPeriodId, int count, string status, int students, IReadOnlyCollection<ClassPeriod> periods)
    {
        var week = await db.AcademicWeeks.AsNoTracking()
            .FirstOrDefaultAsync(w => w.ManagementId == managementId && w.StartDate <= date.Date && w.EndDate >= date.Date);
        var practiceCatalog = new Practice { Code = "PRC-" + code[^3..], SubjectId = subjectId, Name = practice, Status = GeneralStatus.Activo };
        db.Practices.Add(practiceCatalog);
        await db.SaveChangesAsync();
        var schedule = new PracticeSchedule
        {
            ManagementId = managementId, Code = "PP-" + code[^3..], SubjectId = subjectId, PracticeId = practiceCatalog.Id,
            GroupName = "A", TeacherPersonId = teacherId, LaboratoryId = laboratoryId, StudentCount = students,
            AcademicWeekId = week?.Id, WeekNumber = week?.Number, ExecutionDate = date.Date
        };
        var planning = new Planning
        {
            Code = code,
            ManagementId = managementId,
            SubjectId = subjectId,
            TeacherPersonId = teacherId,
            LaboratoryId = laboratoryId,
            PracticeName = practice,
            GroupName = "A",
            Status = status,
            RequestDate = Today,
            AcademicWeekId = week?.Id,
            AcademicWeek = week?.Number ?? 0,
            PracticeSchedule = schedule
        };
        PlanningSlot.Apply(planning, date, startPeriodId, count, periods);
        db.Plannings.Add(planning);
        await db.SaveChangesAsync();
        return planning;
    }

    private static async Task CreateUserAsync(UserManager<User> users, string userName, string firstName, string lastName, string ci, UserRole role, string identityRole)
    {
        var user = new User
        {
            UserName = userName,
            Email = $"{userName}@prueba.univalle.edu",
            EmailConfirmed = true,
            PhoneNumber = "70000000",
            FirstName = firstName,
            LastName = lastName,
            IdentityCard = ci,
            Role = role,
            Status = GeneralStatus.Activo
        };
        var created = await users.CreateAsync(user, Password);
        if (!created.Succeeded)
            throw new InvalidOperationException("No se pudo crear el usuario de prueba: " + string.Join("; ", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, identityRole);
    }
}
