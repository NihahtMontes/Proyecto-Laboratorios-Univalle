using System.Reflection;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true
};

try
{
    var options = ApplyOptions.Parse(args);
    if (options.ShowHelp)
    {
        ApplyOptions.PrintHelp();
        return 0;
    }

    var exitCode = options.Mode switch
    {
        ApplyMode.Plan => await CreatePlanAsync(options),
        ApplyMode.Apply => await ApplyAsync(options),
        _ => throw new ArgumentOutOfRangeException()
    };
    return exitCode;
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine($"Argumentos inválidos: {exception.Message}");
    ApplyOptions.PrintHelp();
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"HistoricalDataApply falló: {Redact(exception.Message)}");
    return 3;
}

async Task<int> CreatePlanAsync(ApplyOptions options)
{
    RequireFile(options.PackagePath, "--package");
    RequireFile(options.IssuesPath, "--issues");
    RequireFile(options.SnapshotPath, "--snapshot");

    var package = await ReadPackageAsync(options.PackagePath!);
    var issues = ReadIssues(options.IssuesPath!);
    var connectionString = ConnectionStringResolver.Resolve();
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var database = await CaptureDatabaseAsync(connection);
    var snapshotValidation = SnapshotManifestValidator.Validate(options.SnapshotPath!, database);
    var legacyReconciliation = await LegacyReconciliationAuditor.CaptureAsync(connection, package);
    var packageSha256 = HashFile(options.PackagePath!);
    var snapshotSha256 = HashFile(options.SnapshotPath!);
    var proposalPath = Path.GetFullPath(options.ReconciliationOutputPath
        ?? Path.ChangeExtension(options.OutputPath!, ".reconciliation.proposed.json"));
    var proposal = LegacyReconciliationApproval.CreateProposal(
        legacyReconciliation,
        database.Database,
        database.ServerFingerprint,
        packageSha256,
        snapshotSha256);
    await WriteJsonAsync(proposalPath, proposal);
    var proposalSha256 = HashFile(proposalPath);
    var approvalValidation = LegacyReconciliationApproval.Validate(
        options.ReconciliationPath,
        legacyReconciliation,
        database.Database,
        database.ServerFingerprint,
        packageSha256,
        snapshotSha256);

    var blockers = new List<string>();
    if (!string.Equals(database.Database, options.ExpectedDatabase, StringComparison.OrdinalIgnoreCase))
    {
        blockers.Add($"La base conectada es '{database.Database}' y no la esperada '{options.ExpectedDatabase}'.");
    }

    if (!database.SchemaReady)
    {
        blockers.Add("El esquema V2 todavía no está completo.");
    }

    blockers.AddRange(snapshotValidation.Blockers.Select(blocker => $"SNAPSHOT_MISMATCH: {blocker}"));

    if (issues.P0 > 0)
    {
        blockers.Add($"Existen {issues.P0} incidencias P0; la carga técnica permanece bloqueada.");
    }

    foreach (var finding in package.Readiness.Findings.Where(finding => finding.BlocksApply))
    {
        blockers.Add($"{finding.Code}: {finding.Detail}");
    }

    blockers.AddRange(legacyReconciliation.Blockers);
    blockers.AddRange(approvalValidation.Blockers.Select(blocker => $"LEGACY_RECONCILIATION_REVIEW_REQUIRED: {blocker}"));

    SqlInspection? sql = null;
    if (string.IsNullOrWhiteSpace(options.SqlPath))
    {
        blockers.Add("Falta --sql con el delta transaccional revisado.");
    }
    else
    {
        RequireFile(options.SqlPath, "--sql");
        sql = InspectSql(options.SqlPath!, packageSha256, snapshotSha256, approvalValidation.Sha256);
        blockers.AddRange(sql.Blockers);
    }

    var report = new HistoricalApplyPlan(
        ContractVersion: 4,
        CreatedAtUtc: DateTime.UtcNow,
        Database: database.Database,
        ServerFingerprint: database.ServerFingerprint,
        DatabaseRowCounts: database.RowCounts,
        DatabaseFingerprints: database.DataFingerprints,
        AppliedMigrations: database.AppliedMigrations,
        SchemaReady: database.SchemaReady,
        PackagePath: Path.GetFullPath(options.PackagePath!),
        PackageSha256: packageSha256,
        WorkbookSha256: package.WorkbookSha256,
        IssuesPath: Path.GetFullPath(options.IssuesPath!),
        IssuesSha256: HashFile(options.IssuesPath!),
        SnapshotPath: Path.GetFullPath(options.SnapshotPath!),
        SnapshotSha256: snapshotSha256,
        SnapshotValidation: snapshotValidation,
        ReconciliationProposalPath: proposalPath,
        ReconciliationProposalSha256: proposalSha256,
        ReconciliationApproval: approvalValidation,
        SqlPath: options.SqlPath is null ? null : Path.GetFullPath(options.SqlPath),
        SqlSha256: sql?.Sha256,
        EntityCounts: package.EntityCounts,
        Issues: issues,
        PackageReadiness: package.Readiness,
        LegacyReconciliation: legacyReconciliation with
        {
            AcceptedInPlan = approvalValidation.Approved && legacyReconciliation.Blockers.Count == 0
        },
        SqlInspection: sql,
        CanApply: blockers.Count == 0,
        Blockers: blockers);

    await WriteJsonAsync(options.OutputPath!, report);
    Console.WriteLine(report.CanApply
        ? "PLAN APROBADO: paquete, snapshot, SQL y base cumplen las puertas de aplicación."
        : $"PLAN BLOQUEADO: {blockers.Count} condición(es) impiden la carga.");
    Console.WriteLine($"Evidencia: {Path.GetFullPath(options.OutputPath!)}");
    Console.WriteLine($"Propuesta de reconciliación por fila: {proposalPath}");
    return report.CanApply ? 0 : 1;
}

async Task<int> ApplyAsync(ApplyOptions options)
{
    RequireFile(options.PlanPath, "--plan");
    var plan = JsonSerializer.Deserialize<HistoricalApplyPlan>(await File.ReadAllTextAsync(options.PlanPath!), jsonOptions)
        ?? throw new InvalidDataException("No se pudo leer el plan aprobado.");
    if (plan.ContractVersion < 4 || plan.PackageReadiness is null)
    {
        throw new InvalidOperationException("El plan usa un contrato anterior y no contiene la auditoría semántica V2; regenere plan.");
    }
    if (plan.PackageReadiness.BlockingFindings > 0)
    {
        throw new InvalidOperationException("La auditoría semántica V2 contiene bloqueos; apply no realizará escrituras.");
    }
    if (plan.LegacyReconciliation is null || !plan.LegacyReconciliation.AcceptedInPlan || plan.LegacyReconciliation.Blockers.Count > 0)
    {
        throw new InvalidOperationException("La reconciliación legacy→V2 no fue aceptada en el plan; apply no realizará escrituras.");
    }
    if (!plan.CanApply || plan.Blockers.Count != 0)
    {
        throw new InvalidOperationException("El plan no está aprobado; apply no realizará escrituras.");
    }

    RequireFile(plan.PackagePath, "package del plan");
    RequireFile(plan.IssuesPath, "issues del plan");
    RequireFile(plan.SnapshotPath, "snapshot del plan");
    RequireFile(plan.SqlPath, "SQL del plan");
    RequireFile(plan.ReconciliationApproval?.Path, "reconciliación aprobada del plan");
    RequireHash(plan.PackagePath, plan.PackageSha256, "paquete");
    RequireHash(plan.IssuesPath, plan.IssuesSha256, "incidencias");
    RequireHash(plan.SnapshotPath, plan.SnapshotSha256, "snapshot");
    RequireHash(plan.SqlPath!, plan.SqlSha256!, "SQL");
    RequireHash(plan.ReconciliationApproval!.Path!, plan.ReconciliationApproval.Sha256!, "reconciliación aprobada");

    var package = await ReadPackageAsync(plan.PackagePath);
    if (package.Readiness.BlockingFindings > 0)
    {
        throw new InvalidOperationException($"La auditoría semántica regenerada contiene {package.Readiness.BlockingFindings} bloqueo(s); apply no realizará escrituras.");
    }
    var issues = ReadIssues(plan.IssuesPath);
    if (issues.P0 != 0)
    {
        throw new InvalidOperationException($"Se detectaron {issues.P0} P0 al revalidar; apply queda bloqueado.");
    }

    var sqlInspection = InspectSql(plan.SqlPath!, plan.PackageSha256, plan.SnapshotSha256, plan.ReconciliationApproval!.Sha256);
    if (sqlInspection.Blockers.Count != 0)
    {
        throw new InvalidOperationException("El SQL dejó de cumplir la política segura.");
    }

    var connectionString = ConnectionStringResolver.Resolve();
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var database = await CaptureDatabaseAsync(connection);
    if (!string.Equals(database.Database, options.ExpectedDatabase, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("--expected-database no coincide con la base abierta.");
    }
    if (!string.Equals(database.Database, plan.Database, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(database.ServerFingerprint, plan.ServerFingerprint, StringComparison.Ordinal))
    {
        throw new InvalidOperationException("La base o el servidor no coinciden con el plan firmado.");
    }
    var snapshotValidation = SnapshotManifestValidator.Validate(plan.SnapshotPath, database);
    if (!snapshotValidation.IsValid)
    {
        throw new InvalidOperationException("La base cambió respecto del snapshot aprobado; apply no realizará escrituras.");
    }
    EnsureSameCounts(plan.DatabaseRowCounts, database.RowCounts, "plan");
    EnsureSameFingerprints(plan.DatabaseFingerprints, database.DataFingerprints, "plan");
    if (!plan.AppliedMigrations.SequenceEqual(database.AppliedMigrations, StringComparer.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Las migraciones aplicadas cambiaron desde la creación del plan.");
    }
    var liveReconciliation = await LegacyReconciliationAuditor.CaptureAsync(connection, package);
    if (liveReconciliation.Blockers.Count > 0
        || !string.Equals(liveReconciliation.MappingSha256, plan.LegacyReconciliation.MappingSha256, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("La reconciliación legacy→V2 cambió o contiene ambigüedades; apply no realizará escrituras.");
    }
    var approvalValidation = LegacyReconciliationApproval.Validate(
        plan.ReconciliationApproval.Path,
        liveReconciliation,
        database.Database,
        database.ServerFingerprint,
        plan.PackageSha256,
        plan.SnapshotSha256);
    if (!approvalValidation.Approved)
    {
        throw new InvalidOperationException("La aprobación por fila ya no coincide con la base, paquete o snapshot vigentes.");
    }

    var isQa = database.Database.EndsWith("_QA", StringComparison.OrdinalIgnoreCase);
    if (!isQa)
    {
        ValidateProductionEvidence(options, plan, database.Database);
    }

    var before = database.RowCounts;
    var fingerprintsBefore = database.DataFingerprints;
    await ExecuteSqlAsync(connection, await File.ReadAllTextAsync(plan.SqlPath!));
    var afterFirstState = await CaptureDatabaseAsync(connection);
    var afterFirst = afterFirstState.RowCounts;

    if (isQa)
    {
        await ExecuteSqlAsync(connection, await File.ReadAllTextAsync(plan.SqlPath!));
        var afterSecondState = await CaptureDatabaseAsync(connection);
        EnsureSameCounts(afterFirstState.RowCounts, afterSecondState.RowCounts, "primer apply");
        EnsureSameFingerprints(afterFirstState.DataFingerprints, afterSecondState.DataFingerprints, "primer apply");
    }

    var result = new ApplyResult(
        ContractVersion: 2,
        AppliedAtUtc: DateTime.UtcNow,
        Database: database.Database,
        ServerFingerprint: database.ServerFingerprint,
        PlanSha256: HashFile(options.PlanPath!),
        PackageSha256: plan.PackageSha256,
        SnapshotSha256: plan.SnapshotSha256,
        SqlSha256: plan.SqlSha256!,
        IsQa: isQa,
        CountsBefore: before,
        CountsAfter: afterFirst,
        FingerprintsBefore: fingerprintsBefore,
        FingerprintsAfter: afterFirstState.DataFingerprints,
        ChecksPassed: true);
    await WriteJsonAsync(options.OutputPath!, result);
    Console.WriteLine(isQa
        ? "APPLY QA APROBADO: el delta se ejecutó dos veces y conservó conteos idempotentes."
        : "APPLY PRODUCCIÓN COMPLETADO: identidad y hashes coincidieron con la evidencia aprobada.");
    Console.WriteLine($"Evidencia: {Path.GetFullPath(options.OutputPath!)}");
    return 0;
}

void EnsureSameCounts(IReadOnlyDictionary<string, long> expected, IReadOnlyDictionary<string, long> actual, string source)
{
    foreach (var (table, count) in expected)
    {
        if (!actual.TryGetValue(table, out var current) || current != count)
            throw new InvalidOperationException($"El conteo de {table} cambió respecto de {source}: {count} -> {current}.");
    }
    foreach (var table in actual.Keys.Except(expected.Keys, StringComparer.OrdinalIgnoreCase))
        throw new InvalidOperationException($"La tabla {table} no existía en {source}.");
}

void EnsureSameFingerprints(IReadOnlyDictionary<string, string> expected, IReadOnlyDictionary<string, string> actual, string source)
{
    foreach (var (table, fingerprint) in expected)
    {
        if (!actual.TryGetValue(table, out var current) || !current.Equals(fingerprint, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"La huella de datos de {table} cambió respecto de {source}.");
    }
    foreach (var table in actual.Keys.Except(expected.Keys, StringComparer.OrdinalIgnoreCase))
        throw new InvalidOperationException($"La huella de {table} no existía en {source}.");
}

void ValidateProductionEvidence(ApplyOptions options, HistoricalApplyPlan plan, string database)
{
    if (!options.AllowProduction)
    {
        throw new InvalidOperationException("Producción exige --allow-production.");
    }

    if (!string.Equals(options.ExpectedDatabase, database, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("--expected-database no coincide con producción.");
    }

    if (!string.Equals(options.PackageSha256, plan.PackageSha256, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(options.SnapshotSha256, plan.SnapshotSha256, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Los hashes declarados de paquete/snapshot no coinciden con el plan.");
    }

    RequireFile(options.BackupPath, "--backup");
    RequireHash(options.BackupPath!, options.BackupSha256!, "respaldo");
}

async Task ExecuteSqlAsync(SqlConnection connection, string sql)
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
    try
    {
        await using var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = 1800 };
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch
        {
            // Conserva la excepción original; SQL Server puede haber revertido la transacción por XACT_ABORT.
        }
        throw;
    }
}

SqlInspection InspectSql(string path, string packageSha256, string snapshotSha256, string? reconciliationSha256)
{
    var sql = File.ReadAllText(path);
    var blockers = new List<string>();
    AddForbidden(@"\bDELETE\b", "DELETE");
    AddForbidden(@"\bTRUNCATE\b", "TRUNCATE");
    AddForbidden(@"\bDROP\b", "DROP");
    AddForbidden(@"\bALTER\b", "ALTER");
    AddForbidden(@"\bCREATE\b", "CREATE");
    AddForbidden(@"\bEXEC(?:UTE)?\b|\bSP_EXECUTESQL\b|\bXP_[A-Z0-9_]*\b", "SQL dinámico o procedimiento ejecutable");
    AddForbidden(@"\bGRANT\b|\bDENY\b|\bREVOKE\b", "cambio de permisos");
    AddForbidden(@"\bDBCC\b|\bBACKUP\b|\bRESTORE\b", "operación administrativa");
    AddForbidden(@"\bUSE\s+\[?[^\s;\]]+\]?", "cambio de base");
    AddForbidden(@"\bKILL\b|\bSHUTDOWN\b|\bWAITFOR\b", "control del servidor o espera");
    AddForbidden(@"\bBULK\s+INSERT\b|\bOPENROWSET\b|\bOPENDATASOURCE\b", "acceso externo");
    AddForbidden(@"\b(?:DISABLE|ENABLE)\s+TRIGGER\b", "cambio de triggers");
    AddForbidden(@"^\s*GO\s*$", "separador GO");

    RequireHeader("HistoricalDataApply-Contract", "4");
    RequireHeader("Package-SHA256", packageSha256);
    RequireHeader("Snapshot-SHA256", snapshotSha256);
    if (string.IsNullOrWhiteSpace(reconciliationSha256))
        blockers.Add("No existe hash de reconciliación aprobada para vincular el SQL.");
    else
        RequireHeader("Reconciliation-SHA256", reconciliationSha256);

    if (!Regex.IsMatch(sql, @"\bSET\s+XACT_ABORT\s+ON\b", RegexOptions.IgnoreCase))
        blockers.Add("El SQL no declara SET XACT_ABORT ON.");
    if (!Regex.IsMatch(sql, @"\bBEGIN\s+TRAN(SACTION)?\b", RegexOptions.IgnoreCase))
        blockers.Add("El SQL no inicia una transacción explícita.");
    if (!Regex.IsMatch(sql, @"\bCOMMIT(\s+TRAN(SACTION)?)?\b", RegexOptions.IgnoreCase))
        blockers.Add("El SQL no confirma la transacción.");

    return new SqlInspection(
        Sha256: HashFile(path),
        Bytes: new FileInfo(path).Length,
        MergeStatements: Count(@"\bMERGE\b"),
        InsertStatements: Count(@"\bINSERT\b"),
        UpdateStatements: Count(@"\bUPDATE\b"),
        Blockers: blockers);

    void AddForbidden(string pattern, string label)
    {
        if (Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
            blockers.Add($"El SQL contiene {label}, operación no permitida por HistoricalDataApply.");
    }

    void RequireHeader(string name, string expected)
    {
        var match = Regex.Match(sql, $@"^\s*--\s*{Regex.Escape(name)}\s*:\s*(?<value>[^\r\n]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (!match.Success || !match.Groups["value"].Value.Trim().Equals(expected, StringComparison.OrdinalIgnoreCase))
            blockers.Add($"El SQL no contiene la cabecera {name} vinculada al plan vigente.");
    }

    int Count(string pattern) => Regex.Matches(sql, pattern, RegexOptions.IgnoreCase).Count;
}

async Task<CanonicalPackageInfo> ReadPackageAsync(string path)
{
    await using var stream = File.OpenRead(path);
    using var document = await JsonDocument.ParseAsync(stream);
    var root = document.RootElement;
    var workbookSha256 = root.TryGetProperty("workbookSha256", out var workbookHash)
        ? workbookHash.GetString() ?? string.Empty
        : string.Empty;
    if (!Regex.IsMatch(workbookSha256, "^[A-Fa-f0-9]{64}$"))
        throw new InvalidDataException("El paquete no contiene workbookSha256 SHA-256 válido.");
    if (!root.TryGetProperty("entities", out var entities) || entities.ValueKind != JsonValueKind.Object)
        throw new InvalidDataException("El paquete no contiene el objeto entities.");

    var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    var canonicalRows = new Dictionary<string, IReadOnlyList<CanonicalEntityRow>>(StringComparer.OrdinalIgnoreCase);
    foreach (var entity in entities.EnumerateObject())
    {
        if (entity.Value.ValueKind != JsonValueKind.Array)
        {
            counts[entity.Name] = 0;
            canonicalRows[entity.Name] = [];
            continue;
        }

        var rows = new List<CanonicalEntityRow>();
        foreach (var row in entity.Value.EnumerateArray())
        {
            var naturalKey = row.TryGetProperty("naturalKey", out var keyValue)
                ? keyValue.GetString() ?? string.Empty
                : string.Empty;
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (row.TryGetProperty("fields", out var fieldValues) && fieldValues.ValueKind == JsonValueKind.Object)
            {
                foreach (var field in fieldValues.EnumerateObject())
                {
                    fields[field.Name] = field.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                        ? null
                        : field.Value.ValueKind == JsonValueKind.String
                            ? field.Value.GetString()
                            : field.Value.ToString();
                }
            }
            rows.Add(new CanonicalEntityRow(naturalKey, fields));
        }
        counts[entity.Name] = rows.Count;
        canonicalRows[entity.Name] = rows;
    }
    var readiness = AuditCanonicalPackage(entities);
    return new CanonicalPackageInfo(workbookSha256.ToUpperInvariant(), counts, readiness, canonicalRows);
}

PackageReadiness AuditCanonicalPackage(JsonElement entities)
{
    var findings = new List<PackageReadinessFinding>();

    AddUnknown("Equipment", "Categoria", "Equipments.Category", "La categoría SQL es obligatoria y no tiene valor Desconocido.");
    AddUnknown("Equipment", "Clasificacion", "Equipments.TypeClassification", "La clasificación SQL es obligatoria y no tiene valor Desconocido.");
    AddUnknown("Equipment", "Estado", "Equipments.Status", "El estado SQL es obligatorio y 'Por confirmar' no equivale a Activo.");
    AddUnknown("EquipmentUnit", "EstadoOperativo", "EquipmentUnits.CurrentStatus", "El estado operativo SQL es obligatorio y no tiene valor Desconocido.");
    AddNullableUnknown("EquipmentUnit", "CondicionFisica", "EquipmentUnits.PhysicalCondition", "Debe convertirse explícitamente a NULL, sin conservar una condición legacy como si estuviera confirmada.");
    AddUnknown("Management", "TipoGestion", "Managements.Type", "El tipo de gestión SQL es obligatorio y no tiene valor Desconocido.");
    AddUnknown("Management", "Estado", "Managements.Status", "El estado de gestión SQL es obligatorio y no tiene valor Desconocido.");
    AddUnknown("Request", "Prioridad", "Requests.Priority", "'Sin especificar' y 'Por confirmar' no son prioridades de negocio.", "Sin especificar");
    AddUnknown("Request", "Estado", "Requests.Status", "El estado de solicitud SQL es obligatorio y no tiene valor Desconocido.");
    AddUnknown("Maintenance", "Estado", "Maintenances.Status", "El estado de mantenimiento SQL es obligatorio y no tiene valor Desconocido.");
    AddUnknown("Departure", "Estado", "Departures.Status", "El estado de salida SQL es obligatorio y no tiene valor Desconocido.");
    AddNullableUnknown("MaintenancePlan", "TipoServicio", "MaintenancePlans.ServiceType", "Se conservará como NULL hasta obtener evidencia.");

    AddMissing("Verification", "FechaVerificacion", "REQUIRED_DATE_UNKNOWN", "Verifications.Date es NOT NULL; no se sustituirá una fecha desconocida por la fecha actual.");
    AddNullableMissing("Request", "FechaSolicitud", "REQUEST_DATE_UNKNOWN", "Requests.RequestDate admite NULL; no se sustituirá por CreatedDate ni por la fecha actual.");
    AddNullableMissing("Departure", "RetornoEstimado", "DEPARTURE_RETURN_UNKNOWN", "Departures.EstimatedReturnDate admite NULL hasta confirmar el retorno.");
    AddNullableMissing("DepartureItem", "Cantidad", "DEPARTURE_ITEM_QUANTITY_UNKNOWN", "DepartureItems.Quantity admite NULL; no se asignará cantidad 1 sin evidencia.");
    AddNullableMissing("DepartureItem", "DescripcionOriginal", "DEPARTURE_ITEM_PRODUCT_UNKNOWN", "DepartureItems.ProductName admite NULL cuando la referencia normalizada identifica el bien.");
    AddNullableUnknown("Verification", "EstadoEquipo", "Verifications.ObservedEquipmentStatus", "Se conservará como NULL si el estado observado no está confirmado.");
    AddNullableUnknown("MaintenancePlan", "TipoMantenimiento", "MaintenancePlans.MaintenanceType", "Se conservará como NULL hasta obtener evidencia.");
    AddNullableMissing("MaintenancePlan", "FechaPlanificada", "MAINTENANCE_PLAN_DATE_UNKNOWN", "MaintenancePlans.PlannedDate admite NULL.");
    AddNullableUnknown("MaintenancePlan", "Estado", "MaintenancePlans.Status", "Se conservará como NULL hasta obtener evidencia.");
    AddNullableMissing("MaintenancePlan", "ResponsableActorCodigo", "MAINTENANCE_PLAN_RESPONSIBLE_UNKNOWN", "MaintenancePlans.ResponsiblePersonId admite NULL.");

    var exactActorRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Técnico", "Proveedor", "Solicitante", "Responsable", "Verificador"
    };
    var actorRoleMismatch = Rows("ActorRole")
        .Count(row => !exactActorRoles.Contains(Field(row, "Rol")));
    if (actorRoleMismatch > 0)
    {
        findings.Add(new PackageReadinessFinding(
            "ACTOR_ROLE_MODEL_MISMATCH",
            "ActorRole",
            "Rol",
            actorRoleMismatch,
            true,
            $"{actorRoleMismatch} roles (solicitante, responsable o verificador) no tienen equivalencia exacta en PersonOperationalRole."));
    }

    return new PackageReadiness(
        BlockingFindings: findings.Count(finding => finding.BlocksApply),
        WarningFindings: findings.Count(finding => !finding.BlocksApply),
        Findings: findings);

    List<JsonElement> Rows(string entityName)
    {
        if (!entities.TryGetProperty(entityName, out var entityRows) || entityRows.ValueKind != JsonValueKind.Array)
        {
            if (!entityName.Equals("MaintenancePlan", StringComparison.OrdinalIgnoreCase)
                || !entities.TryGetProperty("ManagementPlan", out entityRows)
                || entityRows.ValueKind != JsonValueKind.Array)
                return [];
        }
        return entityRows.EnumerateArray().ToList();
    }

    string Field(JsonElement row, string fieldName)
    {
        if (!row.TryGetProperty("fields", out var fields)
            || fields.ValueKind != JsonValueKind.Object
            || !fields.TryGetProperty(fieldName, out var value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return string.Empty;
        return value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() ?? string.Empty : value.ToString().Trim();
    }

    void AddUnknown(string entity, string field, string destination, string detail, params string[] additionalUnknownValues)
    {
        var unknownValues = new HashSet<string>(additionalUnknownValues, StringComparer.OrdinalIgnoreCase)
        {
            "Por confirmar",
            "Pendiente por evidencia"
        };
        var count = Rows(entity).Count(row =>
        {
            var value = Field(row, field);
            return string.IsNullOrWhiteSpace(value) || unknownValues.Contains(value);
        });
        if (count == 0) return;
        findings.Add(new PackageReadinessFinding(
            "UNKNOWN_VALUE_NOT_REPRESENTABLE",
            entity,
            field,
            count,
            true,
            $"{count} fila(s) de {entity}.{field} siguen sin confirmar para {destination}. {detail}"));
    }

    void AddNullableUnknown(string entity, string field, string destination, string detail)
    {
        var count = Rows(entity).Count(row =>
        {
            var value = Field(row, field);
            return value.Equals("Por confirmar", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Pendiente por evidencia", StringComparison.OrdinalIgnoreCase);
        });
        if (count == 0) return;
        findings.Add(new PackageReadinessFinding(
            "UNKNOWN_VALUE_MUST_MAP_TO_NULL",
            entity,
            field,
            count,
            false,
            $"{count} fila(s) de {entity}.{field} son desconocidas para {destination}. {detail}"));
    }

    void AddMissing(string entity, string field, string code, string detail)
    {
        var count = Rows(entity).Count(row => string.IsNullOrWhiteSpace(Field(row, field)));
        if (count == 0) return;
        findings.Add(new PackageReadinessFinding(code, entity, field, count, true, $"{count} fila(s): {detail}"));
    }

    void AddNullableMissing(string entity, string field, string code, string detail)
    {
        var count = Rows(entity).Count(row => string.IsNullOrWhiteSpace(Field(row, field)));
        if (count == 0) return;
        findings.Add(new PackageReadinessFinding(code, entity, field, count, false, $"{count} fila(s): {detail}"));
    }
}

IssueCounts ReadIssues(string path)
{
    var lines = File.ReadAllLines(path, Encoding.UTF8);
    if (lines.Length == 0) throw new InvalidDataException("El archivo de incidencias está vacío.");
    var headers = ParseCsvLine(lines[0]);
    var priorityIndex = headers.FindIndex(value => value.Equals("Priority", StringComparison.OrdinalIgnoreCase));
    if (priorityIndex < 0) throw new InvalidDataException("El CSV de incidencias no contiene Priority.");
    var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var line in lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)))
    {
        var cells = ParseCsvLine(line);
        if (priorityIndex >= cells.Count) continue;
        var priority = cells[priorityIndex].Trim();
        counts[priority] = counts.GetValueOrDefault(priority) + 1;
    }

    return new IssueCounts(counts.GetValueOrDefault("P0"), counts.GetValueOrDefault("P1"), counts.GetValueOrDefault("P2"));
}

List<string> ParseCsvLine(string line)
{
    var result = new List<string>();
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
            else quoted = !quoted;
        }
        else if (character == ',' && !quoted)
        {
            result.Add(current.ToString());
            current.Clear();
        }
        else current.Append(character);
    }
    result.Add(current.ToString());
    return result;
}

async Task<DatabaseInfo> CaptureDatabaseAsync(SqlConnection connection)
{
    var server = connection.DataSource.Trim().ToUpperInvariant();
    var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(server)));
    var counts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    const string countSql = """
        SELECT t.name, SUM(p.rows)
        FROM sys.tables t
        JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1)
        GROUP BY t.name ORDER BY t.name;
        """;
    await using (var command = new SqlCommand(countSql, connection))
    await using (var reader = await command.ExecuteReaderAsync())
        while (await reader.ReadAsync()) counts[reader.GetString(0)] = reader.GetInt64(1);

    var migrations = new List<string>();
    await using (var command = new SqlCommand("SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;", connection))
    await using (var reader = await command.ExecuteReaderAsync())
        while (await reader.ReadAsync()) migrations.Add(reader.GetString(0));

    const string schemaSql = """
        SELECT CASE WHEN
            COL_LENGTH(N'dbo.Equipments', N'CatalogCode') IS NOT NULL AND
            COL_LENGTH(N'dbo.Requests', N'Suggestion') IS NOT NULL AND
            COL_LENGTH(N'dbo.Requests', N'RequestDate') IS NOT NULL AND
            COL_LENGTH(N'dbo.Verifications', N'ObservedEquipmentStatus') IS NOT NULL AND
            COL_LENGTH(N'dbo.MaintenancePlans', N'PlanCode') IS NOT NULL AND
            COL_LENGTH(N'dbo.MaintenancePlans', N'ManagementId') IS NOT NULL AND
            OBJECT_ID(N'dbo.RequestEquipmentUnits', N'U') IS NOT NULL AND
            OBJECT_ID(N'dbo.MaintenanceRequests', N'U') IS NOT NULL AND
            OBJECT_ID(N'dbo.ImportSourceRows', N'U') IS NOT NULL AND
            OBJECT_ID(N'dbo.Articles', N'U') IS NOT NULL
        THEN 1 ELSE 0 END;
        """;
    await using var schemaCommand = new SqlCommand(schemaSql, connection);
    var schemaReady = Convert.ToInt32(await schemaCommand.ExecuteScalarAsync()) == 1;
    var dataFingerprints = await CaptureDataFingerprintsAsync(connection, counts.Keys);
    return new DatabaseInfo(connection.Database, fingerprint, counts, dataFingerprints, migrations, schemaReady);
}

async Task<IReadOnlyDictionary<string, string>> CaptureDataFingerprintsAsync(
    SqlConnection connection,
    IEnumerable<string> availableTables)
{
    var allowedTables = new HashSet<string>(availableTables, StringComparer.OrdinalIgnoreCase);
    var protectedBusinessTables = new[]
    {
        "Articles", "Careers", "CostDetails", "DataQualityIssues", "DepartureItems", "Departures",
        "Equipments", "EquipmentStateHistories", "EquipmentUnits", "Faculties",
        "HistoricalVerificationQuarantines", "ImportBatches", "ImportSourceRows", "Laboratories",
        "MaintenanceParticipants", "MaintenancePlans", "MaintenanceRequests", "Maintenances",
        "ManagementPlans", "Managements", "People", "PersonAliases", "PersonRoleAssignments",
        "RequestEquipmentUnits", "Requests", "VerificationFaults", "Verifications"
    };
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var table in protectedBusinessTables.Where(allowedTables.Contains))
    {
        var sql = $"""
            SELECT CONVERT(varchar(64), HASHBYTES('SHA2_256', COALESCE((
                SELECT source.*
                FROM dbo.[{table}] AS source
                ORDER BY source.[Id]
                FOR JSON PATH, INCLUDE_NULL_VALUES
            ), N'')), 2);
            """;
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 180 };
        result[table] = Convert.ToString(await command.ExecuteScalarAsync())?.ToUpperInvariant()
            ?? throw new InvalidOperationException($"No se pudo calcular la huella de {table}.");
    }
    return result;
}

void RequireFile(string? path, string label)
{
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        throw new ArgumentException($"{label} debe apuntar a un archivo existente.");
}

void RequireHash(string path, string expected, string label)
{
    if (!Regex.IsMatch(expected ?? string.Empty, "^[A-Fa-f0-9]{64}$"))
        throw new ArgumentException($"Falta un SHA-256 válido para {label}.");
    var actual = HashFile(path);
    if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException($"El hash de {label} no coincide con la evidencia aprobada.");
}

string HashFile(string path)
{
    using var stream = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(stream));
}

async Task WriteJsonAsync(string path, object value)
{
    var fullPath = Path.GetFullPath(path);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(value, jsonOptions), Encoding.UTF8);
}

string Redact(string message) => Regex.Replace(message,
    "(?i)(password|pwd|user id|uid)\\s*=\\s*[^;]+", "$1=<redactado>");

enum ApplyMode { Plan, Apply }

sealed record ApplyOptions(
    ApplyMode Mode,
    string? PackagePath,
    string? IssuesPath,
    string? SnapshotPath,
    string? SqlPath,
    string? PlanPath,
    string? OutputPath,
    string? ExpectedDatabase,
    string? ReconciliationPath,
    string? ReconciliationOutputPath,
    bool AllowProduction,
    string? PackageSha256,
    string? SnapshotSha256,
    string? BackupPath,
    string? BackupSha256,
    bool ShowHelp)
{
    public static ApplyOptions Parse(string[] args)
    {
        if (args.Length == 0 || args.Any(arg => arg is "--help" or "-h"))
            return new(ApplyMode.Plan, null, null, null, null, null, null, null, null, null, false, null, null, null, null, true);
        var mode = args[0].ToLowerInvariant() switch
        {
            "plan" => ApplyMode.Plan,
            "apply" => ApplyMode.Apply,
            _ => throw new ArgumentException("El primer argumento debe ser plan o apply.")
        };
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < args.Length; index++)
        {
            var name = args[index];
            if (!name.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Argumento inesperado: {name}");
            if (name.Equals("--allow-production", StringComparison.OrdinalIgnoreCase))
            {
                flags.Add(name);
                continue;
            }
            if (++index >= args.Length) throw new ArgumentException($"Falta el valor de {name}.");
            values[name] = args[index];
        }
        string? Value(string name) => values.GetValueOrDefault(name);
        var output = Value("--output") ?? throw new ArgumentException("Falta --output.");
        var expected = Value("--expected-database") ?? throw new ArgumentException("Falta --expected-database.");
        return new(mode, Value("--package"), Value("--issues"), Value("--snapshot"), Value("--sql"), Value("--plan"), output,
            expected, Value("--reconciliation"), Value("--reconciliation-output"), flags.Contains("--allow-production"), Value("--package-sha256"), Value("--snapshot-sha256"),
            Value("--backup"), Value("--backup-sha256"), false);
    }

    public static void PrintHelp() => Console.WriteLine("""
        Uso:
          HistoricalDataApply plan --package paquete.json --issues incidencias.csv --snapshot preflight.json --sql delta.sql --output plan.json --expected-database BASE [--reconciliation aprobado.json] [--reconciliation-output propuesta.json]
          HistoricalDataApply apply --plan plan.json --output resultado.json --expected-database BASE

        Seguridad:
          plan es de solo lectura y bloquea P0, incompatibilidades semánticas V2↔SQL,
          esquema incompleto, SQL destructivo o hashes faltantes.
          La reconciliación legacy→V2 genera una propuesta por fila. Para aprobarla se exige
          un manifiesto independiente con Status=Approved, revisor, fecha y una decisión válida por mapeo.
          apply acepta directamente solo bases terminadas en _QA y ejecuta dos veces para comprobar idempotencia por conteos.
          Producción exige además --allow-production, --package-sha256, --snapshot-sha256,
          --backup y --backup-sha256. La conexión se lee de User Secrets o ConnectionStrings__DefaultConnection.
        """);
}

sealed record CanonicalPackageInfo(
    string WorkbookSha256,
    IReadOnlyDictionary<string, int> EntityCounts,
    PackageReadiness Readiness,
    IReadOnlyDictionary<string, IReadOnlyList<CanonicalEntityRow>> Rows);
sealed record CanonicalEntityRow(string NaturalKey, IReadOnlyDictionary<string, string?> Fields)
{
    public string Field(string name) => Fields.TryGetValue(name, out var value) ? value?.Trim() ?? string.Empty : string.Empty;
}
sealed record PackageReadiness(
    int BlockingFindings,
    int WarningFindings,
    IReadOnlyList<PackageReadinessFinding> Findings);
sealed record PackageReadinessFinding(
    string Code,
    string Entity,
    string Field,
    int Count,
    bool BlocksApply,
    string Detail);
sealed record IssueCounts(int P0, int P1, int P2);
sealed record SqlInspection(string Sha256, long Bytes, int MergeStatements, int InsertStatements, int UpdateStatements, IReadOnlyList<string> Blockers);
sealed record DatabaseInfo(
    string Database,
    string ServerFingerprint,
    IReadOnlyDictionary<string, long> RowCounts,
    IReadOnlyDictionary<string, string> DataFingerprints,
    IReadOnlyList<string> AppliedMigrations,
    bool SchemaReady);
sealed record HistoricalApplyPlan(
    int ContractVersion,
    DateTime CreatedAtUtc,
    string Database,
    string ServerFingerprint,
    IReadOnlyDictionary<string, long> DatabaseRowCounts,
    IReadOnlyDictionary<string, string> DatabaseFingerprints,
    IReadOnlyList<string> AppliedMigrations,
    bool SchemaReady,
    string PackagePath,
    string PackageSha256,
    string WorkbookSha256,
    string IssuesPath,
    string IssuesSha256,
    string SnapshotPath,
    string SnapshotSha256,
    SnapshotValidation SnapshotValidation,
    string ReconciliationProposalPath,
    string ReconciliationProposalSha256,
    ReconciliationApprovalValidation? ReconciliationApproval,
    string? SqlPath,
    string? SqlSha256,
    IReadOnlyDictionary<string, int> EntityCounts,
    IssueCounts Issues,
    PackageReadiness? PackageReadiness,
    LegacyReconciliation? LegacyReconciliation,
    SqlInspection? SqlInspection,
    bool CanApply,
    IReadOnlyList<string> Blockers);
sealed record ApplyResult(
    int ContractVersion,
    DateTime AppliedAtUtc,
    string Database,
    string ServerFingerprint,
    string PlanSha256,
    string PackageSha256,
    string SnapshotSha256,
    string SqlSha256,
    bool IsQa,
    IReadOnlyDictionary<string, long> CountsBefore,
    IReadOnlyDictionary<string, long> CountsAfter,
    IReadOnlyDictionary<string, string> FingerprintsBefore,
    IReadOnlyDictionary<string, string> FingerprintsAfter,
    bool ChecksPassed);

static class ConnectionStringResolver
{
    private const string EnvironmentKey = "ConnectionStrings__DefaultConnection";
    private const string SecretKey = "ConnectionStrings:DefaultConnection";

    public static string Resolve()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentKey);
        if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;

        var rootAssembly = typeof(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext).Assembly;
        var secretsId = rootAssembly.GetCustomAttribute<Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute>()?.UserSecretsId
            ?? throw new InvalidOperationException("El proyecto no declara UserSecretsId.");
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(appData, "Microsoft", "UserSecrets", secretsId, "secrets.json");
        if (!File.Exists(path)) throw new InvalidOperationException("No se encontró la conexión en User Secrets.");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.TryGetProperty(SecretKey, out var flat)) return flat.GetString()!;
        if (document.RootElement.TryGetProperty("ConnectionStrings", out var section)
            && section.TryGetProperty("DefaultConnection", out var nested)) return nested.GetString()!;
        throw new InvalidOperationException("User Secrets no contiene ConnectionStrings:DefaultConnection.");
    }
}
