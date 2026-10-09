using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>De dónde sale el stock de un artículo.</summary>
public enum StockSource
{
    /// <summary>Sin registro: ni existencias en el origen ni catálogo de equipo vinculado.</summary>
    None,
    /// <summary>Existencias cargadas en el laboratorio de origen (ArticleStocks, Economato por defecto).</summary>
    Inventory,
    /// <summary>Unidades físicas del catálogo de equipo vinculado al artículo.</summary>
    EquipmentUnits
}

/// <summary>Estado operativo derivado de una unidad física (distinto de la condición física).</summary>
public enum UnitOperationalState
{
    Available,
    OnLoan,
    InMaintenance,
    OutOfService
}

/// <summary>
/// Stock de un artículo. Para equipos, <see cref="Available"/> son las unidades disponibles
/// (incluye las "Por confirmar", contadas aparte en <see cref="Unconfirmed"/>).
/// </summary>
public sealed record ArticleStockInfo(
    int ArticleId,
    StockSource Source,
    decimal? Available,
    int TotalUnits = 0,
    int OnLoan = 0,
    int InMaintenance = 0,
    int OutOfService = 0,
    int Unconfirmed = 0)
{
    public static ArticleStockInfo NotRegistered(int articleId) => new(articleId, StockSource.None, null);

    public string SourceText => Source switch
    {
        StockSource.Inventory => "Existencias",
        StockSource.EquipmentUnits => "Unidades físicas",
        _ => "Sin registro"
    };
}

/// <summary>Resultado de comparar la demanda de un artículo en un día con su stock.</summary>
public enum DemandState
{
    /// <summary>La suma del día cabe en el stock.</summary>
    Ok,
    /// <summary>La suma del día supera el stock, pero las prácticas no se cruzan en horario: hay que rotar el material.</summary>
    Rotation,
    /// <summary>En algún momento las prácticas simultáneas piden más que el stock.</summary>
    Conflict,
    /// <summary>No hay stock registrado: no se puede evaluar.</summary>
    NoStock
}

public sealed record WeekPractice(
    int Id,
    string Code,
    DateTime Date,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    string PracticeName,
    string? SubjectName,
    string? GroupName,
    string? LaboratoryCode,
    string Status)
{
    public string TimeText => StartTime.HasValue
        ? $"{StartTime.Value:hh\\:mm}–{(EndTime ?? StartTime.Value):hh\\:mm}"
        : "Sin horario";
}

public sealed record PracticeDemand(int PracticeId, decimal Quantity);

public sealed record ArticleDayDemand(
    DateTime Date,
    int ArticleId,
    string ArticleCode,
    string ArticleName,
    ArticleStockInfo Stock,
    decimal TotalRequested,
    decimal PeakRequested,
    TimeSpan? PeakStart,
    TimeSpan? PeakEnd,
    IReadOnlyList<PracticeDemand> Practices)
{
    public DemandState State => Stock.Available is not decimal available ? DemandState.NoStock
        : PeakRequested > available ? DemandState.Conflict
        : TotalRequested > available ? DemandState.Rotation
        : DemandState.Ok;

    public bool HasIssue => State is DemandState.Conflict or DemandState.Rotation;

    public string PeakText => PeakStart.HasValue
        ? $"{PeakStart.Value:hh\\:mm}–{(PeakEnd ?? PeakStart.Value):hh\\:mm}"
        : "todo el día";
}

public sealed record WeekAvailability(
    DateTime StartDate,
    DateTime EndDate,
    int? WeekNumber,
    IReadOnlyList<WeekPractice> Practices,
    IReadOnlyList<ArticleDayDemand> Demands)
{
    public IEnumerable<DateTime> Days
    {
        get
        {
            for (var d = StartDate.Date; d <= EndDate.Date; d = d.AddDays(1))
                yield return d;
        }
    }

    public IEnumerable<ArticleDayDemand> Issues => Demands.Where(d => d.HasIssue);

    public int ConflictCount => Demands.Count(d => d.State == DemandState.Conflict);
    public int RotationCount => Demands.Count(d => d.State == DemandState.Rotation);

    /// <summary>Peor estado en el que participa cada práctica (solo las que tienen conflicto o rotación).</summary>
    public IReadOnlyDictionary<int, DemandState> PracticeStates =>
        Demands.Where(d => d.HasIssue)
            .SelectMany(d => d.Practices.Select(p => (p.PracticeId, d.State)))
            .GroupBy(x => x.PracticeId)
            .ToDictionary(g => g.Key, g => g.Any(x => x.State == DemandState.Conflict) ? DemandState.Conflict : DemandState.Rotation);
}

/// <summary>Uso de un artículo en un periodo: cuánto se pidió y cuántos días generó rotación o conflicto.</summary>
public sealed record ArticleUsage(
    int ArticleId,
    string ArticleCode,
    string ArticleName,
    decimal TotalRequested,
    int PracticeCount,
    int DaysUsed,
    int RotationDays,
    int ConflictDays,
    decimal? Available)
{
    public int IssueDays => RotationDays + ConflictDays;
}

public interface IArticleAvailabilityService
{
    /// <summary>Ranking de uso por artículo en el periodo (a partir de la demanda diaria).</summary>
    Task<List<ArticleUsage>> GetUsageAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Stock por artículo: equipos vinculados por unidades físicas; el resto por existencias en el laboratorio
    /// indicado o, sin laboratorio (null), el total institucional (Economato + laboratorios).
    /// </summary>
    Task<Dictionary<int, ArticleStockInfo>> GetStockAsync(IEnumerable<int> articleIds, int? originLaboratoryId);

    /// <summary>Demanda por día y artículo de todas las planificaciones vigentes (que ocupan material) entre las fechas, contra el stock (total si no hay origen).</summary>
    Task<WeekAvailability> GetRangeAsync(DateTime startDate, DateTime endDate, int? weekNumber = null, int? originLaboratoryId = null);

    /// <summary>Semana académica de la planificación (o lunes a domingo de su fecha si no tiene semana asignada).</summary>
    Task<WeekAvailability?> GetWeekForPracticeAsync(int practiceId, int? originLaboratoryId = null);
}

public sealed class ArticleAvailabilityService : IArticleAvailabilityService
{
    private static readonly EquipmentStatus[] RetiredStatuses =
        [EquipmentStatus.Decommissioned, EquipmentStatus.Dismantled, EquipmentStatus.Deleted];

    private readonly ApplicationDbContext _context;
    private readonly IL3CircuitService _circuit;

    public ArticleAvailabilityService(ApplicationDbContext context, IL3CircuitService circuit)
    {
        _context = context;
        _circuit = circuit;
    }

    /// <summary>Estado operativo de una unidad a partir de su estado registrado y de los préstamos/mantenimientos abiertos.</summary>
    public static UnitOperationalState ResolveState(EquipmentStatus status, bool hasOpenLoan, bool hasOpenMaintenance)
    {
        if (status == EquipmentStatus.OnLoan || hasOpenLoan) return UnitOperationalState.OnLoan;
        if (status is EquipmentStatus.UnderMaintenance or EquipmentStatus.InRepair || hasOpenMaintenance) return UnitOperationalState.InMaintenance;
        if (status is EquipmentStatus.OutOfService or EquipmentStatus.Broken) return UnitOperationalState.OutOfService;
        return UnitOperationalState.Available;
    }

    public async Task<Dictionary<int, ArticleStockInfo>> GetStockAsync(IEnumerable<int> articleIds, int? originLaboratoryId)
    {
        var ids = articleIds.Distinct().ToList();
        var result = new Dictionary<int, ArticleStockInfo>();
        if (ids.Count == 0) return result;

        var links = await _context.Articles.AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .Select(a => new { a.Id, a.EquipmentId })
            .ToListAsync();

        // Equipos: unidades físicas vigentes del catálogo vinculado.
        var equipmentIds = links.Where(l => l.EquipmentId.HasValue).Select(l => l.EquipmentId!.Value).Distinct().ToList();
        var unitsByEquipment = new Dictionary<int, ArticleStockInfo>();
        if (equipmentIds.Count > 0)
        {
            var units = await _context.EquipmentUnits.AsNoTracking()
                .Where(u => equipmentIds.Contains(u.EquipmentId) && !RetiredStatuses.Contains(u.CurrentStatus))
                .Select(u => new { u.Id, u.EquipmentId, u.CurrentStatus })
                .ToListAsync();
            var unitIds = units.Select(u => u.Id).ToList();

            var openLoanStatuses = new[] { LoanStatus.Active, LoanStatus.Overdue };
            var loanedByHeader = await _context.Departures.AsNoTracking()
                .Where(d => d.EquipmentUnitId != null && unitIds.Contains(d.EquipmentUnitId.Value) && openLoanStatuses.Contains(d.Status))
                .Select(d => d.EquipmentUnitId!.Value)
                .ToListAsync();
            var loanedByItem = await _context.DepartureItems.AsNoTracking()
                .Where(i => i.EquipmentUnitId != null && unitIds.Contains(i.EquipmentUnitId.Value)
                    && i.Departure != null && openLoanStatuses.Contains(i.Departure.Status))
                .Select(i => i.EquipmentUnitId!.Value)
                .ToListAsync();
            var loaned = loanedByHeader.Concat(loanedByItem).ToHashSet();
            var inMaintenance = (await _context.Maintenances.AsNoTracking()
                .Where(m => unitIds.Contains(m.EquipmentUnitId) && m.Status == MaintenanceStatus.InProgress)
                .Select(m => m.EquipmentUnitId)
                .ToListAsync()).ToHashSet();

            foreach (var group in units.GroupBy(u => u.EquipmentId))
            {
                var states = group.Select(u => (u.CurrentStatus, State: ResolveState(u.CurrentStatus, loaned.Contains(u.Id), inMaintenance.Contains(u.Id)))).ToList();
                unitsByEquipment[group.Key] = new ArticleStockInfo(
                    0,
                    StockSource.EquipmentUnits,
                    states.Count(s => s.State == UnitOperationalState.Available),
                    states.Count,
                    states.Count(s => s.State == UnitOperationalState.OnLoan),
                    states.Count(s => s.State == UnitOperationalState.InMaintenance),
                    states.Count(s => s.State == UnitOperationalState.OutOfService),
                    states.Count(s => s.State == UnitOperationalState.Available && s.CurrentStatus == EquipmentStatus.PorConfirmar));
            }
        }

        // Resto: existencias del laboratorio de origen o, sin origen, la suma de todos los laboratorios (D12).
        var inventory = await _context.ArticleStocks.AsNoTracking()
            .Where(s => ids.Contains(s.ArticleId) && (originLaboratoryId == null || s.LaboratoryId == originLaboratoryId))
            .GroupBy(s => s.ArticleId)
            .Select(g => new { ArticleId = g.Key, Quantity = g.Sum(s => s.AvailableQuantity) })
            .ToDictionaryAsync(x => x.ArticleId, x => x.Quantity);

        foreach (var link in links)
        {
            if (link.EquipmentId.HasValue)
            {
                result[link.Id] = unitsByEquipment.TryGetValue(link.EquipmentId.Value, out var info)
                    ? info with { ArticleId = link.Id }
                    : new ArticleStockInfo(link.Id, StockSource.EquipmentUnits, 0);
            }
            else if (inventory.TryGetValue(link.Id, out var quantity))
            {
                result[link.Id] = new ArticleStockInfo(link.Id, StockSource.Inventory, quantity);
            }
            else
            {
                result[link.Id] = ArticleStockInfo.NotRegistered(link.Id);
            }
        }

        return result;
    }

    public async Task<List<ArticleUsage>> GetUsageAsync(DateTime startDate, DateTime endDate)
    {
        var range = await GetRangeAsync(startDate, endDate);
        return range.Demands
            .GroupBy(d => d.ArticleId)
            .Select(g => new ArticleUsage(
                g.Key,
                g.First().ArticleCode,
                g.First().ArticleName,
                g.Sum(d => d.TotalRequested),
                g.SelectMany(d => d.Practices).Select(p => p.PracticeId).Distinct().Count(),
                g.Count(),
                g.Count(d => d.State == DemandState.Rotation),
                g.Count(d => d.State == DemandState.Conflict),
                g.First().Stock.Available))
            .ToList();
    }

    public async Task<WeekAvailability?> GetWeekForPracticeAsync(int practiceId, int? originLaboratoryId = null)
    {
        var practice = await _context.Plannings.AsNoTracking()
            .Where(r => r.Id == practiceId)
            .Select(r => new
            {
                r.ScheduledDate,
                Week = r.AcademicWeekRef != null
                    ? new { r.AcademicWeekRef.Number, r.AcademicWeekRef.StartDate, r.AcademicWeekRef.EndDate }
                    : null
            })
            .FirstOrDefaultAsync();
        if (practice == null) return null;

        if (practice.Week != null)
            return await GetRangeAsync(practice.Week.StartDate, practice.Week.EndDate, practice.Week.Number, originLaboratoryId);

        var date = practice.ScheduledDate.Date;
        var monday = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        return await GetRangeAsync(monday, monday.AddDays(6), null, originLaboratoryId);
    }

    public async Task<WeekAvailability> GetRangeAsync(DateTime startDate, DateTime endDate, int? weekNumber = null, int? originLaboratoryId = null)
    {
        var from = startDate.Date;
        var to = endDate.Date;

        // Los estados no ocupantes (cancelada, reprogramada, reposición) no demandan material.
        var practices = await _context.Plannings.AsNoTracking()
            .Where(r => !r.IsDeleted && !PlanningStatus.NonOccupying.Contains(r.Status)
                && r.ScheduledDate >= from && r.ScheduledDate < to.AddDays(1))
            .OrderBy(r => r.ScheduledDate).ThenBy(r => r.StartTime)
            .Select(r => new
            {
                r.Id,
                r.ScheduledDate,
                r.StartTime,
                r.EndTime,
                r.PracticeName,
                Subject = r.Subject != null ? r.Subject.Name : null,
                Group = r.CourseGroup != null ? r.CourseGroup.Name : r.GroupName,
                Laboratory = r.Laboratory != null ? r.Laboratory.Code : null,
                r.Status
            })
            .ToListAsync();

        var weekPractices = practices
            .Select(p => new WeekPractice(p.Id, Pages.LaboratoryOperations.L3Codes.Format(p.Id), p.ScheduledDate.Date, p.StartTime, p.EndTime,
                p.PracticeName, p.Subject, p.Group, p.Laboratory, p.Status))
            .ToList();
        var practiceById = weekPractices.ToDictionary(p => p.Id);
        var practiceIds = practiceById.Keys.ToList();

        var lines = practiceIds.Count == 0
            ? []
            : await _context.PlanningRequirements.AsNoTracking()
                .Where(d => practiceIds.Contains(d.PlanningId) && d.RequestedQuantity > 0)
                .Select(d => new { d.PlanningId, d.ArticleId, d.Article!.Code, d.Article.Name, Quantity = d.RequestedQuantity!.Value })
                .ToListAsync();

        var stock = await GetStockAsync(lines.Select(l => l.ArticleId), originLaboratoryId);

        var demands = lines
            .GroupBy(l => new { Date = practiceById[l.PlanningId].Date, l.ArticleId })
            .Select(g =>
            {
                // Varias líneas del mismo artículo en una planificación se suman.
                var perPractice = g.GroupBy(l => l.PlanningId)
                    .Select(p => new PracticeDemand(p.Key, p.Sum(l => l.Quantity)))
                    .ToList();
                var (peak, peakStart, peakEnd) = ComputePeak(perPractice, practiceById);
                var first = g.First();
                return new ArticleDayDemand(g.Key.Date, g.Key.ArticleId, first.Code, first.Name,
                    stock.TryGetValue(g.Key.ArticleId, out var info) ? info : ArticleStockInfo.NotRegistered(g.Key.ArticleId),
                    perPractice.Sum(p => p.Quantity), peak, peakStart, peakEnd, perPractice);
            })
            .OrderBy(d => d.Date)
            .ThenByDescending(d => d.State == DemandState.Conflict)
            .ThenByDescending(d => d.State == DemandState.Rotation)
            .ThenBy(d => d.ArticleName)
            .ToList();

        return new WeekAvailability(from, to, weekNumber, weekPractices, demands);
    }

    /// <summary>Pico de demanda simultánea de las prácticas del día (ver <see cref="DemandPeak.Compute"/>).</summary>
    private static (decimal Peak, TimeSpan? Start, TimeSpan? End) ComputePeak(
        IReadOnlyList<PracticeDemand> demands, IReadOnlyDictionary<int, WeekPractice> practices)
        => DemandPeak.Compute(demands
            .Select(d => new TimedDemand(d.Quantity, practices[d.PracticeId].StartTime, practices[d.PracticeId].EndTime))
            .ToList());
}
