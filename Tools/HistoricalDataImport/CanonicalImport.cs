using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal enum ImportContract
{
    Auto,
    Legacy,
    V2
}

internal enum ImportMode
{
    Validate,
    Audit,
    Sql
}

internal enum DataPriority
{
    P0,
    P1,
    P2
}

internal enum ReconciliationStatus
{
    Coincide,
    SoloExcel,
    SoloBase,
    Conflicto,
    InferidoLegado,
    PendienteCliente
}

internal sealed record ImportOptions(
    string WorkbookPath,
    string OutputDirectory,
    ImportContract Contract,
    ImportMode? RequestedMode,
    string? SnapshotPath,
    bool AllowSql,
    bool SeedMode,
    bool SelfCheck,
    bool ShowHelp)
{
    public static ImportOptions Parse(string[] args)
    {
        var defaultWorkbook = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "Excels", "Plantilla_Original.xlsx"));

        var contract = ImportContract.Auto;
        ImportMode? mode = null;
        string? snapshot = null;
        var allowSql = false;
        var seed = false;
        var selfCheck = false;
        var help = false;
        var positional = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                positional.Add(argument);
                continue;
            }

            var pair = argument[2..].Split('=', 2);
            var name = pair[0].ToLowerInvariant();
            var inlineValue = pair.Length == 2 ? pair[1] : null;

            string RequiredValue()
            {
                if (!string.IsNullOrWhiteSpace(inlineValue))
                {
                    return inlineValue;
                }

                if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"La opción --{name} requiere un valor.");
                }

                index++;
                return args[index];
            }

            switch (name)
            {
                case "contract":
                    contract = ParseContract(RequiredValue());
                    break;
                case "mode":
                    mode = ParseMode(RequiredValue());
                    break;
                case "snapshot":
                    snapshot = Path.GetFullPath(RequiredValue());
                    break;
                case "allow-sql":
                    allowSql = true;
                    break;
                case "validate-only":
                    mode = ImportMode.Validate;
                    break;
                case "seed":
                    seed = true;
                    break;
                case "self-check":
                    selfCheck = true;
                    break;
                case "help":
                case "h":
                    help = true;
                    break;
                default:
                    throw new ArgumentException($"Opción desconocida: {argument}");
            }
        }

        var workbook = Path.GetFullPath(positional.Count > 0 ? positional[0] : defaultWorkbook);
        var output = Path.GetFullPath(positional.Count > 1
            ? positional[1]
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tmp", seed ? "seed-data" : "data-audit"));

        return new ImportOptions(workbook, output, contract, mode, snapshot, allowSql, seed, selfCheck, help);
    }

    private static ImportContract ParseContract(string value) => value.ToLowerInvariant() switch
    {
        "auto" => ImportContract.Auto,
        "legacy" => ImportContract.Legacy,
        "v2" => ImportContract.V2,
        _ => throw new ArgumentException("--contract acepta únicamente auto, legacy o v2.")
    };

    private static ImportMode ParseMode(string value) => value.ToLowerInvariant() switch
    {
        "validate" or "validar" => ImportMode.Validate,
        "audit" or "auditar" => ImportMode.Audit,
        "sql" => ImportMode.Sql,
        _ => throw new ArgumentException("--mode acepta únicamente validate, audit o sql.")
    };
}

internal sealed record SourceReference(string Sheet, int Row, string? SourceKey);

internal sealed record CanonicalRow(
    string Entity,
    string NaturalKey,
    IReadOnlyDictionary<string, string?> Fields,
    SourceReference Source,
    bool LegacyInferred = false)
{
    public string Fingerprint()
    {
        var normalized = string.Join("\u001f", Fields
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={Normalize(pair.Value)}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC);
}

internal sealed record CanonicalIssue(
    DataPriority Priority,
    string Code,
    string Message,
    string? Sheet = null,
    int? Row = null,
    string? NaturalKey = null,
    string? SuggestedAction = null);

internal sealed class CanonicalImportPackage
{
    private readonly Dictionary<string, List<CanonicalRow>> _entities = new(StringComparer.OrdinalIgnoreCase);

    public required ImportContract Contract { get; init; }
    public required string WorkbookPath { get; init; }
    public required string WorkbookSha256 { get; init; }
    public required bool Strict { get; init; }
    public IReadOnlyDictionary<string, List<CanonicalRow>> Entities => _entities;
    public List<CanonicalIssue> Issues { get; } = [];
    public List<string> SheetNames { get; } = [];

    public IEnumerable<CanonicalRow> Rows => _entities.Values.SelectMany(rows => rows);

    public void Add(CanonicalRow row)
    {
        if (!_entities.TryGetValue(row.Entity, out var rows))
        {
            rows = [];
            _entities[row.Entity] = rows;
        }

        rows.Add(row);
    }

    public IReadOnlyList<CanonicalRow> Get(string entity) =>
        _entities.TryGetValue(entity, out var rows) ? rows : [];

    public HashSet<string> Keys(string entity) =>
        Get(entity).Select(row => row.NaturalKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}

internal sealed record SnapshotRow(
    string Entity,
    string NaturalKey,
    string? Fingerprint,
    string? SourceKind);

internal sealed record ReconciliationRow(
    string Entity,
    string NaturalKey,
    ReconciliationStatus Status,
    string ExcelFingerprint,
    string? DatabaseFingerprint,
    string Reason);

internal static class LegacyCanonicalAdapter
{
    public static CanonicalImportPackage Convert(AnalysisResult source, string workbookPath)
    {
        var package = new CanonicalImportPackage
        {
            Contract = ImportContract.Legacy,
            WorkbookPath = workbookPath,
            WorkbookSha256 = CanonicalImportPackage.HashFile(workbookPath),
            Strict = false
        };

        package.SheetNames.AddRange(source.SheetSummaries.Select(sheet => sheet.Name));
        foreach (var issue in source.Issues)
        {
            package.Issues.Add(new CanonicalIssue(
                issue.Severity.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ? DataPriority.P0 : DataPriority.P1,
                $"LEGACY_{issue.Severity.ToUpperInvariant()}", issue.Message, issue.Sheet, issue.Row, SuggestedAction: "Revisar la fila original antes de oficializarla."));
        }

        foreach (var reject in source.Rejects)
        {
            package.Issues.Add(new CanonicalIssue(DataPriority.P0, "LEGACY_REJECT", reject.Reason, reject.Sheet, reject.Row,
                SuggestedAction: "Corregir o registrar la fila como pendiente; no se cargará automáticamente."));
        }

        void Add(string entity, string key, string sheet, int row, bool inferred, params (string Key, object? Value)[] fields)
        {
            package.Add(new CanonicalRow(entity, key, fields.ToDictionary(item => item.Key, item => Format(item.Value)),
                new SourceReference(sheet, row, $"{sheet}:{row}"), inferred));
        }

        foreach (var item in source.Faculties) Add("Faculty", item.Name, item.SourceSheet, item.SourceRow, false, ("Name", item.Name), ("Code", item.Code), ("Description", item.Description));
        foreach (var item in source.Careers) Add("Career", item.Name, item.SourceSheet, item.SourceRow, false, ("Name", item.Name), ("FacultyName", item.FacultyName), ("Status", item.Status));
        foreach (var item in source.Laboratories) Add("Laboratory", item.Code, item.SourceSheet, item.SourceRow, false, ("Code", item.Code), ("Name", item.Name), ("FacultyName", item.FacultyName), ("Floor", item.Floor));
        foreach (var item in source.People) Add("Actor", item.Name, item.SourceSheet, item.SourceRow, item.SourceSheet == "system", ("Name", item.Name), ("IsExternal", item.IsExternal), ("IsCompany", item.IsCompany), ("Email", item.Email));
        foreach (var item in source.Managements) Add("Management", item.Code, "8 · Plan de Mantenimiento", item.SourceRow, item.SourceRow == 0, ("Code", item.Code), ("Year", item.Year), ("Semester", item.Semester));
        foreach (var item in source.Equipments) Add("Equipment", item.Key, item.SourceSheet, item.SourceRow, false, ("Name", item.Name), ("Brand", item.Brand), ("Model", item.Model), ("Category", item.Category));
        foreach (var item in source.Units) Add("EquipmentUnit", item.InventoryNumber, item.SourceSheet, item.SourceRow, false, ("InventoryNumber", item.InventoryNumber), ("EquipmentKey", item.EquipmentKey), ("LaboratoryCode", item.LaboratoryCode), ("SerialNumber", item.SerialNumber));
        foreach (var item in source.Verifications) Add("Verification", item.Key, item.SourceSheet, item.SourceRow, item.Date.Date == DateTime.Today, ("InventoryNumber", item.InventoryNumber), ("ManagementCode", item.ManagementCode), ("Date", item.Date), ("Observations", item.Observations));
        foreach (var item in source.Requests) Add("Request", item.Key, item.SourceSheet, item.SourceRow, false, ("InventoryNumber", item.InventoryNumber), ("ManagementCode", item.ManagementCode), ("CreatedDate", item.CreatedDate), ("Description", item.Description));
        foreach (var item in source.Maintenances) Add("Maintenance", item.Key, item.SourceSheet, item.SourceRow, false, ("InventoryNumber", item.InventoryNumber), ("ManagementCode", item.ManagementCode), ("TechnicianName", item.TechnicianName), ("StartDate", item.StartDate));
        foreach (var item in source.CostDetails) Add("CostDetail", $"{item.TargetKind}:{item.TargetKey}:{item.SourceSheet}:{item.SourceRow}", item.SourceSheet, item.SourceRow, false, ("TargetKind", item.TargetKind), ("TargetKey", item.TargetKey), ("Concept", item.Concept), ("Quantity", item.Quantity), ("UnitPrice", item.UnitPrice));
        foreach (var item in source.Departures) Add("Departure", item.Key, item.SourceSheet, item.SourceRow, false, ("InventoryNumber", item.InventoryNumber), ("ManagementCode", item.ManagementCode), ("DepartureDate", item.DepartureDate), ("Borrower", item.BorrowerName));
        foreach (var item in source.DepartureItems) Add("DepartureItem", $"{item.DepartureKey}:{item.SourceSheet}:{item.SourceRow}", item.SourceSheet, item.SourceRow, false, ("DepartureKey", item.DepartureKey), ("InventoryNumber", item.InventoryNumber), ("ProductName", item.ProductName), ("Quantity", item.Quantity));
        foreach (var item in source.ManagementPlans) Add("ManagementPlan", item.Key, item.SourceSheet, item.SourceRow, false, ("ManagementCode", item.ManagementCode), ("InventoryNumber", item.InventoryNumber), ("PlannedDate", item.PlannedDate), ("Status", item.PlanStatus));

        return package;
    }

    private static string? Format(object? value) => value switch
    {
        null => null,
        DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };
}

internal static class CanonicalJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
