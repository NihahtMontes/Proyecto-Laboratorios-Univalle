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

        var warnings = new List<string>();
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var articleIds = wanted.Select(l => details[l.DetailId].ArticleId).Distinct().ToList();
            var stocks = await _context.ArticleStocks.AsTracking()
                .Where(s => s.LaboratoryId == originLaboratoryId && articleIds.Contains(s.ArticleId))
                .ToDictionaryAsync(s => s.ArticleId);

            // Validar todo antes de descontar: una línea sin stock suficiente bloquea la salida completa.
            foreach (var group in wanted.GroupBy(l => details[l.DetailId].ArticleId))
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
                    ArticleId = detail.ArticleId,
                    ProductName = detail.Article?.Name,
                    Quantity = line.Quantity,
                    ReturnedQuantity = 0,
                    MeasurementUnitId = detail.Article?.MeasurementUnitId,
                    UnitOfMeasure = detail.Article?.UnitOfMeasure,
                    Observations = Trim(string.Join(" · ", new[] { unitNote, detail.Observations }.Where(t => !string.IsNullOrWhiteSpace(t))), 500),
                    Code = TemporaryCode()
                });

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
                return L3CircuitResult.Fail("La salida L-3 no existe.");
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
            foreach (var line in wanted)
            {
                var item = items[line.DepartureItemId];
                var pending = (item.Quantity ?? 0) - returnedSoFar.GetValueOrDefault(item.Id);
                if (line.Quantity > pending)
                    return L3CircuitResult.Fail($"{item.ProductName}: se devolvería {line.Quantity:0.##} y solo quedan {pending:0.##} pendientes.");
            }

            var articleIds = wanted.Select(l => items[l.DepartureItemId].ArticleId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
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
                // Solo se repone lo que se descontó: sin registro de stock no hay nada que reponer.
                if (item.ArticleId is int articleId && stocks.TryGetValue(articleId, out var stock))
                {
                    stock.AvailableQuantity += line.Quantity;
                    stock.UpdatedOn = DateTime.UtcNow;
                }
            }

            var fullyReturned = departure.Items.All(i => (i.ReturnedQuantity ?? 0) >= (i.Quantity ?? 0));
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

    private static string TemporaryCode() => "TMP-" + Guid.NewGuid().ToString("N")[..20];

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        return text.Length <= max ? text : text[..max];
    }
}
