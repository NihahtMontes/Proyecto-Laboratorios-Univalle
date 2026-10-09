using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;

namespace Proyecto_Laboratorios_Univalle.Services;

public enum SlotSeverity
{
    /// <summary>Impide confirmar el destino.</summary>
    Block,
    /// <summary>Se puede confirmar, pero la usuaria debe estar avisada.</summary>
    Warning,
    /// <summary>Informativo (p. ej. una regla que no se pudo evaluar por falta de datos).</summary>
    Info
}

public sealed record SlotIssue(string Code, SlotSeverity Severity, string Message, IReadOnlyList<string> Details);

public sealed record MaterialShortage(int ArticleId, string ArticleCode, string ArticleName, decimal Requested, decimal? Available, decimal Shortage);

public sealed record SlotValidationResult(IReadOnlyList<SlotIssue> Issues, IReadOnlyList<MaterialShortage> Shortages)
{
    public bool HasBlocking => Issues.Any(i => i.Severity == SlotSeverity.Block);
    public bool HasWarnings => Issues.Any(i => i.Severity == SlotSeverity.Warning);

    /// <summary>"invalid" si algo bloquea, "warning" si hay advertencias, "valid" en otro caso.</summary>
    public string Level => HasBlocking ? "invalid" : HasWarnings ? "warning" : "valid";
}

/// <summary>Destino propuesto para una planificación existente (reprogramación o reposición).</summary>
public sealed record SlotRequest(int PlanningId, DateTime Date, int StartPeriodId, int LaboratoryId, int TeacherPersonId);

public interface ISlotValidationService
{
    /// <summary>
    /// Valida en el servidor un destino para la planificación. El bloque es indivisible: se mueve con la
    /// cantidad de periodos de la planificación origen. No abre transacciones (se puede llamar dentro de una del llamador).
    /// </summary>
    Task<SlotValidationResult> ValidateAsync(SlotRequest request, CancellationToken ct = default);
}

public sealed class SlotValidationService : ISlotValidationService
{
    public static class Codes
    {
        public const string OriginNotFound = "ORIGIN_NOT_FOUND";
        public const string OriginNotMovable = "ORIGIN_NOT_MOVABLE";
        public const string OriginWithoutPeriods = "ORIGIN_WITHOUT_PERIODS";
        public const string OutOfRange = "OUT_OF_RANGE";
        public const string DayNotEnabled = "DAY_NOT_ENABLED";
        public const string LabInvalid = "LAB_INVALID";
        public const string LabOccupied = "LAB_OCCUPIED";
        public const string Capacity = "CAPACITY";
        public const string CapacityNotEvaluated = "CAPACITY_NOT_EVALUATED";
        public const string Material = "MATERIAL";
        public const string MaterialNotEvaluated = "MATERIAL_NOT_EVALUATED";
        public const string WeekChange = "WEEK_CHANGE";
        public const string Completed = "COMPLETED";
        public const string TeacherBusy = "TEACHER_BUSY";
        public const string OpenDeparture = "OPEN_DEPARTURE";
    }

    /// <summary>Estados de origen que ya fueron reubicados (o anulados) y no se pueden volver a mover.</summary>
    private static readonly string[] NotMovableStatuses = [PlanningStatus.Cancelled, PlanningStatus.Rescheduled, PlanningStatus.Recovered];

    private static readonly LoanStatus[] OpenLoanStatuses = [LoanStatus.Active, LoanStatus.Overdue];

    private readonly ApplicationDbContext _context;
    private readonly IOccupancyService _occupancy;
    private readonly IArticleAvailabilityService _availability;

    public SlotValidationService(ApplicationDbContext context, IOccupancyService occupancy, IArticleAvailabilityService availability)
    {
        _context = context;
        _occupancy = occupancy;
        _availability = availability;
    }

    public async Task<SlotValidationResult> ValidateAsync(SlotRequest request, CancellationToken ct = default)
    {
        var issues = new List<SlotIssue>();
        var shortages = new List<MaterialShortage>();
        SlotValidationResult Result() => new(issues, shortages);

        var origin = await _context.Plannings.AsNoTracking()
            .Where(p => p.Id == request.PlanningId && !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                p.Status,
                p.StatusBeforeRecovery,
                p.ManagementId,
                p.AcademicWeekId,
                p.StartPeriodId,
                p.PeriodCount,
                ScheduleStudents = p.PracticeSchedule != null ? p.PracticeSchedule.StudentCount : null,
                GroupStudents = p.CourseGroup != null ? p.CourseGroup.StudentCount : null
            })
            .FirstOrDefaultAsync(ct);

        if (origin == null)
        {
            issues.Add(Block(Codes.OriginNotFound, "La planificación de origen no existe o fue eliminada."));
            return Result();
        }

        if (NotMovableStatuses.Contains(origin.Status))
        {
            issues.Add(Block(Codes.OriginNotMovable,
                $"La planificación está en estado \"{PlanningStatus.Label(origin.Status)}\": ya fue reubicada y no se puede volver a mover."));
            return Result();
        }

        if (origin.StartPeriodId is null || origin.PeriodCount is not int count || count < 1)
        {
            issues.Add(Block(Codes.OriginWithoutPeriods,
                "La planificación de origen no tiene periodo inicial ni cantidad de periodos: complete su horario antes de moverla."));
            return Result();
        }

        var date = request.Date.Date;
        var excluded = new[] { origin.Id };

        // Rango de periodos del destino.
        var startPeriod = await _context.ClassPeriods.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.StartPeriodId, ct);
        ClassPeriod? lastPeriod = null;
        var fits = false;
        if (startPeriod == null)
        {
            issues.Add(Block(Codes.OutOfRange, "El periodo inicial seleccionado no existe en el catálogo de periodos."));
        }
        else if (!PlanningSlot.Fits(startPeriod.Order, count))
        {
            issues.Add(Block(Codes.OutOfRange,
                $"El bloque de {count} periodos no cabe: P{startPeriod.Order}+{count}−1 supera el periodo {PlanningSlot.MaxPeriods}."));
        }
        else
        {
            var lastOrder = PlanningSlot.LastOrder(startPeriod.Order, count);
            lastPeriod = await _context.ClassPeriods.AsNoTracking().FirstOrDefaultAsync(c => c.Order == lastOrder, ct);
            if (lastPeriod == null)
                issues.Add(Block(Codes.OutOfRange, $"El catálogo de periodos no contiene el periodo P{lastOrder}."));
            else
                fits = true;
        }

        // Día académico del destino dentro de la gestión de la planificación origen.
        int? targetWeekId = null;
        if (PlanningSlot.WeekdayOf(date) == 0)
        {
            issues.Add(Block(Codes.DayNotEnabled, "Los domingos no hay actividades académicas: elija un día de lunes a sábado."));
        }
        else
        {
            var day = await _context.AcademicDays.AsNoTracking()
                .Where(d => d.Date == date && d.AcademicWeek.ManagementId == origin.ManagementId)
                .Select(d => new { d.AcademicWeekId, d.Status, d.Reason })
                .FirstOrDefaultAsync(ct);
            if (day == null)
            {
                issues.Add(Block(Codes.DayNotEnabled,
                    $"El {date:dd/MM/yyyy} está fuera de la gestión de la planificación: no pertenece a ninguna de sus semanas académicas."));
            }
            else
            {
                targetWeekId = day.AcademicWeekId;
                if (day.Status == AcademicDayStatus.Suspended)
                {
                    var reason = string.IsNullOrWhiteSpace(day.Reason) ? "sin motivo registrado" : day.Reason.Trim();
                    issues.Add(Block(Codes.DayNotEnabled, $"El {date:dd/MM/yyyy} está suspendido ({reason})."));
                }
            }
        }

        // Ambiente destino.
        var laboratory = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Id == request.LaboratoryId)
            .Select(l => new { l.Id, l.Code, l.Name, l.Status, l.Capacity })
            .FirstOrDefaultAsync(ct);
        if (laboratory == null || laboratory.Status != GeneralStatus.Activo)
        {
            issues.Add(Block(Codes.LabInvalid, laboratory == null
                ? "El ambiente seleccionado no existe."
                : $"El ambiente {laboratory.Code} está inactivo."));
        }
        else
        {
            if (fits)
            {
                var conflicts = await _occupancy.FindConflictsAsync(laboratory.Id, date, startPeriod!.Order, count, excluded, ct);
                if (conflicts.Count > 0)
                {
                    issues.Add(new SlotIssue(Codes.LabOccupied, SlotSeverity.Block,
                        $"El ambiente {laboratory.Code} ya está ocupado el {date:dd/MM/yyyy} en alguno de esos periodos.",
                        conflicts.Select(c => $"{c.Label} · {c.PeriodsText}").ToList()));
                }
            }

            var groupSize = origin.ScheduleStudents ?? origin.GroupStudents;
            if (laboratory.Capacity is int capacity && groupSize is int students)
            {
                if (students > capacity)
                    issues.Add(Block(Codes.Capacity,
                        $"El grupo tiene {students} estudiantes y el ambiente {laboratory.Code} admite {capacity}."));
            }
            else
            {
                var missing = new List<string>();
                if (laboratory.Capacity is null) missing.Add($"la capacidad del ambiente {laboratory.Code}");
                if (groupSize is null) missing.Add("la cantidad de estudiantes del grupo");
                issues.Add(new SlotIssue(Codes.CapacityNotEvaluated, SlotSeverity.Info,
                    $"No se evaluó la capacidad: falta registrar {string.Join(" y ", missing)}.", []));
            }
        }

        // Advertencias.
        if (origin.Status == PlanningStatus.Finalized
            || origin.Status == PlanningStatus.PendingRecovery && origin.StatusBeforeRecovery == PlanningStatus.Finalized)
            issues.Add(Warning(Codes.Completed, "Esta planificación ya está completada. ¿Seguro que desea reprogramarla?"));

        if (targetWeekId.HasValue && origin.AcademicWeekId.HasValue && targetWeekId != origin.AcademicWeekId)
            issues.Add(Warning(Codes.WeekChange,
                "El destino está en otra semana académica: los insumos se piden por semana, revise el pedido de material."));

        if (fits)
        {
            var teacherConflicts = await _occupancy.FindTeacherConflictsAsync(request.TeacherPersonId, date, startPeriod!.Order, count, excluded, ct);
            if (teacherConflicts.Count > 0)
            {
                issues.Add(new SlotIssue(Codes.TeacherBusy, SlotSeverity.Warning,
                    "El docente elegido tiene otra planificación en alguno de esos periodos.",
                    teacherConflicts.Select(c => $"{c.Label} · {c.PeriodsText}").ToList()));
            }
        }

        var hasOpenDeparture = await _context.Departures.AsNoTracking()
            .AnyAsync(d => d.PlanningId == origin.Id && OpenLoanStatuses.Contains(d.Status), ct);
        if (hasOpenDeparture)
            issues.Add(Warning(Codes.OpenDeparture,
                "La planificación tiene una salida L-3 abierta. El material entregado queda registrado en la planificación de origen; la devolución se registra allí desde Cierre."));

        if (fits)
            await EvaluateMaterialAsync(origin.Id, date, startPeriod!.StartTime, lastPeriod!.EndTime, issues, shortages, ct);

        return Result();
    }

    /// <summary>
    /// "Qué pasaría si": demanda simultánea de la fecha destino (planificaciones vigentes en cualquier ambiente, sin el origen)
    /// más el origen en su nuevo horario, contra el stock total (sin laboratorio de origen).
    /// </summary>
    private async Task EvaluateMaterialAsync(int originId, DateTime date, TimeSpan start, TimeSpan end,
        List<SlotIssue> issues, List<MaterialShortage> shortages, CancellationToken ct)
    {
        var originLines = await _context.PlanningRequirements.AsNoTracking()
            .Where(r => r.PlanningId == originId && r.RequestedQuantity > 0)
            .Select(r => new { r.ArticleId, r.Article!.Code, r.Article.Name, Quantity = r.RequestedQuantity!.Value })
            .ToListAsync(ct);
        if (originLines.Count == 0) return;

        var originArticles = originLines
            .GroupBy(l => l.ArticleId)
            .Select(g => new { ArticleId = g.Key, g.First().Code, g.First().Name, Quantity = g.Sum(l => l.Quantity) })
            .ToList();
        var articleIds = originArticles.Select(a => a.ArticleId).ToList();

        var toExclusive = date.AddDays(1);
        var otherLines = await _context.PlanningRequirements.AsNoTracking()
            .Where(r => articleIds.Contains(r.ArticleId) && r.RequestedQuantity > 0
                && r.PlanningId != originId
                && !r.Planning.IsDeleted && !PlanningStatus.NonOccupying.Contains(r.Planning.Status)
                && r.Planning.ScheduledDate >= date && r.Planning.ScheduledDate < toExclusive)
            .Select(r => new { r.PlanningId, r.ArticleId, Quantity = r.RequestedQuantity!.Value, r.Planning.StartTime, r.Planning.EndTime })
            .ToListAsync(ct);

        var stock = await _availability.GetStockAsync(articleIds, null);

        var notEvaluated = new List<string>();
        var details = new List<string>();
        foreach (var article in originArticles.OrderBy(a => a.Name))
        {
            var info = stock.TryGetValue(article.ArticleId, out var s) ? s : ArticleStockInfo.NotRegistered(article.ArticleId);
            if (info.Available is not decimal available)
            {
                notEvaluated.Add($"{article.Code} · {article.Name}");
                continue;
            }

            // Varias líneas del mismo artículo en una planificación se suman (como en la disponibilidad semanal).
            var demands = otherLines
                .Where(l => l.ArticleId == article.ArticleId)
                .GroupBy(l => l.PlanningId)
                .Select(g => new TimedDemand(g.Sum(l => l.Quantity), g.First().StartTime, g.First().EndTime))
                .Append(new TimedDemand(article.Quantity, start, end))
                .ToList();
            var (peak, peakStart, peakEnd) = DemandPeak.Compute(demands);
            if (peak <= available) continue;

            var shortage = peak - available;
            shortages.Add(new MaterialShortage(article.ArticleId, article.Code, article.Name, article.Quantity, available, shortage));
            var window = peakStart.HasValue ? $" ({peakStart.Value:hh\\:mm}–{(peakEnd ?? peakStart.Value):hh\\:mm})" : string.Empty;
            details.Add($"{article.Code} · {article.Name}: se pedirían {Qty(peak)} a la vez{window} (esta práctica pide {Qty(article.Quantity)}), stock {Qty(available)}, faltan {Qty(shortage)}.");
        }

        if (details.Count > 0)
            issues.Add(new SlotIssue(Codes.Material, SlotSeverity.Warning,
                $"El material no alcanza en el nuevo horario para {details.Count} artículo(s).", details));

        if (notEvaluated.Count > 0)
            issues.Add(new SlotIssue(Codes.MaterialNotEvaluated, SlotSeverity.Info,
                "No se evaluó la disponibilidad de algunos artículos porque no tienen stock registrado.", notEvaluated));
    }

    private static string Qty(decimal value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static SlotIssue Block(string code, string message) => new(code, SlotSeverity.Block, message, []);

    private static SlotIssue Warning(string code, string message) => new(code, SlotSeverity.Warning, message, []);
}
