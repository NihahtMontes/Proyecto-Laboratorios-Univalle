using System.Text.Json;

internal static class SnapshotManifestValidator
{
    public static SnapshotValidation Validate(string path, DatabaseInfo live)
    {
        var blockers = new List<string>();
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        var version = Int(root, "manifestVersion");
        var phase = Text(root, "phase");
        var database = Text(root, "database");
        var serverFingerprint = Text(root, "serverFingerprint");
        var checksPassed = Bool(root, "checksPassed");
        if (version != 1) blockers.Add("El snapshot no usa manifestVersion=1.");
        if (phase is not ("preflight" or "postflight" or "snapshot"))
            blockers.Add("El snapshot no declara phase preflight/postflight/snapshot.");
        if (!checksPassed) blockers.Add("El snapshot no fue aprobado por CloudMigrationGuard.");
        if (!string.Equals(database, live.Database, StringComparison.OrdinalIgnoreCase))
            blockers.Add($"El snapshot corresponde a '{database}' y la conexión actual a '{live.Database}'.");
        if (!string.Equals(serverFingerprint, live.ServerFingerprint, StringComparison.Ordinal))
            blockers.Add("La huella de servidor del snapshot no coincide con la conexión actual.");

        var snapshotMigrations = Strings(root, "appliedMigrations");
        if (!snapshotMigrations.SequenceEqual(live.AppliedMigrations, StringComparer.OrdinalIgnoreCase))
            blockers.Add("Las migraciones aplicadas no coinciden exactamente con el snapshot.");

        var snapshotCounts = LongDictionary(root, "rowCounts");
        foreach (var (table, expected) in snapshotCounts)
        {
            if (!live.RowCounts.TryGetValue(table, out var actual))
                blockers.Add($"La tabla del snapshot '{table}' no existe en la base actual.");
            else if (actual != expected)
                blockers.Add($"Conteo distinto al snapshot en {table}: snapshot={expected}, actual={actual}.");
        }
        foreach (var table in live.RowCounts.Keys.Except(snapshotCounts.Keys, StringComparer.OrdinalIgnoreCase))
            blockers.Add($"La base actual contiene la tabla '{table}' que no figura en el snapshot.");

        return new SnapshotValidation(
            IsValid: blockers.Count == 0,
            ManifestVersion: version,
            Phase: phase,
            Database: database,
            ServerFingerprint: serverFingerprint,
            ChecksPassed: checksPassed,
            AppliedMigrations: snapshotMigrations,
            RowCounts: snapshotCounts,
            Blockers: blockers);
    }

    private static string Text(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static int Int(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : 0;

    private static bool Bool(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False && value.GetBoolean();

    private static IReadOnlyList<string> Strings(JsonElement root, string name)
        => root.TryGetProperty(name, out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToList()
            : [];

    private static IReadOnlyDictionary<string, long> LongDictionary(JsonElement root, string name)
    {
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty(name, out var values) || values.ValueKind != JsonValueKind.Object) return result;
        foreach (var property in values.EnumerateObject())
        {
            if (property.Value.TryGetInt64(out var value)) result[property.Name] = value;
        }
        return result;
    }
}

internal sealed record SnapshotValidation(
    bool IsValid,
    int ManifestVersion,
    string Phase,
    string Database,
    string ServerFingerprint,
    bool ChecksPassed,
    IReadOnlyList<string> AppliedMigrations,
    IReadOnlyDictionary<string, long> RowCounts,
    IReadOnlyList<string> Blockers);
