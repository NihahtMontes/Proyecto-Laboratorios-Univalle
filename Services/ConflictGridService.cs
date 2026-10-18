using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Nombre, descripción y presentación de un tipo de conflicto de material (leyenda, chips y panel).</summary>
public sealed record ConflictTypeInfo(ConflictKind Kind, string Label, string Description, string Css, string Icon);

/// <summary>Fuente única de nombre/color/descripción por tipo de conflicto; la vista solo itera <see cref="All"/>.</summary>
public static class ConflictTypes
{
    /// <summary>Catálogo de tipos con conflicto, en orden de gravedad: solapamiento, escasez, rotación y sin stock.</summary>
    public static IReadOnlyList<ConflictTypeInfo> All { get; } =
    [
        new(ConflictKind.Overlap, "Conflicto de horario", "Varias prácticas a la vez piden más que el stock.", "overlap", "fa-exclamation-circle"),
        new(ConflictKind.Shortage, "Stock insuficiente", "Una práctica sola pide más que el stock.", "shortage", "fa-battery-quarter"),
        new(ConflictKind.Rotation, "Rotación", "Alcanza si el material pasa de una práctica a la siguiente.", "rotation", "fa-sync-alt"),
        new(ConflictKind.NoStock, "Sin stock registrado", "El artículo no tiene existencias registradas.", "nostock", "fa-box-open")
    ];

    /// <summary>Ficha de un tipo; "sin conflicto" cubre los estados ok.</summary>
    public static ConflictTypeInfo Of(ConflictKind kind) => kind switch
    {
        ConflictKind.Overlap => All[0],
        ConflictKind.Shortage => All[1],
        ConflictKind.Rotation => All[2],
        ConflictKind.NoStock => All[3],
        _ => new(ConflictKind.None, "Sin conflicto", "La demanda cabe en el stock.", "ok", "fa-check-circle")
    };
}

/// <summary>Ámbito de una acción del panel: sobre el conflicto, sobre una práctica o sobre el día.</summary>
public enum ConflictActionScope { Conflict, Practice, Day }

/// <summary>
/// Acción para resolver un conflicto. Key estable ("stock", "unit", "reschedule", "reduce", "materials", "grid",
/// "recover-day"): la vista solo itera y se pueden agregar acciones nuevas sin cambiarla.
/// Url null = acción de cliente (modal o form de la página, p. ej. "reduce" y "recover-day").
/// </summary>
public sealed record ConflictAction(string Key, string Label, string Icon, ConflictActionScope Scope, string? Url,
    bool Enabled, string? DisabledReason, string Style);

public sealed record ConflictUserRights(bool IsAdmin, bool CanManage, bool IsSuperAdmin);

/// <param name="SelDate">Fecha del conflicto elegido (yyyy-MM-dd), con <see cref="ArticleId"/> arma el panel de detalle.</param>
public sealed record ConflictGridQuery(int? ManagementId, int? Week, string? Date, string? SelDate, int? ArticleId, ConflictKind? Kind);

/// <summary>Conflicto de material de un artículo en un día. Key = "yyyy-MM-dd|articleId"; Tag = "C1", "C2"…</summary>
public sealed record ConflictItem(string Key, string Tag, ConflictKind Kind, DateTime Date, int ArticleId, string ArticleCode,
    string ArticleName, decimal? Available, decimal TotalRequested, decimal PeakRequested, decimal Shortage, string PeakText,
    IReadOnlyList<int> InvolvedPracticeIds, IReadOnlyList<int> PeriodOrders);

/// <summary>Marca de un conflicto en la celda (día × periodo) de la grilla: carga pedida contra el stock disponible.</summary>
public sealed record ConflictCellMark(string Key, string Tag, ConflictKind Kind, string ArticleName, decimal Load, decimal? Available);

/// <param name="Cells">Marcas del día por orden del periodo.</param>
public sealed record ConflictDay(DateTime Date, int Weekday, string Name, bool InManagement, bool IsSuspended, string? Reason,
    int PracticeCount, IReadOnlyDictionary<int, IReadOnlyList<ConflictCellMark>> Cells, IReadOnlyList<ConflictAction> Actions);

/// <summary>Carga del día en un periodo: pico pedido en la franja y si supera el stock.</summary>
public sealed record ConflictPeriodLoad(int Order, string Label, decimal Load, bool Over);

/// <summary>Fila de práctica del panel de detalle, con sus acciones (reprogramar, reducir, materiales, grilla).</summary>
public sealed record ConflictPracticeRow(WeekPractice Practice, decimal Quantity, bool Involved, bool SuggestedMove,
    IReadOnlyList<int> PeriodOrders, int ManagementId, int? LaboratoryId, string StatusLabel, decimal? ReducibleQuantity,
    IReadOnlyList<ConflictAction> Actions);

/// <summary>Panel de detalle de un conflicto: demanda, consejo, stock, cargas por periodo y prácticas con acciones.</summary>
public sealed record ConflictPanel(ConflictItem Item, ConflictTypeInfo Type, ConflictAdvice Advice, ArticleStockInfo Stock,
    IReadOnlyList<ConflictPeriodLoad> Loads, IReadOnlyList<ConflictPracticeRow> Practices, IReadOnlyList<ConflictAction> Actions);

/// <summary>Grilla semanal de conflictos de material (periodos × días), con los avisos de la semana y el panel elegido.</summary>
public sealed record ConflictWeekGrid(
    AcademicManagementSummary Management,
    IReadOnlyList<AcademicManagementSummary> Managements,
    IReadOnlyList<GridWeekOption> WeekOptions,
    int? SelectedWeekNumber,
    int? PreviousWeekNumber,
    int? NextWeekNumber,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<GridPeriod> Periods,
    IReadOnlyList<ConflictDay> Days,
    IReadOnlyList<ConflictItem> Items,
    IReadOnlyDictionary<ConflictKind, int> Counts,
    ConflictPanel? Selected);

/// <summary>Etiqueta de conflicto que lleva una planificación (misma Tag/Key que la grilla de conflictos de esa semana).</summary>
public sealed record PlanningConflictTag(string Key, string Tag, ConflictKind Kind, DateTime Date, int ArticleId,
    string ArticleName, bool Involved, IReadOnlyList<int> RelatedPlanningIds, string Url);

public interface IConflictGridService
{
    /// <summary>
    /// Arma la grilla semanal de conflictos de material de una gestión (compartida entre laboratorios).
    /// Devuelve null solo si no hay gestión pedida ni actual.
    /// </summary>
    Task<ConflictWeekGrid?> BuildAsync(ConflictGridQuery query, ConflictUserRights rights, string currentUrl, CancellationToken ct = default);

    /// <summary>
    /// Etiquetas por planificación para las semanas que empiezan en los lunes dados (clave = PlanningId).
    /// Solo incluye planificaciones de esos conflictos (las de InvolvedPracticeIds, y las de Practices con Involved=false).
    /// </summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<PlanningConflictTag>>> GetPlanningTagsAsync(IEnumerable<DateTime> weekMondays, CancellationToken ct = default);
}

public sealed class ConflictGridService : IConflictGridService
{
    private static readonly ConflictKind[] PrioridadTipo = [ConflictKind.Overlap, ConflictKind.Shortage, ConflictKind.Rotation, ConflictKind.NoStock];

    private readonly ApplicationDbContext _context;
    private readonly IArticleAvailabilityService _availability;
    private readonly IAcademicManagementService _managements;

    public ConflictGridService(ApplicationDbContext context, IArticleAvailabilityService availability, IAcademicManagementService managements)
    {
        _context = context;
        _availability = availability;
        _managements = managements;
    }

    public async Task<ConflictWeekGrid?> BuildAsync(ConflictGridQuery query, ConflictUserRights rights, string currentUrl, CancellationToken ct = default)
    {
        var management = await _managements.ResolveAsync(query.ManagementId);
        if (management is null) return null;
        var managements = await _managements.ListAsync();

        // Semanas de la gestión (igual que la grilla por laboratorio).
        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Where(w => w.ManagementId == management.Id)
            .OrderBy(w => w.Number)
            .Select(w => new GridWeekOption(w.Number, w.StartDate, w.EndDate))
            .ToListAsync(ct);

        if (weeks.Count == 0)
        {
            // Sin calendario académico no hay semana que mostrar; la pantalla muestra su estado vacío.
            return new ConflictWeekGrid(management, managements, [], null, null, null,
                DateTime.Today, DateTime.Today, (await LoadPeriodsAsync(ct)).Select(p => p.Period).ToList(), [], [],
                new Dictionary<ConflictKind, int>(), null);
        }

        // Semana elegida: SelDate → Date → número → la que contiene hoy → la primera (como la grilla).
        var selected = SelectWeek(weeks, query.Week, query.SelDate, query.Date);
        var selectedIndex = weeks.IndexOf(selected);
        var monday = selected.StartDate.Date;

        // Periodos, disponibilidad y avisos de la semana: cálculo común con GetPlanningTagsAsync.
        var (periods, availability, builders) = await ComputeWeekAsync(monday, selected.Number, ct);

        // Días académicos de la semana: suspensión y motivo (igual que la grilla).
        var academicDays = (await _context.AcademicDays.AsNoTracking()
                .Where(d => d.AcademicWeek.ManagementId == management.Id && d.Date >= monday && d.Date < monday.AddDays(6))
                .Select(d => new { d.Date, d.Status, d.Reason })
                .ToListAsync(ct))
            .GroupBy(d => d.Date.Date)
            .ToDictionary(g => g.Key, g => g.First());

        var practicesByDate = availability.Practices
            .GroupBy(p => p.Date.Date)
            .ToDictionary(g => g.Key, g => g.Count());
        var practiceById = availability.Practices.ToDictionary(p => p.Id);

        var demandByKey = builders.ToDictionary(b => b.Item.Key, b => b.Demand);
        var items = builders.Select(b => b.Item).ToList();

        // Los conteos son de la semana completa: no cambian con el filtro de tipo.
        var counts = items.GroupBy(i => i.Kind)
            .ToDictionary(g => g.Key, g => g.Count());

        // Con filtro de tipo, Items y Cells se limitan a ese tipo; los Tags ya quedaron calculados.
        var visibles = query.Kind.HasValue ? items.Where(i => i.Kind == query.Kind.Value).ToList() : items;

        var cellsByDay = visibles
            .SelectMany(i => i.PeriodOrders.Select(order => (i.Date.Date, order,
                Mark: new ConflictCellMark(i.Key, i.Tag, i.Kind, i.ArticleName,
                    LoadOfPeriod(demandByKey[i.Key], order, periods), i.Available))))
            .GroupBy(x => x.Date)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<int, IReadOnlyList<ConflictCellMark>>)g
                    .GroupBy(x => x.order)
                    .ToDictionary(o => o.Key, o => (IReadOnlyList<ConflictCellMark>)o.Select(x => x.Mark).ToList()));

        var days = Enumerable.Range(1, 6).Select(d =>
        {
            var date = monday.AddDays(d - 1);
            var exists = academicDays.TryGetValue(date, out var day);
            var suspended = exists && day!.Status == Models.Enums.AcademicDayStatus.Suspended;
            var practiceCount = practicesByDate.GetValueOrDefault(date);
            var canRecover = rights.CanManage && exists && !suspended && practiceCount > 0;
            string? recoverReason = canRecover ? null
                : !rights.CanManage ? "No tiene permiso para reponer días"
                : !exists ? "El día no pertenece a la gestión"
                : suspended ? "El día ya está suspendido"
                : "No hay prácticas que reponer";
            var actions = new List<ConflictAction>
            {
                new("recover-day", "Reponer día", "fa-calendar-times", ConflictActionScope.Day, null, canRecover,
                    recoverReason, "btn-outline-danger")
            };
            return new ConflictDay(date, d, AcademicWeekGenerator.WeekdayNames[d], exists, suspended,
                suspended ? day!.Reason : null, practiceCount,
                cellsByDay.TryGetValue(date, out var cells)
                    ? cells
                    : new Dictionary<int, IReadOnlyList<ConflictCellMark>>(),
                actions);
        }).ToList();

        // Panel de detalle: SelDate + ArticleId de un aviso (aunque esté fuera del filtro activo).
        ConflictPanel? panel = null;
        if (DateOnly(query.SelDate) is { } selDate && query.ArticleId.HasValue)
        {
            var item = items.FirstOrDefault(i => i.Date.Date == selDate && i.ArticleId == query.ArticleId.Value);
            if (item is not null)
                panel = await BuildPanelAsync(item, demandByKey[item.Key], practiceById, periods, rights, currentUrl, ct);
        }

        return new ConflictWeekGrid(
            management,
            managements,
            weeks,
            selected.Number,
            selectedIndex > 0 ? weeks[selectedIndex - 1].Number : null,
            selectedIndex + 1 < weeks.Count ? weeks[selectedIndex + 1].Number : null,
            monday,
            selected.EndDate.Date,
            periods.Select(p => p.Period).ToList(),
            days,
            visibles,
            counts,
            panel);
    }

    // ==================== Etiquetas por planificación ====================

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, IReadOnlyList<PlanningConflictTag>>> GetPlanningTagsAsync(
        IEnumerable<DateTime> weekMondays, CancellationToken ct = default)
    {
        // Lunes normalizados y sin duplicados: todas las fechas de una misma semana comparten cálculo.
        var lunes = (weekMondays ?? [])
            .Where(d => d != default)
            .Select(d => d.Date.AddDays(-(((int)d.Date.DayOfWeek + 6) % 7)))
            .Distinct()
            .OrderBy(d => d)
            .ToList();
        if (lunes.Count == 0) return new Dictionary<int, IReadOnlyList<PlanningConflictTag>>();

        var tagsByPlanning = new Dictionary<int, List<PlanningConflictTag>>();
        foreach (var monday in lunes)
        {
            var (_, _, builders) = await ComputeWeekAsync(monday, null, ct);
            foreach (var builder in builders)
            {
                var item = builder.Item;
                var involved = builder.Demand.InvolvedPracticeIds.ToHashSet();
                var url = $"/LaboratoryOperations/Conflictos?selDate={item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&articleId={item.ArticleId}";
                foreach (var practice in builder.Demand.Practices)
                {
                    // Pide el artículo del conflicto: involucrada si cruza en el tramo sin stock; si no, etiqueta "floja".
                    var tag = new PlanningConflictTag(
                        item.Key, item.Tag, item.Kind, item.Date, item.ArticleId, item.ArticleName,
                        involved.Contains(practice.PracticeId),
                        involved.Where(id => id != practice.PracticeId).ToList(),
                        url);
                    if (!tagsByPlanning.TryGetValue(practice.PracticeId, out var tags))
                        tagsByPlanning[practice.PracticeId] = tags = [];
                    tags.Add(tag);
                }
            }
        }

        return tagsByPlanning.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<PlanningConflictTag>)kv.Value
                .OrderBy(t => TipoIndex(t.Kind))
                .ThenBy(t => int.TryParse(t.Tag.AsSpan(1), out var n) ? n : int.MaxValue)
                .ToList());
    }

    // ==================== Semana común (grilla y etiquetas) ====================

    /// <summary>Datos comunes de una semana: periodos, disponibilidad y avisos con Tag secuencial.</summary>
    private sealed record WeekComputation(
        List<(GridPeriod Period, TimeSpan Start, TimeSpan End)> Periods,
        WeekAvailability Availability,
        List<ItemBuilder> Builders);

    private async Task<List<(GridPeriod Period, TimeSpan Start, TimeSpan End)>> LoadPeriodsAsync(CancellationToken ct) =>
        (await _context.ClassPeriods.AsNoTracking()
                .OrderBy(p => p.Order)
                .Select(p => new { p.Id, p.Order, p.StartTime, p.EndTime })
                .ToListAsync(ct))
            .Select(p => (Period: new GridPeriod(p.Id, p.Order, $"{p.StartTime:hh\\:mm} – {p.EndTime:hh\\:mm}"), p.StartTime, p.EndTime))
            .ToList();

    /// <summary>Periodos, disponibilidad de la semana (sin laboratorio ni gestión: el material es compartido) y avisos.</summary>
    private async Task<WeekComputation> ComputeWeekAsync(DateTime monday, int? weekNumber, CancellationToken ct)
    {
        var periods = await LoadPeriodsAsync(ct);
        var availability = await _availability.GetRangeAsync(monday, monday.AddDays(6), weekNumber);

        // Avisos con sus marcas por periodo y su orden estable (fecha, primer periodo, tipo, nombre).
        var builders = BuildBuilders(availability, periods);

        return new WeekComputation(periods, availability, builders);
    }

    // ==================== Avisos y marcas ====================

    private sealed record ItemBuilder(ConflictItem Item, ArticleDayDemand Demand);

    /// <summary>Datos de una planificación proyectados para el panel (gestión, ambiente y estado).</summary>
    private sealed record PlanningRow(int Id, int ManagementId, int? LaboratoryId, string Status);

    /// <summary>Avisos de la semana con Tag secuencial, orden estable y sus periodos marcados.</summary>
    private static List<ItemBuilder> BuildBuilders(WeekAvailability availability, List<(GridPeriod Period, TimeSpan Start, TimeSpan End)> periods)
    {
        var builders = availability.Demands.Where(d => d.IsNotice)
            .Select(d =>
            {
                // Solapamiento/escasez marcan los periodos del tramo que supera el stock; rotación/sin stock, todos los ocupados.
                var marcas = d.Kind is ConflictKind.Overlap or ConflictKind.Shortage ? d.OverSegments : d.Segments;
                var orders = periods
                    .Where(p => marcas.Any(s => p.Start < s.End && p.End > s.Start))
                    .Select(p => p.Period.Order)
                    .OrderBy(o => o)
                    .ToList();
                return new ItemBuilder(new ConflictItem(
                    Key(d.Date, d.ArticleId), string.Empty, d.Kind, d.Date, d.ArticleId, d.ArticleCode, d.ArticleName,
                    d.Stock.Available, d.TotalRequested, d.PeakRequested, d.Shortage, d.PeakText,
                    d.InvolvedPracticeIds, orders), d);
            })
            .OrderBy(b => b.Item.Date)
            .ThenBy(b => b.Item.PeriodOrders.Count > 0 ? b.Item.PeriodOrders[0] : int.MaxValue)
            .ThenBy(b => TipoIndex(b.Item.Kind))
            .ThenBy(b => b.Item.ArticleName)
            .ToList();

        return builders.Select((b, i) => b with { Item = b.Item with { Tag = $"C{i + 1}" } }).ToList();
    }

    private static int TipoIndex(ConflictKind kind) =>
        Array.IndexOf(PrioridadTipo, kind) is var i && i >= 0 ? i : PrioridadTipo.Length;

    private static string Key(DateTime date, int articleId) => $"{date:yyyy-MM-dd}|{articleId}";

    /// <summary>Carga máxima de la demanda en un periodo (0 si ninguna franja lo cubre).</summary>
    private static decimal LoadOfPeriod(ArticleDayDemand demand, int order, List<(GridPeriod Period, TimeSpan Start, TimeSpan End)> periods)
    {
        var periodo = periods.FirstOrDefault(p => p.Period.Order == order);
        if (periodo == default) return 0;
        return demand.Segments.Where(s => periodo.Start < s.End && periodo.End > s.Start)
            .Select(s => s.Load)
            .DefaultIfEmpty(0)
            .Max();
    }

    // ==================== Panel de detalle ====================

    private async Task<ConflictPanel> BuildPanelAsync(ConflictItem item, ArticleDayDemand demand,
        IReadOnlyDictionary<int, WeekPractice> practiceById, List<(GridPeriod Period, TimeSpan Start, TimeSpan End)> periods,
        ConflictUserRights rights, string currentUrl, CancellationToken ct)
    {
        // Prácticas que piden el artículo ese día, ordenadas por horario y luego por Id.
        var practices = demand.Practices
            .Select(p => practiceById[p.PracticeId])
            .OrderBy(p => p.StartTime)
            .ThenBy(p => p.Id)
            .ToList();
        var advice = ConflictAdvisor.Advise(demand, practices);

        // Cargas por periodo, del primero al último con demanda (inclusive); Over las que superan el stock.
        var conCarga = periods
            .Select(p => (p.Period, Load: LoadOfPeriod(demand, p.Period.Order, periods)))
            .ToList();
        var conDemanda = conCarga.Select((x, i) => (x.Load, i)).Where(x => x.Load > 0).Select(x => x.i).ToList();
        var loads = conDemanda.Count == 0
            ? []
            : conCarga.GetRange(conDemanda[0], conDemanda[^1] - conDemanda[0] + 1)
                .Select(x => new ConflictPeriodLoad(x.Period.Order, x.Period.Label, x.Load,
                    demand.Stock.Available is decimal disponible && x.Load > disponible))
                .ToList();

        // Datos reales de las planificaciones del día (gestión, ambiente y estado).
        var ids = demand.Practices.Select(p => p.PracticeId).ToList();
        var rows = await _context.Plannings.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new PlanningRow(p.Id, p.ManagementId, p.LaboratoryId, p.Status))
            .ToListAsync(ct);
        var rowById = rows.ToDictionary(r => r.Id);

        var involved = demand.InvolvedPracticeIds.ToHashSet();
        var suggested = advice.SuggestedMoveIds.ToHashSet();
        var dateText = item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var escaped = Uri.EscapeDataString(currentUrl);

        var practiceRows = new List<ConflictPracticeRow>(practices.Count);
        foreach (var practice in practices)
        {
            var quantity = demand.Practices.Single(p => p.PracticeId == practice.Id).Quantity;
            var row = rowById.GetValueOrDefault(practice.Id);
            var orders = !practice.StartTime.HasValue
                ? periods.Select(p => p.Period.Order).ToList()
                : periods.Where(p => practice.StartTime < p.End && (practice.EndTime ?? practice.StartTime) > p.Start)
                    .Select(p => p.Period.Order).ToList();

            var (reducible, reduceReason) = await ReducibleAsync(practice.Id, row, demand.ArticleId, ct);
            practiceRows.Add(new ConflictPracticeRow(
                practice,
                quantity,
                involved.Contains(practice.Id),
                suggested.Contains(practice.Id),
                orders,
                row?.ManagementId ?? 0,
                row?.LaboratoryId,
                PlanningStatus.Label(row?.Status ?? practice.Status),
                reducible,
                PracticeActions(practice, row, dateText, escaped, rights, reducible, reduceReason)));
        }

        return new ConflictPanel(item, ConflictTypes.Of(demand.Kind), advice, demand.Stock, loads, practiceRows,
            [await ConflictActionAsync(demand, rights, currentUrl, escaped, ct)]);
    }

    /// <summary>Cantidad reducible: exactamente una línea del artículo sin unidad física, en planificación Planificada y gestión escribible.</summary>
    private async Task<(decimal? Quantity, string? Reason)> ReducibleAsync(int planningId, PlanningRow? row, int articleId, CancellationToken ct)
    {
        if (row is null || row.Status != PlanningStatus.Planned) return (null, "La planificación ya fue aprobada");
        if (!await PlanningContextLoader.IsWritableAsync(_managements, planningId)) return (null, "Gestión de solo lectura");

        var lines = await _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.PlanningId == planningId && d.ArticleId == articleId && d.EquipmentUnitId == null)
            .Select(d => d.RequestedQuantity)
            .ToListAsync(ct);
        if (lines.Count == 1 && lines[0] is decimal cantidad) return (cantidad, null);
        return (null, "Tiene varias líneas o unidades físicas: edítalo en Materiales");
    }

    /// <summary>Acciones por práctica, siempre en este orden: reprogramar, reducir, materiales y ver en grilla.</summary>
    private static List<ConflictAction> PracticeActions(WeekPractice practice, PlanningRow? row,
        string dateText, string escaped, ConflictUserRights rights, decimal? reducible, string? reduceReason)
    {
        var labPart = row?.LaboratoryId is int labId ? $"&laboratoryId={labId}" : string.Empty;
        var hasSchedule = practice.StartTime.HasValue;
        var reschedulable = row != null && GridRelocation.IsReschedulableStatus(row.Status);
        var rescheduleEnabled = rights.IsSuperAdmin && hasSchedule && reschedulable;
        string? rescheduleReason = rescheduleEnabled ? null
            : !rights.IsSuperAdmin ? "Solo SuperAdmin puede reprogramar"
            : "Estado no reprogramable";

        var reduceEnabled = reducible.HasValue && rights.CanManage;
        string? reduceDisabled = reduceEnabled ? null
            : reducible.HasValue ? "No tiene permiso para modificar cantidades"
            : reduceReason;

        return
        [
            new("reschedule", "Reprogramar", "fa-random", ConflictActionScope.Practice,
                $"/LaboratoryOperations/Grilla?managementId={row?.ManagementId}{labPart}&date={dateText}&reprogramar={practice.Id}&returnUrl={escaped}",
                rescheduleEnabled, rescheduleReason, "btn-outline-primary"),
            new("reduce", "Reducir cantidad", "fa-minus-circle", ConflictActionScope.Practice, null, reduceEnabled,
                reduceDisabled, "btn-outline-warning"),
            new("materials", "Materiales", "fa-clipboard-list", ConflictActionScope.Practice,
                $"/LaboratoryOperations/Requerimientos?planId={practice.Id}&returnUrl={escaped}", true, null, "btn-outline-secondary"),
            new("grid", "Ver en grilla", "mdi mdi-table-large", ConflictActionScope.Practice,
                $"/LaboratoryOperations/Grilla?managementId={row?.ManagementId}{labPart}&date={dateText}&resaltar={practice.Id}",
                true, null, "btn-outline-secondary")
        ];
    }

    /// <summary>Acción sobre las existencias del conflicto: alta de unidad (equipos) o pantalla de existencias (inventario).</summary>
    private async Task<ConflictAction> ConflictActionAsync(ArticleDayDemand demand, ConflictUserRights rights,
        string currentUrl, string escaped, CancellationToken ct)
    {
        var style = demand.Kind == ConflictKind.Rotation ? "btn-outline-primary" : "btn-danger";
        if (demand.Stock.Source == StockSource.EquipmentUnits)
        {
            var equipmentId = await _context.Articles.AsNoTracking()
                .Where(a => a.Id == demand.ArticleId)
                .Select(a => a.EquipmentId)
                .FirstOrDefaultAsync(ct);
            return new ConflictAction("unit", "Registrar unidad", "fa-boxes", ConflictActionScope.Conflict,
                equipmentId.HasValue ? $"/EquipmentUnits/Create?equipmentId={equipmentId.Value}&returnUrl={escaped}" : null,
                rights.CanManage,
                rights.CanManage ? null : "No tiene permiso para registrar unidades",
                style);
        }

        var firstPractice = demand.Practices.Select(p => p.PracticeId).FirstOrDefault();
        var laboratoryId = await PlanningStockGate.StockLaboratoryIdAsync(_context, firstPractice);
        var url = laboratoryId.HasValue ? PlanningStockGate.StockEditUrl(laboratoryId.Value, demand.ArticleId, currentUrl) : null;
        var canEdit = rights.IsAdmin && url != null;
        return new ConflictAction("stock", "Actualizar stock", "fa-boxes", ConflictActionScope.Conflict, url, canEdit,
            canEdit ? null : rights.IsAdmin ? "No se encontró el laboratorio de existencias" : "Solo un administrador actualiza existencias",
            style);
    }

    // ==================== Utilidades ====================

    /// <summary>Fecha yyyy-MM-dd o null si el texto no es válido.</summary>
    private static DateTime? DateOnly(string? text) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null;

    /// <summary>Semana elegida: fecha → número → la que contiene hoy → la primera (misma regla que la grilla).</summary>
    private static GridWeekOption SelectWeek(List<GridWeekOption> weeks, int? weekNumber, string? selDate, string? date)
    {
        var porFecha = DateOnly(selDate) ?? DateOnly(date);
        if (porFecha is { } parsed)
        {
            var porDia = weeks.FirstOrDefault(w => parsed >= w.StartDate.Date && parsed <= w.StartDate.Date.AddDays(6));
            if (porDia is not null) return porDia;
        }

        if (weekNumber.HasValue)
        {
            var porNumero = weeks.FirstOrDefault(w => w.Number == weekNumber.Value);
            if (porNumero is not null) return porNumero;
        }

        var today = DateTime.Today;
        return weeks.FirstOrDefault(w => today >= w.StartDate.Date && today <= w.StartDate.Date.AddDays(6))
            ?? weeks[0];
    }
}
