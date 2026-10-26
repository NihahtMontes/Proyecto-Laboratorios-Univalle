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
/// Circuito L-3 por artículo: la salida descuenta el stock del laboratorio de origen y la devolución lo repone.
/// Reglas (decisiones D2/D2b): con registro de stock y cantidad insuficiente se bloquea; sin registro se permite
/// sin descontar y queda una incidencia STOCK_NOT_REGISTERED. La suma devuelta nunca supera lo entregado.
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
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // El stock de almacén solo aplica a las líneas de inventario: los equipos se gobiernan por sus unidades físicas.
            var inventoryArticleIds = wanted
                .Where(l => !IsEquipmentLine(details[l.DetailId]))
                .Select(l => details[l.DetailId].ArticleId)
                .Distinct().ToList();
            var stocks = await _context.ArticleStocks.AsTracking()
                .Where(s => s.LaboratoryId == originLaboratoryId && inventoryArticleIds.Contains(s.ArticleId))
                .ToDictionaryAsync(s => s.ArticleId);

            // Validar todo antes de descontar: una línea sin stock suficiente bloquea la salida completa.
            foreach (var group in wanted.Where(l => !IsEquipmentLine(details[l.DetailId])).GroupBy(l => details[l.DetailId].ArticleId))
            {
                var total = group.Sum(l => l.Quantity);
                if (stocks.TryGetValue(group.Key, out var stock) && stock.AvailableQuantity < total)
                {
                    await transaction.RollbackAsync();
                    var article = details[group.First().DetailId].Article;
                    return L3CircuitResult.Fail(
                        $"Stock insuficiente de {article?.Code} · {article?.Name} en {origin.Code}: disponible {stock.AvailableQuantity:0.##}, solicitado {total:0.##}.");
                }
            }

            var departure = new Departure
            {
                ManagementId = request.ManagementId,
                PlanningId = request.Id,
                OriginLaboratoryId = originLaboratoryId,
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

            var unregistered = new List<Article>();
            foreach (var line in wanted)
            {
                var detail = details[line.DetailId];
                var unitNote = detail.EquipmentUnit != null ? $"Unidad {detail.EquipmentUnit.InventoryNumber}" : null;
                departure.Items.Add(new DepartureItem
                {
                    // El ítem referencia la unidad O el artículo (CK_DepartureItems_ExactlyOneReference), nunca ambos.
                    // La unidad entregada queda como prestada mientras esta salida esté abierta (disponibilidad deducida, sin cambiar su estado).
                    ArticleId = detail.EquipmentUnitId != null ? null : detail.ArticleId,
                    EquipmentUnitId = detail.EquipmentUnitId,
                    ProductName = detail.Article?.Name,
                    Quantity = line.Quantity,
                    ReturnedQuantity = 0,
                    MeasurementUnitId = detail.Article?.MeasurementUnitId,
                    UnitOfMeasure = detail.Article?.UnitOfMeasure,
                    Observations = Trim(string.Join(" · ", new[] { unitNote, detail.Observations }.Where(t => !string.IsNullOrWhiteSpace(t))), 500),
                    Code = TemporaryCode()
                });

                // Las líneas de equipo físico no descuentan existencias ni generan aviso de stock sin registro.
                if (IsEquipmentLine(detail)) continue;

                if (stocks.TryGetValue(detail.ArticleId, out var stock))
                {
                    stock.AvailableQuantity -= line.Quantity;
                    stock.UpdatedOn = DateTime.UtcNow;
                }
                else if (detail.Article != null && unregistered.All(a => a.Id != detail.ArticleId))
                {
                    unregistered.Add(detail.Article);
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
                foreach (var article in unregistered)
                {
                    _context.DataQualityIssues.Add(new DataQualityIssue
                    {
                        ImportBatchId = batchId,
                        IssueCode = "STOCK_NOT_REGISTERED",
                        EntityName = "DepartureItem",
                        EntityKey = $"{departure.Code}|{article.Code}",
                        FieldName = "AvailableQuantity",
                        SourceCode = departure.Code,
                        OriginalValue = origin.Code,
                        Description = $"Salida de {article.Code} · {article.Name} sin registro de stock en {origin.Code}: se entregó sin descontar.",
                        Severity = DataQualityIssueSeverity.Warning,
                        Status = DataQualityIssueStatus.Open
                    });
                    warnings.Add($"{article.Code} · {article.Name} no tiene stock registrado en {origin.Code}: se entregó sin descontar.");
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
                    i.Id, ArticleOf(i), i.ProductName ?? "", i.Quantity ?? 0, returnedSoFar.GetValueOrDefault(i.Id))).ToList(),
                covered, wanted);
            if (outcome.Error != null) return L3CircuitResult.Fail(outcome.Error);

            // Solo los ítems de inventario manejan existencias: las unidades físicas no descontaron stock al salir.
            var articleIds = wanted.Select(l => items[l.DepartureItemId])
                .Where(i => i.EquipmentUnitId == null && i.ArticleId != null)
                .Select(i => i.ArticleId!.Value).Distinct().ToList();
            var stocks = departure.OriginLaboratoryId.HasValue
                ? await _context.ArticleStocks.AsTracking()
                    .Where(s => s.LaboratoryId == departure.OriginLaboratoryId && articleIds.Contains(s.ArticleId))
                    .ToDictionaryAsync(s => s.ArticleId)
                : new Dictionary<int, ArticleStock>();

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
                // Solo se repone lo que se descontó: las unidades físicas no descontaron existencias
                // (su disponibilidad se deduce de la salida abierta) y sin registro de stock no hay nada que reponer.
                if (item.EquipmentUnitId == null && item.ArticleId is int articleId && stocks.TryGetValue(articleId, out var stock))
                {
                    stock.AvailableQuantity += line.Quantity;
                    stock.UpdatedOn = DateTime.UtcNow;
                }
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
