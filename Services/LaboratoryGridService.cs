using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Grilla semanal por laboratorio (réplica del formulario RE-10-VAC-007 "Registro de horarios – Horario por aula").
/// Solo lectura.
/// </summary>
public sealed record LaboratoryGrid(
    AcademicManagementSummary Management,
    IReadOnlyList<AcademicManagementSummary> Managements,
    IReadOnlyList<GridLaboratoryOption> Laboratories,
    GridLaboratory? Laboratory,
    IReadOnlyList<GridPeriod> Periods,
    IReadOnlyList<GridWeekOption> WeekOptions,
    int? SelectedWeekNumber,
    int? PreviousWeekNumber,
    int? NextWeekNumber,
    bool TwoWeeks,
    IReadOnlyList<GridWeek> Weeks)
{
    public bool HasWeeks => WeekOptions.Count > 0;
    public bool HasLaboratories => Laboratories.Count > 0;
    /// <summary>Solo una gestión activa o inactiva admite nuevas planificaciones.</summary>
    public bool CanAdd => Management.IsEditable;
}

public sealed record GridLaboratoryOption(int Id, string Code, string Name, int PlanningCount)
{
    public string Label => $"{Code} – {Name}";
}

public sealed record GridLaboratory(int Id, string Code, string Name, string? Floor, string? Building, string? Block, string? Room, int? Capacity)
{
    public string Label => $"{Code} – {Name}";

    /// <summary>"PLANTA BAJA · Campus Tiquipaya · Ambiente X" o "No registrada".</summary>
    public string LocationText
    {
        get
        {
            var parts = new[] { Floor, Building, Block, Room }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim())
                .ToList();
            return parts.Count == 0 ? "No registrada" : string.Join(" · ", parts);
        }
    }

    public string CapacityText => Capacity.HasValue ? $"{Capacity} estudiantes" : "No registrada";
}

public sealed record GridPeriod(int Id, int Order, string Label);

public sealed record GridWeekOption(int Number, DateTime StartDate, DateTime EndDate)
{
    public string RangeText => $"{StartDate:dd/MM} – {EndDate:dd/MM}";
}

public sealed record GridWeek(int Number, DateTime StartDate, DateTime EndDate, IReadOnlyList<GridDay> Days, IReadOnlyList<GridBlock> Unscheduled)
{
    public string RangeText => $"{StartDate:dd/MM} – {EndDate:dd/MM}";
    public int BlockCount => Days.Sum(d => d.Blocks.Count) + Unscheduled.Count;
}

/// <param name="InManagement">False si la fecha no tiene día académico (fuera de la gestión).</param>
public sealed record GridDay(int Weekday, string Name, DateTime Date, bool InManagement, bool IsSuspended, string? Reason, IReadOnlyList<GridBlock> Blocks)
{
    /// <summary>Solo los días habilitados de la gestión aceptan bloques.</summary>
    public bool AcceptsBlocks => InManagement && !IsSuspended;

    /// <summary>True si algún bloque posicionado cubre el periodo indicado.</summary>
    public bool IsBusy(int order) => Blocks.Any(b => b.StartOrder is int s && order >= s && order <= b.EndOrder);

    /// <summary>
    /// True si el periodo está libre pero contiguo a algún bloque con periodo inicial:
    /// es el periodo de separación que debe quedar libre entre prácticas del ambiente.
    /// </summary>
    public bool IsBuffer(int order)
        => !IsBusy(order)
        && Blocks.Any(b => b.Occupies && b.StartOrder is int s && (order == s - 1 || order == b.EndOrder + 1));
}

/// <param name="StartOrder">Orden del periodo inicial (null = planificación legada sin periodo).</param>
/// <param name="LaneIndex">Carril (0..LaneCount-1) cuando hay bloques solapados en el mismo día.</param>
public sealed record GridBlock(
    int PlanningId,
    string Code,
    int LaboratoryId,
    DateTime Date,
    int Weekday,
    int? StartOrder,
    int PeriodCount,
    string SubjectShort,
    string SubjectName,
    string GroupName,
    string TeacherName,
    string PracticeName,
    string Status,
    string StatusLabel,
    string StatusCss,
    bool Occupies,
    string NextUrl,
    string TimeText,
    int LaneIndex = 0,
    int LaneCount = 1,
    GridRelocation? Relocation = null)
{
    public int EndOrder => StartOrder is int s ? PlanningSlot.LastOrder(s, PeriodCount) : 0;

    /// <summary>Se puede reprogramar (individualmente) desde la grilla: Planificada, Aprobada o Finalizada con periodos.</summary>
    public bool IsReschedulable => StartOrder.HasValue && GridRelocation.ReschedulableStatuses.Contains(Status);

    public string Tooltip =>
        $"{Code} · {PracticeName}\nMateria: {SubjectName}\nGrupo: {GroupName}\nDocente: {TeacherName}\nHorario: {Date:dd/MM/yyyy} · {TimeText}\nEstado: {StatusLabel}"
        + (Relocation is null ? string.Empty : $"\n{Relocation.Text}");
}

public enum GridRelocationKind
{
    /// <summary>El bloque es un origen ya reubicado: enlaza a su nueva ubicación.</summary>
    MovedTo,
    /// <summary>El bloque es el destino de una reprogramación o reposición: enlaza a su origen.</summary>
    MovedFrom
}

/// <summary>Marca de reprogramación/reposición de un bloque, con enlace a la grilla del otro extremo.</summary>
/// <param name="IsRecovery">True si el movimiento fue una reposición de día (Rescheduling con DayRecoveryId).</param>
public sealed record GridRelocation(GridRelocationKind Kind, bool IsRecovery, string Text, string Url)
{
    /// <summary>Estados que se pueden reprogramar individualmente (PendienteReposicion solo vía reposición de día).</summary>
    public static readonly string[] ReschedulableStatuses = [PlanningStatus.Planned, PlanningStatus.Approved, PlanningStatus.Finalized];

    public static bool IsReschedulableStatus(string? status) => status != null && ReschedulableStatuses.Contains(status);

    /// <summary>Sufijo de clase CSS: reprogramación (violeta) o reposición (naranja).</summary>
    public string Css => IsRecovery ? "recovery" : "rescheduling";

    public string Icon => Kind == GridRelocationKind.MovedTo
        ? "mdi-arrow-right-bold"
        : IsRecovery ? "mdi-backup-restore" : "mdi-calendar-clock";

    /// <summary>URL de la grilla de un ambiente en la semana de una fecha.</summary>
    public static string GridUrl(int managementId, int? laboratoryId, DateTime date) =>
        $"/LaboratoryOperations/Grilla?managementId={managementId}"
        + (laboratoryId.HasValue ? $"&laboratoryId={laboratoryId.Value}" : string.Empty)
        + $"&date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    /// <summary>URL del modo reprogramación de la grilla para una planificación.</summary>
    public static string RescheduleUrl(int planningId) => $"/LaboratoryOperations/Grilla?reprogramar={planningId}";
}

public interface ILaboratoryGridService
{
    /// <summary>
    /// Arma la grilla semanal de un laboratorio. Devuelve null si no hay gestión (pedida o actual).
    /// </summary>
    /// <param name="date">Fecha yyyy-MM-dd: elige la semana de la gestión que la contiene.</param>
    /// <param name="twoWeeks">Incluye además la semana siguiente (si existe).</param>
    Task<LaboratoryGrid?> BuildAsync(int? managementId, int? laboratoryId, int? weekNumber, string? date, bool twoWeeks);
}

public sealed class LaboratoryGridService : ILaboratoryGridService
{
    private const int MaxSubjectShortLength = 24;
    private const int MaxWordLength = 5;

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "DE", "DEL", "LA", "LAS", "EL", "LOS", "Y", "EN", "A"
    };

    private static readonly Regex RomanNumeral = new(
        "^M{0,3}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;

    public LaboratoryGridService(ApplicationDbContext context, IAcademicManagementService managements)
    {
        _context = context;
        _managements = managements;
    }

    public async Task<LaboratoryGrid?> BuildAsync(int? managementId, int? laboratoryId, int? weekNumber, string? date, bool twoWeeks)
    {
        var management = await _managements.ResolveAsync(managementId);
        if (management is null) return null;

        var managements = await _managements.ListAsync();

        var periods = (await _context.ClassPeriods.AsNoTracking()
                .OrderBy(p => p.Order)
                .Select(p => new { p.Id, p.Order, p.StartTime, p.EndTime })
                .ToListAsync())
            .Select(p => new GridPeriod(p.Id, p.Order, $"{p.StartTime:hh\\:mm} – {p.EndTime:hh\\:mm}"))
            .ToList();
        var maxOrder = periods.Count > 0 ? periods.Max(p => p.Order) : PlanningSlot.MaxPeriods;

        var labs = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo)
            .OrderBy(l => l.Code)
            .Select(l => new GridLaboratory(l.Id, l.Code, l.Name, l.Floor, l.Building, l.Block, l.Room, l.Capacity))
            .ToListAsync();

        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Where(w => w.ManagementId == management.Id)
            .OrderBy(w => w.Number)
            .Select(w => new GridWeekOption(w.Number, w.StartDate, w.EndDate))
            .ToListAsync();

        if (weeks.Count == 0)
        {
            var fallbackLab = labs.FirstOrDefault(l => l.Id == laboratoryId) ?? labs.FirstOrDefault();
            return new LaboratoryGrid(management, managements, ToOptions(labs, new Dictionary<int, int>()), fallbackLab, periods, weeks,
                null, null, null, twoWeeks, []);
        }

        // Semana elegida: fecha → número → la que contiene hoy → la primera.
        var selected = SelectWeek(weeks, weekNumber, date);
        var selectedIndex = weeks.IndexOf(selected);
        var visibleWeeks = twoWeeks && selectedIndex + 1 < weeks.Count
            ? new List<GridWeekOption> { selected, weeks[selectedIndex + 1] }
            : new List<GridWeekOption> { selected };

        var from = visibleWeeks[0].StartDate.Date;
        var to = visibleWeeks[^1].EndDate.Date.AddDays(1);

        // Planificaciones de todos los laboratorios en el rango (permite elegir el laboratorio por defecto
        // y contar planificaciones por ambiente sin consultas adicionales).
        var plannings = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == management.Id && !p.IsDeleted && p.Status != PlanningStatus.Cancelled
                && p.ScheduledDate >= from && p.ScheduledDate < to
                && (p.LaboratoryId != null || p.AllLaboratories))
            .OrderBy(p => p.ScheduledDate)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.LaboratoryId,
                p.AllLaboratories,
                p.ScheduledDate,
                p.ScheduledEndDate,
                p.Status,
                p.TeacherPersonId,
                p.PracticeName,
                StartOrder = p.StartPeriod != null ? (int?)p.StartPeriod.Order : null,
                p.PeriodCount,
                SubjectName = p.Subject != null ? p.Subject.Name : null,
                GroupLabel = !string.IsNullOrEmpty(p.GroupName) ? p.GroupName : (p.Group != null ? p.Group.Name : null),
                HasRequirements = p.Requirements.Any()
            })
            .ToListAsync();

        var countByLab = labs.ToDictionary(
            l => l.Id,
            l => plannings.Count(p => p.AllLaboratories || p.LaboratoryId == l.Id));

        // Laboratorio: el pedido → el primero con planificaciones en la semana elegida → el primero por código.
        var selectedWeekEnd = selected.EndDate.Date.AddDays(1);
        var lab = labs.FirstOrDefault(l => l.Id == laboratoryId)
            ?? labs.FirstOrDefault(l => plannings.Any(p => p.LaboratoryId == l.Id
                && p.ScheduledDate >= selected.StartDate.Date && p.ScheduledDate < selectedWeekEnd))
            ?? labs.FirstOrDefault();

        var labPlannings = lab is null
            ? []
            : plannings.Where(p => p.AllLaboratories || p.LaboratoryId == lab.Id).ToList();

        var teacherIds = labPlannings.Select(p => p.TeacherPersonId).Distinct().ToList();
        var teachers = teacherIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.Interns.AsNoTracking()
                .Where(i => teacherIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Name);

        var relocations = await LoadRelocationsAsync(labPlannings.Select(p => p.Id).ToList());

        var days = await _context.AcademicDays.AsNoTracking()
            .Where(d => d.AcademicWeek.ManagementId == management.Id && d.Date >= from && d.Date < to)
            .Select(d => new { d.Date, d.Status, d.Reason })
            .ToListAsync();
        var dayByDate = days.GroupBy(d => d.Date.Date).ToDictionary(g => g.Key, g => g.First());

        var blocks = labPlannings
            .Select(p =>
            {
                var subjectName = string.IsNullOrWhiteSpace(p.SubjectName) ? "Sin materia" : p.SubjectName!;
                var startOrder = p.StartOrder is int s && p.PeriodCount is int c && c >= 1 && s >= 1 && s <= maxOrder ? p.StartOrder : null;
                var count = startOrder is int so ? Math.Min(p.PeriodCount!.Value, maxOrder - so + 1) : (p.PeriodCount ?? 1);
                return new GridBlock(
                    p.Id,
                    L3Codes.Format(p.Id, p.Code),
                    lab!.Id,
                    p.ScheduledDate.Date,
                    AcademicWeekGenerator.WeekdayOf(p.ScheduledDate),
                    startOrder,
                    count,
                    Abbreviate(subjectName),
                    subjectName,
                    string.IsNullOrWhiteSpace(p.GroupLabel) ? "—" : p.GroupLabel!,
                    teachers.TryGetValue(p.TeacherPersonId, out var t) ? t : "—",
                    p.PracticeName,
                    p.Status,
                    PlanningStatus.Label(p.Status),
                    PlanningStatusView.Css(p.Status),
                    PlanningStatus.Occupies(p.Status),
                    PlanningStatusView.NextUrl(p.Id, p.Status, p.HasRequirements),
                    PlanningStatusView.TimeText(p.StartOrder, p.PeriodCount, p.ScheduledDate, p.ScheduledEndDate),
                    Relocation: relocations.TryGetValue(p.Id, out var relocation) ? relocation : null);
            })
            .Where(b => b.Weekday >= 1) // los domingos no existen en el calendario académico
            .ToList();

        var gridWeeks = visibleWeeks.Select(w =>
        {
            var weekStart = w.StartDate.Date;
            var weekEnd = weekStart.AddDays(6);
            var weekBlocks = blocks.Where(b => b.Date >= weekStart && b.Date < weekEnd).ToList();

            var gridDays = Enumerable.Range(1, 6).Select(d =>
            {
                var dayDate = weekStart.AddDays(d - 1);
                var exists = dayByDate.TryGetValue(dayDate, out var day);
                var suspended = exists && day!.Status == AcademicDayStatus.Suspended;
                var positioned = weekBlocks
                    .Where(b => b.Date == dayDate && b.StartOrder.HasValue)
                    .OrderBy(b => b.StartOrder).ThenBy(b => b.Occupies ? 1 : 0).ThenBy(b => b.PlanningId)
                    .ToList();
                return new GridDay(d, AcademicWeekGenerator.WeekdayNames[d], dayDate, exists, suspended,
                    suspended ? day!.Reason : null, AssignLanes(positioned));
            }).ToList();

            var unscheduled = weekBlocks
                .Where(b => !b.StartOrder.HasValue)
                .OrderBy(b => b.Date).ThenBy(b => b.PlanningId)
                .ToList();

            return new GridWeek(w.Number, weekStart, w.EndDate.Date, gridDays, unscheduled);
        }).ToList();

        return new LaboratoryGrid(
            management,
            managements,
            ToOptions(labs, countByLab),
            lab,
            periods,
            weeks,
            selected.Number,
            selectedIndex > 0 ? weeks[selectedIndex - 1].Number : null,
            selectedIndex + 1 < weeks.Count ? weeks[selectedIndex + 1].Number : null,
            twoWeeks,
            gridWeeks);
    }

    /// <summary>
    /// Marcas de reprogramación/reposición de los bloques visibles, en una sola consulta a Reschedulings:
    /// el origen enlaza a su destino y el destino a su origen. Si un bloque fuera ambas cosas, prima "nueva ubicación".
    /// </summary>
    private async Task<Dictionary<int, GridRelocation>> LoadRelocationsAsync(List<int> planningIds)
    {
        var result = new Dictionary<int, GridRelocation>();
        if (planningIds.Count == 0) return result;

        var rows = await _context.Reschedulings.AsNoTracking()
            .Where(r => planningIds.Contains(r.OriginPlanningId) || planningIds.Contains(r.DestinationPlanningId))
            .Select(r => new
            {
                r.OriginPlanningId,
                r.DestinationPlanningId,
                DestinationCode = r.DestinationPlanning.Code,
                r.OriginDate,
                r.OriginLaboratoryId,
                r.DestinationDate,
                r.DestinationLaboratoryId,
                r.DayRecoveryId,
                OriginManagementId = r.OriginPlanning.ManagementId,
                DestinationManagementId = r.DestinationPlanning.ManagementId
            })
            .ToListAsync();

        foreach (var r in rows.Where(r => planningIds.Contains(r.DestinationPlanningId)))
        {
            var isRecovery = r.DayRecoveryId.HasValue;
            var text = isRecovery ? $"Reposición del {r.OriginDate:dd/MM}" : $"Reprogramada desde {r.OriginDate:dd/MM}";
            result[r.DestinationPlanningId] = new GridRelocation(GridRelocationKind.MovedFrom, isRecovery, text,
                GridRelocation.GridUrl(r.OriginManagementId, r.OriginLaboratoryId, r.OriginDate));
        }

        foreach (var r in rows.Where(r => planningIds.Contains(r.OriginPlanningId)))
        {
            result[r.OriginPlanningId] = new GridRelocation(GridRelocationKind.MovedTo, r.DayRecoveryId.HasValue,
                $"Ver nueva ubicación: {L3Codes.Format(r.DestinationPlanningId, r.DestinationCode)} {r.DestinationDate:dd/MM}",
                GridRelocation.GridUrl(r.DestinationManagementId, r.DestinationLaboratoryId, r.DestinationDate));
        }

        return result;
    }

    private static List<GridLaboratoryOption> ToOptions(List<GridLaboratory> labs, IReadOnlyDictionary<int, int> counts) =>
        labs.Select(l => new GridLaboratoryOption(l.Id, l.Code, l.Name, counts.TryGetValue(l.Id, out var n) ? n : 0)).ToList();

    private static GridWeekOption SelectWeek(List<GridWeekOption> weeks, int? weekNumber, string? date)
    {
        if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            var byDate = weeks.FirstOrDefault(w => parsed.Date >= w.StartDate.Date && parsed.Date <= w.StartDate.Date.AddDays(6));
            if (byDate is not null) return byDate;
        }

        if (weekNumber.HasValue)
        {
            var byNumber = weeks.FirstOrDefault(w => w.Number == weekNumber.Value);
            if (byNumber is not null) return byNumber;
        }

        var today = DateTime.Today;
        return weeks.FirstOrDefault(w => today >= w.StartDate.Date && today <= w.StartDate.Date.AddDays(6))
            ?? weeks[0];
    }

    /// <summary>
    /// Agrupa los bloques del día que se solapan (clusters transitivos) y asigna a cada uno un carril,
    /// para dibujarlos lado a lado. blocks debe venir ordenado por StartOrder.
    /// </summary>
    private static List<GridBlock> AssignLanes(List<GridBlock> blocks)
    {
        var result = new List<GridBlock>(blocks.Count);
        var cluster = new List<(GridBlock Block, int Lane)>();
        var laneEnds = new List<int>();
        var clusterEnd = 0;

        void Flush()
        {
            var laneCount = Math.Max(1, laneEnds.Count);
            result.AddRange(cluster.Select(c => c.Block with { LaneIndex = c.Lane, LaneCount = laneCount }));
            cluster.Clear();
            laneEnds.Clear();
            clusterEnd = 0;
        }

        foreach (var block in blocks)
        {
            var start = block.StartOrder!.Value;
            if (cluster.Count > 0 && start > clusterEnd) Flush();

            var lane = laneEnds.FindIndex(end => end < start);
            if (lane < 0)
            {
                lane = laneEnds.Count;
                laneEnds.Add(block.EndOrder);
            }
            else
            {
                laneEnds[lane] = block.EndOrder;
            }

            cluster.Add((block, lane));
            clusterEnd = Math.Max(clusterEnd, block.EndOrder);
        }

        if (cluster.Count > 0) Flush();
        return result;
    }

    /// <summary>
    /// Abreviatura de la materia para la grilla: mayúsculas sin tildes, sin palabras vacías,
    /// cada palabra truncada a 5 letras (salvo números romanos y números), máximo 24 caracteres.
    /// </summary>
    public static string Abbreviate(string? subjectName)
    {
        if (string.IsNullOrWhiteSpace(subjectName)) return "SIN MATERIA";

        var normalized = subjectName.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var clean = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            clean.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }

        var words = clean.ToString().Normalize(NormalizationForm.FormC)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !StopWords.Contains(w))
            .Select(w => RomanNumeral.IsMatch(w) || w.All(char.IsDigit) || w.Length <= MaxWordLength
                ? w
                : w[..MaxWordLength])
            .ToList();

        if (words.Count == 0) return "SIN MATERIA";

        var result = string.Join(' ', words);
        return result.Length > MaxSubjectShortLength ? result[..MaxSubjectShortLength].TrimEnd() : result;
    }
}
