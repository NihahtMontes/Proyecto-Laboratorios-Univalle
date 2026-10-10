using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Celda del horario semanal: día (1 = lunes ... 6 = sábado) + orden del periodo (1..12).</summary>
public sealed record WeekSlot(int Weekday, int PeriodOrder);

/// <summary>Grupo de una materia con su horario semanal.</summary>
/// <param name="Summary">Resumen legible, p. ej. "Lunes periodos 1–3 · Miércoles periodo 4" ("Sin horario" si no tiene).</param>
public sealed record GroupScheduleView(int CourseGroupId, string Code, string Name, string TeacherName,
    int? StudentCount, IReadOnlyList<WeekSlot> Slots, string Summary);

/// <summary>
/// Clase de OTRO grupo que comparte estudiantes con el grupo consultado (misma gestión, carrera, semestre y
/// nombre de grupo, otra materia). Se usa para mostrar choques al cargar el horario y al reprogramar.
/// </summary>
public sealed record SiblingClass(int CourseGroupId, string SubjectName, string GroupName, int Weekday, int PeriodOrder);

/// <summary>Resultado de comparar un destino (fecha + periodos) con el horario del grupo de la planificación.</summary>
/// <param name="HasSchedule">False si el grupo no tiene horario cargado (no se bloquea nada).</param>
/// <param name="Collisions">Clases de otras materias del mismo grupo de estudiantes que caen en el destino.</param>
/// <param name="OwnClassOrders">Periodos del destino en que el grupo tiene clase de esta misma materia (sugeridos).</param>
public sealed record GroupDestinationCheck(bool HasSchedule, IReadOnlyList<SiblingClass> Collisions, IReadOnlyList<int> OwnClassOrders);

/// <summary>
/// Clave de cohorte (VIRTUAL, sin tabla): misma gestión, carrera, semestre y nombre de grupo
/// (sin mayúsculas ni espacios extremos). Es la misma regla de "mismos estudiantes" de GetSiblingClassesAsync.
/// </summary>
public sealed record CohortKey(int CareerId, int Semester, string GroupName);

/// <summary>Cohorte disponible para armar el horario semanal por semestre-grupo.</summary>
/// <param name="SubjectCount">Cantidad de materias (grupos activos) que integran el cohorte.</param>
/// <param name="Label">Etiqueta visible, p. ej. "Sem 3 · Grupo A".</param>
public sealed record CohortOption(int CareerId, string CareerName, int Semester, string GroupName, int SubjectCount, string Label);

/// <summary>Materia-grupo del cohorte con su horario semanal y color de la paleta (por posición).</summary>
/// <param name="Summary">Resumen legible, p. ej. "Lunes periodos 1–3 · Miércoles periodo 4" ("Sin horario" si no tiene).</param>
public sealed record CohortSubjectView(int CourseGroupId, int SubjectId, string SubjectName, string? SubjectCode, string GroupName,
    string TeacherName, IReadOnlyList<WeekSlot> Slots, string Summary, int ColorIndex);

/// <summary>
/// Celda donde el docente de una materia del cohorte ya da clase en OTRO grupo activo de la misma
/// gestión, fuera del cohorte (choque de docente: aviso informativo; el guardado sí lo bloquea).
/// </summary>
public sealed record CohortBusyCell(int CourseGroupId, int Weekday, int PeriodOrder, string OtherSubjectName);

/// <summary>Horario semanal completo de un cohorte (todas sus materias con grupo).</summary>
public sealed record CohortScheduleView(int CareerId, string CareerName, int Semester, string GroupName, string Label,
    IReadOnlyList<CohortSubjectView> Subjects, IReadOnlyList<CohortBusyCell> TeacherBusy);

/// <summary>Materia del semestre del cohorte que todavía no tiene grupo con ese nombre en la gestión.</summary>
public sealed record CohortPendingSubject(int SubjectId, string SubjectName);

/// <summary>Clase del docente en otro grupo activo de la gestión (no puede dictar dos clases a la vez).</summary>
public sealed record TeacherBusySlot(int Weekday, int PeriodOrder, string SubjectName, string GroupName);

/// <summary>
/// Contexto del formulario Crear/Editar grupo: con qué otras materias comparte estudiantes (cohorte = carrera +
/// semestre + nombre de grupo), qué materias del semestre faltan y cuándo está ocupado el docente.
/// </summary>
/// <param name="HasCohort">False si la materia no tiene carrera o semestre, o si aún no hay nombre de grupo.</param>
/// <param name="Siblings">Otras materias del cohorte con su horario (excluye la materia y el grupo del formulario).</param>
public sealed record GroupFormContext(bool HasCohort, int? CareerId, string? CareerName, int? Semester, string? GroupName,
    string? CohortLabel, IReadOnlyList<CohortSubjectView> Siblings, IReadOnlyList<CohortPendingSubject> PendingSubjects,
    IReadOnlyList<TeacherBusySlot> TeacherBusy)
{
    /// <summary>Primera clase del docente que cae en alguna de las celdas indicadas; null si no hay choque.</summary>
    public TeacherBusySlot? FindTeacherClash(IEnumerable<WeekSlot> slots)
    {
        var cells = slots.Select(c => (c.Weekday, c.PeriodOrder)).ToHashSet();
        return TeacherBusy.FirstOrDefault(b => cells.Contains((b.Weekday, b.PeriodOrder)));
    }
}

public interface IGroupScheduleService
{
    /// <summary>Grupos activos de la materia en la gestión, con su horario (ordenados por nombre).</summary>
    Task<IReadOnlyList<GroupScheduleView>> GetSubjectScheduleAsync(int subjectId, int managementId, CancellationToken ct = default);

    /// <summary>
    /// Reemplaza el horario de cada grupo indicado por el conjunto recibido (los grupos deben ser de la materia y gestión).
    /// Ignora celdas fuera de rango (día 1..6, periodo existente). Devuelve un mensaje de error en español o null si guardó.
    /// </summary>
    Task<string?> SaveSubjectScheduleAsync(int subjectId, int managementId,
        IReadOnlyDictionary<int, IReadOnlyCollection<WeekSlot>> slotsByGroup, CancellationToken ct = default);

    /// <summary>Clases de los grupos que comparten estudiantes con el grupo indicado (otras materias).</summary>
    Task<IReadOnlyList<SiblingClass>> GetSiblingClassesAsync(int courseGroupId, CancellationToken ct = default);

    /// <summary>Horario propio del grupo (sus celdas).</summary>
    Task<IReadOnlyList<WeekSlot>> GetGroupSlotsAsync(int courseGroupId, CancellationToken ct = default);

    /// <summary>Compara un destino con el horario del grupo: choques con otras materias y clases propias.</summary>
    Task<GroupDestinationCheck> CheckDestinationAsync(int courseGroupId, DateTime date, int startOrder, int periodCount,
        CancellationToken ct = default);

    /// <summary>Resumen legible de un conjunto de celdas ("Lunes periodos 1–3 · Miércoles periodo 4").</summary>
    string Summarize(IEnumerable<WeekSlot> slots);

    /// <summary>
    /// Coortes (carrera + semestre + nombre de grupo) con grupos activos en la gestión cuya materia tiene
    /// carrera y semestre registrados; opcionalmente solo los de una carrera. Orden: carrera, semestre, grupo.
    /// </summary>
    Task<IReadOnlyList<CohortOption>> ListCohortsAsync(int managementId, int? careerId = null, CancellationToken ct = default);

    /// <summary>Horario semanal del cohorte: materias con sus slots y choques de docente fuera del cohorte.</summary>
    Task<CohortScheduleView> GetCohortScheduleAsync(int managementId, int careerId, int semester, string groupName,
        CancellationToken ct = default);

    /// <summary>
    /// Reemplaza el horario de los grupos del cohorte indicado (los grupos enviados deben pertenecer al cohorte).
    /// Rechaza dos materias del cohorte en la misma celda y el choque del docente con un grupo fuera del cohorte.
    /// Ignora celdas fuera de rango (día 1..6, periodo existente). Devuelve un mensaje de error en español o null si guardó.
    /// </summary>
    Task<string?> SaveCohortScheduleAsync(int managementId, int careerId, int semester, string groupName,
        IReadOnlyDictionary<int, IReadOnlyCollection<WeekSlot>> slotsByGroup, CancellationToken ct = default);

    /// <summary>
    /// Contexto del formulario de grupo: horario de las otras materias del cohorte (misma carrera, semestre y nombre
    /// de grupo), materias del semestre sin grupo y clases del docente en otros grupos de la gestión.
    /// <paramref name="excludeGroupId"/> es el grupo que se edita (no cuenta como ocupación propia).
    /// </summary>
    Task<GroupFormContext> GetGroupFormContextAsync(int managementId, int subjectId, string? groupName, int? teacherPersonId,
        int? excludeGroupId, CancellationToken ct = default);
}

public sealed class GroupScheduleService : IGroupScheduleService
{
    /// <summary>Nombres visibles de los días 1..6 (domingo = 0 no tiene clases).</summary>
    private static readonly string[] DayNames = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    private const string NoScheduleText = "Sin horario";

    private readonly ApplicationDbContext _context;

    public GroupScheduleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GroupScheduleView>> GetSubjectScheduleAsync(int subjectId, int managementId, CancellationToken ct = default)
    {
        var groups = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.SubjectId == subjectId && g.ManagementId == managementId && g.Status == GeneralStatus.Activo)
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Code, g.Name, g.TeacherPersonId, g.StudentCount })
            .ToListAsync(ct);
        if (groups.Count == 0) return [];

        // Person.FullName es NotMapped: se proyecta el nombre del docente y el texto se arma en memoria.
        var teacherIds = groups.Select(g => g.TeacherPersonId).Distinct().ToList();
        var teacherNames = await _context.Interns.AsNoTracking()
            .Where(i => teacherIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name })
            .ToDictionaryAsync(i => i.Id, i => i.Name, ct);

        var groupIds = groups.Select(g => g.Id).ToList();
        var slotsByGroup = await LoadSlotsByGroupAsync(groupIds, ct);

        return groups.Select(g =>
        {
            var slots = slotsByGroup.TryGetValue(g.Id, out var s) ? s : Array.Empty<WeekSlot>();
            return new GroupScheduleView(
                g.Id,
                g.Code,
                g.Name,
                teacherNames.TryGetValue(g.TeacherPersonId, out var teacher) ? teacher : $"Docente #{g.TeacherPersonId}",
                g.StudentCount,
                slots,
                Summarize(slots));
        }).ToList();
    }

    public async Task<string?> SaveSubjectScheduleAsync(int subjectId, int managementId,
        IReadOnlyDictionary<int, IReadOnlyCollection<WeekSlot>> slotsByGroup, CancellationToken ct = default)
    {
        var subjectExists = await _context.Subjects.AsNoTracking().AnyAsync(s => s.Id == subjectId, ct);
        if (!subjectExists)
            return "La materia indicada no existe en el catálogo.";

        var groupIds = slotsByGroup.Keys.ToList();
        var groups = await _context.CourseGroups.AsNoTracking()
            .Where(g => groupIds.Contains(g.Id) && g.Status == GeneralStatus.Activo)
            .Select(g => new { g.Id, g.Name, g.SubjectId, g.ManagementId })
            .ToListAsync(ct);
        var foreign = groups.FirstOrDefault(g => g.SubjectId != subjectId || g.ManagementId != managementId);
        if (foreign != null)
            return $"El grupo {foreign.Name} no pertenece a la materia seleccionada en esta gestión.";
        if (groups.Count < groupIds.Count)
            return "Uno de los grupos indicados ya no está activo o fue eliminado: recargue la página.";

        // Catálogo de periodos: orden del periodo → Id.
        var periodIdByOrder = await _context.ClassPeriods.AsNoTracking()
            .Select(p => new { p.Id, p.Order })
            .ToDictionaryAsync(p => p.Order, p => p.Id, ct);

        // Celdas válidas sin duplicados por grupo: se ignoran días fuera de 1..6 y periodos inexistentes.
        var desired = new Dictionary<int, HashSet<(int Weekday, int ClassPeriodId)>>();
        foreach (var (groupId, cells) in slotsByGroup)
        {
            var set = new HashSet<(int, int)>();
            foreach (var cell in cells)
            {
                if (cell.Weekday is < 1 or > 6) continue;
                if (!periodIdByOrder.TryGetValue(cell.PeriodOrder, out var periodId)) continue;
                set.Add((cell.Weekday, periodId));
            }
            desired[groupId] = set;
        }

        // El servidor también impide que el grupo quede en clase de dos materias a la vez (no solo el JS):
        // cubre pestañas desactualizadas y envíos manuales.
        foreach (var group in groups)
        {
            var cells = desired[group.Id];
            if (cells.Count == 0) continue;
            var siblings = await GetSiblingClassesAsync(group.Id, ct);
            var clash = siblings.FirstOrDefault(s =>
                cells.Contains((s.Weekday, periodIdByOrder.GetValueOrDefault(s.PeriodOrder))));
            if (clash != null)
                return $"El grupo {group.Name} ya tiene clase de {clash.SubjectName} el {DayNames[clash.Weekday - 1].ToLowerInvariant()} " +
                    $"en el periodo {clash.PeriodOrder}: quite esa celda o corrija el horario de la otra materia.";
        }

        // Reemplazo del conjunto: los slots son configuración del catálogo (RemoveRange aprobado).
        await ReplaceSlotsAsync(desired, ct);
        return null;
    }

    public async Task<IReadOnlyList<SiblingClass>> GetSiblingClassesAsync(int courseGroupId, CancellationToken ct = default)
    {
        var group = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.Id == courseGroupId)
            .Select(g => new { g.Name, g.SubjectId, g.ManagementId, CareerId = g.Subject!.CareerId, Semester = g.Subject!.Semester })
            .FirstOrDefaultAsync(ct);
        if (group == null || group.CareerId is not int careerId || group.Semester is not int semester)
            return [];

        // Regla "mismos estudiantes": otra materia activa de la misma gestión, carrera y semestre,
        // con el mismo nombre de grupo (sin mayúsculas ni espacios extremos).
        var candidates = await _context.CourseGroupScheduleSlots.AsNoTracking()
            .Where(s => s.CourseGroupId != courseGroupId
                && s.CourseGroup!.ManagementId == group.ManagementId
                && s.CourseGroup!.Status == GeneralStatus.Activo
                && s.CourseGroup!.SubjectId != group.SubjectId
                && s.CourseGroup!.Subject!.CareerId == careerId
                && s.CourseGroup!.Subject!.Semester == semester)
            .Select(s => new
            {
                CourseGroupId = s.CourseGroupId,
                GroupName = s.CourseGroup!.Name,
                SubjectName = s.CourseGroup.Subject!.Name,
                s.Weekday,
                PeriodOrder = s.ClassPeriod!.Order
            })
            .ToListAsync(ct);

        return candidates
            .Where(s => SameGroupName(s.GroupName, group.Name))
            .OrderBy(s => s.SubjectName).ThenBy(s => s.GroupName)
            .ThenBy(s => s.Weekday).ThenBy(s => s.PeriodOrder)
            .Select(s => new SiblingClass(s.CourseGroupId, s.SubjectName, s.GroupName, s.Weekday, s.PeriodOrder))
            .ToList();
    }

    public async Task<IReadOnlyList<WeekSlot>> GetGroupSlotsAsync(int courseGroupId, CancellationToken ct = default)
    {
        var slots = await _context.CourseGroupScheduleSlots.AsNoTracking()
            .Where(s => s.CourseGroupId == courseGroupId)
            .Select(s => new { s.Weekday, PeriodOrder = s.ClassPeriod!.Order })
            .ToListAsync(ct);
        return slots
            .OrderBy(s => s.Weekday).ThenBy(s => s.PeriodOrder)
            .Select(s => new WeekSlot(s.Weekday, s.PeriodOrder))
            .ToList();
    }

    public async Task<GroupDestinationCheck> CheckDestinationAsync(int courseGroupId, DateTime date, int startOrder, int periodCount,
        CancellationToken ct = default)
    {
        var weekday = PlanningSlot.WeekdayOf(date);
        var hasSchedule = await _context.CourseGroupScheduleSlots.AsNoTracking()
            .AnyAsync(s => s.CourseGroupId == courseGroupId, ct);

        // Domingo = 0: no hay clases ni choques.
        if (weekday == 0)
            return new GroupDestinationCheck(hasSchedule, [], []);

        var lastOrder = PlanningSlot.LastOrder(startOrder, periodCount);

        var ownOrders = await _context.CourseGroupScheduleSlots.AsNoTracking()
            .Where(s => s.CourseGroupId == courseGroupId && s.Weekday == weekday
                && s.ClassPeriod!.Order >= startOrder && s.ClassPeriod!.Order <= lastOrder)
            .Select(s => s.ClassPeriod!.Order)
            .Distinct()
            .ToListAsync(ct);
        ownOrders.Sort();

        var siblings = await GetSiblingClassesAsync(courseGroupId, ct);
        var collisions = siblings
            .Where(s => s.Weekday == weekday && s.PeriodOrder >= startOrder && s.PeriodOrder <= lastOrder)
            .ToList();

        return new GroupDestinationCheck(hasSchedule, collisions, ownOrders);
    }

    public string Summarize(IEnumerable<WeekSlot> slots)
    {
        if (slots is null) return NoScheduleText;

        var parts = new List<string>();
        foreach (var day in slots.Where(s => s.Weekday >= 1 && s.Weekday <= DayNames.Length).GroupBy(s => s.Weekday).OrderBy(g => g.Key))
        {
            var orders = day.Select(s => s.PeriodOrder).Where(o => o >= 1).ToList();
            foreach (var (start, count) in MergeRuns(orders))
                parts.Add($"{DayNames[day.Key - 1]} {PeriodsTextLower(start, count)}");
        }

        return parts.Count == 0 ? NoScheduleText : string.Join(" · ", parts);
    }

    public async Task<IReadOnlyList<CohortOption>> ListCohortsAsync(int managementId, int? careerId = null, CancellationToken ct = default)
    {
        // Person.FullName es NotMapped y el nombre de grupo se normaliza en memoria (Trim + mayúsculas).
        var rows = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.ManagementId == managementId
                && g.Status == GeneralStatus.Activo
                && g.Subject!.CareerId != null
                && g.Subject!.Semester != null)
            .Select(g => new
            {
                CareerId = g.Subject!.CareerId!.Value,
                CareerName = g.Subject.Career != null ? g.Subject.Career.Name : "",
                Semester = g.Subject!.Semester!.Value,
                g.SubjectId,
                GroupName = g.Name
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => (r.CareerId, r.Semester, Key: r.GroupName.Trim().ToUpperInvariant()))
            .Select(g =>
            {
                var first = g.First();
                var groupName = first.GroupName.Trim();
                return new CohortOption(
                    first.CareerId,
                    first.CareerName ?? "",
                    first.Semester,
                    groupName,
                    g.Select(x => x.SubjectId).Distinct().Count(),
                    CohortLabel(first.Semester, groupName));
            })
            .OrderBy(o => o.CareerName).ThenBy(o => o.CareerId)
            .ThenBy(o => o.Semester).ThenBy(o => o.GroupName)
            .ToList();
    }

    public async Task<CohortScheduleView> GetCohortScheduleAsync(int managementId, int careerId, int semester, string groupName,
        CancellationToken ct = default)
    {
        var cohort = await LoadCohortGroupsAsync(managementId, careerId, semester, groupName, ct);
        var careerName = await _context.Careers.AsNoTracking()
            .Where(c => c.Id == careerId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct) ?? $"Carrera #{careerId}";

        // Docentes del cohorte (nombre armado en memoria: Person.FullName es NotMapped).
        var teacherIds = cohort.Select(g => g.TeacherPersonId).Distinct().ToList();
        var teacherNames = await _context.Interns.AsNoTracking()
            .Where(i => teacherIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name })
            .ToDictionaryAsync(i => i.Id, i => i.Name, ct);

        var groupIds = cohort.Select(g => g.Id).ToList();
        var slotsByGroup = groupIds.Count == 0
            ? new Dictionary<int, IReadOnlyList<WeekSlot>>()
            : await LoadSlotsByGroupAsync(groupIds, ct);

        var subjects = new List<CohortSubjectView>();
        for (var i = 0; i < cohort.Count; i++)
        {
            var g = cohort[i];
            var slots = slotsByGroup.TryGetValue(g.Id, out var s) ? s : Array.Empty<WeekSlot>();
            subjects.Add(new CohortSubjectView(
                g.Id,
                g.SubjectId,
                g.SubjectName,
                string.IsNullOrWhiteSpace(g.SubjectCode) ? null : g.SubjectCode,
                g.Name.Trim(),
                teacherNames.TryGetValue(g.TeacherPersonId, out var teacher) ? teacher : $"Docente #{g.TeacherPersonId}",
                slots,
                Summarize(slots),
                i));
        }

        // Choque de docente (informativo): el mismo docente ya da clase en otro grupo activo de la
        // gestión, fuera del cohorte, en esas celdas. Se reporta por cada materia del cohorte afectada.
        var teacherBusy = new List<CohortBusyCell>();
        if (groupIds.Count > 0)
        {
            var busyRows = await _context.CourseGroupScheduleSlots.AsNoTracking()
                .Where(s => s.CourseGroup!.ManagementId == managementId
                    && s.CourseGroup!.Status == GeneralStatus.Activo
                    && !groupIds.Contains(s.CourseGroupId)
                    && teacherIds.Contains(s.CourseGroup!.TeacherPersonId))
                .Select(s => new
                {
                    s.CourseGroupId,
                    TeacherPersonId = s.CourseGroup!.TeacherPersonId,
                    s.Weekday,
                    PeriodOrder = s.ClassPeriod!.Order,
                    OtherSubjectName = s.CourseGroup!.Subject!.Name
                })
                .ToListAsync(ct);
            teacherBusy = busyRows
                .SelectMany(s => cohort
                    .Where(g => g.TeacherPersonId == s.TeacherPersonId)
                    .Select(g => new CohortBusyCell(g.Id, s.Weekday, s.PeriodOrder, s.OtherSubjectName)))
                .OrderBy(s => s.CourseGroupId).ThenBy(s => s.Weekday).ThenBy(s => s.PeriodOrder)
                .ToList();
        }

        return new CohortScheduleView(
            careerId,
            careerName,
            semester,
            cohort.Count > 0 ? cohort[0].Name.Trim() : groupName.Trim(),
            CohortLabel(semester, groupName),
            subjects,
            teacherBusy);
    }

    public async Task<GroupFormContext> GetGroupFormContextAsync(int managementId, int subjectId, string? groupName,
        int? teacherPersonId, int? excludeGroupId, CancellationToken ct = default)
    {
        // Clases del docente en otros grupos activos de la gestión (un docente no dicta dos clases a la vez).
        var teacherBusy = new List<TeacherBusySlot>();
        if (teacherPersonId is int teacherId && teacherId > 0)
        {
            teacherBusy = (await _context.CourseGroupScheduleSlots.AsNoTracking()
                    .Where(sl => sl.CourseGroup!.ManagementId == managementId
                        && sl.CourseGroup!.Status == GeneralStatus.Activo
                        && sl.CourseGroup!.TeacherPersonId == teacherId
                        && (excludeGroupId == null || sl.CourseGroupId != excludeGroupId))
                    .Select(sl => new
                    {
                        sl.Weekday,
                        PeriodOrder = sl.ClassPeriod!.Order,
                        SubjectName = sl.CourseGroup!.Subject!.Name,
                        GroupName = sl.CourseGroup!.Name
                    })
                    .ToListAsync(ct))
                .OrderBy(b => b.Weekday).ThenBy(b => b.PeriodOrder)
                .Select(b => new TeacherBusySlot(b.Weekday, b.PeriodOrder, b.SubjectName, b.GroupName.Trim()))
                .ToList();
        }

        var subject = await _context.Subjects.AsNoTracking()
            .Where(x => x.Id == subjectId)
            .Select(x => new { x.CareerId, x.Semester })
            .FirstOrDefaultAsync(ct);
        if (subject?.CareerId is not int careerId || subject.Semester is not int semester || string.IsNullOrWhiteSpace(groupName))
        {
            return new GroupFormContext(false, subject?.CareerId, null, subject?.Semester, groupName?.Trim(), null,
                [], [], teacherBusy);
        }

        var cohort = await GetCohortScheduleAsync(managementId, careerId, semester, groupName, ct);
        var siblings = cohort.Subjects
            .Where(x => x.SubjectId != subjectId && (excludeGroupId == null || x.CourseGroupId != excludeGroupId))
            .ToList();

        // Materias activas del mismo semestre y carrera que aún no tienen este grupo.
        var withGroup = cohort.Subjects.Select(x => x.SubjectId).ToHashSet();
        withGroup.Add(subjectId);
        var pending = (await _context.Subjects.AsNoTracking()
                .Where(x => x.Status == GeneralStatus.Activo && x.CareerId == careerId && x.Semester == semester)
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct))
            .Where(x => !withGroup.Contains(x.Id))
            .Select(x => new CohortPendingSubject(x.Id, x.Name))
            .ToList();

        return new GroupFormContext(true, careerId, cohort.CareerName, semester, groupName.Trim(), cohort.Label,
            siblings, pending, teacherBusy);
    }

    public async Task<string?> SaveCohortScheduleAsync(int managementId, int careerId, int semester, string groupName,
        IReadOnlyDictionary<int, IReadOnlyCollection<WeekSlot>> slotsByGroup, CancellationToken ct = default)
    {
        var cohort = await LoadCohortGroupsAsync(managementId, careerId, semester, groupName, ct);
        var cohortIds = cohort.Select(g => g.Id).ToHashSet();

        // Todos los grupos enviados deben pertenecer al cohorte (materia, carrera, semestre y grupo homónimo).
        var groupIds = slotsByGroup.Keys.ToList();
        var sent = await _context.CourseGroups.AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(ct);
        var foreign = sent.FirstOrDefault(g => !cohortIds.Contains(g.Id));
        if (foreign != null)
            return $"El grupo {foreign.Name} no pertenece al cohorte {CohortLabel(semester, groupName)} en esta gestión.";
        if (sent.Count < groupIds.Count)
            return "Uno de los grupos indicados ya no está activo o fue eliminado: recargue la página.";

        // Catálogo de periodos: orden del periodo → Id (y el inverso para los mensajes).
        var periodIdByOrder = await _context.ClassPeriods.AsNoTracking()
            .Select(p => new { p.Id, p.Order })
            .ToDictionaryAsync(p => p.Order, p => p.Id, ct);
        var periodOrderById = periodIdByOrder.ToDictionary(p => p.Value, p => p.Key);

        // Celdas válidas sin duplicados por grupo: se ignoran días fuera de 1..6 y periodos inexistentes.
        var desired = new Dictionary<int, HashSet<(int Weekday, int ClassPeriodId)>>();
        foreach (var (groupId, cells) in slotsByGroup)
        {
            var set = new HashSet<(int, int)>();
            foreach (var cell in cells)
            {
                if (cell.Weekday is < 1 or > 6) continue;
                if (!periodIdByOrder.TryGetValue(cell.PeriodOrder, out var periodId)) continue;
                set.Add((cell.Weekday, periodId));
            }
            desired[groupId] = set;
        }

        // El cohorte no puede tener dos materias en la misma celda (día + periodo).
        // Los grupos del cohorte que no vienen en el envío conservan su horario guardado y también cuentan.
        var keptIds = cohort.Where(g => !desired.ContainsKey(g.Id)).Select(g => g.Id).ToList();
        var cellsByGroup = desired.ToDictionary(d => d.Key, d => d.Value);
        if (keptIds.Count > 0)
        {
            var kept = await _context.CourseGroupScheduleSlots.AsNoTracking()
                .Where(s => keptIds.Contains(s.CourseGroupId))
                .Select(s => new { s.CourseGroupId, s.Weekday, s.ClassPeriodId })
                .ToListAsync(ct);
            foreach (var slot in kept)
            {
                if (!cellsByGroup.TryGetValue(slot.CourseGroupId, out var set))
                    cellsByGroup[slot.CourseGroupId] = set = [];
                set.Add((slot.Weekday, slot.ClassPeriodId));
            }
        }

        var byCell = new Dictionary<(int Weekday, int ClassPeriodId), (int GroupId, string SubjectName)>();
        foreach (var group in cohort.Where(g => cellsByGroup.ContainsKey(g.Id)))
        {
            var subjectName = group.SubjectName;
            foreach (var cell in cellsByGroup[group.Id])
            {
                if (!byCell.TryGetValue(cell, out var occupant))
                {
                    byCell[cell] = (group.Id, subjectName);
                    continue;
                }
                return $"El {DayNames[cell.Weekday - 1]} en el periodo {periodOrderById[cell.ClassPeriodId]} " +
                    $"hay dos materias: {occupant.SubjectName} y {subjectName}.";
            }
        }

        // Choque del docente consigo mismo en otro grupo activo de la gestión, fuera del cohorte.
        foreach (var group in cohort.Where(g => desired.ContainsKey(g.Id)))
        {
            var cells = desired[group.Id];
            if (cells.Count == 0) continue;
            var busy = await _context.CourseGroupScheduleSlots.AsNoTracking()
                .Where(s => s.CourseGroup!.ManagementId == managementId
                    && s.CourseGroup!.Status == GeneralStatus.Activo
                    && s.CourseGroupId != group.Id
                    && s.CourseGroup!.TeacherPersonId == group.TeacherPersonId
                    && !cohortIds.Contains(s.CourseGroupId))
                .Select(s => new
                {
                    s.Weekday,
                    PeriodOrder = s.ClassPeriod!.Order,
                    OtherSubjectName = s.CourseGroup!.Subject!.Name,
                    OtherGroupName = s.CourseGroup!.Name
                })
                .ToListAsync(ct);
            var clash = busy.FirstOrDefault(s => cells.Contains((s.Weekday, periodIdByOrder.GetValueOrDefault(s.PeriodOrder))));
            if (clash != null)
                return $"El docente del grupo {group.Name} ({group.SubjectName}) ya tiene clase de {clash.OtherSubjectName} " +
                    $"(grupo {clash.OtherGroupName.Trim()}) el {DayNames[clash.Weekday - 1].ToLowerInvariant()} en el periodo " +
                    $"{clash.PeriodOrder}: quite esa celda o corrija el horario de la otra materia.";
        }

        // Reemplazo del conjunto: los slots son configuración del catálogo (RemoveRange aprobado).
        await ReplaceSlotsAsync(desired, ct);
        return null;
    }

    /// <summary>Etiqueta visible del cohorte: "Sem N · Grupo X" (o "Grupo X" si la materia no tiene semestre).</summary>
    public static string CohortLabel(int? semester, string groupName)
        => semester is int s ? $"Sem {s} · Grupo {groupName.Trim()}" : $"Grupo {groupName.Trim()}";

    /// <summary>Slots de varios grupos en una consulta, ordenados por día y orden del periodo.</summary>
    private async Task<Dictionary<int, IReadOnlyList<WeekSlot>>> LoadSlotsByGroupAsync(IReadOnlyCollection<int> groupIds, CancellationToken ct)
    {
        var slots = await _context.CourseGroupScheduleSlots.AsNoTracking()
            .Where(s => groupIds.Contains(s.CourseGroupId))
            .Select(s => new { s.CourseGroupId, s.Weekday, PeriodOrder = s.ClassPeriod!.Order })
            .ToListAsync(ct);
        return slots
            .GroupBy(s => s.CourseGroupId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<WeekSlot>)g
                    .OrderBy(s => s.Weekday).ThenBy(s => s.PeriodOrder)
                    .Select(s => new WeekSlot(s.Weekday, s.PeriodOrder))
                    .ToList());
    }

    /// <summary>
    /// Grupos activos del cohorte (gestión + carrera + semestre + nombre de grupo homónimo), con los datos
    /// de su materia; ordenados por materia y nombre de grupo. El nombre se compara sin mayúsculas ni espacios.
    /// </summary>
    private async Task<List<(int Id, int SubjectId, string SubjectName, string SubjectCode, string Name, int TeacherPersonId)>>
        LoadCohortGroupsAsync(int managementId, int careerId, int semester, string groupName, CancellationToken ct)
    {
        var rows = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.ManagementId == managementId
                && g.Status == GeneralStatus.Activo
                && g.Subject!.CareerId == careerId
                && g.Subject!.Semester == semester)
            .Select(g => new
            {
                g.Id,
                g.SubjectId,
                SubjectName = g.Subject!.Name,
                SubjectCode = g.Subject.Code,
                g.Name,
                g.TeacherPersonId
            })
            .ToListAsync(ct);

        return rows
            .Where(g => SameGroupName(g.Name, groupName))
            .OrderBy(g => g.SubjectName).ThenBy(g => g.Name)
            .Select(g => (g.Id, g.SubjectId, g.SubjectName, g.SubjectCode, g.Name, g.TeacherPersonId))
            .ToList();
    }

    /// <summary>Reemplazo del conjunto de celdas de los grupos indicados (RemoveRange aprobado: configuración).</summary>
    private async Task ReplaceSlotsAsync(IReadOnlyDictionary<int, HashSet<(int Weekday, int ClassPeriodId)>> desired, CancellationToken ct)
    {
        var groupIds = desired.Keys.ToList();
        var existing = await _context.CourseGroupScheduleSlots.AsTracking()
            .Where(s => groupIds.Contains(s.CourseGroupId))
            .ToListAsync(ct);
        var obsolete = existing.Where(s => desired.TryGetValue(s.CourseGroupId, out var set)
            && !set.Contains((s.Weekday, s.ClassPeriodId))).ToList();
        if (obsolete.Count > 0)
            _context.CourseGroupScheduleSlots.RemoveRange(obsolete);

        var current = existing.Select(s => (s.CourseGroupId, s.Weekday, s.ClassPeriodId)).ToHashSet();
        foreach (var (groupId, cells) in desired)
        {
            foreach (var (weekday, periodId) in cells)
            {
                if (current.Contains((groupId, weekday, periodId))) continue;
                _context.CourseGroupScheduleSlots.Add(new CourseGroupScheduleSlot
                {
                    CourseGroupId = groupId,
                    Weekday = weekday,
                    ClassPeriodId = periodId
                });
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>Compara nombres de grupo sin distinguir mayúsculas ni espacios extremos.</summary>
    private static bool SameGroupName(string? a, string? b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>"Periodos 1–3" → "periodos 1–3" (minúscula inicial para el resumen).</summary>
    private static string PeriodsTextLower(int startOrder, int count)
    {
        var text = PlanningSlot.PeriodsText(startOrder, count);
        return char.ToLowerInvariant(text[0]) + text[1..];
    }

    /// <summary>Une órdenes consecutivos en tramos [inicio, cantidad], ordenados y sin duplicados.</summary>
    private static List<(int Start, int Count)> MergeRuns(IEnumerable<int> orders)
    {
        var runs = new List<(int Start, int Count)>();
        var previous = int.MinValue;
        foreach (var order in orders.Distinct().OrderBy(o => o))
        {
            if (runs.Count > 0 && previous == order - 1)
                runs[^1] = (runs[^1].Start, runs[^1].Count + 1);
            else
                runs.Add((order, 1));
            previous = order;
        }
        return runs;
    }
}
