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
    Task<TeacherOpResult> ApproveAsync(int reviewerUserId, int planningId, string? comment, IReadOnlyList<RequestLineInput>? adjustedLines,
        bool allowShortage = false, IReadOnlyList<AssignmentInput>? assignments = null);
    Task<TeacherOpResult> ObserveAsync(int reviewerUserId, int planningId, string comment);
    Task<TeacherOpResult> RejectAsync(int reviewerUserId, int planningId, string reason);
    Task<IReadOnlyList<RequestLineView>> GetLinesAsync(int planningId);
    Task<IReadOnlyList<ReviewEventView>> GetHistoryAsync(int planningId);
    /// <summary>Otras solicitudes pendientes que piden el mismo ambiente en un horario que se cruza.</summary>
    Task<IReadOnlyList<Planning>> FindCompetingRequestsAsync(int planningId);
    /// <summary>
    /// Evalúa el material de varias solicitudes del mismo día con una sola consulta de rango, artículos, stock y
    /// no devueltos (la bandeja de revisión lo usa en vez de llamar a <see cref="CheckMaterialAsync"/> por fila).
    /// </summary>
    Task<IReadOnlyList<DayMaterialCheck>> CheckMaterialForDayAsync(DateTime date, IReadOnlyList<MaterialDayRequest> requests);

    /// <summary>
    /// Sugerencia de asignación de origen por línea (V13): laboratorios activos con existencias del artículo y su
    /// libre (existencia − asignado a otras planificaciones que se cruzan − no devuelto), y el reparto sugerido
    /// (Economato primero; el faltante queda en el Economato).
    /// </summary>
    Task<IReadOnlyList<AssignmentSuggestion>> SuggestAssignmentsAsync(int planningId);

    /// <summary>Prácticas programadas del docente en la gestión vigente, desde hoy, que aún no tienen solicitud vigente.</summary>
    Task<IReadOnlyList<ProgrammedPractice>> GetProgrammedAsync(int teacherPersonId, RequestWindow window, int max = 20);

    /// <summary>Control semanal de la gestión vigente: programadas (previstas) frente a las que ya tienen solicitud.</summary>
    Task<IReadOnlyList<WeekCoverage>> GetWeeklyCoverageAsync(DateTime from, int weeks);
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

    /// <summary>Artículo tal como se muestra en el semáforo de material.</summary>
    private sealed record ArticleBasic(int Id, string Code, string Name, string? Category, ArticleType? Type);

    /// <summary>Demanda de un artículo en una fecha (para consumibles: todo lo pedido entre hoy y la fecha se gasta).</summary>
    private sealed record ConsumableDemand(DateTime Date, int ArticleId, IReadOnlyList<PracticeDemand> Practices);

    /// <summary>
    /// Datos de material cargados una sola vez y reutilizados por todas las evaluaciones de una misma operación
    /// (buscador de horarios, bandeja de revisión): catálogo, stock, lo no devuelto de prácticas que ya terminaron
    /// y la demanda de consumibles desde hoy hasta la última fecha evaluada (una sola consulta de rango extra).
    /// </summary>
    private sealed class MaterialContext
    {
        public required Dictionary<int, ArticleBasic> Articles { get; init; }
        public required Dictionary<int, ArticleStockInfo> Stock { get; init; }

        /// <summary>Por artículo: entregado y no devuelto de salidas abiertas cuya práctica terminó antes de la ventana pedida.</summary>
        public required IReadOnlyDictionary<int, decimal> Overdue { get; init; }

        /// <summary>Demanda diaria de artículos consumibles entre hoy y el fin de la operación (se descuenta al consumirse).</summary>
        public required IReadOnlyList<ConsumableDemand> ConsumableDemands { get; init; }
    }

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

    public async Task<IReadOnlyList<DayMaterialCheck>> CheckMaterialForDayAsync(DateTime date, IReadOnlyList<MaterialDayRequest> requests)
    {
        if (requests.Count == 0) return [];
        var periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        var ids = requests.SelectMany(r => r.Lines).Where(l => l.Quantity > 0).Select(l => l.ArticleId).Distinct().ToList();
        var range = ids.Count == 0 ? null : await _availability.GetRangeAsync(date.Date, date.Date);
        // Las solicitudes de la bandeja son siempre futuras: lo no devuelto que afecta al día es el de prácticas
        // terminadas antes de que empiece el día (las del propio día se ven como demanda del horario).
        var context = ids.Count == 0 ? null : await LoadMaterialContextAsync(ids, date.Date, date.Date);

        var result = new List<DayMaterialCheck>();
        foreach (var request in requests)
        {
            var start = periods.FirstOrDefault(p => p.Id == request.StartPeriodId);
            if (range == null || context == null || start == null)
            {
                result.Add(new DayMaterialCheck(request.PlanningId, []));
                continue;
            }
            var last = periods.FirstOrDefault(p => p.Order == PlanningSlot.LastOrder(start.Order, request.PeriodCount)) ?? start;
            result.Add(new DayMaterialCheck(request.PlanningId,
                EvaluateMaterial(range, date.Date, start.StartTime, last.EndTime, request.Lines, request.PlanningId, context)));
        }
        return result;
    }

    private async Task<MaterialContext> LoadMaterialContextAsync(IReadOnlyCollection<int> articleIds, DateTime windowStart,
        DateTime? consumableUntil = null)
    {
        var articles = await _context.Articles.AsNoTracking()
            .Where(a => articleIds.Contains(a.Id))
            .Select(a => new ArticleBasic(a.Id, a.Code, a.Name, a.ArticleCategory != null ? a.ArticleCategory.Name : a.Category, a.ArticleType))
            .ToDictionaryAsync(a => a.Id);
        var stock = await _availability.GetStockAsync(articleIds, null);
        var overdue = await StockRules.OverdueOutstandingAsync(_context, articleIds, windowStart);

        // Consumibles: lo pedido por prácticas que ocupan desde hoy hasta la última fecha de la operación ya no estará
        // (se gasta al usarse). Una sola consulta de rango por operación, reutilizada por todas las fechas evaluadas.
        var consumableDemands = new List<ConsumableDemand>();
        var until = consumableUntil ?? windowStart.Date;
        if (articles.Values.Any(a => a.Type == ArticleType.Consumable) && until.Date >= DateTime.Today)
        {
            var from = DateTime.Today;
            var range = await _availability.GetRangeAsync(from, until.Date);
            consumableDemands = range.Demands
                .Select(d => new ConsumableDemand(d.Date, d.ArticleId, d.Practices))
                .ToList();
        }
        return new MaterialContext { Articles = articles, Stock = stock, Overdue = overdue, ConsumableDemands = consumableDemands };
    }

    private async Task<IReadOnlyList<MaterialLineCheck>> EvaluateMaterialAsync(WeekAvailability range, DateTime date, TimeSpan from, TimeSpan to,
        IReadOnlyList<RequestLineInput> lines, int? excludePlanningId)
    {
        var merged = lines.Where(l => l.Quantity > 0).GroupBy(l => l.ArticleId)
            .Select(g => new RequestLineInput(g.Key, g.Sum(l => l.Quantity))).ToList();
        if (merged.Count == 0) return [];
        var context = await LoadMaterialContextAsync(merged.Select(l => l.ArticleId).ToList(), date + from, date.Date);
        return EvaluateMaterial(range, date, from, to, merged, excludePlanningId, context);
    }

    private static IReadOnlyList<MaterialLineCheck> EvaluateMaterial(WeekAvailability range, DateTime date, TimeSpan from, TimeSpan to,
        IReadOnlyList<RequestLineInput> lines, int? excludePlanningId, MaterialContext context)
    {
        var merged = lines.Where(l => l.Quantity > 0).GroupBy(l => l.ArticleId)
            .Select(g => new RequestLineInput(g.Key, g.Sum(l => l.Quantity))).ToList();
        if (merged.Count == 0) return [];
        var practices = range.Practices.ToDictionary(p => p.Id);

        var result = new List<MaterialLineCheck>();
        foreach (var line in merged)
        {
            if (!context.Articles.TryGetValue(line.ArticleId, out var article)) continue;
            var isConsumable = article.Type == ArticleType.Consumable;
            var demand = range.Demands.FirstOrDefault(d => d.Date == date && d.ArticleId == line.ArticleId);
            var uses = new List<MaterialUse>();
            decimal usedByOthers = 0;
            if (isConsumable)
            {
                // Los consumibles no vuelven: todo lo pedido por prácticas que ocupan desde hoy hasta la fecha pedida
                // se resta (no solo lo que se cruza en horario), porque se gasta al usarse.
                foreach (var d in context.ConsumableDemands.Where(d => d.ArticleId == line.ArticleId && d.Date <= date))
                {
                    foreach (var p in d.Practices.Where(p => p.PracticeId != excludePlanningId))
                    {
                        usedByOthers += p.Quantity;
                        var wp = practices.GetValueOrDefault(p.PracticeId);
                        if (wp == null) continue;
                        uses.Add(new MaterialUse(wp.Id, wp.Code, $"{wp.SubjectName ?? wp.PracticeName} ({wp.GroupName ?? "—"}) · {wp.LaboratoryCode ?? "—"}",
                            wp.StartTime, wp.EndTime, p.Quantity));
                    }
                }
            }
            else if (demand != null)
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

            var info = context.Stock.GetValueOrDefault(line.ArticleId);
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
                // Acta de stock: disponible = existencia − reservas coincidentes − préstamos no devueltos − mantenimiento.
                // Los consumibles ya entregados no aparecen como "sin devolver": su existencia bajó al entregarse.
                var overdue = isConsumable ? 0 : context.Overdue.GetValueOrDefault(line.ArticleId);
                var effectiveTotal = Math.Max(0, total - overdue);
                free = Math.Max(0, effectiveTotal - usedByOthers);
                var overdueNote = overdue > 0 ? $" ({overdue:0.##} sin devolver de prácticas anteriores)" : string.Empty;
                if (isConsumable)
                {
                    // Texto propio del consumible: explica que lo pedido antes de la fecha ya no estará.
                    if (line.Quantity > total)
                    {
                        state = MaterialState.Short;
                        explanation = $"Pides {line.Quantity:0.##} y en toda la institución hay {total:0.##}.";
                    }
                    else if (line.Quantity > free)
                    {
                        state = MaterialState.Short;
                        explanation = $"Stock {total:0.##}: se consumen {usedByOthers:0.##} en prácticas anteriores a tu fecha"
                            + (uses.Count > 0 ? $" ({string.Join(", ", uses.Take(2).Select(u => $"{u.Code}"))})" : string.Empty)
                            + $". Puedes pedir hasta {free:0.##}.";
                    }
                    else if (line.Quantity == free && usedByOthers > 0)
                    {
                        state = MaterialState.Tight;
                        explanation = $"Alcanza justo: quedan {free:0.##} después de lo que se consumen las prácticas anteriores a tu fecha.";
                    }
                    else
                    {
                        state = MaterialState.Ok;
                        explanation = usedByOthers > 0
                            ? $"Libres {free:0.##} de {total:0.##} (se consumen {usedByOthers:0.##} en prácticas anteriores a tu fecha)."
                            : $"Libres {total:0.##} para tu fecha.";
                    }
                }
                else if (line.Quantity > total)
                {
                    state = MaterialState.Short;
                    explanation = $"Pides {line.Quantity:0.##} y en toda la institución hay {total:0.##}{overdueNote}.";
                }
                else if (line.Quantity > free)
                {
                    state = MaterialState.Short;
                    var releaseAt = uses.Where(u => u.End.HasValue).Select(u => u.End!.Value).DefaultIfEmpty().Max();
                    explanation = $"Stock {total:0.##}{overdueNote}: {usedByOthers:0.##} en uso" + (uses.Count > 0
                            ? $" por {string.Join(", ", uses.Take(2).Select(u => $"{u.Code} ({u.TimeText})"))}" : string.Empty)
                        + (releaseAt > TimeSpan.Zero ? $". Se libera a las {releaseAt:hh\\:mm}: elige un horario después o pide {free:0.##}." : $". Puedes pedir hasta {free:0.##}.");
                }
                else if (line.Quantity == free && (usedByOthers > 0 || overdue > 0))
                {
                    state = MaterialState.Tight;
                    explanation = $"Alcanza justo: usas las {free:0.##} unidades libres en ese horario.";
                }
                else
                {
                    state = MaterialState.Ok;
                    explanation = usedByOthers > 0
                        ? $"Libres {free:0.##} de {total:0.##} en ese horario ({usedByOthers:0.##} en uso por otras prácticas{overdueNote})."
                        : overdue > 0
                            ? $"Libres {free:0.##} de {total:0.##} en ese horario{overdueNote}."
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
        // Material cargado UNA vez por búsqueda (catálogo, stock y no devueltos): antes se consultaba por cada candidato.
        var materialContext = range != null
            ? await LoadMaterialContextAsync(lines.Where(l => l.Quantity > 0).Select(l => l.ArticleId).Distinct().ToList(), days.First().Date,
                days.Last().Date)
            : null;

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
                    if (range != null && materialContext != null)
                    {
                        var material = EvaluateMaterial(range, date, start.StartTime, end, lines, null, materialContext);
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
                .Where(s => s.ManagementId == group.ManagementId && s.PracticeId == practice.Id
                    && (s.CourseGroupId == group.Id || s.GroupName == group.Name))
                .OrderBy(s => s.CourseGroupId == group.Id ? 0 : 1)
                .FirstOrDefaultAsync();
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
                ScalingFactor = group.ScalingFactor,
                // Marca del flujo docente: permite cancelar la programación si la solicitud se rechaza o retira
                // sin tocar las importadas (ImportBatchId) ni las creadas por otro flujo.
                CreatedById = teacher.UserId
            };
            // La programación puede ser compartida por varias sesiones del mismo grupo: el número de estudiantes
            // de esta solicitud solo se escribe al crearla (una corrección no pisa el valor de las otras sesiones).
            if (newSchedule && input.StudentCount.HasValue) schedule.StudentCount = input.StudentCount;
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
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!await _lock.TryAcquireAsync([PlanningLockResources.Planning(planningId)]))
                return TeacherOpResult.Fail(PlanningLockResources.BusyMessage + ".");

            // Relectura bajo bloqueo: el revisor pudo aprobar/observar mientras el docente confirmaba el retiro.
            var plan = await _context.Plannings.AsTracking().Include(p => p.PracticeSchedule)
                .FirstOrDefaultAsync(p => p.Id == planningId);
            if (plan == null || plan.TeacherPersonId != teacher.PersonId)
            {
                await transaction.RollbackAsync();
                return TeacherOpResult.Fail("La solicitud no existe.");
            }
            if (!PlanningStatus.PendingReview.Contains(plan.Status))
            {
                await transaction.RollbackAsync();
                return TeacherOpResult.Fail("Solo puedes retirar una solicitud que todavía no fue aprobada ni rechazada.");
            }

            var from = plan.Status;
            plan.Status = PlanningStatus.Cancelled;
            await CancelOrphanScheduleAsync(plan);
            _context.PlanningReviews.Add(new PlanningReview
            {
                PlanningId = plan.Id, Action = PlanningReviewAction.Withdrawn, FromStatus = from, ToStatus = PlanningStatus.Cancelled,
                Comment = Trim(reason, 1000), UserId = teacher.UserId, CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new TeacherOpResult(true, $"Solicitud {L3Codes.Format(plan.Id, plan.Code)} retirada.", plan.Id);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo retirar la solicitud {PlanningId}", planningId);
            return TeacherOpResult.Fail("No se pudo retirar la solicitud. Intenta nuevamente.");
        }
    }

    // ───────────────────────────── Revisión (Supervisor / Administrador / SuperAdmin) ─────────────────────────────

    public async Task<TeacherOpResult> ApproveAsync(int reviewerUserId, int planningId, string? comment,
        IReadOnlyList<RequestLineInput>? adjustedLines, bool allowShortage = false, IReadOnlyList<AssignmentInput>? assignments = null)
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

            // Sobreventa: las solicitudes pendientes no consumen stock, así que el material se revalida recién al
            // aprobar, con los ajustes ya aplicados en memoria y excluyendo esta planificación (igual que CheckMaterialAsync).
            var finalLines = FinalLines(plan.Requirements, adjustedLines);
            string? shortageDetail = null;
            if (finalLines.Count > 0)
            {
                var material = await CheckMaterialAsync(date, startPeriodId, count, finalLines, plan.Id);
                var shortages = material.Where(m => m.State == MaterialState.Short).ToList();
                if (shortages.Count > 0)
                {
                    shortageDetail = string.Join("; ", shortages.Select(s => $"{s.Name}: pide {s.Requested:0.##}, libre {(s.Free ?? 0):0.##}"));
                    if (!allowShortage)
                    {
                        await transaction.RollbackAsync();
                        return TeacherOpResult.Fail("No se puede aprobar: no alcanza el material en ese horario: " + shortageDetail
                            + ". Ajusta cantidades, observa la solicitud o marca «Aprobar igual con faltantes».");
                    }
                    // Aprobación consciente de los faltantes: queda asentada en el historial y avisada al revisor.
                    review.Comment = Trim($"{comment} [Aprobada con faltantes: {shortageDetail}]", 1000);
                }
            }

            // ─── Asignaciones de origen (V13: SolicitudL3Asignaciones) ───
            // Con la líneas finales ya aplicadas: si el revisor no eligió orígenes se usan los sugeridos; si eligió,
            // se valida que cubran exactamente cada línea y que ningún origen entregue más que su libre.
            var finalByArticle = finalLines.ToDictionary(l => l.ArticleId, l => l.Quantity);
            var unitLineArticles = plan.Requirements.Where(r => r.EquipmentUnitId != null).Select(r => r.ArticleId).ToHashSet();
            var catalogUnitArticles = (await _context.Articles.AsNoTracking()
                .Where(a => finalByArticle.Keys.Contains(a.Id) && a.EquipmentId != null)
                .Select(a => a.Id).ToListAsync()).ToHashSet();
            var assignable = finalByArticle
                .Where(kv => kv.Value > 0 && !unitLineArticles.Contains(kv.Key) && !catalogUnitArticles.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            IReadOnlyList<AssignmentInput>? effectiveAssignments = assignments;
            if (effectiveAssignments == null && assignable.Count > 0)
            {
                var suggested = await SuggestAssignmentsCoreAsync(plan.Id, plan.ScheduledDate.Date, plan.StartTime, plan.EndTime, assignable);
                effectiveAssignments = suggested.SelectMany(s => s.Suggested).ToList();
            }

            if (effectiveAssignments is { Count: > 0 })
            {
                var assignmentError = await ValidateAndApplyAssignmentsAsync(plan, assignable, effectiveAssignments, allowShortage,
                    snapshot.StartTime, snapshot.EndTime);
                if (assignmentError != null)
                {
                    await transaction.RollbackAsync();
                    return TeacherOpResult.Fail(assignmentError);
                }
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
            if (shortageDetail != null)
                warnings.Add($"Aprobada con faltantes de material: {shortageDetail}.");
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

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!await _lock.TryAcquireAsync([PlanningLockResources.Planning(planningId)]))
                return TeacherOpResult.Fail(PlanningLockResources.BusyMessage + ".");

            // Relectura bajo bloqueo: observar/rechazar compite con la aprobación y con el retiro del docente.
            var plan = await _context.Plannings.AsTracking().Include(p => p.PracticeSchedule)
                .FirstOrDefaultAsync(p => p.Id == planningId);
            if (plan == null)
            {
                await transaction.RollbackAsync();
                return TeacherOpResult.Fail("La solicitud no existe.");
            }
            if (plan.Status != PlanningStatus.Requested)
            {
                await transaction.RollbackAsync();
                return TeacherOpResult.Fail($"La solicitud está {PlanningStatus.Label(plan.Status)}: solo se revisa una solicitud en revisión.");
            }

            var from = plan.Status;
            plan.Status = toStatus;
            if (action == PlanningReviewAction.Rejected) await CancelOrphanScheduleAsync(plan);
            _context.PlanningReviews.Add(new PlanningReview
            {
                PlanningId = plan.Id, Action = action, FromStatus = from, ToStatus = toStatus,
                Comment = text, UserId = reviewerUserId, CreatedDate = DateTime.UtcNow
            });
            await NotifyTeacherAsync(plan, title, $"{L3Codes.Format(plan.Id, plan.Code)}: {hint}", icon);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new TeacherOpResult(true, action == PlanningReviewAction.Rejected
                ? $"Solicitud {L3Codes.Format(plan.Id, plan.Code)} rechazada."
                : $"Solicitud {L3Codes.Format(plan.Id, plan.Code)} devuelta al docente con observaciones.", plan.Id);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo revisar la solicitud {PlanningId} (acción {Action})", planningId, action);
            return TeacherOpResult.Fail("No se pudo guardar la revisión. Intenta nuevamente.");
        }
    }

    /// <summary>
    /// Cancela la programación creada por el flujo docente si esta planificación era su único uso vigente:
    /// nunca se tocan las importadas (ImportBatchId) ni las que otra planificación no rechazada/cancelada usa.
    /// Observar NO cancela: el docente puede corregir y reenviar la misma solicitud.
    /// </summary>
    private async Task CancelOrphanScheduleAsync(Planning plan)
    {
        if (plan.PracticeScheduleId is not int sid || plan.PracticeSchedule is not { ImportBatchId: null } schedule) return;
        // Solo si la creó la cuenta del propio docente al enviar la solicitud (no el asistente del administrador).
        if (schedule.CreatedById is not int creatorId
            || !await _context.Users.AsNoTracking().AnyAsync(u => u.Id == creatorId && u.PersonId == plan.TeacherPersonId))
            return;
        var inUse = await _context.Plannings.AsNoTracking()
            .AnyAsync(p => p.PracticeScheduleId == sid && p.Id != plan.Id && !p.IsDeleted
                && p.Status != PlanningStatus.Rejected && p.Status != PlanningStatus.Cancelled);
        if (!inUse) schedule.Status = PracticeScheduleStatus.Cancelled;
    }

    // ───────────────────────────── Programación → solicitudes ─────────────────────────────

    /// <summary>Planificaciones que cuentan como "solicitud recibida" de una programación (no rechazadas ni retiradas).</summary>
    private static readonly string[] NotReceivedStatuses = [PlanningStatus.Rejected, PlanningStatus.Cancelled];

    public async Task<IReadOnlyList<ProgrammedPractice>> GetProgrammedAsync(int teacherPersonId, RequestWindow window, int max = 20)
    {
        if (window.ManagementId is not int managementId) return [];
        var today = DateTime.Today;
        var rows = await _context.PracticeSchedules.AsNoTracking()
            .Where(s => s.ManagementId == managementId && s.Status == PracticeScheduleStatus.Scheduled && !s.IsDeleted
                && s.CourseGroupId != null && s.CourseGroup!.TeacherPersonId == teacherPersonId && s.CourseGroup.Status == GeneralStatus.Activo
                && s.ExecutionDate != null && s.ExecutionDate >= today
                && !_context.Plannings.Any(p => p.PracticeScheduleId == s.Id && !p.IsDeleted && !NotReceivedStatuses.Contains(p.Status)))
            .OrderBy(s => s.ExecutionDate)
            .Take(max)
            .Select(s => new
            {
                s.Id, CourseGroupId = s.CourseGroupId!.Value, s.PracticeId, Date = s.ExecutionDate!.Value, s.WeekNumber,
                Practice = s.Practice!.Name, Subject = s.Subject!.Name, Group = s.CourseGroup!.Name, s.LaboratoryId
            })
            .ToListAsync();
        return rows.Select(r => new ProgrammedPractice(r.Id, r.CourseGroupId, r.PracticeId, r.Date.Date, r.WeekNumber, r.Practice,
            r.Subject, r.Group, r.LaboratoryId, window.Contains(r.Date.Date))).ToList();
    }

    public async Task<IReadOnlyList<WeekCoverage>> GetWeeklyCoverageAsync(DateTime from, int weeks)
    {
        var management = await _managements.GetCurrentAsync();
        if (management == null || weeks < 1) return [];
        var monday = from.Date.AddDays(-(((int)from.DayOfWeek + 6) % 7));
        var until = monday.AddDays(7 * weeks);
        var rows = await _context.PracticeSchedules.AsNoTracking()
            .Where(s => s.ManagementId == management.Id && !s.IsDeleted && s.ExecutionDate != null
                && s.ExecutionDate >= monday && s.ExecutionDate < until
                && (s.Status == PracticeScheduleStatus.Scheduled || s.Status == PracticeScheduleStatus.Executed))
            .Select(s => new
            {
                Date = s.ExecutionDate!.Value, s.WeekNumber,
                Label = s.Subject!.Name + " · Grupo " + (s.CourseGroup != null ? s.CourseGroup.Name : s.GroupName) + " · " + s.Practice!.Name,
                Received = _context.Plannings.Any(p => p.PracticeScheduleId == s.Id && !p.IsDeleted && !NotReceivedStatuses.Contains(p.Status))
            })
            .ToListAsync();

        var result = new List<WeekCoverage>();
        for (var w = 0; w < weeks; w++)
        {
            var start = monday.AddDays(7 * w);
            var end = start.AddDays(6);
            var week = rows.Where(r => r.Date.Date >= start && r.Date.Date <= end).ToList();
            result.Add(new WeekCoverage(week.Select(r => r.WeekNumber).FirstOrDefault(n => n != null), start, end, week.Count,
                week.Count(r => r.Received),
                week.Where(r => !r.Received).OrderBy(r => r.Date).Take(5).Select(r => $"{r.Label} ({r.Date:dd/MM})").ToList()));
        }
        return result;
    }

    // ───────────────────────────── Asignación de origen (V13: SolicitudL3Asignaciones) ─────────────────────────────

    public async Task<IReadOnlyList<AssignmentSuggestion>> SuggestAssignmentsAsync(int planningId)
    {
        var plan = await _context.Plannings.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planningId);
        if (plan == null) return [];
        var lines = await _context.PlanningRequirements.AsNoTracking()
            .Where(r => r.PlanningId == planningId && r.EquipmentUnitId == null && r.Article!.EquipmentId == null)
            .Select(r => new { r.ArticleId, Quantity = r.RequestedQuantity ?? r.ExpectedQuantity })
            .ToListAsync();
        var quantities = lines.GroupBy(l => l.ArticleId).Select(g => new { g.Key, Quantity = g.Sum(l => l.Quantity) })
            .Where(l => l.Quantity > 0).ToDictionary(l => l.Key, l => l.Quantity);
        return quantities.Count == 0 ? [] : await SuggestAssignmentsCoreAsync(planningId, plan.ScheduledDate.Date, plan.StartTime, plan.EndTime, quantities);
    }

    /// <summary>
    /// Libre por origen en el horario de la práctica y reparto sugerido. Libre = existencia del origen − lo asignado a ese
    /// origen por otras planificaciones que ocupan y se cruzan (sin asignaciones cuentan en el Economato; los consumibles
    /// restan todo lo pedido desde hoy hasta la fecha) − lo no devuelto de prácticas anteriores en ese origen.
    /// Sugerido: Economato primero, luego el resto por libre descendente; lo que no alcanza queda en el Economato.
    /// </summary>
    private async Task<List<AssignmentSuggestion>> SuggestAssignmentsCoreAsync(int planningId, DateTime date, TimeSpan? start, TimeSpan? end,
        IReadOnlyDictionary<int, decimal> quantities)
    {
        var free = await FreeByOriginAsync(planningId, date, start, end, quantities.Keys.ToList());
        var articles = await _context.Articles.AsNoTracking()
            .Where(a => quantities.Keys.Contains(a.Id))
            .Select(a => new { a.Id, a.Code, a.Name, a.ArticleType })
            .ToDictionaryAsync(a => a.Id);

        var result = new List<AssignmentSuggestion>();
        foreach (var (articleId, quantity) in quantities)
        {
            if (!articles.TryGetValue(articleId, out var article)) continue;
            var origins = free.Origins
                .Where(o => o.ArticleId == articleId || (o.ArticleId == null && o.LaboratoryId == free.EconomatoId))
                .GroupBy(o => o.LaboratoryId).Select(g => g.First())
                .Select(o => new OriginOption(o.LaboratoryId, o.Code, o.Name, free.Free.GetValueOrDefault((o.LaboratoryId, articleId))))
                .OrderBy(o => o.LaboratoryId == free.EconomatoId ? 0 : 1).ThenByDescending(o => o.Free).ThenBy(o => o.Code)
                .ToList();

            var suggested = new List<AssignmentInput>();
            var remaining = quantity;
            foreach (var origin in origins)
            {
                if (remaining <= 0) break;
                var take = Math.Min(remaining, origin.Free);
                if (take <= 0) continue;
                suggested.Add(new AssignmentInput(articleId, origin.LaboratoryId, take));
                remaining -= take;
            }
            if (remaining > 0)
            {
                // No alcanza: lo que falta queda en el Economato (o en el primer origen) para que el revisor decida.
                var fallback = free.EconomatoId ?? origins.Select(o => (int?)o.LaboratoryId).FirstOrDefault();
                if (fallback is int labId)
                {
                    var existing = suggested.FindIndex(a => a.LaboratoryId == labId);
                    if (existing >= 0) suggested[existing] = suggested[existing] with { Quantity = suggested[existing].Quantity + remaining };
                    else suggested.Add(new AssignmentInput(articleId, labId, remaining));
                }
            }
            result.Add(new AssignmentSuggestion(articleId, article.Code, article.Name, quantity, article.ArticleType == ArticleType.Consumable,
                origins, suggested));
        }
        return result;
    }

    /// <summary>Orígenes posibles (con stock del artículo, más el Economato) y su libre por (laboratorio, artículo).</summary>
    private sealed record OriginRow(int LaboratoryId, string Code, string Name, int? ArticleId);
    private sealed record OriginFree(int? EconomatoId, List<OriginRow> Origins, Dictionary<(int, int), decimal> Free);

    private async Task<OriginFree> FreeByOriginAsync(int planningId, DateTime date, TimeSpan? start, TimeSpan? end, IReadOnlyCollection<int> articleIds)
    {
        var economato = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Code == L3CircuitService.DefaultOriginLaboratoryCode && l.Status == GeneralStatus.Activo)
            .Select(l => new OriginRow(l.Id, l.Code, l.Name, null)).FirstOrDefaultAsync();
        var stocks = await _context.ArticleStocks.AsNoTracking()
            .Where(s => articleIds.Contains(s.ArticleId) && s.Laboratory!.Status == GeneralStatus.Activo)
            .Select(s => new { s.LaboratoryId, s.ArticleId, s.AvailableQuantity, s.Laboratory!.Code, s.Laboratory.Name })
            .ToListAsync();
        var consumables = (await _context.Articles.AsNoTracking()
            .Where(a => articleIds.Contains(a.Id) && a.ArticleType == ArticleType.Consumable)
            .Select(a => a.Id).ToListAsync()).ToHashSet();

        // Otras planificaciones que ocupan: el mismo día y cruzándose (reutilizables) o desde hoy hasta la fecha (consumibles).
        var windowStart = start ?? TimeSpan.Zero;
        var windowEnd = end ?? new TimeSpan(23, 59, 59);
        var from = consumables.Count > 0 && DateTime.Today < date ? DateTime.Today : date;
        var next = date.AddDays(1);
        var others = await _context.Plannings.AsNoTracking()
            .Where(p => p.Id != planningId && !p.IsDeleted && !PlanningStatus.NonOccupying.Contains(p.Status)
                && p.Status != PlanningStatus.Finalized && p.ScheduledDate >= from && p.ScheduledDate < next)
            .Select(p => new { p.Id, p.ScheduledDate, p.StartTime, p.EndTime })
            .ToListAsync();
        var overlapping = others
            .Where(o => o.ScheduledDate.Date == date && Overlaps(o.StartTime, o.EndTime, windowStart, windowEnd))
            .Select(o => o.Id).ToHashSet();
        var ids = others.Select(o => o.Id).ToList();
        var demands = ids.Count == 0
            ? []
            : await _context.PlanningRequirements.AsNoTracking()
                .Where(r => ids.Contains(r.PlanningId) && articleIds.Contains(r.ArticleId))
                .Select(r => new
                {
                    r.PlanningId, r.ArticleId, Quantity = r.RequestedQuantity ?? r.ExpectedQuantity,
                    Assignments = r.Assignments.Select(a => new { a.LaboratoryId, a.Quantity }).ToList()
                })
                .ToListAsync();

        var taken = new Dictionary<(int, int), decimal>();
        void Take(int labId, int articleId, decimal qty) => taken[(labId, articleId)] = taken.GetValueOrDefault((labId, articleId)) + qty;
        foreach (var d in demands)
        {
            // Reutilizables: solo cuentan las que se cruzan en horario; consumibles: todas las anteriores o del día.
            if (!consumables.Contains(d.ArticleId) && !overlapping.Contains(d.PlanningId)) continue;
            if (d.Assignments.Count > 0)
                foreach (var a in d.Assignments) Take(a.LaboratoryId, d.ArticleId, a.Quantity);
            else if (economato != null)
                Take(economato.LaboratoryId, d.ArticleId, d.Quantity);
        }
        var overdue = await StockRules.OverdueByOriginAsync(_context, articleIds, date + windowStart);

        var freeMap = new Dictionary<(int, int), decimal>();
        foreach (var s in stocks)
            freeMap[(s.LaboratoryId, s.ArticleId)] = Math.Max(0, s.AvailableQuantity
                - taken.GetValueOrDefault((s.LaboratoryId, s.ArticleId)) - overdue.GetValueOrDefault((s.LaboratoryId, s.ArticleId)));

        var origins = stocks.Select(s => new OriginRow(s.LaboratoryId, s.Code, s.Name, s.ArticleId)).ToList();
        if (economato != null) origins.Add(economato);
        return new OriginFree(economato?.LaboratoryId, origins, freeMap);
    }

    /// <summary>
    /// Valida y guarda las asignaciones: cada línea asignable queda cubierta exactamente, solo orígenes activos y, salvo
    /// <paramref name="allowShortage"/>, ningún origen entrega más que su libre. Reemplaza las asignaciones previas.
    /// </summary>
    private async Task<string?> ValidateAndApplyAssignmentsAsync(Planning plan, IReadOnlyDictionary<int, decimal> assignable,
        IReadOnlyList<AssignmentInput> assignments, bool allowShortage, TimeSpan? start, TimeSpan? end)
    {
        var wanted = assignments.Where(a => a.Quantity > 0).ToList();
        if (wanted.Any(a => decimal.Round(a.Quantity, 2) != a.Quantity || a.Quantity > 999999))
            return "Las cantidades por origen admiten hasta 2 decimales.";
        if (wanted.Any(a => !assignable.ContainsKey(a.ArticleId)))
            return "Hay asignaciones de artículos que no están en la solicitud.";
        var labIds = wanted.Select(a => a.LaboratoryId).Distinct().ToList();
        var labs = await _context.Laboratories.AsNoTracking()
            .Where(l => labIds.Contains(l.Id) && l.Status == GeneralStatus.Activo)
            .ToDictionaryAsync(l => l.Id, l => l.Code);
        if (labs.Count != labIds.Count) return "Hay orígenes que no son laboratorios activos.";

        var articleIds = assignable.Keys.ToList();
        var names = await _context.Articles.AsNoTracking().Where(a => articleIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name);
        foreach (var (articleId, quantity) in assignable)
        {
            var sum = wanted.Where(a => a.ArticleId == articleId).Sum(a => a.Quantity);
            if (sum != quantity)
                return $"El origen de {names.GetValueOrDefault(articleId, "un artículo")} suma {sum:0.##} y la línea pide {quantity:0.##}: ajusta el reparto.";
        }

        if (!allowShortage)
        {
            var free = await FreeByOriginAsync(plan.Id, plan.ScheduledDate.Date, start, end, articleIds);
            var over = wanted
                .GroupBy(a => (a.LaboratoryId, a.ArticleId))
                .Select(g => new { g.Key.LaboratoryId, g.Key.ArticleId, Qty = g.Sum(a => a.Quantity), Free = free.Free.GetValueOrDefault(g.Key) })
                .Where(x => x.Qty > x.Free)
                .ToList();
            if (over.Count > 0)
                return "No se puede aprobar: " + string.Join("; ", over.Select(x =>
                        $"origen {labs[x.LaboratoryId]}: asigna {x.Qty:0.##} de {names.GetValueOrDefault(x.ArticleId, "artículo")}, libre {x.Free:0.##}"))
                    + ". Ajusta el reparto o marca «Aprobar igual con faltantes».";
        }

        // Reemplazo: las asignaciones previas de la planificación se sustituyen por las nuevas.
        var requirementIds = plan.Requirements.Where(r => r.Id > 0).Select(r => r.Id).ToList();
        var previous = await _context.PlanningAssignments.AsTracking()
            .Where(a => requirementIds.Contains(a.PlanningRequirementId)).ToListAsync();
        _context.PlanningAssignments.RemoveRange(previous);
        foreach (var group in wanted.GroupBy(a => a.ArticleId))
        {
            var requirement = plan.Requirements
                .Where(r => r.ArticleId == group.Key && _context.Entry(r).State != EntityState.Deleted)
                .OrderBy(r => r.Id)
                .First();
            foreach (var byLab in group.GroupBy(a => a.LaboratoryId))
                requirement.Assignments.Add(new PlanningAssignment { LaboratoryId = byLab.Key, Quantity = byLab.Sum(a => a.Quantity) });
        }
        return null;
    }

    /// <summary>Líneas finales de la solicitud tal como quedarán tras aprobar (con o sin ajustes del revisor).</summary>
    private static List<RequestLineInput> FinalLines(IEnumerable<PlanningRequirement> requirements, IReadOnlyList<RequestLineInput>? adjusted)
    {
        var previous = requirements
            .GroupBy(r => r.ArticleId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.RequestedQuantity ?? r.ExpectedQuantity));
        IEnumerable<RequestLineInput> lines;
        if (adjusted != null)
        {
            var wanted = adjusted.GroupBy(l => l.ArticleId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
            lines = previous.Keys.Union(wanted.Keys)
                .Select(id => new RequestLineInput(id, wanted.TryGetValue(id, out var next) ? next : previous[id]));
        }
        else
        {
            lines = previous.Select(kv => new RequestLineInput(kv.Key, kv.Value));
        }
        return lines.Where(l => l.Quantity > 0).ToList();
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
