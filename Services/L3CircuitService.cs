using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Resultado de una operación del circuito L-3 (sin excepciones hacia la vista).</summary>
public sealed record L3CircuitResult(bool Ok, string Message, int? EntityId = null, IReadOnlyList<string>? Warnings = null)
{
    public static L3CircuitResult Fail(string message) => new(false, message);
}

public sealed record DepartureLine(int DetailId, decimal Quantity);
public sealed record ReturnLine(int DepartureItemId, decimal Quantity);

/// <summary>Ítem de una salida atribuido a su artículo (null si la unidad física no tiene artículo vinculado).</summary>
public sealed record L3ReturnItemLine(int ItemId, int? ArticleId, string Name, decimal Delivered, decimal Returned);

/// <summary>
/// Reglas puras de la devolución L-3: el pendiente por artículo descuenta lo cubierto por incidentes L-5
/// (BUG: sin este descuento se podía devolver y reponer stock de material declarado perdido/roto) y la salida
/// cierra cuando por artículo devuelto + cubierto >= entregado (sin artículo: todo devuelto).
/// </summary>
public static class L3ReturnRules
{
    /// <summary>Resultado de evaluar una devolución: mensaje de error (null si pasa) y si la salida queda cerrada.</summary>
    public sealed record ReturnOutcome(string? Error, bool FullyReturned);

    public static ReturnOutcome Evaluate(
        IReadOnlyList<L3ReturnItemLine> items,
        IReadOnlyDictionary<int, int> coveredByArticle,
        IReadOnlyList<ReturnLine> wanted)
    {
        var byItem = items.ToDictionary(i => i.ItemId);
        var lines = wanted.Where(l => l.Quantity > 0).ToList();

        // Por ítem: la cantidad devuelta no supera lo entregado menos lo ya devuelto.
        foreach (var line in lines)
        {
            var item = byItem[line.DepartureItemId];
            var pending = item.Delivered - item.Returned;
            if (line.Quantity > pending)
                return new ReturnOutcome($"{item.Name}: se devolvería {line.Quantity:0.##} y solo quedan {pending:0.##} pendientes.", false);
        }

        // Por artículo: lo pedido no supera el pendiente sin cubrir por incidentes L-5 de la planificación.
        var returningByItem = lines.ToDictionary(l => l.DepartureItemId, l => l.Quantity);
        foreach (var group in lines.GroupBy(l => byItem[l.DepartureItemId].ArticleId))
        {
            if (group.Key is not int articleId) continue;
            var pending = items.Where(i => i.ArticleId == articleId).Sum(i => Math.Max(0, i.Delivered - i.Returned));
            var covered = Math.Min(coveredByArticle.GetValueOrDefault(articleId), pending);
            var uncovered = Math.Max(0, pending - covered);
            var total = group.Sum(l => l.Quantity);
            if (total > uncovered)
                return new ReturnOutcome(
                    $"{byItem[group.First().DepartureItemId].Name}: se devolvería {total:0.##} y solo quedan {uncovered:0.##} sin cubrir (cubierto por incidentes: {covered:0.##}).",
                    false);
        }

        // Cierre por artículo: devuelto + cubierto L-5 >= entregado; ítems sin artículo atribuido: todo devuelto.
        // Así un origen reprogramado (no finalizable) también cierra su salida al quedar todo devuelto o cubierto.
        var fullyReturned = items
            .GroupBy(i => i.ArticleId)
            .All(g =>
            {
                var delivered = g.Sum(i => i.Delivered);
                var returned = g.Sum(i => i.Returned + returningByItem.GetValueOrDefault(i.ItemId));
                return g.Key is int articleId
                    ? returned + coveredByArticle.GetValueOrDefault(articleId) >= delivered
                    : returned >= delivered;
            });
        return new ReturnOutcome(null, fullyReturned);
    }
}

/// <summary>
/// Reglas del modelo de stock por horario (Sprint 3): la existencia de un artículo reutilizable no cambia al
/// entregarlo ni al devolverlo; lo entregado y aún no devuelto solo cuenta como "fuera ahora".
/// </summary>
public static class StockRules
{
    /// <summary>Fila pendiente de devolver: ítem de artículo reutilizable en una salida abierta, con su origen real.</summary>
    private sealed record PendingRow(int OriginLaboratoryId, int ArticleId, int? PlanningId, DateTime? PracticeEnd, decimal Pending);

    /// <summary>
    /// Ítems de artículos REUTILIZABLES en salidas abiertas con lo que falta devolver. El origen de cada ítem es el suyo
    /// (salidas desde varios laboratorios) o, si no tiene, el de la salida. Los consumibles se excluyen: se gastan al
    /// entregarse (su existencia ya bajó) y nunca quedan "fuera".
    /// </summary>
    private static async Task<List<PendingRow>> PendingRowsAsync(ApplicationDbContext context, IReadOnlyCollection<int> articleIds,
        int? originLaboratoryId)
    {
        if (articleIds.Count == 0) return [];
        var open = new[] { LoanStatus.Active, LoanStatus.Overdue };
        var query = context.DepartureItems.AsNoTracking()
            .Where(i => i.ArticleId != null && articleIds.Contains(i.ArticleId.Value) && i.EquipmentUnitId == null
                && i.Departure != null && open.Contains(i.Departure.Status)
                && (i.Article!.ArticleType == null || i.Article.ArticleType != ArticleType.Consumable));
        if (originLaboratoryId is int origin)
            query = query.Where(i => (i.OriginLaboratoryId ?? i.Departure!.OriginLaboratoryId) == origin);
        var rows = await query
            .Select(i => new
            {
                Origin = i.OriginLaboratoryId ?? i.Departure!.OriginLaboratoryId,
                ArticleId = i.ArticleId!.Value,
                i.Departure!.PlanningId,
                PlannedDate = (DateTime?)i.Departure.Planning!.ScheduledDate,
                PlannedEnd = i.Departure.Planning.EndTime,
                Delivered = i.Quantity ?? 0,
                Returned = i.ReturnedQuantity ?? 0
            })
            .ToListAsync();
        if (rows.Count == 0) return [];

        // Lo cubierto por incidentes L-5 ya salió de la existencia: no cuenta como pendiente de devolver.
        var planningIds = rows.Where(r => r.PlanningId != null).Select(r => r.PlanningId!.Value).Distinct().ToList();
        var covered = planningIds.Count == 0
            ? new Dictionary<(int, int), decimal>()
            : (await context.L5Incidents.AsNoTracking()
                    .Where(i => i.PlanningId != null && planningIds.Contains(i.PlanningId.Value) && i.ArticleId != null)
                    .GroupBy(i => new { PlanningId = i.PlanningId!.Value, ArticleId = i.ArticleId!.Value })
                    .Select(g => new { g.Key.PlanningId, g.Key.ArticleId, Total = g.Sum(i => i.AffectedQuantity) })
                    .ToListAsync())
                .ToDictionary(c => (c.PlanningId, c.ArticleId), c => (decimal)c.Total);

        var endOfDay = new TimeSpan(23, 59, 59);
        var result = new List<PendingRow>();
        // La cobertura L-5 de una planificación se reparte entre sus orígenes en orden.
        foreach (var g in rows.GroupBy(r => new { r.PlanningId, r.ArticleId }))
        {
            var cover = g.Key.PlanningId is int pid ? covered.GetValueOrDefault((pid, g.Key.ArticleId)) : 0;
            foreach (var r in g.OrderBy(r => r.Origin))
            {
                var pending = Math.Max(0, r.Delivered - r.Returned);
                var use = Math.Min(cover, pending);
                cover -= use;
                if (pending - use <= 0 || r.Origin is not int originId) continue;
                var end = r.PlannedDate.HasValue ? r.PlannedDate.Value.Date + (r.PlannedEnd ?? endOfDay) : (DateTime?)null;
                result.Add(new PendingRow(originId, r.ArticleId, r.PlanningId, end, pending - use));
            }
        }
        return result;
    }

    /// <summary>
    /// Por artículo: lo entregado desde <paramref name="originLaboratoryId"/> en salidas abiertas que aún no volvió
    /// (descontado lo cubierto por L-5). Es lo que está "fuera ahora" de ese origen.
    /// </summary>
    public static async Task<Dictionary<int, decimal>> OutstandingAsync(ApplicationDbContext context, int originLaboratoryId,
        IReadOnlyCollection<int> articleIds) =>
        (await PendingRowsAsync(context, articleIds, originLaboratoryId))
            .GroupBy(r => r.ArticleId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Pending));

    /// <summary>
    /// Por artículo (todos los orígenes): lo no devuelto de prácticas que ya terminaron antes de <paramref name="asOfLocal"/>
    /// (fin = ScheduledDate + EndTime; sin EndTime, fin del día). Las salidas sin planificación cuentan siempre.
    /// </summary>
    public static async Task<Dictionary<int, decimal>> OverdueOutstandingAsync(ApplicationDbContext context,
        IReadOnlyCollection<int> articleIds, DateTime asOfLocal) =>
        (await PendingRowsAsync(context, articleIds, null))
            .Where(r => r.PracticeEnd == null || r.PracticeEnd.Value < asOfLocal)
            .GroupBy(r => r.ArticleId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Pending));

    /// <summary>Igual que <see cref="OverdueOutstandingAsync"/> pero por (origen, artículo), para el libre por laboratorio.</summary>
    public static async Task<Dictionary<(int LaboratoryId, int ArticleId), decimal>> OverdueByOriginAsync(ApplicationDbContext context,
        IReadOnlyCollection<int> articleIds, DateTime asOfLocal) =>
        (await PendingRowsAsync(context, articleIds, null))
            .Where(r => r.PracticeEnd == null || r.PracticeEnd.Value < asOfLocal)
            .GroupBy(r => (r.OriginLaboratoryId, r.ArticleId))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Pending));
}

/// <summary>
/// Circuito L-3 por artículo con stock por horario: la salida y la devolución NO cambian la existencia
/// (el material reutilizable vuelve); la existencia solo baja con un incidente L-5 (ApplicationDbContext).
/// Reglas: con registro de stock y existencia libre insuficiente se bloquea la entrega; sin registro se permite
/// y queda una incidencia STOCK_NOT_REGISTERED. La suma devuelta nunca supera lo entregado.
/// </summary>
public interface IL3CircuitService
{
    Task<int?> GetDefaultOriginLaboratoryIdAsync();
    Task<L3CircuitResult> CreateDepartureAsync(int planningId, int originLaboratoryId, IReadOnlyList<DepartureLine> lines, string? observations);
    Task<L3CircuitResult> RegisterReturnAsync(int departureId, int responsiblePersonId, DateTime returnDate, IReadOnlyList<ReturnLine> lines, string? observations);
}

public class L3CircuitService : IL3CircuitService
{
    public const string DefaultOriginLaboratoryCode = "ECONO";
    private const string OperationBatchCode = "APP-L3-OPERACION";

    /// <summary>Unidades retiradas del inventario: la misma lista que ArticleAvailabilityService (privada allí).</summary>
    private static readonly EquipmentStatus[] UnitRetiredStatuses =
        [EquipmentStatus.Decommissioned, EquipmentStatus.Dismantled, EquipmentStatus.Deleted];

    private readonly ApplicationDbContext _context;
    private readonly ILogger<L3CircuitService> _logger;

    public L3CircuitService(ApplicationDbContext context, ILogger<L3CircuitService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<int?> GetDefaultOriginLaboratoryIdAsync() =>
        _context.Laboratories.AsNoTracking()
            .Where(l => l.Code == DefaultOriginLaboratoryCode)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

    public async Task<L3CircuitResult> CreateDepartureAsync(int planningId, int originLaboratoryId,
        IReadOnlyList<DepartureLine> lines, string? observations)
    {
        var request = await _context.Plannings.AsNoTracking()
            .Include(r => r.Laboratory)
            .FirstOrDefaultAsync(r => r.Id == planningId);
        if (request == null) return L3CircuitResult.Fail("La planificación no existe.");
        if (request.Status != PlanningStatus.Approved)
            return L3CircuitResult.Fail("Solo se registra la salida de una planificación aprobada.");
        if (await _context.Departures.AsNoTracking().AnyAsync(d => d.PlanningId == planningId))
            return L3CircuitResult.Fail("La planificación ya tiene una salida registrada.");

        var origin = await _context.Laboratories.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == originLaboratoryId && l.Status == GeneralStatus.Activo);
        if (origin == null) return L3CircuitResult.Fail("Selecciona un laboratorio de origen activo.");

        var details = await _context.PlanningRequirements.AsNoTracking()
            .Include(d => d.Article)
            .Include(d => d.EquipmentUnit)
            .Where(d => d.PlanningId == planningId)
            .ToDictionaryAsync(d => d.Id);
        var wanted = lines.Where(l => l.Quantity > 0).ToList();
        if (wanted.Count == 0) return L3CircuitResult.Fail("Indica al menos una cantidad a entregar.");
        if (wanted.Select(l => l.DetailId).Distinct().Count() != wanted.Count || wanted.Any(l => !details.ContainsKey(l.DetailId)))
            return L3CircuitResult.Fail("Hay líneas que no pertenecen a esta planificación.");
        if (wanted.Any(l => decimal.Round(l.Quantity, 2) != l.Quantity || l.Quantity > 999999))
            return L3CircuitResult.Fail("Las cantidades admiten hasta 2 decimales.");

        // Líneas de unidad física (equipos con inventario): la unidad se entrega de a una y debe estar libre.
        var unitLines = wanted.Where(l => details[l.DetailId].EquipmentUnitId != null).ToList();
        foreach (var line in unitLines)
        {
            var unit = details[line.DetailId].EquipmentUnit!;
            if (line.Quantity > 1m)
                return L3CircuitResult.Fail($"La unidad {unit.InventoryNumber} se entrega de a una (cantidad 1).");
        }
        if (unitLines.Count > 0)
        {
            var unavailable = await GetUnavailableUnitReasonAsync(unitLines.Select(l => details[l.DetailId].EquipmentUnit!).ToList());
            if (unavailable != null) return L3CircuitResult.Fail(unavailable);
        }

        var warnings = new List<string>();

        // Asignaciones del revisor (V13): cada línea sale de sus orígenes en orden (Economato primero). Lo entregado de más
        // o las líneas sin asignación salen del origen elegido en el formulario.
        var economatoId = await GetDefaultOriginLaboratoryIdAsync();
        var detailIds = details.Keys.ToList();
        var assignmentRows = await _context.PlanningAssignments.AsNoTracking()
            .Where(a => detailIds.Contains(a.PlanningRequirementId))
            .Select(a => new { a.Id, a.PlanningRequirementId, a.LaboratoryId, a.Quantity })
            .ToListAsync();
        var assignmentsByDetail = assignmentRows
            .GroupBy(a => a.PlanningRequirementId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.LaboratoryId == economatoId ? 0 : 1).ThenBy(a => a.Id).ToList());

        var allocations = new List<(DepartureLine Line, int OriginId, decimal Quantity, bool FromAssignment)>();
        foreach (var line in wanted)
        {
            var detail = details[line.DetailId];
            if (IsEquipmentLine(detail) || !assignmentsByDetail.TryGetValue(line.DetailId, out var assigned))
            {
                allocations.Add((line, originLaboratoryId, line.Quantity, false));
                continue;
            }
            var remaining = line.Quantity;
            foreach (var a in assigned)
            {
                if (remaining <= 0) break;
                var take = Math.Min(remaining, a.Quantity);
                allocations.Add((line, a.LaboratoryId, take, true));
                remaining -= take;
            }
            if (remaining > 0) allocations.Add((line, originLaboratoryId, remaining, true));
        }

        var originIds = allocations.Select(a => a.OriginId).Distinct().ToList();
        var origins = await _context.Laboratories.AsNoTracking()
            .Where(l => originIds.Contains(l.Id) && l.Status == GeneralStatus.Activo)
            .ToDictionaryAsync(l => l.Id, l => l.Code);
        if (origins.Count != originIds.Count)
            return L3CircuitResult.Fail("Hay orígenes asignados que ya no son laboratorios activos: revisa la asignación de la solicitud.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // El stock de almacén solo aplica a las líneas de inventario: los equipos se gobiernan por sus unidades físicas.
            var inventory = allocations.Where(a => !IsEquipmentLine(details[a.Line.DetailId])).ToList();
            var inventoryArticleIds = inventory.Select(a => details[a.Line.DetailId].ArticleId).Distinct().ToList();
            var stocks = await _context.ArticleStocks.AsTracking()
                .Where(s => originIds.Contains(s.LaboratoryId) && inventoryArticleIds.Contains(s.ArticleId))
                .ToDictionaryAsync(s => (s.LaboratoryId, s.ArticleId));

            // Modelo de stock por horario: la existencia de un reutilizable no cambia al entregar; lo que está fuera ahora
            // (otras salidas abiertas del mismo origen) se descuenta solo para esta entrega física. Los consumibles sí
            // descuentan su existencia al entregarse (se gastan en la práctica).
            var outNowByOrigin = new Dictionary<int, Dictionary<int, decimal>>();
            foreach (var id in inventory.Select(a => a.OriginId).Distinct())
                outNowByOrigin[id] = await StockRules.OutstandingAsync(_context, id, inventoryArticleIds);

            // Validar todo antes de entregar: un origen sin existencia libre suficiente bloquea la salida completa.
            foreach (var group in inventory.GroupBy(a => (a.OriginId, details[a.Line.DetailId].ArticleId)))
            {
                var total = group.Sum(a => a.Quantity);
                if (!stocks.TryGetValue(group.Key, out var stock)) continue;
                var article = details[group.First().Line.DetailId].Article;
                var outNow = IsConsumable(article) ? 0 : outNowByOrigin[group.Key.OriginId].GetValueOrDefault(group.Key.ArticleId);
                var free = stock.AvailableQuantity - outNow;
                if (free < total)
                {
                    await transaction.RollbackAsync();
                    var outText = outNow > 0
                        ? $" ({outNow:0.##} de {stock.AvailableQuantity:0.##} siguen entregados en otras prácticas sin devolver)"
                        : string.Empty;
                    return L3CircuitResult.Fail(
                        $"Stock insuficiente de {article?.Code} · {article?.Name} en {origins[group.Key.OriginId]}: libre ahora {Math.Max(0, free):0.##}, solicitado {total:0.##}{outText}.");
                }
            }

            var mainOrigin = allocations.GroupBy(a => a.OriginId)
                .OrderByDescending(g => g.Sum(a => a.Quantity)).ThenBy(g => g.Key == originLaboratoryId ? 0 : 1)
                .Select(g => g.Key).First();
            var departure = new Departure
            {
                ManagementId = request.ManagementId,
                PlanningId = request.Id,
                OriginLaboratoryId = mainOrigin,
                BorrowerId = request.TeacherPersonId,
                Destination = request.Laboratory?.Name ?? "Todos los laboratorios",
                Type = DepartureType.InternalLoan,
                DepartureDate = DateTime.UtcNow,
                EstimatedReturnDate = request.ScheduledEndDate ?? request.ScheduledDate,
                DepartureObservations = Trim(observations, 500),
                Status = LoanStatus.Active,
                Code = TemporaryCode()
            };
            _context.Departures.Add(departure);

            var unregistered = new List<(Article Article, int OriginId)>();
            foreach (var allocation in allocations)
            {
                var detail = details[allocation.Line.DetailId];
                var unitNote = detail.EquipmentUnit != null ? $"Unidad {detail.EquipmentUnit.InventoryNumber}" : null;
                departure.Items.Add(new DepartureItem
                {
                    // El ítem referencia la unidad O el artículo (CK_DepartureItems_ExactlyOneReference), nunca ambos.
                    // La unidad entregada queda como prestada mientras esta salida esté abierta (disponibilidad deducida, sin cambiar su estado).
                    ArticleId = detail.EquipmentUnitId != null ? null : detail.ArticleId,
                    EquipmentUnitId = detail.EquipmentUnitId,
                    OriginLaboratoryId = allocation.FromAssignment ? allocation.OriginId : null,
                    ProductName = detail.Article?.Name,
                    Quantity = allocation.Quantity,
                    ReturnedQuantity = 0,
                    MeasurementUnitId = detail.Article?.MeasurementUnitId,
                    UnitOfMeasure = detail.Article?.UnitOfMeasure,
                    Observations = Trim(string.Join(" · ", new[] { unitNote, detail.Observations }.Where(t => !string.IsNullOrWhiteSpace(t))), 500),
                    Code = TemporaryCode()
                });

                // Las líneas de equipo físico no descuentan existencias ni generan aviso de stock sin registro.
                if (IsEquipmentLine(detail)) continue;

                if (stocks.TryGetValue((allocation.OriginId, detail.ArticleId), out var stock))
                {
                    // Solo el consumible baja la existencia al entregarse; el reutilizable vuelve al terminar la práctica.
                    if (IsConsumable(detail.Article))
                    {
                        stock.AvailableQuantity = Math.Max(0, stock.AvailableQuantity - allocation.Quantity);
                        stock.UpdatedOn = DateTime.UtcNow;
                    }
                }
                else if (detail.Article != null && !unregistered.Any(u => u.Article.Id == detail.ArticleId && u.OriginId == allocation.OriginId))
                {
                    unregistered.Add((detail.Article, allocation.OriginId));
                }
            }

            await _context.SaveChangesAsync();

            departure.Code = $"SAL-{departure.Id:00000}";
            var index = 1;
            foreach (var item in departure.Items.OrderBy(i => i.Id))
                item.Code = $"{departure.Code}-{index++:00}";

            if (unregistered.Count > 0)
            {
                var batchId = await GetOperationBatchIdAsync();
                foreach (var (article, unregisteredOrigin) in unregistered)
                {
                    _context.DataQualityIssues.Add(new DataQualityIssue
                    {
                        ImportBatchId = batchId,
                        IssueCode = "STOCK_NOT_REGISTERED",
                        EntityName = "DepartureItem",
                        EntityKey = $"{departure.Code}|{article.Code}",
                        FieldName = "AvailableQuantity",
                        SourceCode = departure.Code,
                        OriginalValue = origins[unregisteredOrigin],
                        Description = $"Salida de {article.Code} · {article.Name} sin registro de stock en {origins[unregisteredOrigin]}: se entregó sin descontar.",
                        Severity = DataQualityIssueSeverity.Warning,
                        Status = DataQualityIssueStatus.Open
                    });
                    warnings.Add($"{article.Code} · {article.Name} no tiene stock registrado en {origins[unregisteredOrigin]}: se entregó sin descontar.");
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new L3CircuitResult(true, $"Salida {departure.Code} registrada.", departure.Id, warnings);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return L3CircuitResult.Fail("El stock cambió mientras se registraba la salida. Revisa las cantidades e inténtalo de nuevo.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo registrar la salida de la planificación {PlanningId}", planningId);
            return L3CircuitResult.Fail("No se pudo registrar la salida. Intente nuevamente.");
        }
    }

    public async Task<L3CircuitResult> RegisterReturnAsync(int departureId, int responsiblePersonId, DateTime returnDate,
        IReadOnlyList<ReturnLine> lines, string? observations)
    {
        var wanted = lines.Where(l => l.Quantity > 0).ToList();
        if (wanted.Count == 0) return L3CircuitResult.Fail("Indica al menos una cantidad devuelta.");
        if (wanted.Select(l => l.DepartureItemId).Distinct().Count() != wanted.Count)
            return L3CircuitResult.Fail("Hay líneas repetidas en la devolución.");
        if (wanted.Any(l => decimal.Round(l.Quantity, 2) != l.Quantity))
            return L3CircuitResult.Fail("Las cantidades admiten hasta 2 decimales.");
        if (!await _context.People.AsNoTracking().AnyAsync(p => p.Id == responsiblePersonId))
            return L3CircuitResult.Fail("Selecciona el responsable de la devolución.");
        if (returnDate.Date > DateTime.Today.AddDays(1))
            return L3CircuitResult.Fail("La fecha de devolución no puede ser futura.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var departure = await _context.Departures.AsTracking()
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == departureId);
            if (departure == null || !departure.PlanningId.HasValue)
                return L3CircuitResult.Fail("La entrega de material no existe.");
            if (departure.Status is LoanStatus.Returned or LoanStatus.Cancelled)
                return L3CircuitResult.Fail("La salida ya fue devuelta por completo o está anulada.");
            var request = await _context.Plannings.AsNoTracking().FirstOrDefaultAsync(r => r.Id == departure.PlanningId);
            // La salida está abierta (validado arriba): un origen reprogramado o en reposición también cierra su material.
            if (request == null || !PlanningStatus.AllowsL3Return(request.Status, hasOpenDeparture: true))
                return L3CircuitResult.Fail("Solo se registran devoluciones de una planificación aprobada (o reubicada con material pendiente) y no finalizada.");

            var items = departure.Items.ToDictionary(i => i.Id);
            if (wanted.Any(l => !items.ContainsKey(l.DepartureItemId)))
                return L3CircuitResult.Fail("Hay líneas que no pertenecen a esta salida.");
            // Los consumibles se gastan en la práctica: quedan saldados al entregarse y no admiten devolución.
            var consumableItemIds = (await _context.DepartureItems.AsNoTracking()
                    .Where(i => i.DepartureId == departure.Id && i.Article != null && i.Article.ArticleType == ArticleType.Consumable)
                    .Select(i => i.Id).ToListAsync())
                .ToHashSet();
            if (wanted.Any(l => consumableItemIds.Contains(l.DepartureItemId)))
                return L3CircuitResult.Fail("Los consumibles no se devuelven: se gastan en la práctica.");

            // Saldo real desde las devoluciones registradas (el acumulado de DepartureItem es solo un reflejo).
            var itemIds = items.Keys.ToList();
            var returnedSoFar = await _context.DepartureReturnItems.AsNoTracking()
                .Where(r => itemIds.Contains(r.DepartureItemId))
                .GroupBy(r => r.DepartureItemId)
                .Select(g => new { g.Key, Total = g.Sum(r => r.ReturnedQuantity) })
                .ToDictionaryAsync(x => x.Key, x => x.Total);
            // Atribución por artículo: los ítems de unidad física (sin ArticleId) toman el artículo activo
            // vinculado a su equipo, con el mismo criterio que L-3 (BuildReturnRowsAsync) y L-5 (LoadAsync).
            var unitIds = items.Values.Where(i => i.EquipmentUnitId != null).Select(i => i.EquipmentUnitId!.Value).ToList();
            var unitEquipment = unitIds.Count == 0
                ? new Dictionary<int, int>()
                : await _context.EquipmentUnits.AsNoTracking()
                    .Where(u => unitIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.EquipmentId);
            var equipmentIds = unitEquipment.Values.Distinct().ToList();
            var articleByEquipment = equipmentIds.Count == 0
                ? new Dictionary<int, int>()
                : (await _context.Articles.AsNoTracking()
                        .Where(a => a.EquipmentId != null && equipmentIds.Contains(a.EquipmentId.Value) && a.Status == GeneralStatus.Activo)
                        .OrderBy(a => a.Id)
                        .Select(a => new { EquipmentId = a.EquipmentId!.Value, a.Id })
                        .ToListAsync())
                    .GroupBy(a => a.EquipmentId)
                    .ToDictionary(g => g.Key, g => g.First().Id);

            int? ArticleOf(DepartureItem item)
            {
                if (item.ArticleId is int articleId) return articleId;
                return item.EquipmentUnitId is int unitId
                    && unitEquipment.TryGetValue(unitId, out var equipmentId)
                    && articleByEquipment.TryGetValue(equipmentId, out var linkedId)
                        ? linkedId
                        : null;
            }

            // Faltantes cubiertos por incidentes L-5 del artículo en esta planificación: no deben volver (ni reponer stock).
            var covered = await _context.L5Incidents.AsNoTracking()
                .Where(i => i.PlanningId == departure.PlanningId && i.ArticleId != null)
                .GroupBy(i => i.ArticleId!.Value)
                .Select(g => new { ArticleId = g.Key, Total = g.Sum(i => i.AffectedQuantity) })
                .ToDictionaryAsync(x => x.ArticleId, x => x.Total);

            // Validación por ítem y por artículo (descuenta lo cubierto por L-5) y cierre de la salida.
            var outcome = L3ReturnRules.Evaluate(
                items.Values.Select(i => new L3ReturnItemLine(
                    i.Id, ArticleOf(i), i.ProductName ?? "", i.Quantity ?? 0,
                    consumableItemIds.Contains(i.Id) ? i.Quantity ?? 0 : returnedSoFar.GetValueOrDefault(i.Id))).ToList(),
                covered, wanted);
            if (outcome.Error != null) return L3CircuitResult.Fail(outcome.Error);

            var ret = new DepartureReturn
            {
                DepartureId = departure.Id,
                ResponsiblePersonId = responsiblePersonId,
                ReturnDate = returnDate.Date,
                Observations = Trim(observations, 1000),
                Code = TemporaryCode()
            };
            foreach (var line in wanted)
            {
                var item = items[line.DepartureItemId];
                ret.Items.Add(new DepartureReturnItem { DepartureItemId = item.Id, ReturnedQuantity = line.Quantity, Code = TemporaryCode() });
                item.ReturnedQuantity = returnedSoFar.GetValueOrDefault(item.Id) + line.Quantity;
                // La devolución no repone existencia: la entrega nunca la descontó (modelo de stock por horario).
            }

            // Cierre según L3ReturnRules: por artículo, devuelto + cubierto L-5 >= entregado (ítems sin artículo: todo devuelto).
            var fullyReturned = outcome.FullyReturned;
            ret.Status = fullyReturned ? DepartureReturnStatus.Complete : DepartureReturnStatus.Partial;
            if (fullyReturned)
            {
                departure.Status = LoanStatus.Returned;
                departure.ActualReturnDate = returnDate.Date;
            }

            _context.DepartureReturns.Add(ret);
            await _context.SaveChangesAsync();

            ret.Code = $"DEV-{ret.Id:00000}";
            var index = 1;
            foreach (var item in ret.Items.OrderBy(i => i.Id))
                item.Code = $"{ret.Code}-{index++:00}";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new L3CircuitResult(true, fullyReturned
                ? $"Devolución {ret.Code} registrada: la salida quedó devuelta por completo."
                : $"Devolución parcial {ret.Code} registrada.", ret.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return L3CircuitResult.Fail("El stock cambió mientras se registraba la devolución. Inténtalo de nuevo.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo registrar la devolución de la salida {DepartureId}", departureId);
            return L3CircuitResult.Fail("No se pudo registrar la devolución. Intente nuevamente.");
        }
    }

    /// <summary>Lote técnico de la aplicación para incidencias de operación (DataQualityIssue exige lote).</summary>
    private async Task<int> GetOperationBatchIdAsync()
    {
        var id = await _context.ImportBatches.AsNoTracking()
            .Where(b => b.Code == OperationBatchCode).Select(b => (int?)b.Id).FirstOrDefaultAsync();
        if (id.HasValue) return id.Value;

        var batch = new ImportBatch
        {
            Code = OperationBatchCode,
            SourceType = "Aplicacion",
            ContractVersion = "operacion-l3",
            SourceName = "Operación de laboratorios (salidas L-3)",
            SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(OperationBatchCode))).ToLowerInvariant(),
            ImportedAt = DateTime.UtcNow,
            Notes = "Incidencias registradas por la aplicación durante la operación del circuito L-3."
        };
        _context.ImportBatches.Add(batch);
        await _context.SaveChangesAsync();
        return batch.Id;
    }

    /// <summary>Insumo que se gasta en la práctica: descuenta existencia al entregarse y no se devuelve.</summary>
    private static bool IsConsumable(Article? article) => article?.ArticleType == ArticleType.Consumable;

    /// <summary>Línea cuyo artículo representa un equipo del catálogo: se entrega como unidad física, no como existencias de almacén.</summary>
    private static bool IsEquipmentLine(PlanningRequirement detail) =>
        detail.EquipmentUnitId != null || detail.Article?.EquipmentId != null;

    /// <summary>
    /// Razón por la que alguna unidad de la salida no está disponible (null si todas están libres): retirada del
    /// inventario, salida abierta, mantenimiento en curso o estado no operativo según ResolveState. La unidad no
    /// se marca "En Préstamo": la disponibilidad se deduce de la salida abierta (decisión del usuario).
    /// </summary>
    private async Task<string?> GetUnavailableUnitReasonAsync(IReadOnlyCollection<EquipmentUnit> units)
    {
        var byId = units.GroupBy(u => u.Id).ToDictionary(g => g.Key, g => g.First());
        var unitIds = byId.Keys.ToList();

        // Estado vigente de catálogo (la línea se planificó antes; puede haber cambiado desde entonces).
        var currentStatuses = await _context.EquipmentUnits.AsNoTracking()
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.CurrentStatus);

        var openLoanStatuses = new[] { LoanStatus.Active, LoanStatus.Overdue };
        var loaned = new HashSet<int>((await _context.Departures.AsNoTracking()
                .Where(d => d.EquipmentUnitId != null && unitIds.Contains(d.EquipmentUnitId.Value) && openLoanStatuses.Contains(d.Status))
                .Select(d => d.EquipmentUnitId!.Value)
                .ToListAsync())
            .Concat(await _context.DepartureItems.AsNoTracking()
                .Where(i => i.EquipmentUnitId != null && unitIds.Contains(i.EquipmentUnitId.Value)
                    && i.Departure != null && openLoanStatuses.Contains(i.Departure.Status))
                .Select(i => i.EquipmentUnitId!.Value)
                .ToListAsync()));
        var inMaintenance = (await _context.Maintenances.AsNoTracking()
            .Where(m => unitIds.Contains(m.EquipmentUnitId) && m.Status == MaintenanceStatus.InProgress)
            .Select(m => m.EquipmentUnitId)
            .ToListAsync()).ToHashSet();

        foreach (var unit in byId.Values)
        {
            var status = currentStatuses.GetValueOrDefault(unit.Id, unit.CurrentStatus);
            if (UnitRetiredStatuses.Contains(status))
                return $"La unidad {unit.InventoryNumber} no está disponible para la salida (retirada del inventario). Quítala de la solicitud o espera a que se libere.";
            var state = ArticleAvailabilityService.ResolveState(status, loaned.Contains(unit.Id), inMaintenance.Contains(unit.Id));
            if (state == UnitOperationalState.Available) continue;
            var motivo = state switch
            {
                UnitOperationalState.OnLoan => "ya entregada en una salida abierta",
                UnitOperationalState.InMaintenance => "en mantenimiento",
                _ => "fuera de servicio"
            };
            return $"La unidad {unit.InventoryNumber} no está disponible para la salida ({motivo}). Quítala de la solicitud o espera a que se libere.";
        }
        return null;
    }

    private static string TemporaryCode() => "TMP-" + Guid.NewGuid().ToString("N")[..20];

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        return text.Length <= max ? text : text[..max];
    }
}
