using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services.PlanningImport;

/// <summary>
/// Qué hay que preparar en el sistema antes de revisar las prácticas del generador: la gestión (estado y fechas),
/// el calendario académico y los grupos de materia. Si no hay nada que preparar, se pasa directo a la revisión.
/// </summary>
public sealed record GeneratorSetupPlan(
    string ManagementCode,
    int? ManagementId,
    string? Error,
    IReadOnlyList<string> ManagementChanges,
    bool CalendarNeeded,
    string CalendarText,
    IReadOnlyList<string> GroupsToCreate,
    IReadOnlyList<string> GroupsToUpdate,
    IReadOnlyList<string> Notices)
{
    public bool HasChanges => Error == null
        && (ManagementChanges.Count > 0 || CalendarNeeded || GroupsToCreate.Count > 0 || GroupsToUpdate.Count > 0);
}

public interface IGeneratorSetupService
{
    /// <summary>Revisa (sin escribir) qué hay que preparar para cargar el generador.</summary>
    Task<GeneratorSetupPlan> PlanAsync(GeneratorWorkbook generator, CancellationToken ct = default);

    /// <summary>Aplica la preparación: gestión lista para planificar, calendario de lunes a sábado y grupos.</summary>
    Task<CalendarResult> ApplyAsync(GeneratorWorkbook generator, CancellationToken ct = default);
}

public sealed class GeneratorSetupService : IGeneratorSetupService
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicCalendarService _calendar;
    private readonly ILogger<GeneratorSetupService> _logger;

    public GeneratorSetupService(ApplicationDbContext context, IAcademicCalendarService calendar, ILogger<GeneratorSetupService> logger)
    {
        _context = context;
        _calendar = calendar;
        _logger = logger;
    }

    private sealed record GroupChange(GeneratorGroup Source, int SubjectId, CourseGroup? Existing, int? TeacherId,
        int? LaboratoryId, List<string> Fields);

    private sealed record Work(Management? Management, string? Error, List<string> ManagementChanges, bool CalendarNeeded,
        string CalendarText, List<GroupChange> Groups, List<string> Notices);

    public async Task<GeneratorSetupPlan> PlanAsync(GeneratorWorkbook generator, CancellationToken ct = default)
    {
        var work = await BuildAsync(generator, tracking: false, ct);
        return new GeneratorSetupPlan(
            generator.ManagementCode, work.Management?.Id, work.Error, work.ManagementChanges, work.CalendarNeeded, work.CalendarText,
            work.Groups.Where(g => g.Existing == null).Select(Describe).ToList(),
            work.Groups.Where(g => g.Existing != null).Select(Describe).ToList(),
            work.Notices);
    }

    public async Task<CalendarResult> ApplyAsync(GeneratorWorkbook generator, CancellationToken ct = default)
    {
        var work = await BuildAsync(generator, tracking: true, ct);
        if (work.Error != null || work.Management is not Management management)
            return new CalendarResult(false, work.Error ?? "No se encontró la gestión.");
        var start = generator.ClassesStart!.Value;
        var end = generator.ClassesEnd!.Value;

        // 1) Gestión lista para planificar: editable y con fechas que cubren las clases.
        if (management.Status is not (ManagementStatus.Active or ManagementStatus.Inactive))
            management.Status = ManagementStatus.Inactive;
        if (management.StartDate is not DateTime s || s.Date > start) management.StartDate = start;
        if (management.PlannedEndDate is not DateTime e || e.Date < end) management.PlannedEndDate = end;
        await _context.SaveChangesAsync(ct);

        // 2) Calendario lunes a sábado (los feriados se suspenden después en el calendario académico).
        if (work.CalendarNeeded)
        {
            var calendar = await _calendar.GenerateWeeksAsync(management.Id, start, end, updateClassDates: true);
            if (!calendar.Ok) return calendar;
        }

        // 3) Grupos: se crean los que faltan y se actualizan los datos que cambiaron.
        var created = 0;
        var updated = 0;
        foreach (var change in work.Groups)
        {
            var src = change.Source;
            var group = change.Existing;
            if (group == null)
            {
                if (change.TeacherId is not int teacherId) continue;   // sin docente no se puede crear (ya se avisó)
                group = new CourseGroup
                {
                    Code = await UniqueCodeAsync($"GRP-{management.Code}-{src.SubjectCode}-{src.Group}", ct),
                    ManagementId = management.Id,
                    SubjectId = change.SubjectId,
                    Name = src.Group,
                    TeacherPersonId = teacherId,
                    Status = GeneralStatus.Activo
                };
                _context.CourseGroups.Add(group);
                created++;
            }
            else
                updated++;
            if (change.TeacherId is int t) group.TeacherPersonId = t;
            if (change.LaboratoryId is int lab) group.LaboratoryId = lab;
            if (src.Weekday is int day) group.Weekday = day;
            if (src.Students is int students) group.StudentCount = students;
            if (src.Factor is int factor) group.ScalingFactor = factor;
            group.IsDemonstrative = src.Demonstrative;
        }
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Generador {Code}: gestión preparada, {Created} grupos creados y {Updated} actualizados", management.Code, created, updated);

        return new CalendarResult(true,
            $"Gestión {management.Code} preparada: calendario del {start:dd/MM/yyyy} al {end:dd/MM/yyyy}"
            + (created + updated > 0 ? $", {created} grupo(s) creado(s) y {updated} actualizado(s)." : "."));
    }

    private async Task<string> UniqueCodeAsync(string code, CancellationToken ct)
    {
        var candidate = code.Length > 50 ? code[..50] : code;
        for (var i = 2; await _context.CourseGroups.AnyAsync(g => g.Code == candidate, ct) || _context.CourseGroups.Local.Any(g => g.Code == candidate); i++)
            candidate = $"{(code.Length > 46 ? code[..46] : code)}-{i}";
        return candidate;
    }

    private static string Describe(GroupChange g) =>
        $"{g.Source.SubjectCode} · {g.Source.SubjectName} · grupo {g.Source.Group}"
        + (g.Fields.Count > 0 ? $" ({string.Join(", ", g.Fields)})" : "");

    private async Task<Work> BuildAsync(GeneratorWorkbook generator, bool tracking, CancellationToken ct)
    {
        var notices = new List<string>();
        Work Fail(string error) => new(null, error, [], false, "", [], notices);

        if (generator.ManagementCode.Length == 0)
            return Fail("No se pudo leer la gestión del título de la hoja CONFIG (se esperaba algo como «GESTIÓN 1-2027»).");
        if (generator.ClassesStart is not DateTime start || generator.ClassesEnd is not DateTime end || end < start)
            return Fail("No se pudieron leer las semanas académicas de la hoja CONFIG.");

        var managements = tracking ? _context.Managements.AsTracking() : _context.Managements.AsNoTracking();
        var management = await managements.FirstOrDefaultAsync(m => m.Code == generator.ManagementCode, ct);
        if (management == null)
            return Fail($"No existe la gestión {generator.ManagementCode}. Créela en Gestiones y vuelva a subir el archivo.");
        if (management.Type == ManagementType.Corrective || management.Semester <= 0)
            return Fail($"La gestión {management.Code} no es una gestión académica.");

        var changes = new List<string>();
        if (management.Status is not (ManagementStatus.Active or ManagementStatus.Inactive))
            changes.Add($"Estado: {Label(management.Status)} → Inactiva (lista para planificar; no cambia la gestión actual).");
        if (management.StartDate is not DateTime ms || ms.Date > start)
            changes.Add($"Inicio de la gestión: {management.StartDate:dd/MM/yyyy} → {start:dd/MM/yyyy}.");
        if (management.PlannedEndDate is not DateTime me || me.Date < end)
            changes.Add($"Fin planificado de la gestión: {management.PlannedEndDate:dd/MM/yyyy} → {end:dd/MM/yyyy} (la última semana de clases termina el {end:dd/MM/yyyy}).");

        var dates = Enumerable.Range(0, (end - start).Days + 1).Select(i => start.AddDays(i))
            .Where(d => d.DayOfWeek != DayOfWeek.Sunday).ToList();
        var existingDays = (await _context.AcademicDays.AsNoTracking()
            .Where(d => d.AcademicWeek.ManagementId == management.Id && d.Date >= start && d.Date <= end)
            .Select(d => d.Date).ToListAsync(ct)).Select(d => d.Date).ToHashSet();
        var classDatesChange = management.ClassesStartDate?.Date != start || management.ClassesEndDate?.Date != end;
        var calendarNeeded = classDatesChange || dates.Any(d => !existingDays.Contains(d));
        var weeks = dates.Select(d => d.AddDays(-(((int)d.DayOfWeek + 6) % 7))).Distinct().Count();
        var calendarText = calendarNeeded
            ? $"Calendario: {weeks} semanas de lunes a sábado, del {start:dd/MM/yyyy} al {end:dd/MM/yyyy}, todos los días habilitados (los feriados se suspenden después en el calendario académico)."
            : "";
        var outside = generator.Rows.Where(r => r.Date is DateTime d && (d < start || d > end)).Select(r => r.RowNumber).ToList();
        if (outside.Count > 0)
            notices.Add($"{outside.Count} práctica(s) tienen fecha fuera de las semanas de CONFIG (filas {string.Join(", ", outside.Take(10))}{(outside.Count > 10 ? "…" : "")}).");

        // Grupos de materia
        var subjects = (await _context.Subjects.AsNoTracking().Where(s => s.Status == GeneralStatus.Activo)
            .Select(s => new { s.Id, s.Code }).ToListAsync(ct))
            .GroupBy(s => PlanningImportService.Norm(s.Code)).ToDictionary(g => g.Key, g => g.First().Id);
        var groupsQuery = _context.CourseGroups.Where(g => g.ManagementId == management.Id && g.Status != GeneralStatus.Eliminado);
        var groups = await (tracking ? groupsQuery.AsTracking() : groupsQuery.AsNoTracking()).ToListAsync(ct);
        var labs = (await _context.Laboratories.AsNoTracking().Where(l => l.Status == GeneralStatus.Activo)
            .Select(l => new { l.Id, l.Code }).ToListAsync(ct))
            .GroupBy(l => PlanningImportService.Norm(l.Code)).ToDictionary(g => g.Key, g => g.First().Id);
        var teachers = (await ReschedulingService.ActiveTeachers(_context).Select(t => new { t.Id, t.Name }).ToListAsync(ct))
            .Select(t => (t.Id, t.Name, Key: PlanningImportService.NormPerson(t.Name))).ToList();

        var result = new List<GroupChange>();
        foreach (var src in generator.Groups)
        {
            var label = $"{src.SubjectCode} grupo {src.Group}";
            if (!subjects.TryGetValue(PlanningImportService.Norm(src.SubjectCode), out var subjectId))
            {
                notices.Add($"La materia {src.SubjectCode} ({src.SubjectName}) no existe o está inactiva en el sistema: sus prácticas no se podrán cargar.");
                continue;
            }
            var (teacherId, teacherProblem) = MatchTeacher(teachers, src.Teacher);
            if (teacherProblem != null) notices.Add($"{label}: {teacherProblem}");
            int? labId = labs.TryGetValue(PlanningImportService.Norm(src.Laboratory), out var l) ? l : null;
            if (labId == null && src.Laboratory.Length > 0)
                notices.Add($"{label}: el laboratorio «{src.Laboratory}» no existe en el sistema.");

            var existing = groups.FirstOrDefault(g => g.SubjectId == subjectId && PlanningImportService.Norm(g.Name) == PlanningImportService.Norm(src.Group));
            var fields = new List<string>();
            if (existing == null)
            {
                if (teacherId == null)
                {
                    notices.Add($"{label}: no se crea el grupo porque no se identificó al docente.");
                    continue;
                }
                fields.Add("nuevo");
            }
            else
            {
                if (teacherId is int t && existing.TeacherPersonId != t) fields.Add("docente");
                if (labId is int lab && existing.LaboratoryId != lab) fields.Add("laboratorio");
                if (src.Weekday is int day && existing.Weekday != day) fields.Add("día");
                if (src.Students is int n && existing.StudentCount != n) fields.Add($"estudiantes {existing.StudentCount?.ToString() ?? "—"} → {n}");
                if (src.Factor is int f && existing.ScalingFactor != f) fields.Add($"factor {existing.ScalingFactor?.ToString() ?? "—"} → {f}");
                if (existing.IsDemonstrative != src.Demonstrative) fields.Add("demostrativa");
                if (fields.Count == 0) continue;
            }
            result.Add(new GroupChange(src, subjectId, existing, teacherId, labId, fields));
        }

        return new Work(management, null, changes, calendarNeeded, calendarText, result, notices);
    }

    /// <summary>
    /// Docente por nombre como lo escribe la Inge ("ING PEDRO GUERECA"): coincidencia exacta sin títulos o, si no,
    /// la única persona cuyo nombre contiene todas las palabras escritas (suele faltar el segundo apellido).
    /// </summary>
    public static (int? Id, string? Problem) MatchTeacher(IReadOnlyList<(int Id, string Name, string Key)> teachers, string name)
    {
        var key = PlanningImportService.NormPerson(name);
        if (key.Length == 0) return (null, "no tiene docente.");
        var exact = teachers.Where(t => t.Key == key).ToList();
        if (exact.Count == 1) return (exact[0].Id, null);
        if (exact.Count > 1) return (null, $"hay {exact.Count} docentes llamados «{name}»: diferéncielos en Personas.");
        var tokens = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contained = tokens.Length < 2 ? [] : teachers
            .Where(t => tokens.All(x => t.Key.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(x))).ToList();
        return contained.Count switch
        {
            1 => (contained[0].Id, null),
            0 => (null, $"no hay un docente activo llamado «{name}»."),
            _ => (null, $"«{name}» coincide con {contained.Count} docentes ({string.Join(", ", contained.Select(c => c.Name))}): diferéncielos en Personas.")
        };
    }

    private static string Label(ManagementStatus status) => status switch
    {
        ManagementStatus.Active => "Activa",
        ManagementStatus.Inactive => "Inactiva",
        ManagementStatus.Completed => "Completada (histórico)",
        _ => status.ToString()
    };
}
