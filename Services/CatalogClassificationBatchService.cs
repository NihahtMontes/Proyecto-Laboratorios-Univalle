using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

public sealed record CatalogClassificationRow(
    int SourceRowNumber,
    string CatalogCode,
    EquipmentClassificationReviewStatus ReviewStatus,
    EquipmentCategory? Category,
    EquipmentTypeClassification? TypeClassification,
    UtensilType? UtensilType,
    GeneralStatus? GeneralStatus,
    string? OtherDetail,
    string? EvidenceReference,
    int? ResponsiblePersonId,
    string? ResponsibleSnapshot,
    DateTime? DecisionDate,
    string? V2Value,
    string? CloudLegacyValue,
    string? LocalReferenceValue,
    string? TechnicalProposal);

public sealed record CatalogClassificationBatchPlan(
    bool CanStage,
    int Total,
    int Confirmed,
    int Pending,
    IReadOnlyList<string> DatabaseOnlyCodes,
    IReadOnlyList<string> Blockers);

public sealed record CatalogClassificationBatchResult(
    int ImportBatchId,
    string BatchCode,
    bool Changed,
    int SourceRows,
    int DecisionsApplied);

public interface ICatalogClassificationBatchService
{
    Task<CatalogClassificationBatchPlan> PlanAsync(
        IReadOnlyCollection<CatalogClassificationRow> rows,
        CancellationToken cancellationToken = default);

    Task<CatalogClassificationBatchResult> StageAsync(
        string sourceName,
        string sourceSha256,
        IReadOnlyCollection<CatalogClassificationRow> rows,
        CancellationToken cancellationToken = default);

    Task<CatalogClassificationBatchResult> PromoteAsync(
        int importBatchId,
        int recordedByUserId,
        bool dataApprovalGranted,
        int globalP0Count,
        CancellationToken cancellationToken = default);
}

public sealed class CatalogClassificationBatchService : ICatalogClassificationBatchService
{
    public const string ContractVersion = "catalog-classification-v1";
    public const string SourceSheet = "03_CATALOGOS_POR_CONFIRMAR";
    public const int ExpectedCatalogCount = 107;
    public const int ExpectedDatabaseOnlyCount = 5;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ApplicationDbContext _context;
    private readonly IEquipmentClassificationDecisionService _decisionService;

    public CatalogClassificationBatchService(
        ApplicationDbContext context,
        IEquipmentClassificationDecisionService decisionService)
    {
        _context = context;
        _decisionService = decisionService;
    }

    public async Task<CatalogClassificationBatchPlan> PlanAsync(
        IReadOnlyCollection<CatalogClassificationRow> rows,
        CancellationToken cancellationToken = default)
    {
        var blockers = ValidateRows(rows);
        var inputCodes = rows.Select(row => NormalizeCode(row.CatalogCode))
            .Where(code => code.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var databaseCodes = await _context.Equipments.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.CatalogCode != null)
            .Select(item => item.CatalogCode!)
            .ToListAsync(cancellationToken);

        var missing = inputCodes.Except(databaseCodes, StringComparer.OrdinalIgnoreCase).OrderBy(code => code).ToList();
        if (missing.Count > 0)
            blockers.Add($"No existen en la base: {string.Join(", ", missing)}.");

        var databaseOnly = databaseCodes.Except(inputCodes, StringComparer.OrdinalIgnoreCase).OrderBy(code => code).ToList();
        if (databaseOnly.Count != ExpectedDatabaseOnlyCount)
            blockers.Add($"La cola DatabaseOnly contiene {databaseOnly.Count}; se esperaban {ExpectedDatabaseOnlyCount}.");

        var confirmed = rows.Count(row => row.ReviewStatus == EquipmentClassificationReviewStatus.Confirmed);
        return new CatalogClassificationBatchPlan(
            blockers.Count == 0,
            rows.Count,
            confirmed,
            rows.Count - confirmed,
            databaseOnly,
            blockers);
    }

    public async Task<CatalogClassificationBatchResult> StageAsync(
        string sourceName,
        string sourceSha256,
        IReadOnlyCollection<CatalogClassificationRow> rows,
        CancellationToken cancellationToken = default)
    {
        var hash = NormalizeSha256(sourceSha256);
        var plan = await PlanAsync(rows, cancellationToken);
        if (!plan.CanStage)
            throw new InvalidOperationException(string.Join(" ", plan.Blockers));

        var batchCode = $"CCV1-{hash[..16]}";
        var existing = await _context.ImportBatches.AsNoTracking()
            .SingleOrDefaultAsync(batch => batch.Code == batchCode, cancellationToken);
        if (existing != null)
        {
            var count = await _context.ImportSourceRows.CountAsync(row => row.ImportBatchId == existing.Id, cancellationToken);
            return new CatalogClassificationBatchResult(existing.Id, existing.Code, false, count, 0);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var batch = new ImportBatch
        {
            Code = batchCode,
            SourceType = "Excel",
            ContractVersion = ContractVersion,
            SourceName = Required(sourceName, nameof(sourceName), 260),
            SourceSha256 = hash,
            Status = ImportBatchStatus.Prepared,
            Notes = $"Lote de {ExpectedCatalogCount} catálogos. DatabaseOnly: {string.Join(", ", plan.DatabaseOnlyCodes)}"
        };
        _context.ImportBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var source in rows.OrderBy(row => row.SourceRowNumber))
        {
            var row = source with { CatalogCode = NormalizeCode(source.CatalogCode) };
            var confirmed = row.ReviewStatus == EquipmentClassificationReviewStatus.Confirmed;
            _context.ImportSourceRows.Add(new ImportSourceRow
            {
                ImportBatchId = batch.Id,
                SourceSheet = SourceSheet,
                SourceRowNumber = row.SourceRowNumber,
                SourceRowKey = $"{ContractVersion}:{row.CatalogCode}",
                OriginalIdentifier = row.CatalogCode,
                TargetEntityName = nameof(Equipment),
                TargetEntityKey = row.CatalogCode,
                MigrationStatus = ImportSourceRowStatus.Pending,
                ReconciliationStatus = confirmed ? DataReconciliationStatus.Match : DataReconciliationStatus.PendingClient,
                OriginalDataJson = JsonSerializer.Serialize(row, JsonOptions),
                Notes = confirmed ? "Respuesta completa; promoción conjunta pendiente." : "Pendiente de respuesta aprobada."
            });
            if (!confirmed)
                _context.DataQualityIssues.Add(PendingIssue(batch.Id, row));
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CatalogClassificationBatchResult(batch.Id, batch.Code, true, rows.Count, 0);
    }

    public async Task<CatalogClassificationBatchResult> PromoteAsync(
        int importBatchId,
        int recordedByUserId,
        bool dataApprovalGranted,
        int globalP0Count,
        CancellationToken cancellationToken = default)
    {
        if (!dataApprovalGranted)
            throw new InvalidOperationException("Datos todavía no aprobó la conciliación.");
        if (globalP0Count != 0)
            throw new InvalidOperationException($"La promoción exige P0 global igual a cero; actual: {globalP0Count}.");

        var promoter = await _context.Users.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == recordedByUserId, cancellationToken)
            ?? throw new InvalidOperationException("El usuario técnico no existe.");
        if (promoter.Role is not (UserRole.Administrador or UserRole.SuperAdmin))
            throw new UnauthorizedAccessException("Solo Administrator o SuperAdmin puede promover el lote.");

        var batch = await _context.ImportBatches.AsTracking()
            .SingleOrDefaultAsync(item => item.Id == importBatchId, cancellationToken)
            ?? throw new InvalidOperationException("El lote no existe.");
        if (batch.ContractVersion != ContractVersion)
            throw new InvalidOperationException("El contrato del lote no es catalog-classification-v1.");
        if (batch.Status == ImportBatchStatus.Official)
        {
            var applied = await _context.EquipmentClassificationDecisions.CountAsync(
                decision => decision.ImportBatchId == batch.Id, cancellationToken);
            var rowCount = await _context.ImportSourceRows.CountAsync(row => row.ImportBatchId == batch.Id, cancellationToken);
            return new CatalogClassificationBatchResult(batch.Id, batch.Code, false, rowCount, applied);
        }

        var sourceRows = await _context.ImportSourceRows.AsTracking()
            .Where(row => row.ImportBatchId == batch.Id && row.SourceSheet == SourceSheet && row.TargetEntityName == nameof(Equipment))
            .OrderBy(row => row.SourceRowNumber)
            .ToListAsync(cancellationToken);
        var rows = sourceRows.Select(DeserializeRow).ToList();
        var plan = await PlanAsync(rows, cancellationToken);
        if (!plan.CanStage || plan.Confirmed != ExpectedCatalogCount)
            throw new InvalidOperationException(
                $"Se exigen {ExpectedCatalogCount} confirmados; confirmados: {plan.Confirmed}, pendientes: {plan.Pending}. "
                + string.Join(" ", plan.Blockers));

        var hasCriticalIssue = await _context.DataQualityIssues.AnyAsync(issue =>
            issue.ImportBatchId == batch.Id && issue.Severity == DataQualityIssueSeverity.Critical
            && issue.Status == DataQualityIssueStatus.Open, cancellationToken);
        if (hasCriticalIssue)
            throw new InvalidOperationException("El lote contiene incidencias críticas abiertas.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var appliedCount = 0;
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var sourceRow = sourceRows[index];
            var result = await _decisionService.ApplyAsync(new EquipmentClassificationDecisionInput(
                $"{batch.Code}:{row.CatalogCode}", row.CatalogCode,
                EquipmentClassificationReviewStatus.Confirmed,
                row.Category, row.TypeClassification, row.UtensilType, row.GeneralStatus,
                row.OtherDetail, row.EvidenceReference, row.ResponsiblePersonId,
                row.ResponsibleSnapshot, row.DecisionDate, null,
                batch.Id, sourceRow.Id, recordedByUserId), cancellationToken);
            if (result.Applied) appliedCount++;
            sourceRow.MigrationStatus = ImportSourceRowStatus.Migrated;
            sourceRow.ReconciliationStatus = DataReconciliationStatus.Match;
            sourceRow.LastReconciledDate = DateTime.UtcNow;
        }

        var issues = await _context.DataQualityIssues
            .Where(issue => issue.ImportBatchId == batch.Id && issue.Status == DataQualityIssueStatus.Open)
            .ToListAsync(cancellationToken);
        foreach (var issue in issues)
        {
            issue.Status = DataQualityIssueStatus.Corrected;
            issue.ResolvedDate = DateTime.UtcNow;
            issue.ResolvedByUserId = recordedByUserId;
            issue.ResolutionNotes = "Resuelto por lote completo aprobado de clasificación.";
        }
        batch.Status = ImportBatchStatus.Official;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CatalogClassificationBatchResult(batch.Id, batch.Code, appliedCount > 0, sourceRows.Count, appliedCount);
    }

    private static List<string> ValidateRows(IReadOnlyCollection<CatalogClassificationRow> rows)
    {
        var blockers = new List<string>();
        if (rows.Count != ExpectedCatalogCount)
            blockers.Add($"El lote contiene {rows.Count} filas; debe contener {ExpectedCatalogCount}.");
        var duplicates = rows.GroupBy(row => NormalizeCode(row.CatalogCode), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Key.Length == 0 || group.Count() > 1)
            .Select(group => group.Key.Length == 0 ? "(vacío)" : group.Key).ToList();
        if (duplicates.Count > 0)
            blockers.Add($"CatalogCode vacío o repetido: {string.Join(", ", duplicates)}.");
        if (rows.GroupBy(row => row.SourceRowNumber).Any(group => group.Count() > 1))
            blockers.Add("Existen filas de procedencia repetidas.");

        foreach (var row in rows.Where(row => row.ReviewStatus == EquipmentClassificationReviewStatus.Confirmed))
        {
            var valid = row.Category.HasValue && row.GeneralStatus.HasValue
                && EquipmentClassificationRules.IsConfirmedCombinationValid(
                    row.Category.Value, row.TypeClassification, row.UtensilType, row.OtherDetail)
                && !string.IsNullOrWhiteSpace(row.EvidenceReference)
                && !string.IsNullOrWhiteSpace(row.ResponsibleSnapshot)
                && row.DecisionDate.HasValue;
            if (!valid) blockers.Add($"{row.CatalogCode}: decisión confirmada incompleta o jerarquía inválida.");
        }
        return blockers;
    }

    private static DataQualityIssue PendingIssue(int batchId, CatalogClassificationRow row) => new()
    {
        ImportBatchId = batchId,
        IssueCode = "CAT_CLASS_PENDING_CLIENT",
        EntityName = nameof(Equipment),
        EntityKey = row.CatalogCode,
        FieldName = nameof(Equipment.ClassificationReviewStatus),
        SourceSheet = SourceSheet,
        SourceRowNumber = row.SourceRowNumber,
        SourceCode = row.CatalogCode,
        Description = "La clasificación requiere confirmación funcional de la cliente.",
        Severity = DataQualityIssueSeverity.Error,
        Status = DataQualityIssueStatus.Open
    };

    private static CatalogClassificationRow DeserializeRow(ImportSourceRow row) =>
        JsonSerializer.Deserialize<CatalogClassificationRow>(row.OriginalDataJson ?? string.Empty, JsonOptions)
        ?? throw new InvalidDataException($"No se pudo reconstruir la fila {row.SourceRowNumber}.");

    private static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string NormalizeSha256(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException("El SHA-256 debe tener 64 caracteres hexadecimales.", nameof(value));
        return normalized;
    }

    private static string Required(string? value, string parameterName, int maxLength)
    {
        var clean = value?.Trim();
        if (string.IsNullOrWhiteSpace(clean)) throw new ArgumentException("El valor es obligatorio.", parameterName);
        return clean.Length <= maxLength ? clean : clean[..maxLength];
    }
}
