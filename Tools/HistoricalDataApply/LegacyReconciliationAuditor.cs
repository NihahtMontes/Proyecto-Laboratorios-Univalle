using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

internal static class LegacyReconciliationAuditor
{
    public static async Task<LegacyReconciliation> CaptureAsync(SqlConnection connection, CanonicalPackageInfo package)
    {
        var databaseRows = await ReadDatabaseRowsAsync(connection);
        var byEntityAndKey = databaseRows.ToDictionary(
            row => $"{row.Entity}|{row.LegacyKey}",
            StringComparer.OrdinalIgnoreCase);
        var matchedDatabaseKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mappings = new List<LegacyReconciliationMapping>();
        var blockers = new List<string>();

        foreach (var verification in Rows(package, "Verification"))
        {
            var sourceRow = ParseSourceRow(verification.NaturalKey, "VER-S03");
            var expectedKey = sourceRow.HasValue ? $"VER_R{sourceRow.Value}" : null;
            AddExpected("Verification", verification, expectedKey, verification.Field("NumeroInventario"), "CanonicalPrimary");
        }

        var requestLinks = Rows(package, "RequestEquipmentUnit")
            .GroupBy(row => row.Field("SolicitudCodigo"), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(row => row.NaturalKey, StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);
        foreach (var request in Rows(package, "Request"))
        {
            var sourceRow = ParseSourceRow(request.NaturalKey, "SOL-S04");
            if (!sourceRow.HasValue)
            {
                mappings.Add(new LegacyReconciliationMapping("Request", request.NaturalKey, null, null, null, null, "Ambiguous", "El código V2 no contiene la fila fuente esperada.", BusinessFingerprint("Request", request), null));
                blockers.Add($"Request {request.NaturalKey}: no se pudo obtener la fila fuente.");
                continue;
            }

            if (!requestLinks.TryGetValue(request.NaturalKey, out var links) || links.Count == 0)
            {
                mappings.Add(new LegacyReconciliationMapping("Request", request.NaturalKey, null, null, null, null, "SourceOnlyWithoutUnit", "Cabecera V2 sin relación a unidad; no existe clave REQ_R determinista.", BusinessFingerprint("Request", request), null));
                continue;
            }

            for (var index = 0; index < links.Count; index++)
            {
                var inventory = links[index].Field("NumeroInventario");
                var expectedKey = $"REQ_R{sourceRow.Value}_{SanitizeKey(inventory)}";
                AddExpected("Request", request, expectedKey, inventory, index == 0 ? "CanonicalPrimary" : "LegacyExplodedDuplicate");
            }
        }

        foreach (var maintenance in Rows(package, "Maintenance"))
        {
            var sourceRow = ParseSourceRow(maintenance.NaturalKey, "MAN-S05");
            var inventory = maintenance.Field("NumeroInventario");
            var expectedKey = sourceRow.HasValue ? $"MNT_R{sourceRow.Value}_{SanitizeKey(inventory)}" : null;
            AddExpected("Maintenance", maintenance, expectedKey, inventory, "CanonicalPrimary");
        }

        foreach (var cost in Rows(package, "CostDetail"))
            AddExpected("CostDetail", cost, cost.NaturalKey, cost.Field("InventarioOrigen"), "CanonicalPrimary");

        foreach (var departure in Rows(package, "Departure"))
            AddExpected("Departure", departure, departure.NaturalKey, string.Empty, "CanonicalPrimary");

        foreach (var detail in Rows(package, "DepartureItem"))
        {
            mappings.Add(new LegacyReconciliationMapping(
                "DepartureItem",
                detail.NaturalKey,
                null,
                null,
                detail.Field("NumeroInventario"),
                null,
                "SourceOnly",
                "DepartureItems no posee clave histórica propia; requiere conciliación por cabecera y huella de detalle.",
                BusinessFingerprint("DepartureItem", detail),
                null));
        }

        foreach (var row in databaseRows.Where(row => !matchedDatabaseKeys.Contains($"{row.Entity}|{row.LegacyKey}")))
        {
            mappings.Add(new LegacyReconciliationMapping(
                row.Entity,
                null,
                row.LegacyKey,
                row.Id,
                row.InventoryNumber,
                row.InventoryNumber,
                "DatabaseOnly",
                SourceKind(row.LegacyKey),
                null,
                row.BusinessFingerprint));
        }

        var orderedMappings = mappings
            .OrderBy(mapping => mapping.Entity, StringComparer.OrdinalIgnoreCase)
            .ThenBy(mapping => mapping.CanonicalKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(mapping => mapping.LegacyKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(mapping => mapping.LegacyId)
            .ToList();
        var mappingSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(orderedMappings))));

        return new LegacyReconciliation(
            RequiresReview: mappings.Count > 0,
            AcceptedInPlan: false,
            MappingSha256: mappingSha256,
            ContentMatched: mappings.Count(mapping => mapping.CanonicalKey is not null && mapping.LegacyId.HasValue && mapping.Disposition is "CanonicalPrimary" or "LegacyExplodedDuplicate"),
            SourceOnly: mappings.Count(mapping => mapping.CanonicalKey is not null && !mapping.LegacyId.HasValue && mapping.Disposition != "Ambiguous"),
            DatabaseOnly: mappings.Count(mapping => mapping.CanonicalKey is null && mapping.LegacyId.HasValue),
            LegacyExplodedDuplicates: mappings.Count(mapping => mapping.Disposition == "LegacyExplodedDuplicate" && mapping.LegacyId.HasValue),
            Ambiguous: mappings.Count(mapping => mapping.Disposition == "Ambiguous"),
            BusinessConflicts: mappings.Count(mapping => mapping.Disposition == "BusinessConflict"),
            Mappings: orderedMappings,
            Blockers: blockers);

        void AddExpected(string entity, CanonicalEntityRow canonicalRow, string? expectedKey, string expectedInventory, string matchedDisposition)
        {
            var canonicalKey = canonicalRow.NaturalKey;
            var expectedBusinessValues = BusinessValues(entity, canonicalRow);
            var expectedBusinessFingerprint = BusinessFingerprint(entity, canonicalRow);
            if (string.IsNullOrWhiteSpace(expectedKey))
            {
                mappings.Add(new LegacyReconciliationMapping(entity, canonicalKey, null, null, expectedInventory, null, "Ambiguous", "El código V2 no permite derivar la clave legacy.", expectedBusinessFingerprint, null));
                blockers.Add($"{entity} {canonicalKey}: no se pudo derivar la clave legacy.");
                return;
            }

            if (!byEntityAndKey.TryGetValue($"{entity}|{expectedKey}", out var databaseRow))
            {
                mappings.Add(new LegacyReconciliationMapping(entity, canonicalKey, expectedKey, null, expectedInventory, null, "SourceOnly", "No existe fila legacy directa; se evaluará como alta después de resolver el contrato semántico.", expectedBusinessFingerprint, null));
                return;
            }

            if (!string.Equals(NormalizeInventory(expectedInventory), NormalizeInventory(databaseRow.InventoryNumber), StringComparison.OrdinalIgnoreCase))
            {
                mappings.Add(new LegacyReconciliationMapping(entity, canonicalKey, expectedKey, databaseRow.Id, expectedInventory, databaseRow.InventoryNumber, "Ambiguous", "La clave coincide, pero el inventario legacy es diferente.", expectedBusinessFingerprint, databaseRow.BusinessFingerprint));
                blockers.Add($"{entity} {canonicalKey}: {expectedKey} apunta al inventario '{databaseRow.InventoryNumber}' y la V2 espera '{expectedInventory}'.");
                return;
            }

            if (!string.Equals(expectedBusinessFingerprint, databaseRow.BusinessFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                matchedDatabaseKeys.Add($"{entity}|{databaseRow.LegacyKey}");
                mappings.Add(new LegacyReconciliationMapping(
                    entity, canonicalKey, databaseRow.LegacyKey, databaseRow.Id,
                    expectedInventory, databaseRow.InventoryNumber,
                    "BusinessConflict",
                    "La clave y el inventario coinciden, pero difieren los campos de negocio estables.",
                    expectedBusinessFingerprint, databaseRow.BusinessFingerprint,
                    expectedBusinessValues, databaseRow.BusinessValues,
                    DifferenceFields(expectedBusinessValues, databaseRow.BusinessValues)));
                blockers.Add($"{entity} {canonicalKey}: la huella de contenido no coincide con {databaseRow.LegacyKey}.");
                return;
            }

            matchedDatabaseKeys.Add($"{entity}|{databaseRow.LegacyKey}");
            mappings.Add(new LegacyReconciliationMapping(
                entity, canonicalKey, databaseRow.LegacyKey, databaseRow.Id,
                expectedInventory, databaseRow.InventoryNumber, matchedDisposition,
                "Coincidencia determinista por hoja/fila original, inventario y huella de negocio estable.",
                expectedBusinessFingerprint, databaseRow.BusinessFingerprint,
                expectedBusinessValues, databaseRow.BusinessValues, []));
        }
    }

    private static IReadOnlyList<CanonicalEntityRow> Rows(CanonicalPackageInfo package, string entity)
        => package.Rows.TryGetValue(entity, out var rows) ? rows : [];

    private static int? ParseSourceRow(string naturalKey, string prefix)
    {
        var match = Regex.Match(naturalKey, $"^{Regex.Escape(prefix)}-R0*(?<row>[0-9]+)$", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups["row"].Value, out var row) ? row : null;
    }

    private static string SanitizeKey(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToUpperInvariant(character));
        }
        return builder.Length == 0 ? "NA" : builder.ToString();
    }

    private static string NormalizeInventory(string? value)
        => new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string BusinessFingerprint(string entity, CanonicalEntityRow row)
        => Fingerprint(BusinessValues(entity, row).Values);

    private static IReadOnlyDictionary<string, string?> BusinessValues(string entity, CanonicalEntityRow row)
    {
        var values = entity switch
        {
            "Verification" => new (string Field, string? Value)[]
            {
                ("GestionCodigo", row.Field("GestionCodigo")),
                ("FechaVerificacion", row.Field("FechaVerificacion"))
            },
            "Request" =>
            [
                ("GestionCodigo", row.Field("GestionCodigo")),
                ("FechaSolicitud", row.Field("FechaSolicitud")),
                ("ProblemaReportado", row.Field("ProblemaReportado"))
            ],
            "Maintenance" =>
            [
                ("GestionCodigo", row.Field("GestionCodigo")),
                ("FechaProgramada", row.Field("FechaProgramada")),
                ("FechaInicio", row.Field("FechaInicio")),
                ("FechaFin", row.Field("FechaFin")),
                ("TrabajoRealizado", row.Field("TrabajoRealizado"))
            ],
            "CostDetail" =>
            [
                ("Fecha", row.Field("Fecha")),
                ("Concepto", row.Field("Concepto")),
                ("Cantidad", row.Field("Cantidad")),
                ("PrecioUnitarioBs", row.Field("PrecioUnitarioBs")),
                ("NumeroFactura", row.Field("NumeroFactura"))
            ],
            "Departure" =>
            [
                ("GestionCodigo", row.Field("GestionCodigo")),
                ("FechaSalida", row.Field("FechaSalida")),
                ("Destino", row.Field("Destino"))
            ],
            "DepartureItem" =>
            [
                ("Referencia", string.IsNullOrWhiteSpace(row.Field("NumeroInventario")) ? row.Field("ArticuloCodigo") : row.Field("NumeroInventario")),
                ("DescripcionOriginal", row.Field("DescripcionOriginal")),
                ("Cantidad", row.Field("Cantidad")),
                ("UnidadMedida", row.Field("UnidadMedida"))
            ],
            _ => [("NaturalKey", row.NaturalKey)]
        };
        return values.ToDictionary(value => value.Field, value => value.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string[] BusinessFieldNames(string entity) => entity switch
    {
        "Verification" => ["GestionCodigo", "FechaVerificacion"],
        "Request" => ["GestionCodigo", "FechaSolicitud", "ProblemaReportado"],
        "Maintenance" => ["GestionCodigo", "FechaProgramada", "FechaInicio", "FechaFin", "TrabajoRealizado"],
        "CostDetail" => ["Fecha", "Concepto", "Cantidad", "PrecioUnitarioBs", "NumeroFactura"],
        "Departure" => ["GestionCodigo", "FechaSalida", "Destino"],
        "DepartureItem" => ["Referencia", "DescripcionOriginal", "Cantidad", "UnidadMedida"],
        _ => ["NaturalKey"]
    };

    private static IReadOnlyList<string> DifferenceFields(
        IReadOnlyDictionary<string, string?> expected,
        IReadOnlyDictionary<string, string?> database)
        => expected.Keys
            .Where(field => !string.Equals(
                NormalizeBusinessValue(expected[field]),
                NormalizeBusinessValue(database.GetValueOrDefault(field)),
                StringComparison.Ordinal))
            .ToList();

    private static string NormalizeBusinessValue(string? value)
        => new((value ?? string.Empty)
            .Normalize(NormalizationForm.FormKC)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

    private static string Fingerprint(IEnumerable<string?> values)
    {
        var normalized = string.Join("|", values.Select(NormalizeBusinessValue));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static string SourceKind(string key)
    {
        if (key.StartsWith("INF_", StringComparison.OrdinalIgnoreCase)) return "InferidoLegado; conservar como SoloBase hasta obtener evidencia.";
        if (key.StartsWith("PUR_", StringComparison.OrdinalIgnoreCase)) return "Adquisición legacy; conservar como SoloBase.";
        return "Registro legacy sin correspondencia V2; conservar como SoloBase y revisar.";
    }

    private static async Task<List<LegacyDatabaseRow>> ReadDatabaseRowsAsync(SqlConnection connection)
    {
        const string sql = """
            SELECT N'Verification' AS EntityName, v.Id, v.HistoricalSourceKey, u.InventoryNumber,
                   mg.Code, CONVERT(nvarchar(30), v.[Date], 23), NULL, NULL, NULL
            FROM dbo.Verifications v
            INNER JOIN dbo.EquipmentUnits u ON u.Id = v.EquipmentUnitId
            INNER JOIN dbo.Managements mg ON mg.Id = v.ManagementId
            WHERE v.HistoricalSourceKey IS NOT NULL
            UNION ALL
            SELECT N'Request', r.Id, r.HistoricalSourceKey, u.InventoryNumber,
                   mg.Code, CONVERT(nvarchar(30), r.CreatedDate, 23), r.Description, NULL, NULL
            FROM dbo.Requests r
            LEFT JOIN dbo.EquipmentUnits u ON u.Id = r.EquipmentUnitId
            INNER JOIN dbo.Managements mg ON mg.Id = r.ManagementId
            WHERE r.HistoricalSourceKey IS NOT NULL
            UNION ALL
            SELECT N'Maintenance', m.Id, m.HistoricalSourceKey, u.InventoryNumber,
                   mg.Code, CONVERT(nvarchar(30), m.ScheduledDate, 23),
                   CONVERT(nvarchar(30), m.StartDate, 23), CONVERT(nvarchar(30), m.EndDate, 23), m.Description
            FROM dbo.Maintenances m
            INNER JOIN dbo.EquipmentUnits u ON u.Id = m.EquipmentUnitId
            INNER JOIN dbo.Managements mg ON mg.Id = m.ManagementId
            WHERE m.HistoricalSourceKey IS NOT NULL
            UNION ALL
            SELECT N'CostDetail', c.Id,
                   COALESCE(c.HistoricalSourceKey, CONCAT(N'DBNULL_COST_', c.Id)),
                   COALESCE(mu.InventoryNumber, ru.InventoryNumber),
                   CONVERT(nvarchar(30), c.CostDate, 23), c.Concept,
                   CONVERT(nvarchar(80), c.Quantity), CONVERT(nvarchar(80), c.UnitPrice), c.InvoiceNumber
            FROM dbo.CostDetails c
            LEFT JOIN dbo.Maintenances cm ON cm.Id = c.MaintenanceId
            LEFT JOIN dbo.EquipmentUnits mu ON mu.Id = cm.EquipmentUnitId
            LEFT JOIN dbo.Requests cr ON cr.Id = c.RequestId
            LEFT JOIN dbo.EquipmentUnits ru ON ru.Id = cr.EquipmentUnitId
            UNION ALL
            SELECT N'Departure', d.Id,
                   COALESCE(d.HistoricalSourceKey, CONCAT(N'DBNULL_DEPARTURE_', d.Id)),
                   u.InventoryNumber,
                   mg.Code, CONVERT(nvarchar(30), d.DepartureDate, 23), d.Destination, NULL, NULL
            FROM dbo.Departures d
            INNER JOIN dbo.Managements mg ON mg.Id = d.ManagementId
            LEFT JOIN dbo.EquipmentUnits u ON u.Id = d.EquipmentUnitId
            UNION ALL
            SELECT N'DepartureItem', di.Id, CONCAT(N'DBNULL_DEPARTURE_ITEM_', di.Id),
                   COALESCE(u.InventoryNumber, a.Code),
                   COALESCE(u.InventoryNumber, a.Code), di.ProductName,
                   CONVERT(nvarchar(80), di.Quantity), di.UnitOfMeasure, NULL
            FROM dbo.DepartureItems di
            LEFT JOIN dbo.EquipmentUnits u ON u.Id = di.EquipmentUnitId
            LEFT JOIN dbo.Articles a ON a.Id = di.ArticleId;
            """;
        var result = new List<LegacyDatabaseRow>();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var entity = reader.GetString(0);
            var businessValues = BusinessFieldNames(entity)
                .Select((field, index) => new
                {
                    Field = field,
                    Value = reader.IsDBNull(4 + index) ? null : reader.GetString(4 + index)
                })
                .ToDictionary(item => item.Field, item => item.Value, StringComparer.OrdinalIgnoreCase);
            var businessFingerprint = Fingerprint(businessValues.Values);
            result.Add(new LegacyDatabaseRow(
                entity,
                reader.GetInt32(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                businessFingerprint,
                businessValues));
        }
        return result;
    }

    private sealed record LegacyDatabaseRow(
        string Entity,
        int Id,
        string LegacyKey,
        string? InventoryNumber,
        string BusinessFingerprint,
        IReadOnlyDictionary<string, string?> BusinessValues);
}

internal sealed record LegacyReconciliation(
    bool RequiresReview,
    bool AcceptedInPlan,
    string MappingSha256,
    int ContentMatched,
    int SourceOnly,
    int DatabaseOnly,
    int LegacyExplodedDuplicates,
    int Ambiguous,
    int BusinessConflicts,
    IReadOnlyList<LegacyReconciliationMapping> Mappings,
    IReadOnlyList<string> Blockers);

internal sealed record LegacyReconciliationMapping(
    string Entity,
    string? CanonicalKey,
    string? LegacyKey,
    int? LegacyId,
    string? ExpectedInventory,
    string? DatabaseInventory,
    string Disposition,
    string Detail,
    string? ExpectedBusinessFingerprint,
    string? DatabaseBusinessFingerprint,
    IReadOnlyDictionary<string, string?>? ExpectedBusinessValues = null,
    IReadOnlyDictionary<string, string?>? DatabaseBusinessValues = null,
    IReadOnlyList<string>? BusinessDifferenceFields = null);

internal static class LegacyReconciliationApproval
{
    public static LegacyReconciliationManifest CreateProposal(
        LegacyReconciliation reconciliation,
        string database,
        string serverFingerprint,
        string packageSha256,
        string snapshotSha256)
    {
        var decisions = reconciliation.Mappings.Select(mapping => new LegacyReconciliationDecision(
            mapping.Entity,
            mapping.CanonicalKey,
            mapping.LegacyKey,
            mapping.LegacyId,
            mapping.Disposition,
            "Pending",
            null)).ToList();
        return new LegacyReconciliationManifest(
            ContractVersion: 1,
            Status: "Proposed",
            CreatedAtUtc: DateTime.UtcNow,
            Database: database,
            ServerFingerprint: serverFingerprint,
            PackageSha256: packageSha256,
            SnapshotSha256: snapshotSha256,
            MappingSha256: reconciliation.MappingSha256,
            ReviewedBy: null,
            ReviewedAtUtc: null,
            ReviewNotes: null,
            Decisions: decisions);
    }

    public static ReconciliationApprovalValidation Validate(
        string? path,
        LegacyReconciliation expected,
        string database,
        string serverFingerprint,
        string packageSha256,
        string snapshotSha256)
    {
        var blockers = new List<string>();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            blockers.Add("Falta --reconciliation con un manifiesto revisado y aprobado.");
            return new ReconciliationApprovalValidation(false, null, null, blockers);
        }

        LegacyReconciliationManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<LegacyReconciliationManifest>(File.ReadAllText(path), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException exception)
        {
            blockers.Add($"El manifiesto de reconciliación no es JSON válido: {exception.Message}");
            return new ReconciliationApprovalValidation(false, Path.GetFullPath(path), HashFile(path), blockers);
        }

        if (manifest is null)
        {
            blockers.Add("No se pudo deserializar el manifiesto de reconciliación.");
            return new ReconciliationApprovalValidation(false, Path.GetFullPath(path), HashFile(path), blockers);
        }

        if (manifest.ContractVersion != 1) blockers.Add("El contrato del manifiesto de reconciliación no es 1.");
        if (!manifest.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)) blockers.Add("El manifiesto de reconciliación no tiene Status=Approved.");
        if (string.IsNullOrWhiteSpace(manifest.ReviewedBy) || !manifest.ReviewedAtUtc.HasValue) blockers.Add("Faltan ReviewedBy o ReviewedAtUtc en la aprobación.");
        if (!string.Equals(manifest.Database, database, StringComparison.OrdinalIgnoreCase)) blockers.Add("La base del manifiesto de reconciliación no coincide.");
        if (!string.Equals(manifest.ServerFingerprint, serverFingerprint, StringComparison.Ordinal)) blockers.Add("La huella del servidor del manifiesto de reconciliación no coincide.");
        if (!string.Equals(manifest.PackageSha256, packageSha256, StringComparison.OrdinalIgnoreCase)) blockers.Add("El hash del paquete del manifiesto de reconciliación no coincide.");
        if (!string.Equals(manifest.SnapshotSha256, snapshotSha256, StringComparison.OrdinalIgnoreCase)) blockers.Add("El hash del snapshot del manifiesto de reconciliación no coincide.");
        if (!string.Equals(manifest.MappingSha256, expected.MappingSha256, StringComparison.OrdinalIgnoreCase)) blockers.Add("El hash de mapeos del manifiesto de reconciliación no coincide.");

        var expectedByKey = expected.Mappings.ToDictionary(Identity, StringComparer.Ordinal);
        var decisionsByKey = new Dictionary<string, LegacyReconciliationDecision>(StringComparer.Ordinal);
        foreach (var decision in manifest.Decisions)
        {
            var key = Identity(decision);
            if (!decisionsByKey.TryAdd(key, decision)) blockers.Add($"Decisión duplicada en reconciliación: {key}.");
        }
        foreach (var (key, mapping) in expectedByKey)
        {
            if (!decisionsByKey.TryGetValue(key, out var decision))
            {
                blockers.Add($"Falta decisión de reconciliación para {key}.");
                continue;
            }
            if (!AllowedDecisions(mapping.Disposition).Contains(decision.Decision, StringComparer.OrdinalIgnoreCase))
            {
                blockers.Add($"Decisión '{decision.Decision}' no permitida para {key} ({mapping.Disposition}).");
            }
        }
        foreach (var extra in decisionsByKey.Keys.Except(expectedByKey.Keys, StringComparer.Ordinal))
            blockers.Add($"El manifiesto contiene una decisión ajena al mapeo vigente: {extra}.");

        return new ReconciliationApprovalValidation(
            blockers.Count == 0,
            Path.GetFullPath(path),
            HashFile(path),
            blockers);
    }

    private static string Identity(LegacyReconciliationMapping mapping)
        => $"{mapping.Entity}|{mapping.CanonicalKey ?? "<null>"}|{mapping.LegacyKey ?? "<null>"}|{mapping.LegacyId?.ToString() ?? "<null>"}|{mapping.Disposition}";

    private static string Identity(LegacyReconciliationDecision decision)
        => $"{decision.Entity}|{decision.CanonicalKey ?? "<null>"}|{decision.LegacyKey ?? "<null>"}|{decision.LegacyId?.ToString() ?? "<null>"}|{decision.Disposition}";

    private static string[] AllowedDecisions(string disposition) => disposition switch
    {
        "CanonicalPrimary" => ["ReuseLegacy", "Quarantine"],
        "LegacyExplodedDuplicate" => ["PreserveDuplicate", "Quarantine"],
        "SourceOnly" => ["Insert", "Quarantine"],
        "SourceOnlyWithoutUnit" => ["Quarantine"],
        "DatabaseOnly" => ["PreserveDatabaseOnly", "Quarantine"],
        "BusinessConflict" => ["UpdateLegacyFromCanonical", "Quarantine"],
        "Ambiguous" => ["Quarantine"],
        _ => ["Quarantine"]
    };

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}

internal sealed record LegacyReconciliationManifest(
    int ContractVersion,
    string Status,
    DateTime CreatedAtUtc,
    string Database,
    string ServerFingerprint,
    string PackageSha256,
    string SnapshotSha256,
    string MappingSha256,
    string? ReviewedBy,
    DateTime? ReviewedAtUtc,
    string? ReviewNotes,
    IReadOnlyList<LegacyReconciliationDecision> Decisions);

internal sealed record LegacyReconciliationDecision(
    string Entity,
    string? CanonicalKey,
    string? LegacyKey,
    int? LegacyId,
    string Disposition,
    string Decision,
    string? Notes);

internal sealed record ReconciliationApprovalValidation(
    bool Approved,
    string? Path,
    string? Sha256,
    IReadOnlyList<string> Blockers);
