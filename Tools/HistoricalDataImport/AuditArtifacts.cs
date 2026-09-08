using System.Globalization;
using System.Text;
using System.Text.Json;

internal sealed class CanonicalArtifactWriter
{
    private readonly string _outputDirectory;
    private readonly CanonicalImportPackage _package;
    private readonly string? _snapshotPath;

    public CanonicalArtifactWriter(string outputDirectory, CanonicalImportPackage package, string? snapshotPath)
    {
        _outputDirectory = outputDirectory;
        _package = package;
        _snapshotPath = snapshotPath;
    }

    public IReadOnlyList<ReconciliationRow> WriteAll()
    {
        Directory.CreateDirectory(_outputDirectory);
        var snapshot = SnapshotReader.Read(_snapshotPath, _package);
        var reconciliation = BuildReconciliation(snapshot);
        RegisterReconciliationBlockers(reconciliation);
        WriteSummary(reconciliation, snapshot is not null);
        WriteIssues();
        WriteReconciliation(reconciliation);
        WriteCanonicalPackage();
        WritePreview();
        return reconciliation;
    }

    public void WriteSqlBlocked(params string[] reasons)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Generación SQL V2 bloqueada");
        builder.AppendLine();
        builder.AppendLine("No se produjo ningún script de escritura de datos de negocio.");
        builder.AppendLine();
        builder.AppendLine("## Motivos");
        foreach (var reason in reasons)
        {
            builder.AppendLine($"- {reason}");
        }

        builder.AppendLine();
        builder.AppendLine("La validación y la conciliación permanecen disponibles para corregir los datos sin asumir relaciones.");
        File.WriteAllText(Path.Combine(_outputDirectory, "sql-bloqueado.md"), builder.ToString(), Encoding.UTF8);
    }

    private List<ReconciliationRow> BuildReconciliation(IReadOnlyList<SnapshotRow>? snapshot)
    {
        var result = new List<ReconciliationRow>();
        if (snapshot is null)
        {
            result.AddRange(_package.Rows.Select(row => new ReconciliationRow(
                row.Entity, row.NaturalKey,
                row.LegacyInferred ? ReconciliationStatus.InferidoLegado : ReconciliationStatus.PendienteCliente,
                row.Fingerprint(), null,
                row.LegacyInferred
                    ? "La fila proviene de una inferencia del contrato legacy."
                    : "No se proporcionó snapshot de base de datos; no es posible afirmar Coincide/SoloExcel/SoloBase.")));
            return result;
        }

        var database = new Dictionary<string, SnapshotRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in snapshot)
        {
            var composite = Composite(row.Entity, row.NaturalKey);
            if (!database.TryAdd(composite, row))
            {
                _package.Issues.Add(new CanonicalIssue(DataPriority.P0, "SNAPSHOT_DUPLICATE_KEY",
                    $"El snapshot repite {row.Entity}/{row.NaturalKey}.", NaturalKey: row.NaturalKey,
                    SuggestedAction: "Regenerar el snapshot desde una consulta que garantice claves únicas."));
            }
        }

        var excelKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in _package.Rows)
        {
            var composite = Composite(row.Entity, row.NaturalKey);
            excelKeys.Add(composite);
            var excelFingerprint = row.Fingerprint();
            if (row.LegacyInferred)
            {
                result.Add(new ReconciliationRow(row.Entity, row.NaturalKey, ReconciliationStatus.InferidoLegado,
                    excelFingerprint, database.TryGetValue(composite, out var inferredDb) ? inferredDb.Fingerprint : null,
                    "El valor fue inferido por el contrato legacy y requiere confirmación."));
                continue;
            }

            if (row.Entity.Equals("DataQualityIssue", StringComparison.OrdinalIgnoreCase)
                && !CanonicalPackageValidator.Value(row, "EstadoResolucion").Equals("Resuelta", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new ReconciliationRow(row.Entity, row.NaturalKey, ReconciliationStatus.PendienteCliente,
                    excelFingerprint, database.TryGetValue(composite, out var pendingDb) ? pendingDb.Fingerprint : null,
                    "La incidencia de calidad sigue abierta."));
                continue;
            }

            if (!database.TryGetValue(composite, out var dbRow))
            {
                result.Add(new ReconciliationRow(row.Entity, row.NaturalKey, ReconciliationStatus.SoloExcel,
                    excelFingerprint, null, "La clave no aparece en el snapshot suministrado."));
            }
            else if (string.IsNullOrWhiteSpace(dbRow.Fingerprint))
            {
                result.Add(new ReconciliationRow(row.Entity, row.NaturalKey, ReconciliationStatus.Conflicto,
                    excelFingerprint, null, "El snapshot no contiene Fingerprint; la igualdad no puede comprobarse."));
            }
            else if (excelFingerprint.Equals(dbRow.Fingerprint, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new ReconciliationRow(row.Entity, row.NaturalKey, ReconciliationStatus.Coincide,
                    excelFingerprint, dbRow.Fingerprint, "Clave y huella canónica coinciden."));
            }
            else
            {
                result.Add(new ReconciliationRow(row.Entity, row.NaturalKey, ReconciliationStatus.Conflicto,
                    excelFingerprint, dbRow.Fingerprint, "La clave coincide, pero la huella de contenido es diferente."));
            }
        }

        result.AddRange(database
            .Where(pair => !excelKeys.Contains(pair.Key))
            .Select(pair => new ReconciliationRow(pair.Value.Entity, pair.Value.NaturalKey,
                pair.Value.SourceKind?.Equals("LegacyInferred", StringComparison.OrdinalIgnoreCase) == true
                    ? ReconciliationStatus.InferidoLegado
                    : ReconciliationStatus.SoloBase,
                string.Empty, pair.Value.Fingerprint,
                "La clave existe en el snapshot, pero no en el Excel V2.")));
        return result;
    }

    private void RegisterReconciliationBlockers(IEnumerable<ReconciliationRow> reconciliation)
    {
        foreach (var conflict in reconciliation.Where(row => row.Status == ReconciliationStatus.Conflicto))
        {
            _package.Issues.Add(new CanonicalIssue(DataPriority.P0, "RECONCILIATION_CONFLICT",
                $"Conflicto de conciliación en {conflict.Entity}/{conflict.NaturalKey}: {conflict.Reason}",
                NaturalKey: conflict.NaturalKey,
                SuggestedAction: "Comparar ambos valores y aprobar explícitamente cuál prevalece."));
        }
    }

    private void WriteSummary(IReadOnlyList<ReconciliationRow> reconciliation, bool hasSnapshot)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Diagnóstico profesional de importación histórica");
        builder.AppendLine();
        builder.AppendLine($"- Contrato detectado: **{_package.Contract}**");
        builder.AppendLine($"- Modo estricto sin inferencias: **{(_package.Strict ? "Sí" : "No (compatibilidad legacy)")}**");
        builder.AppendLine($"- SHA-256 del archivo: `{_package.WorkbookSha256}`");
        builder.AppendLine($"- Filas canónicas: **{_package.Rows.Count()}**");
        builder.AppendLine($"- Snapshot de base: **{(hasSnapshot ? "Disponible" : "No proporcionado")}**");
        builder.AppendLine();
        builder.AppendLine("## Prioridad y urgencia");
        builder.AppendLine();
        builder.AppendLine("- **P0 — Bloqueante inmediato:** impide generar/aplicar SQL; incluye contrato roto, FK inexistente, claves repetidas, exclusividad inválida o conflicto con la base.");
        builder.AppendLine("- **P1 — Alta, antes de QA:** dato ambiguo que debe confirmar la cliente o el responsable institucional.");
        builder.AppendLine("- **P2 — Control y mejora:** calidad no bloqueante que debe cerrarse antes de la entrega definitiva.");
        builder.AppendLine();
        foreach (var priority in Enum.GetValues<DataPriority>())
        {
            builder.AppendLine($"- {priority}: {_package.Issues.Count(issue => issue.Priority == priority)}");
        }

        builder.AppendLine();
        builder.AppendLine("## Conteos por entidad");
        foreach (var entity in _package.Entities.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"- {entity.Key}: {entity.Value.Count}");
        }

        builder.AppendLine();
        builder.AppendLine("## Conciliación");
        foreach (var status in Enum.GetValues<ReconciliationStatus>())
        {
            builder.AppendLine($"- {status}: {reconciliation.Count(row => row.Status == status)}");
        }

        builder.AppendLine();
        builder.AppendLine("## Decisión de carga");
        builder.AppendLine(_package.Issues.Any(issue => issue.Priority == DataPriority.P0)
            ? "**BLOQUEADA.** Resolver todos los P0 y volver a generar la conciliación."
            : hasSnapshot
                ? "**ELEGIBLE PARA PREPARAR DELTA.** Requiere todavía revisión humana y autorización `--allow-sql`."
                : "**SOLO VALIDACIÓN.** Falta un snapshot de base para calcular un delta seguro.");
        builder.AppendLine();
        builder.AppendLine("El importador nunca se conecta ni escribe directamente en SQL Server; cualquier SQL producido es un artefacto offline para revisión.");
        File.WriteAllText(Path.Combine(_outputDirectory, "resumen.md"), builder.ToString(), Encoding.UTF8);
    }

    private void WriteIssues()
    {
        var rows = _package.Issues
            .OrderBy(issue => issue.Priority)
            .ThenBy(issue => issue.Sheet)
            .ThenBy(issue => issue.Row)
            .Select(issue => new[]
            {
                issue.Priority.ToString(), issue.Code, issue.Sheet, issue.Row?.ToString(CultureInfo.InvariantCulture),
                issue.NaturalKey, issue.Message, issue.SuggestedAction
            });
        CsvFile.Write(Path.Combine(_outputDirectory, "incidencias.csv"),
            ["Priority", "Code", "Sheet", "Row", "NaturalKey", "Message", "SuggestedAction"], rows);
    }

    private void WriteReconciliation(IEnumerable<ReconciliationRow> rows)
    {
        CsvFile.Write(Path.Combine(_outputDirectory, "reconciliacion.csv"),
            ["Entity", "NaturalKey", "Status", "ExcelFingerprint", "DatabaseFingerprint", "Reason"],
            rows.Select(row => new[] { row.Entity, row.NaturalKey, row.Status.ToString(), row.ExcelFingerprint, row.DatabaseFingerprint, row.Reason }));
    }

    private void WriteCanonicalPackage()
    {
        var document = new
        {
            contract = _package.Contract.ToString(),
            strict = _package.Strict,
            workbookSha256 = _package.WorkbookSha256,
            sheets = _package.SheetNames,
            entities = _package.Entities.ToDictionary(pair => pair.Key, pair => pair.Value.Select(row => new
            {
                row.NaturalKey,
                row.Fields,
                row.Source,
                row.LegacyInferred,
                fingerprint = row.Fingerprint()
            }))
        };
        File.WriteAllText(Path.Combine(_outputDirectory, "paquete-canonico.json"),
            JsonSerializer.Serialize(document, CanonicalJson.Options), Encoding.UTF8);
    }

    private void WritePreview()
    {
        CsvFile.Write(Path.Combine(_outputDirectory, "vista-canonica.csv"),
            ["Entity", "NaturalKey", "Fingerprint", "SourceSheet", "SourceRow", "LegacyInferred", "FieldsJson"],
            _package.Rows.Select(row => new[]
            {
                row.Entity, row.NaturalKey, row.Fingerprint(), row.Source.Sheet,
                row.Source.Row.ToString(CultureInfo.InvariantCulture), row.LegacyInferred ? "true" : "false",
                JsonSerializer.Serialize(row.Fields)
            }));
    }

    private static string Composite(string entity, string key) => $"{entity}\u001f{key}";
}

internal static class SnapshotReader
{
    public static IReadOnlyList<SnapshotRow>? Read(string? path, CanonicalImportPackage package)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!File.Exists(path))
        {
            package.Issues.Add(new CanonicalIssue(DataPriority.P0, "SNAPSHOT_NOT_FOUND",
                $"No existe el snapshot '{path}'.", SuggestedAction: "Exportar el snapshot y repetir la conciliación."));
            return [];
        }

        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length == 0)
        {
            package.Issues.Add(new CanonicalIssue(DataPriority.P0, "SNAPSHOT_EMPTY", "El snapshot está vacío."));
            return [];
        }

        var headers = CsvFile.ParseLine(lines[0]);
        var positions = headers.Select((header, index) => (header, index))
            .ToDictionary(item => item.header, item => item.index, StringComparer.OrdinalIgnoreCase);
        if (!positions.ContainsKey("Entity") || !positions.ContainsKey("NaturalKey"))
        {
            package.Issues.Add(new CanonicalIssue(DataPriority.P0, "SNAPSHOT_CONTRACT_INVALID",
                "El snapshot debe incluir Entity y NaturalKey; Fingerprint y SourceKind son opcionales."));
            return [];
        }

        string? Get(IReadOnlyList<string> cells, string name) =>
            positions.TryGetValue(name, out var index) && index < cells.Count && !string.IsNullOrWhiteSpace(cells[index])
                ? cells[index].Trim()
                : null;

        var rows = new List<SnapshotRow>();
        for (var lineNumber = 1; lineNumber < lines.Length; lineNumber++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineNumber]))
            {
                continue;
            }

            var cells = CsvFile.ParseLine(lines[lineNumber]);
            var entity = Get(cells, "Entity");
            var key = Get(cells, "NaturalKey");
            if (entity is null || key is null)
            {
                package.Issues.Add(new CanonicalIssue(DataPriority.P0, "SNAPSHOT_ROW_INVALID",
                    "La fila de snapshot no tiene Entity/NaturalKey.", "SNAPSHOT", lineNumber + 1));
                continue;
            }

            rows.Add(new SnapshotRow(entity, key, Get(cells, "Fingerprint"), Get(cells, "SourceKind")));
        }

        return rows;
    }
}

internal static class NormalizedSqlBuilder
{
    public static string Build(CanonicalImportPackage package, IReadOnlyList<ReconciliationRow> reconciliation)
    {
        var allowed = reconciliation
            .Where(row => row.Status == ReconciliationStatus.SoloExcel)
            .Select(row => $"{row.Entity}\u001f{row.NaturalKey}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = package.Rows.Where(row => allowed.Contains($"{row.Entity}\u001f{row.NaturalKey}")).ToList();
        var batchCode = $"HIST-V2-{package.WorkbookSha256[..12]}";
        var builder = new StringBuilder();
        builder.AppendLine("SET NOCOUNT ON;");
        builder.AppendLine("SET XACT_ABORT ON;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.ImportBatches', N'U') IS NULL THROW 51020, 'Falta dbo.ImportBatches.', 1;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.ImportSourceRows', N'U') IS NULL THROW 51021, 'Falta dbo.ImportSourceRows.', 1;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.DataQualityIssues', N'U') IS NULL THROW 51022, 'Falta dbo.DataQualityIssues.', 1;");
        builder.AppendLine("IF COL_LENGTH(N'dbo.Equipments', N'CatalogCode') IS NULL THROW 51023, 'El esquema normalizado V2 no está aplicado: Equipments.CatalogCode.', 1;");
        builder.AppendLine("IF COL_LENGTH(N'dbo.Careers', N'Code') IS NULL THROW 51024, 'El esquema normalizado V2 no está aplicado: Careers.Code.', 1;");
        builder.AppendLine("IF COL_LENGTH(N'dbo.People', N'ActorCode') IS NULL THROW 51025, 'El esquema normalizado V2 no está aplicado: People.ActorCode.', 1;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.RequestEquipmentUnits', N'U') IS NULL THROW 51026, 'Falta dbo.RequestEquipmentUnits.', 1;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.MaintenanceRequests', N'U') IS NULL THROW 51027, 'Falta dbo.MaintenanceRequests.', 1;");
        builder.AppendLine("IF OBJECT_ID(N'dbo.Articles', N'U') IS NULL THROW 51028, 'Falta dbo.Articles.', 1;");
        builder.AppendLine("BEGIN TRY");
        builder.AppendLine("BEGIN TRANSACTION;");
        builder.AppendLine("DECLARE @Now datetime2 = SYSUTCDATETIME();");
        builder.AppendLine("DECLARE @BatchId int;");
        builder.AppendLine($"SELECT @BatchId = Id FROM dbo.ImportBatches WHERE Code = {Sql(batchCode)};");
        builder.AppendLine("IF @BatchId IS NULL");
        builder.AppendLine("BEGIN");
        builder.AppendLine($"    INSERT INTO dbo.ImportBatches (Code, SourceType, ContractVersion, SourceName, SourceSha256, ImportedAt, Status, Notes) VALUES ({Sql(batchCode)}, N'Excel', N'v2-normalized', {Sql(Path.GetFileName(package.WorkbookPath))}, {Sql(package.WorkbookSha256)}, @Now, 0, N'Delta conciliado; filas de negocio quedan Pending hasta ejecución por el adaptador transaccional.');");
        builder.AppendLine("    SET @BatchId = CONVERT(int, SCOPE_IDENTITY());");
        builder.AppendLine("END;");
        builder.AppendLine();
        builder.AppendLine("-- Este script registra el delta aprobado de forma idempotente en la capa de gobierno.");
        builder.AppendLine("-- No elimina ni sobrescribe datos de negocio; el adaptador transaccional debe resolver las FK por códigos externos.");

        foreach (var row in rows)
        {
            var payload = JsonSerializer.Serialize(row.Fields);
            builder.AppendLine($"IF NOT EXISTS (SELECT 1 FROM dbo.ImportSourceRows WHERE ImportBatchId=@BatchId AND SourceRowKey={Sql(row.Source.SourceKey ?? $"{row.Source.Sheet}:{row.Source.Row}")} AND TargetEntityName={Sql(row.Entity)})");
            builder.AppendLine($"    INSERT INTO dbo.ImportSourceRows (ImportBatchId, SourceSheet, SourceRowNumber, SourceRowKey, OriginalIdentifier, TargetEntityName, TargetEntityKey, MigrationStatus, ReconciliationStatus, OriginalDataJson, Notes, CreatedDate) VALUES (@BatchId, {Sql(row.Source.Sheet)}, {row.Source.Row}, {Sql(row.Source.SourceKey ?? $"{row.Source.Sheet}:{row.Source.Row}")}, {Sql(row.NaturalKey)}, {Sql(row.Entity)}, {Sql(row.NaturalKey)}, 0, 2, {Sql(payload)}, N'Pendiente de aplicación al esquema normalizado; no inferir relaciones.', @Now);");
        }

        foreach (var issue in package.Issues)
        {
            var severity = issue.Priority switch { DataPriority.P0 => 3, DataPriority.P1 => 2, _ => 1 };
            builder.AppendLine($"IF NOT EXISTS (SELECT 1 FROM dbo.DataQualityIssues WHERE ImportBatchId=@BatchId AND IssueCode={Sql(issue.Code)} AND ISNULL(EntityKey,N'')=ISNULL({Sql(issue.NaturalKey)},N'') AND ISNULL(SourceSheet,N'')=ISNULL({Sql(issue.Sheet)},N'') AND ISNULL(SourceRowNumber,-1)=ISNULL({Number(issue.Row)},-1))");
            builder.AppendLine($"    INSERT INTO dbo.DataQualityIssues (ImportBatchId, IssueCode, EntityName, EntityKey, SourceSheet, SourceRowNumber, Description, Severity, Status, CreatedDate, ResolutionNotes) VALUES (@BatchId, {Sql(issue.Code)}, N'CanonicalImport', {Sql(issue.NaturalKey)}, {Sql(issue.Sheet)}, {Number(issue.Row)}, {Sql(Truncate(issue.Message, 1000))}, {severity}, 0, @Now, {Sql(issue.SuggestedAction)});");
        }

        builder.AppendLine("COMMIT TRANSACTION;");
        builder.AppendLine("END TRY");
        builder.AppendLine("BEGIN CATCH");
        builder.AppendLine("    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;");
        builder.AppendLine("    THROW;");
        builder.AppendLine("END CATCH;");
        return builder.ToString();
    }

    private static string Sql(string? value) => string.IsNullOrWhiteSpace(value) ? "NULL" : $"N'{value.Replace("'", "''")}'";
    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "NULL";
    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}

internal static class CsvFile
{
    public static void Write(string path, string[] headers, IEnumerable<string?[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(Escape)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(Escape)));
        }

        File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
    }

    public static List<string> ParseLine(string line)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        cells.Add(current.ToString());
        return cells;
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
