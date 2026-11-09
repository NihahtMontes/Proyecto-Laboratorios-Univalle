using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services.Notifications;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;

namespace Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

/// <summary>
/// Solicitudes de práctica de docentes (Sprint 3). Una solicitud es una <see cref="Planning"/> en estado
/// Solicitada/Observada que NO ocupa ambiente ni material (solo avisa); al aprobarse pasa a Aprobada y entra
/// al flujo existente (Cierre: L-3/L-5). Todo cambio de estado queda en <see cref="PlanningReview"/>.
/// </summary>
public interface ITeacherRequestService
{
    Task<TeacherProfile?> GetProfileAsync(int userId);
    Task<RequestWindow> GetWindowAsync(DateTime today);
    Task<SlotCheckResult> CheckSlotAsync(int teacherPersonId, RequestInput input, int? excludePlanningId, bool forApproval = false);
    Task<IReadOnlyList<MaterialLineCheck>> CheckMaterialAsync(DateTime date, int startPeriodId, int periodCount,
        IReadOnlyList<RequestLineInput> lines, int? excludePlanningId);
    Task<IReadOnlyList<SlotSuggestion>> SuggestAsync(int teacherPersonId, int courseGroupId, int periodCount, int? preferredLabId,
        IReadOnlyList<RequestLineInput> lines, int max = 3);
    Task<TeacherOpResult> SubmitAsync(TeacherProfile teacher, RequestInput input, int? planningId, string? note);
    Task<TeacherOpResult> WithdrawAsync(TeacherProfile teacher, int planningId, string? reason);
    Task<TeacherOpResult> ApproveAsync(int reviewerUserId, int planningId, string? comment, IReadOnlyList<RequestLineInput>? adjustedLines);
    Task<TeacherOpResult> ObserveAsync(int reviewerUserId, int planningId, string comment);
    Task<TeacherOpResult> RejectAsync(int reviewerUserId, int planningId, string reason);
    Task<IReadOnlyList<RequestLineView>> GetLinesAsync(int planningId);
    Task<IReadOnlyList<ReviewEventView>> GetHistoryAsync(int planningId);
    /// <summary>Otras solicitudes pendientes que piden el mismo ambiente en un horario que se cruza.</summary>
    Task<IReadOnlyList<Planning>> FindCompetingRequestsAsync(int planningId);
}

public sealed class TeacherRequestService : ITeacherRequestService
{
    /// <summary>Tipos de ambiente que no se solicitan para prácticas.</summary>
    public static readonly string[] NonRequestableTypes = ["Almacen", "Almacén", "Oficina", "Sala", "Vestidor", "Area común", "Área común"];

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IOccupancyService _occupancy;
    private readonly IGroupScheduleService _groups;
    private readonly IArticleAvailabilityService _availability;
    private readonly IPlanningLock _lock;
    private readonly TeacherRequestOptions _options;
    private readonly ILogger<TeacherRequestService> _logger;

    public TeacherRequestService(ApplicationDbContext context, IAcademicManagementService managements, IOccupancyService occupancy,
        IGroupScheduleService groups, IArticleAvailabilityService availability, IPlanningLock planningLock,
        IOptions<TeacherRequestOptions> options, ILogger<TeacherRequestService> logger)
    {
        _context = context;
        _managements = managements;
        _occupancy = occupancy;
        _groups = groups;
        _availability = availability;
        _lock = planningLock;
        _options = options.Value;
        _logger = logger;
    }

    // ───────────────────────────── Perfil y ventana ─────────────────────────────

    public async Task<TeacherProfile?> GetProfileAsync(int userId)
    {
        var row = await _context.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.Role == UserRole.Docente && u.PersonId != null)
            .Select(u => new { u.Id, PersonId = u.PersonId!.Value, u.FirstName, u.LastName })
            .FirstOrDefaultAsync();
        if (row == null) return null;
        var name = await PersonNameAsync(row.PersonId) ?? $"{row.FirstName} {row.LastName}".Trim();
        return new TeacherProfile(row.Id, row.PersonId, name);
    }

    public async Task<RequestWindow> GetWindowAsync(DateTime today)
    {
        var minDate = AddBusinessDays(today.Date, Math.Max(0, _options.MinLeadBusinessDays));
        var management = await _managements.GetCurrentAsync();
        if (management == null || management.Status != ManagementStatus.Active)
            return new RequestWindow(null, null, minDate, minDate, "No hay una gestión académica activa: por ahora no se reciben solicitudes.");

        var lastClassDay = await _context.AcademicDays.AsNoTracking()
            .Where(d => d.AcademicWeek.ManagementId == management.Id && d.Status == AcademicDayStatus.Enabled)
            .MaxAsync(d => (DateTime?)d.Date);
        var managementEnd = lastClassDay ?? management.CalendarEnd;
        if (managementEnd == null)
            return new RequestWindow(management.Id, management.Code, minDate, minDate,
                $"La gestión {management.Code} todavía no tiene calendario académico.");

        var maxDate = today.Date.AddDays(Math.Max(1, _options.MaxLeadDays));
        if (managementEnd.Value.Date < maxDate) maxDate = managementEnd.Value.Date;
        if (minDate > maxDate)
            return new RequestWindow(management.Id, management.Code, minDate, maxDate,
                $"La gestión {management.Code} termina el {managementEnd:dd/MM/yyyy}: ya no hay fechas con la anticipación mínima de {_options.MinLeadBusinessDays} días hábiles.");
        return new RequestWindow(management.Id, management.Code, minDate, maxDate, null);
    }

    /// <summary>Suma días hábiles (lunes a viernes) a una fecha.</summary>
    public static DateTime AddBusinessDays(DateTime date, int days)
    {
        var result = date.Date;
        while (days > 0)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) days--;
        }
        return result;
    }

    // ───────────────────────────── Validación del horario ─────────────────────────────

    public async Task<SlotCheckResult> CheckSlotAsync(int teacherPersonId, RequestInput input, int? excludePlanningId, bool forApproval = false)
    {
        var issues = new List<CheckIssue>();
        var date = input.Date.Date;
        var exclude = excludePlanningId is int id ? new[] { id } : null;

        if (!forApproval)
        {
            var window = await GetWindowAsync(DateTime.Today);
            if (!window.IsOpen)
                issues.Add(Block("WINDOW_CLOSED", "Solicitudes cerradas", window.ClosedReason!));
            else if (!window.Contains(date))
                issues.Add(Block("OUT_OF_WINDOW", "Fecha fuera de plazo",
                    $"Solo puedes solicitar {window.RangeText} (anticipación mínima de {_options.MinLeadBusinessDays} días hábiles y hasta {_options.MaxLeadDays} días)."));
        }

        var periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        var start = periods.FirstOrDefault(p => p.Id == input.StartPeriodId);
        if (start == null)
            return new SlotCheckResult([.. issues, Block("PERIOD", "Horario incompleto", "Elige el periodo de inicio.")]);
        if (!PlanningSlot.Fits(start.Order, input.PeriodCount, periods.Count))
            return new SlotCheckResult([.. issues, Block("PERIOD", "No cabe en la jornada",
                $"Desde el periodo {start.Order} solo caben {periods.Count - start.Order + 1} periodo(s).")]);
        if (PlanningSlot.WeekdayOf(date) == 0)
            return new SlotCheckResult([.. issues, Block("SUNDAY", "Domingo", "Los domingos no hay actividades académicas.")]);

        var group = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.Id == input.CourseGroupId)
            .Select(g => new { g.Id, g.ManagementId, g.Name, g.StudentCount, Subject = g.Subject!.Name })
            .FirstOrDefaultAsync();
        if (group == null)
            return new SlotCheckResult([.. issues, Block("GROUP", "Grupo", "Selecciona uno de tus grupos.")]);

        var day = await _context.AcademicDays.AsNoTracking()
            .Where(d => d.Date == date && d.AcademicWeek.ManagementId == group.ManagementId)
            .Select(d => new { d.Status, d.Reason })
            .FirstOrDefaultAsync();
        if (day == null)
            issues.Add(Block("DAY", "Día sin clases", $"El {date:dd/MM/yyyy} no forma parte del calendario académico."));
        else if (day.Status != AcademicDayStatus.Enabled)
            issues.Add(Block("DAY", "Día suspendido",
                $"El {date:dd/MM/yyyy} está suspendido{(string.IsNullOrWhiteSpace(day.Reason) ? "." : $": {day.Reason.Trim()}")}"));

        var lab = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Id == input.LaboratoryId)
            .Select(l => new { l.Id, l.Code, l.Name, l.Status, l.Type, l.Capacity })
            .FirstOrDefaultAsync();
        if (lab == null || lab.Status != GeneralStatus.Activo || (lab.Type != null && NonRequestableTypes.Contains(lab.Type)))
            issues.Add(Block("LAB", "Ambiente", "Elige un laboratorio activo para prácticas."));
        else
        {
            var conflicts = await _occupancy.FindConflictsAsync(lab.Id, date, start.Order, input.PeriodCount, exclude);
            if (conflicts.Count > 0)
                issues.Add(Block("LAB_OCCUPIED", $"{lab.Code} ocupado",
                    $"El laboratorio ya tiene práctica aprobada ({string.Join("; ", conflicts.Select(c => $"{c.PeriodsText}"))}) y entre prácticas debe quedar un periodo libre para limpieza."));

            var students = input.StudentCount ?? group.StudentCount;
            if (PlanningAssignmentRules.CapacityError(students, lab.Capacity, lab.Code) is string capacityError)
                issues.Add(Block("CAPACITY", "Capacidad", capacityError));

            // Regla: la solicitud pendiente solo avisa (no bloquea a otros docentes).
            var pending = await PendingOverlapsAsync(lab.Id, date, start.Order, input.PeriodCount, excludePlanningId);
            if (pending.Count > 0)
                issues.Add(Warn("PENDING_COMPETITION", "Otra solicitud pendiente",
                    $"Hay {pending.Count} solicitud(es) en revisión para {lab.Code} en un horario cercano. Se aprobará la que el revisor decida; si aprueban la otra, te avisaremos."));
        }

        var teacherConflicts = await _occupancy.FindTeacherConflictsAsync(teacherPersonId, date, start.Order, input.PeriodCount, exclude);
        if (teacherConflicts.Count > 0)
            issues.Add(Block("TEACHER_BUSY", "Ya tienes una práctica",
                $"El docente ya tiene otra práctica aprobada en ese horario: {string.Join("; ", teacherConflicts.Select(c => c.Label))}."));

        var groupCheck = await _groups.CheckDestinationAsync(group.Id, date, start.Order, input.PeriodCount);
        if (groupCheck.Collisions.Count > 0)
            issues.Add(Block("GROUP_IN_CLASS", "El grupo tiene otra clase",
                $"El grupo {group.Name} tiene clase de {string.Join(", ", groupCheck.Collisions.Select(c => c.SubjectName).Distinct())} en ese horario."));
        else if (groupCheck.OwnClassOrders.Count > 0)
            issues.Add(new CheckIssue("GROUP_OWN_CLASS", CheckSeverity.Info, "Horario de clase del grupo",
                $"Coincide con el horario de clase de {group.Subject} del grupo {group.Name}."));

        return new SlotCheckResult(issues);
    }

    private async Task<List<Planning>> PendingOverlapsAsync(int laboratoryId, DateTime date, int startOrder, int periodCount, int? excludeId)
    {
        var day = date.Date;
        var next = day.AddDays(1);
        var candidates = await _context.Plannings.AsNoTracking()
            .Include(p => p.StartPeriod)
            .Where(p => !p.IsDeleted && PlanningStatus.PendingReview.Contains(p.Status) && p.LaboratoryId == laboratoryId
                && p.ScheduledDate >= day && p.ScheduledDate < next && (excludeId == null || p.Id != excludeId))
            .ToListAsync();
        return candidates
            .Where(p => p.StartPeriod != null && p.PeriodCount is int count
                && PlanningSlot.OverlapsWithGap(p.StartPeriod.Order, count, startOrder, periodCount))
            .ToList();
    }

    public async Task<IReadOnlyList<Planning>> FindCompetingRequestsAsync(int planningId)
    {
        var plan = await _context.Plannings.AsNoTracking().Include(p => p.StartPeriod).FirstOrDefaultAsync(p => p.Id == planningId);
        if (plan?.LaboratoryId is not int labId || plan.StartPeriod == null || plan.PeriodCount is not int count) return [];
        return await PendingOverlapsAsync(labId, plan.ScheduledDate, plan.StartPeriod.Order, count, plan.Id);
    }

    // ───────────────────────────── Material por horario ─────────────────────────────

    public async Task<IReadOnlyList<MaterialLineCheck>> CheckMaterialAsync(DateTime date, int startPeriodId, int periodCount,
        IReadOnlyList<RequestLineInput> lines, int? excludePlanningId)
    {
        var periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        var start = periods.FirstOrDefault(p => p.Id == startPeriodId);
        if (start == null || lines.Count == 0) return [];
        var last = periods.FirstOrDefault(p => p.Order == PlanningSlot.LastOrder(start.Order, periodCount)) ?? start;
        var range = await _availability.GetRangeAsync(date.Date, date.Date);
        return await EvaluateMaterialAsync(range, date.Date, start.StartTime, last.EndTime, lines, excludePlanningId);
    }

    private async Task<IReadOnlyList<MaterialLineCheck>> EvaluateMaterialAsync(WeekAvailability range, DateTime date, TimeSpan from, TimeSpan to,
        IReadOnlyList<RequestLineInput> lines, int? excludePlanningId)
    {
        var merged = lines.Where(l => l.Quantity > 0).GroupBy(l => l.ArticleId)
            .Select(g => new RequestLineInput(g.Key, g.Sum(l => l.Quantity))).ToList();
        if (merged.Count == 0) return [];
        var ids = merged.Select(l => l.ArticleId).ToList();
        var articles = await _context.Articles.AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.Code, a.Name, Category = a.ArticleCategory != null ? a.ArticleCategory.Name : a.Category })
            .ToDictionaryAsync(a => a.Id);
        var stock = await _availability.GetStockAsync(ids, null);
        var practices = range.Practices.ToDictionary(p => p.Id);

        var result = new List<MaterialLineCheck>();
        foreach (var line in merged)
        {
            if (!articles.TryGetValue(line.ArticleId, out var article)) continue;
            var demand = range.Demands.FirstOrDefault(d => d.Date == date && d.ArticleId == line.ArticleId);
            var uses = new List<MaterialUse>();
            decimal usedByOthers = 0;
            if (demand != null)
            {
                foreach (var p in demand.Practices.Where(p => p.PracticeId != excludePlanningId))
                {
                    var wp = practices[p.PracticeId];
                    if (Overlaps(wp.StartTime, wp.EndTime, from, to))
                        uses.Add(new MaterialUse(wp.Id, wp.Code, $"{wp.SubjectName ?? wp.PracticeName} ({wp.GroupName ?? "—"}) · {wp.LaboratoryCode ?? "—"}",
                            wp.StartTime, wp.EndTime, p.Quantity));
                }
                // Pico simultáneo de las otras prácticas dentro de la ventana pedida (tramos de DemandPeak).
                var timed = uses.Select(u => (u.PlanningId, new TimedDemand(u.Quantity, u.Start, u.End))).ToList();
                usedByOthers = DemandPeak.Segments(timed)
                    .Where(s => s.Start < to && from < s.End)
                    .Select(s => s.Load)
                    .DefaultIfEmpty(0)
                    .Max();
            }

            var info = stock.GetValueOrDefault(line.ArticleId);
            decimal? available = info?.Available;
            MaterialState state;
            decimal? free = null;
            string explanation;
            if (available is not decimal total)
            {
                state = MaterialState.NoStock;
                explanation = "Este artículo no tiene stock registrado: el revisor confirmará si hay.";
            }
            else
            {
                free = Math.Max(0, total - usedByOthers);
                if (line.Quantity > total)
                {
                    state = MaterialState.Short;
                    explanation = $"Pides {line.Quantity:0.##} y en toda la institución hay {total:0.##}.";
                }
                else if (line.Quantity > free)
                {
                    state = MaterialState.Short;
                    var releaseAt = uses.Where(u => u.End.HasValue).Select(u => u.End!.Value).DefaultIfEmpty().Max();
                    explanation = $"Stock {total:0.##}: {usedByOthers:0.##} en uso por {string.Join(", ", uses.Take(2).Select(u => $"{u.Code} ({u.TimeText})"))}"
                        + (releaseAt > TimeSpan.Zero ? $". Se libera a las {releaseAt:hh\\:mm}: elige un horario después o pide {free:0.##}." : $". Puedes pedir hasta {free:0.##}.");
                }
                else if (line.Quantity == free && usedByOthers > 0)
                {
                    state = MaterialState.Tight;
                    explanation = $"Alcanza justo: usas las {free:0.##} unidades libres en ese horario.";
                }
                else
                {
                    state = MaterialState.Ok;
                    explanation = usedByOthers > 0
                        ? $"Libres {free:0.##} de {total:0.##} en ese horario ({usedByOthers:0.##} en uso por otras prácticas)."
                        : $"Libres {total:0.##} en ese horario.";
                }
            }
            result.Add(new MaterialLineCheck(article.Id, article.Code, article.Name, article.Category, line.Quantity, available,
                usedByOthers, free, state, uses, explanation));
        }
        return result.OrderBy(r => r.State switch { MaterialState.Short => 0, MaterialState.NoStock => 1, MaterialState.Tight => 2, _ => 3 })
            .ThenBy(r => r.Name).ToList();
    }

    /// <summary>Dos rangos horarios se cruzan; una práctica sin horario ocupa todo el día.</summary>
    private static bool Overlaps(TimeSpan? start, TimeSpan? end, TimeSpan from, TimeSpan to)
    {
        if (start is not TimeSpan s) return true;
        var e = end ?? s;
        return s < to && from < (e > s ? e : s.Add(TimeSpan.FromMinutes(1)));
    }

    // ───────────────────────────── Buscador de horario libre ─────────────────────────────

    public async Task<IReadOnlyList<SlotSuggestion>> SuggestAsync(int teacherPersonId, int courseGroupId, int periodCount, int? preferredLabId,
        IReadOnlyList<RequestLineInput> lines, int max = 3)
    {
        var window = await GetWindowAsync(DateTime.Today);
        if (!window.IsOpen || periodCount < 1) return [];
        var group = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.Id == courseGroupId && g.TeacherPersonId == teacherPersonId)
            .Select(g => new { g.Id, g.SubjectId, g.LaboratoryId, g.ManagementId, g.StudentCount })
            .FirstOrDefaultAsync();
        if (group == null) return [];

        var periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        var labs = await RequestableLaboratoriesQuery()
            .Select(l => new { l.Id, l.Code, l.Name, l.Capacity })
            .ToListAsync();
        if (labs.Count == 0) return [];

        // Ambientes candidatos: el preferido, el del grupo y los que la materia usó antes (por frecuencia).
        var usedBySubject = await _context.Plannings.AsNoTracking()
            .Where(p => p.SubjectId == group.SubjectId && p.LaboratoryId != null && !p.IsDeleted)
            .GroupBy(p => p.LaboratoryId!.Value)
            .Select(g => new { LabId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(3)
            .Select(x => x.LabId)
            .ToListAsync();
        var preferred = new List<int>();
        foreach (var id in new[] { preferredLabId, group.LaboratoryId }.Where(i => i.HasValue).Select(i => i!.Value).Concat(usedBySubject))
            if (!preferred.Contains(id) && labs.Any(l => l.Id == id)) preferred.Add(id);
        var candidateLabs = preferred.Count > 0 ? labs.Where(l => preferred.Contains(l.Id)).OrderBy(l => preferred.IndexOf(l.Id)).ToList() : labs;

        var days = await _context.AcademicDays.AsNoTracking()
            .Where(d => d.AcademicWeek.ManagementId == group.ManagementId && d.Status == AcademicDayStatus.Enabled
                && d.Date >= window.MinDate && d.Date <= window.MaxDate)
            .OrderBy(d => d.Date)
            .Select(d => d.Date)
            .Take(45)
            .ToListAsync();
        if (days.Count == 0) return [];

        var blocks = await _occupancy.GetAsync(new OccupancyQuery(days.First(), days.Last()));
        var ownSlots = await _groups.GetGroupSlotsAsync(group.Id);
        var range = lines.Count > 0 ? await _availability.GetRangeAsync(days.First(), days.Last()) : null;

        var found = new List<(SlotSuggestion Slot, int Score)>();
        foreach (var date in days)
        {
            var weekday = PlanningSlot.WeekdayOf(date);
            var dayBlocks = blocks.Where(b => b.Date == date.Date).ToList();
            foreach (var lab in candidateLabs)
            {
                if (PlanningAssignmentRules.CapacityError(group.StudentCount, lab.Capacity, lab.Code) != null) continue;
                foreach (var start in periods.Where(p => PlanningSlot.Fits(p.Order, periodCount, periods.Count)))
                {
                    if (dayBlocks.Any(b => b.LaboratoryId == lab.Id && PlanningSlot.OverlapsWithGap(b.StartOrder, b.PeriodCount, start.Order, periodCount)))
                        continue;
                    if (dayBlocks.Any(b => b.TeacherPersonId == teacherPersonId && PlanningSlot.Overlaps(b.StartOrder, b.PeriodCount, start.Order, periodCount)))
                        continue;

                    var lastOrder = PlanningSlot.LastOrder(start.Order, periodCount);
                    var end = periods.First(p => p.Order == lastOrder).EndTime;
                    var reasons = new List<string>();
                    var score = 0;
                    var ownHits = Enumerable.Range(start.Order, periodCount).Count(o => ownSlots.Any(s => s.Weekday == weekday && s.PeriodOrder == o));
                    if (ownHits == periodCount) { score += 40; reasons.Add("En el horario de clase de tu grupo"); }
                    else if (ownHits > 0) { score += 15; reasons.Add("Coincide en parte con tu horario de clase"); }
                    var labRank = preferred.IndexOf(lab.Id);
                    if (labRank == 0) { score += 20; reasons.Add("Laboratorio habitual"); }
                    else if (labRank > 0) score += 10;

                    var materialIssues = 0;
                    if (range != null)
                    {
                        var material = await EvaluateMaterialAsync(range, date, start.StartTime, end, lines, null);
                        materialIssues = material.Count(m => m.State == MaterialState.Short);
                        if (materialIssues == 0) { score += 30; reasons.Add("Material completo"); }
                        else reasons.Add($"{materialIssues} artículo(s) no alcanzan");
                    }
                    score -= (date - days.First()).Days; // lo más próximo primero
                    found.Add((new SlotSuggestion(date, start.Id, start.Order, periodCount, lab.Id, lab.Code, lab.Name,
                        start.StartTime, end, materialIssues, reasons), score));
                }
            }
            // Con suficientes candidatos de fechas distintas no hace falta seguir recorriendo el calendario.
            if (found.Select(f => f.Slot.Date).Distinct().Count() >= max * 3) break;
        }

        // Una sugerencia por día (la mejor), verificando el horario de otras materias del grupo solo en las elegidas.
        var result = new List<SlotSuggestion>();
        foreach (var candidate in found.OrderByDescending(f => f.Score).ThenBy(f => f.Slot.Date).ThenBy(f => f.Slot.StartOrder))
        {
            if (result.Any(r => r.Date == candidate.Slot.Date)) continue;
            var check = await _groups.CheckDestinationAsync(group.Id, candidate.Slot.Date, candidate.Slot.StartOrder, periodCount);
            if (check.Collisions.Count > 0) continue;
            result.Add(candidate.Slot);
            if (result.Count >= max) break;
        }
        return result;
    }

    /// <summary>Ambientes activos que se pueden solicitar para prácticas (excluye almacén, oficinas, vestidores…).</summary>
    public IQueryable<Laboratory> RequestableLaboratoriesQuery() =>
        _context.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo && (l.Type == null || !NonRequestableTypes.Contains(l.Type)))
            .OrderBy(l => l.Code);

    // ───────────────────────────── Envío (docente) ─────────────────────────────

    public async Task<TeacherOpResult> SubmitAsync(TeacherProfile teacher, RequestInput input, int? planningId, string? note)
    {
        var window = await GetWindowAsync(DateTime.Today);
        if (!window.IsOpen) return TeacherOpResult.Fail(window.ClosedReason!);

        var group = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.Id == input.CourseGroupId)
            .Select(g => new { g.Id, g.Name, g.SubjectId, g.ManagementId, g.TeacherPersonId, g.Status, g.ScalingFactor })
            .FirstOrDefaultAsync();
        if (group == null || group.TeacherPersonId != teacher.PersonId || group.Status != GeneralStatus.Activo)
            return TeacherOpResult.Fail("Selecciona uno de tus grupos activos.");
        if (group.ManagementId != window.ManagementId)
            return TeacherOpResult.Fail($"El grupo no pertenece a la gestión vigente ({window.ManagementCode}).");

        var practice = await _context.Practices.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == input.PracticeId && p.SubjectId == group.SubjectId && p.Status == GeneralStatus.Activo);
        if (practice == null) return TeacherOpResult.Fail("Selecciona una práctica del catálogo de la materia.");
        if (input.StudentCount is int students && (students < 1 || students > 500))
            return TeacherOpResult.Fail("La cantidad de estudiantes debe estar entre 1 y 500.");

        var lines = input.Lines.Where(l => l.Quantity > 0).GroupBy(l => l.ArticleId)
            .Select(g => new RequestLineInput(g.Key, g.Sum(l => l.Quantity))).ToList();
        if (lines.Count == 0) return TeacherOpResult.Fail("Agrega al menos un artículo con cantidad mayor a 0.");
        if (lines.Any(l => l.Quantity > 9999 || decimal.Round(l.Quantity, 2) != l.Quantity))
            return TeacherOpResult.Fail("Las cantidades deben ser menores a 10 000 y con hasta 2 decimales.");
        var articleIds = lines.Select(l => l.ArticleId).ToList();
        var activeArticles = await _context.Articles.AsNoTracking()
            .Where(a => articleIds.Contains(a.Id) && a.Status == GeneralStatus.Activo)
            .Select(a => new { a.Id, a.UnitOfMeasure })
            .ToDictionaryAsync(a => a.Id);
        if (activeArticles.Count != articleIds.Count) return TeacherOpResult.Fail("Hay artículos que ya no están activos en el catálogo: quítalos.");

        Planning? existing = null;
        if (planningId is int pid)
        {
            existing = await _context.Plannings.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pid);
            if (existing == null || existing.TeacherPersonId != teacher.PersonId)
                return TeacherOpResult.Fail("La solicitud no existe.");
            if (existing.Status != PlanningStatus.Observed)
                return TeacherOpResult.Fail("Solo puedes corregir una solicitud observada.");
        }

        var check = await CheckSlotAsync(teacher.PersonId, input, planningId);
        if (check.HasBlocks) return TeacherOpResult.Fail(string.Join(" ", check.Blocks.Select(b => b.Message)));

        var date = input.Date.Date;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var resources = new List<string>
            {
                PlanningLockResources.ManagementDay(group.ManagementId, date),
                PlanningLockResources.LaboratoryDay(input.LaboratoryId, date),
                PlanningLockResources.TeacherDay(teacher.PersonId, date)
            };
            if (planningId is int lockId) resources.Add(PlanningLockResources.Planning(lockId));
            if (!await _lock.TryAcquireAsync(resources))
                return TeacherOpResult.Fail(PlanningLockResources.BusyMessage + ".");

            // Revalidación bajo bloqueo: el ambiente pudo aprobarse para otra práctica mientras se llenaba el formulario.
            var recheck = await CheckSlotAsync(teacher.PersonId, input, planningId);
            if (recheck.HasBlocks)
            {
                await transaction.RollbackAsync();
                return TeacherOpResult.Fail(string.Join(" ", recheck.Blocks.Select(b => b.Message)));
            }

            // Programación de la práctica (gestión + práctica + grupo, índice único): es el vínculo con el catálogo y se
            // reutiliza aunque el grupo ya tenga esa práctica (un docente puede repetirla las veces que necesite; cada
            // sesión es su propia Planning con fecha, horario y material). Solo se crea si el grupo nunca la tuvo.
            var schedule = await _context.PracticeSchedules.AsTracking()
                .FirstOrDefaultAsync(s => s.ManagementId == group.ManagementId && s.PracticeId == practice.Id && s.GroupName == group.Name);
            var newSchedule = schedule == null;
            schedule ??= new PracticeSchedule
            {
                Code = "TMP-" + Guid.NewGuid().ToString("N")[..20],
                Status = PracticeScheduleStatus.Scheduled,
                ManagementId = group.ManagementId,
                SubjectId = group.SubjectId,
                PracticeId = practice.Id,
                CourseGroupId = group.Id,
                GroupName = group.Name,
                TeacherPersonId = teacher.PersonId,
                ScalingFactor = group.ScalingFactor
            };
            if (input.StudentCount.HasValue) schedule.StudentCount = input.StudentCount;
            if (newSchedule) _context.PracticeSchedules.Add(schedule);

            var day = await _context.AcademicDays.AsNoTracking()
                .Where(d => d.Date == date && d.AcademicWeek.ManagementId == group.ManagementId)
                .Select(d => new { d.AcademicWeekId, d.AcademicWeek.Number })
                .FirstAsync();
            var lab = await _context.Laboratories.AsNoTracking().FirstAsync(l => l.Id == input.LaboratoryId);
            var periods = await _context.ClassPeriods.AsNoTracking().ToListAsync();

            var planning = existing == null
                ? new Planning { RequestDate = DateTime.UtcNow }
                : await _context.Plannings.AsTracking().Include(p => p.Requirements).FirstAsync(p => p.Id == existing.Id);
            var fromStatus = existing?.Status ?? PlanningStatus.Requested;

            planning.ManagementId = group.ManagementId;
            planning.AcademicWeekId = day.AcademicWeekId;
            planning.AcademicWeek = day.Number;
            planning.LaboratoryId = lab.Id;
            planning.CampusId = lab.CampusId;
            planning.AllLaboratories = false;
            planning.TeacherPersonId = teacher.PersonId;
            planning.SubjectId = group.SubjectId;
            planning.CourseGroupId = group.Id;
            planning.GroupName = group.Name;
            planning.PracticeName = PlanificacionModel.PracticeLabel(practice);
            planning.PracticeNumber = practice.Number;
            planning.Observations = Trim(input.Observations, 1000);
            planning.Status = PlanningStatus.Requested;
            planning.RequestDate = DateTime.UtcNow;
            planning.PracticeSchedule = schedule;
            PlanningSlot.Apply(planning, date, input.StartPeriodId, input.PeriodCount, periods);

            // Líneas: se sincronizan con lo enviado (una solicitud no aprobada no tiene salidas ni historial de material).
            var current = planning.Requirements.ToDictionary(r => r.ArticleId);
            foreach (var removed in current.Values.Where(r => !articleIds.Contains(r.ArticleId)).ToList())
                _context.PlanningRequirements.Remove(removed);
            foreach (var line in lines)
            {
                if (current.TryGetValue(line.ArticleId, out var req))
                {
                    req.RequestedQuantity = line.Quantity;
                    req.ExpectedQuantity = (int)Math.Ceiling(line.Quantity);
                }
                else
                {
                    planning.Requirements.Add(new PlanningRequirement
                    {
                        ArticleId = line.ArticleId,
                        RequestedQuantity = line.Quantity,
                        ExpectedQuantity = (int)Math.Ceiling(line.Quantity)
                    });
                }
            }

            if (existing == null) _context.Plannings.Add(planning);
            await _context.SaveChangesAsync();
            if (newSchedule) schedule.Code = $"PRG-{schedule.Id:00000}";
            planning.Code ??= L3Codes.Format(planning.Id);

            _context.PlanningReviews.Add(new PlanningReview
            {
                PlanningId = planning.Id,
                Action = existing == null ? PlanningReviewAction.Submitted : PlanningReviewAction.Resubmitted,
                FromStatus = existing == null ? "Nueva" : fromStatus,
                ToStatus = PlanningStatus.Requested,
                Comment = Trim(note, 1000),
                UserId = teacher.UserId,
                CreatedDate = DateTime.UtcNow
            });
            await NotifyReviewersAsync(planning, teacher.Name, existing != null);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var warnings = check.Warnings.Select(w => w.Message).ToList();
            return new TeacherOpResult(true,
                existing == null
                    ? $"Solicitud {planning.Code} enviada. Te avisaremos cuando sea revisada."
                    : $"Solicitud {planning.Code} corregida y reenviada a revisión.",
                planning.Id, warnings);
        }
        catch (ArgumentException ex)
        {
            await transaction.RollbackAsync();
            return TeacherOpResult.Fail(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo guardar la solicitud docente {PlanningId}", planningId);
            return TeacherOpResult.Fail("No se pudo guardar la solicitud. Intenta nuevamente.");
        }
    }

    public async Task<TeacherOpResult> WithdrawAsync(TeacherProfile teacher, int planningId, string? reason)
    {
        var plan = await _context.Plannings.AsTracking().FirstOrDefaultAsync(p => p.Id == planningId);
        if (plan == null || plan.TeacherPersonId != teacher.PersonId) return TeacherOpResult.Fail("La solicitud no existe.");
        if (!PlanningStatus.PendingReview.Contains(plan.Status))
            return TeacherOpResult.Fail("Solo puedes retirar una solicitud que todavía no fue aprobada ni rechazada.");
        var from = plan.Status;
        plan.Status = PlanningStatus.Cancelled;
        _context.PlanningReviews.Add(new PlanningReview
        {
            PlanningId = plan.Id, Action = PlanningReviewAction.Withdrawn, FromStatus = from, ToStatus = PlanningStatus.Cancelled,
            Comment = Trim(reason, 1000), UserId = teacher.UserId, CreatedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return new TeacherOpResult(true, $"Solicitud {L3Codes.Format(plan.Id, plan.Code)} retirada.", plan.Id);
    }

    // ───────────────────────────── Revisión (Supervisor / Administrador / SuperAdmin) ─────────────────────────────

    public async Task<TeacherOpResult> ApproveAsync(int reviewerUserId, int planningId, string? comment, IReadOnlyList<RequestLineInput>? adjustedLines)
    {
        var snapshot = await _context.Plannings.AsNoTracking().Include(p => p.StartPeriod).FirstOrDefaultAsync(p => p.Id == planningId);
        if (snapshot == null) return TeacherOpResult.Fail("La solicitud no existe.");
        if (snapshot.Status != PlanningStatus.Requested)
            return TeacherOpResult.Fail($"La solicitud está {PlanningStatus.Label(snapshot.Status)}: solo se aprueba una solicitud en revisión.");
        if (!await _managements.IsPlanningWritableAsync(planningId)) return TeacherOpResult.Fail(AcademicManagementService.ReadOnlyMessage);
        if (snapshot.LaboratoryId is not int labId || snapshot.StartPeriodId is not int startPeriodId || snapshot.PeriodCount is not int count
            || snapshot.CourseGroupId is not int groupId)
            return TeacherOpResult.Fail("La solicitud no tiene horario completo.");

        if (adjustedLines != null)
        {
            if (adjustedLines.All(l => l.Quantity <= 0)) return TeacherOpResult.Fail("La práctica debe conservar al menos un artículo.");
            if (adjustedLines.Any(l => l.Quantity < 0 || l.Quantity > 9999 || decimal.Round(l.Quantity, 2) != l.Quantity))
                return TeacherOpResult.Fail("Las cantidades deben estar entre 0 y 9 999, con hasta 2 decimales.");
        }

        var date = snapshot.ScheduledDate.Date;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!await _lock.TryAcquireAsync([
                    PlanningLockResources.Planning(planningId),
                    PlanningLockResources.ManagementDay(snapshot.ManagementId, date),
                    PlanningLockResources.LaboratoryDay(labId, date),
                    PlanningLockResources.TeacherDay(snapshot.TeacherPersonId, date)]))
                return TeacherOpResult.Fail(PlanningLockResources.BusyMessage + ".");

            var input = new RequestInput
            {
                CourseGroupId = groupId, Date = date, StartPeriodId = startPeriodId, PeriodCount = count, LaboratoryId = labId
            };
            var check = await CheckSlotAsync(snapshot.TeacherPersonId, input, planningId, forApproval: true);
            if (check.HasBlocks)
            {
                await transaction.RollbackAsync();
                return TeacherOpResult.Fail("No se puede aprobar: " + string.Join(" ", check.Blocks.Select(b => b.Message))
                    + " Puedes observarla para que el docente elija otro horario.");
            }

            var plan = await _context.Plannings.AsTracking().Include(p => p.Requirements).Include(p => p.PracticeSchedule)
                .FirstAsync(p => p.Id == planningId);
            if (plan.Status != PlanningStatus.Requested)
            {
                await transaction.RollbackAsync();
                return TeacherOpResult.Fail("La solicitud cambió de estado mientras se revisaba. Actualiza la página.");
            }

            var review = new PlanningReview
            {
                PlanningId = plan.Id, FromStatus = plan.Status, ToStatus = PlanningStatus.Approved,
                Comment = Trim(comment, 1000), UserId = reviewerUserId, CreatedDate = DateTime.UtcNow,
                Action = PlanningReviewAction.Approved
            };

            if (adjustedLines != null)
            {
                var wanted = adjustedLines.GroupBy(l => l.ArticleId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
                var newIds = wanted.Where(w => w.Value > 0).Select(w => w.Key).Where(id => plan.Requirements.All(r => r.ArticleId != id)).ToList();
                if (newIds.Count > 0 && await _context.Articles.AsNoTracking().CountAsync(a => newIds.Contains(a.Id) && a.Status == GeneralStatus.Activo) != newIds.Count)
                {
                    await transaction.RollbackAsync();
                    return TeacherOpResult.Fail("Hay artículos agregados que no están activos.");
                }
                foreach (var req in plan.Requirements.ToList())
                {
                    var previous = req.RequestedQuantity ?? 0;
                    var next = wanted.GetValueOrDefault(req.ArticleId, previous);
                    if (next == previous) continue;
                    review.Lines.Add(new PlanningReviewLine { ArticleId = req.ArticleId, PreviousQuantity = previous, NewQuantity = next > 0 ? next : null });
                    if (next > 0)
                    {
                        req.RequestedQuantity = next;
                        req.ExpectedQuantity = (int)Math.Ceiling(next);
                    }
                    else
                    {
                        _context.PlanningRequirements.Remove(req);
                    }
                }
                foreach (var id in newIds)
                {
                    plan.Requirements.Add(new PlanningRequirement { ArticleId = id, RequestedQuantity = wanted[id], ExpectedQuantity = (int)Math.Ceiling(wanted[id]) });
                    review.Lines.Add(new PlanningReviewLine { ArticleId = id, PreviousQuantity = null, NewQuantity = wanted[id] });
                }
                if (review.Lines.Count > 0) review.Action = PlanningReviewAction.ApprovedWithChanges;
            }

            plan.Status = PlanningStatus.Approved;
            // La programación puede ser compartida por varias sesiones del mismo grupo y práctica: solo refleja la fecha
            // de esta sesión si ninguna otra sesión vigente la usa (no se reescribe la fecha de sesiones anteriores).
            var sharedSchedule = plan.PracticeScheduleId is int sid && await _context.Plannings.AsNoTracking()
                .AnyAsync(p => p.PracticeScheduleId == sid && p.Id != plan.Id && !p.IsDeleted && !PlanningStatus.NonOccupying.Contains(p.Status));
            if (plan.PracticeSchedule is { } schedule && !sharedSchedule)
            {
                schedule.LaboratoryId = plan.LaboratoryId;
                schedule.TeacherPersonId = plan.TeacherPersonId;
                schedule.AcademicWeekId = plan.AcademicWeekId;
                schedule.WeekNumber = plan.AcademicWeek is >= 1 and <= 30 ? plan.AcademicWeek : null;
                schedule.ExecutionDate = plan.ScheduledDate.Date;
                schedule.StartTime = plan.StartTime;
                schedule.EndTime = plan.EndTime;
            }
            _context.PlanningReviews.Add(review);

            var competitors = await PendingOverlapsAsync(labId, date, snapshot.StartPeriod!.Order, count, plan.Id);
            await NotifyTeacherAsync(plan, review.Action == PlanningReviewAction.ApprovedWithChanges
                    ? "Solicitud aprobada con cambios" : "Solicitud aprobada",
                $"{L3Codes.Format(plan.Id, plan.Code)} · {plan.ScheduledDate:dd/MM} {plan.StartTime:hh\\:mm}"
                + (review.Lines.Count > 0 ? ": el revisor ajustó cantidades." : ": ya está en tu agenda."),
                "fas fa-check-circle text-success");
            foreach (var other in competitors)
                await NotifyTeacherAsync(other, "Horario aprobado para otra práctica",
                    $"{L3Codes.Format(other.Id, other.Code)}: el ambiente se asignó a otra solicitud en ese horario; el revisor te indicará otro.",
                    "fas fa-exclamation-triangle text-warning");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var warnings = new List<string>();
            if (competitors.Count > 0)
                warnings.Add($"{competitors.Count} solicitud(es) pendiente(s) del mismo ambiente quedaron sin horario: {string.Join(", ", competitors.Select(c => L3Codes.Format(c.Id, c.Code)))}. Obsérvalas o recházalas.");
            return new TeacherOpResult(true, $"Solicitud {L3Codes.Format(plan.Id, plan.Code)} aprobada.", plan.Id, warnings);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo aprobar la solicitud {PlanningId}", planningId);
            return TeacherOpResult.Fail("No se pudo aprobar la solicitud. Intenta nuevamente.");
        }
    }

    public Task<TeacherOpResult> ObserveAsync(int reviewerUserId, int planningId, string comment) =>
        CloseReviewAsync(reviewerUserId, planningId, comment, PlanningStatus.Observed, PlanningReviewAction.Observed,
            "Solicitud observada", "revisa las observaciones y reenvíala.", "fas fa-comment-dots text-warning");

    public Task<TeacherOpResult> RejectAsync(int reviewerUserId, int planningId, string reason) =>
        CloseReviewAsync(reviewerUserId, planningId, reason, PlanningStatus.Rejected, PlanningReviewAction.Rejected,
            "Solicitud rechazada", "revisa el motivo.", "fas fa-times-circle text-danger");

    private async Task<TeacherOpResult> CloseReviewAsync(int reviewerUserId, int planningId, string comment, string toStatus,
        PlanningReviewAction action, string title, string hint, string icon)
    {
        var text = comment?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length < 5)
            return TeacherOpResult.Fail(action == PlanningReviewAction.Rejected
                ? "Escribe el motivo del rechazo (mínimo 5 caracteres)."
                : "Escribe qué debe corregir el docente (mínimo 5 caracteres).");
        if (text.Length > 1000) return TeacherOpResult.Fail("El comentario no puede superar los 1 000 caracteres.");

        var plan = await _context.Plannings.AsTracking().FirstOrDefaultAsync(p => p.Id == planningId);
        if (plan == null) return TeacherOpResult.Fail("La solicitud no existe.");
        if (plan.Status != PlanningStatus.Requested)
            return TeacherOpResult.Fail($"La solicitud está {PlanningStatus.Label(plan.Status)}: solo se revisa una solicitud en revisión.");

        var from = plan.Status;
        plan.Status = toStatus;
        _context.PlanningReviews.Add(new PlanningReview
        {
            PlanningId = plan.Id, Action = action, FromStatus = from, ToStatus = toStatus,
            Comment = text, UserId = reviewerUserId, CreatedDate = DateTime.UtcNow
        });
        await NotifyTeacherAsync(plan, title, $"{L3Codes.Format(plan.Id, plan.Code)}: {hint}", icon);
        await _context.SaveChangesAsync();
        return new TeacherOpResult(true, action == PlanningReviewAction.Rejected
            ? $"Solicitud {L3Codes.Format(plan.Id, plan.Code)} rechazada."
            : $"Solicitud {L3Codes.Format(plan.Id, plan.Code)} devuelta al docente con observaciones.", plan.Id);
    }

    // ───────────────────────────── Consultas compartidas ─────────────────────────────

    public async Task<IReadOnlyList<RequestLineView>> GetLinesAsync(int planningId)
    {
        var lines = await _context.PlanningRequirements.AsNoTracking()
            .Where(r => r.PlanningId == planningId)
            .Select(r => new
            {
                r.Id, r.ArticleId, r.Article!.Code, r.Article.Name,
                Category = r.Article.ArticleCategory != null ? r.Article.ArticleCategory.Name : r.Article.Category,
                r.Article.UnitOfMeasure, Quantity = r.RequestedQuantity ?? r.ExpectedQuantity
            })
            .OrderBy(r => r.Name)
            .ToListAsync();
        // Lo pedido originalmente por el docente: el "antes" del primer cambio del revisor sobre cada artículo.
        var changes = await _context.PlanningReviewLines.AsNoTracking()
            .Where(l => l.PlanningReview!.PlanningId == planningId)
            .OrderBy(l => l.PlanningReview!.CreatedDate)
            .Select(l => new { l.ArticleId, l.PreviousQuantity })
            .ToListAsync();
        var original = changes.GroupBy(c => c.ArticleId).ToDictionary(g => g.Key, g => g.First().PreviousQuantity);
        return lines.Select(l => new RequestLineView(l.Id, l.ArticleId, l.Code, l.Name, l.Category, l.UnitOfMeasure, l.Quantity,
            original.TryGetValue(l.ArticleId, out var before) ? before ?? 0 : null)).ToList();
    }

    public async Task<IReadOnlyList<ReviewEventView>> GetHistoryAsync(int planningId)
    {
        var rows = await _context.PlanningReviews.AsNoTracking()
            .Where(r => r.PlanningId == planningId)
            .OrderBy(r => r.CreatedDate).ThenBy(r => r.Id)
            .Select(r => new
            {
                r.Id, r.Action, r.FromStatus, r.ToStatus, r.Comment, r.CreatedDate,
                User = r.User!.FirstName + " " + r.User.LastName,
                Lines = r.Lines.Select(l => new ReviewLineView(l.Article!.Name, l.PreviousQuantity, l.NewQuantity)).ToList()
            })
            .ToListAsync();
        return rows.Select(r => new ReviewEventView(r.Id, r.Action, EnumHelper.GetDisplayName(r.Action), r.FromStatus, r.ToStatus,
            r.Comment, r.User.Trim(), r.CreatedDate, r.Lines)).ToList();
    }

    // ───────────────────────────── Avisos ─────────────────────────────

    private async Task NotifyReviewersAsync(Planning planning, string teacherName, bool resubmitted)
    {
        var reviewers = await _context.Users.AsNoTracking()
            .Where(u => u.Status == GeneralStatus.Activo
                && (u.Role == UserRole.Supervisor || u.Role == UserRole.Administrador || u.Role == UserRole.SuperAdmin))
            .Select(u => u.Id)
            .ToListAsync();
        foreach (var userId in reviewers)
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = resubmitted ? "Solicitud corregida" : "Nueva solicitud de práctica",
                Message = Trim($"{teacherName} · {planning.PracticeName} · {planning.ScheduledDate:dd/MM} {planning.StartTime:hh\\:mm}", 255)!,
                ActionUrl = $"/LaboratoryOperations/Solicitudes/Revisar?id={planning.Id}",
                IconClass = "fas fa-inbox text-primary",
                ManagementId = planning.ManagementId,
                Scope = NotificationScopes.Planning,
                CreatedAt = DateTime.UtcNow
            });
    }

    private async Task NotifyTeacherAsync(Planning planning, string title, string message, string icon)
    {
        var users = await _context.Users.AsNoTracking()
            .Where(u => u.PersonId == planning.TeacherPersonId && u.Status == GeneralStatus.Activo)
            .Select(u => u.Id)
            .ToListAsync();
        foreach (var userId in users)
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = Trim(title, 100)!,
                Message = Trim(message, 255)!,
                ActionUrl = $"/Docente/Solicitud?id={planning.Id}",
                IconClass = icon,
                ManagementId = planning.ManagementId,
                Scope = NotificationScopes.Planning,
                CreatedAt = DateTime.UtcNow
            });
    }

    private async Task<string?> PersonNameAsync(int personId)
    {
        var intern = await _context.Interns.AsNoTracking().Where(i => i.Id == personId)
            .Select(i => new { i.Title, i.Name }).FirstOrDefaultAsync();
        if (intern != null) return string.IsNullOrWhiteSpace(intern.Title) ? intern.Name : $"{intern.Title} {intern.Name}";
        return await _context.Externs.AsNoTracking().Where(e => e.Id == personId).Select(e => e.Name).FirstOrDefaultAsync();
    }

    private static CheckIssue Block(string code, string title, string message) => new(code, CheckSeverity.Block, title, message);
    private static CheckIssue Warn(string code, string title, string message) => new(code, CheckSeverity.Warning, title, message);

    private static string? Trim(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var value = text.Trim();
        return value.Length <= max ? value : value[..max];
    }
}
