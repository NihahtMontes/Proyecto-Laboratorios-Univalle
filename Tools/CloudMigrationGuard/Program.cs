using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

const string AlignMigration = "20260827022733_AlignHistoricalWorkbookV2";
const string SuggestionMigration = "20260827105439_AddHistoricalRequestSuggestion";
const string CompleteHistoricalMigration = "20260828161956_CompleteHistoricalWorkbookV2Model";
const string ClassificationGovernanceMigration = "20260830202548_AddEquipmentClassificationGovernance";

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

try
{
    var options = GuardOptions.Parse(args);
    if (options.ShowHelp)
    {
        GuardOptions.PrintHelp();
        return 0;
    }

    var connectionString = ConnectionStringResolver.Resolve();
    var result = options.Mode switch
    {
        GuardMode.Preflight => await RunPreflightAsync(connectionString, options),
        GuardMode.Postflight => await RunPostflightAsync(connectionString, options),
        GuardMode.Snapshot => await RunNormalizedSnapshotAsync(connectionString, options),
        _ => throw new InvalidOperationException("Modo no implementado.")
    };

    Console.WriteLine(result.Message);
    Console.WriteLine($"Evidencia: {options.OutputPath}");
    return result.Success ? 0 : 1;
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine($"Argumentos inválidos: {exception.Message}");
    GuardOptions.PrintHelp();
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"CloudMigrationGuard falló: {Redact(exception.Message)}");
    return 3;
}

async Task<GuardResult> RunPreflightAsync(string connectionString, GuardOptions options)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var snapshot = await CaptureAsync(connection);
    var failures = ValidateIdentity(snapshot, options.ExpectedDatabase).ToList();

    var pending = new[] { AlignMigration, SuggestionMigration }
        .Where(migration => !snapshot.AppliedMigrations.Contains(migration, StringComparer.OrdinalIgnoreCase))
        .ToArray();
    if (!pending.SequenceEqual([AlignMigration, SuggestionMigration], StringComparer.OrdinalIgnoreCase))
    {
        failures.Add($"Se esperaban exactamente las dos migraciones V2 pendientes; se detectó: {string.Join(", ", pending.DefaultIfEmpty("ninguna"))}.");
    }

    if (snapshot.Schema.V2ObjectsPresent != 0)
    {
        failures.Add($"Existen {snapshot.Schema.V2ObjectsPresent} objetos V2 antes de aplicar la migración; posible aplicación parcial.");
    }

    AddZeroRequirement(failures, snapshot.Blockers.DepartureItemsWithoutEquipmentUnit, "DepartureItems sin EquipmentUnitId");
    AddZeroRequirement(failures, snapshot.Blockers.RequestsWithInvalidEquipmentUnit, "solicitudes con unidad inexistente");
    AddZeroRequirement(failures, snapshot.Blockers.MaintenancesWithInvalidRequest, "mantenimientos con solicitud inexistente");
    AddZeroRequirement(failures, snapshot.Blockers.DisabledOrUntrustedConstraints, "restricciones deshabilitadas o no confiables");

    var manifest = new MigrationManifest(
        ManifestVersion: 1,
        Phase: "preflight",
        CapturedAtUtc: DateTime.UtcNow,
        Database: snapshot.Database,
        ServerFingerprint: snapshot.ServerFingerprint,
        Engine: snapshot.Engine,
        AppliedMigrations: snapshot.AppliedMigrations,
        ExpectedPendingMigrations: [AlignMigration, SuggestionMigration],
        RowCounts: snapshot.RowCounts,
        Schema: snapshot.Schema,
        Blockers: snapshot.Blockers,
        ChecksPassed: failures.Count == 0,
        Failures: failures);

    await WriteJsonAsync(options.OutputPath, manifest);
    return new GuardResult(failures.Count == 0,
        failures.Count == 0
            ? "PREFLIGHT APROBADO: identidad, migraciones, relaciones y restricciones cumplen las puertas V2."
            : $"PREFLIGHT BLOQUEADO: {failures.Count} condición(es) requieren atención.");
}

async Task<GuardResult> RunPostflightAsync(string connectionString, GuardOptions options)
{
    if (string.IsNullOrWhiteSpace(options.BaselinePath) || !File.Exists(options.BaselinePath))
    {
        throw new ArgumentException("postflight requiere --baseline con un manifiesto preflight existente.");
    }

    var baseline = JsonSerializer.Deserialize<MigrationManifest>(await File.ReadAllTextAsync(options.BaselinePath), jsonOptions)
        ?? throw new InvalidDataException("No se pudo leer el manifiesto baseline.");
    if (!baseline.ChecksPassed)
    {
        throw new InvalidDataException("El baseline no fue aprobado; no puede usarse para postflight.");
    }

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var snapshot = await CaptureAsync(connection);
    var failures = ValidateIdentity(snapshot, options.ExpectedDatabase).ToList();

    if (!string.Equals(snapshot.Database, baseline.Database, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(snapshot.ServerFingerprint, baseline.ServerFingerprint, StringComparison.Ordinal))
    {
        failures.Add("La base o el servidor no coinciden con el manifiesto preflight.");
    }

    foreach (var (table, expected) in baseline.RowCounts)
    {
        if (table.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!snapshot.RowCounts.TryGetValue(table, out var actual))
        {
            failures.Add($"La tabla baseline {table} ya no existe.");
        }
        else if (actual != expected)
        {
            failures.Add($"Conteo alterado en {table}: antes={expected}, después={actual}.");
        }
    }

    foreach (var migration in new[] { AlignMigration, SuggestionMigration })
    {
        if (!snapshot.AppliedMigrations.Contains(migration, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add($"No está registrada la migración {migration}.");
        }
    }

    if (!snapshot.Schema.IsCompleteV2)
    {
        failures.Add("El contrato de esquema V2 está incompleto.");
    }

    RequireCount(snapshot.RowCounts, "RequestEquipmentUnits", baseline.Blockers.RequestsEligibleForBridgeBackfill, failures);
    RequireCount(snapshot.RowCounts, "MaintenanceRequests", baseline.Blockers.MaintenancesEligibleForBridgeBackfill, failures);
    RequireCount(snapshot.RowCounts, "Articles", 0, failures);
    RequireCount(snapshot.RowCounts, "ImportSourceRows", 0, failures);
    AddZeroRequirement(failures, snapshot.Blockers.RequestEquipmentUnitOrphans, "relaciones solicitud-unidad huérfanas");
    AddZeroRequirement(failures, snapshot.Blockers.MaintenanceRequestOrphans, "relaciones mantenimiento-solicitud huérfanas");
    AddZeroRequirement(failures, snapshot.Blockers.DisabledOrUntrustedConstraints, "restricciones deshabilitadas o no confiables");

    var checkConstraintRows = await CountCheckConstraintViolationsAsync(connection);
    AddZeroRequirement(failures, checkConstraintRows, "filas reportadas por DBCC CHECKCONSTRAINTS");

    var report = new MigrationPostflightReport(
        ManifestVersion: 1,
        Phase: "postflight",
        CapturedAtUtc: DateTime.UtcNow,
        Database: snapshot.Database,
        ServerFingerprint: snapshot.ServerFingerprint,
        AppliedMigrations: snapshot.AppliedMigrations,
        RowCounts: snapshot.RowCounts,
        Schema: snapshot.Schema,
        Blockers: snapshot.Blockers,
        BaselinePath: Path.GetFullPath(options.BaselinePath),
        ChecksPassed: failures.Count == 0,
        Failures: failures);
    await WriteJsonAsync(options.OutputPath, report);

    return new GuardResult(failures.Count == 0,
        failures.Count == 0
            ? "POSTFLIGHT APROBADO: esquema V2, backfills, conteos y restricciones son correctos."
            : $"POSTFLIGHT FALLÓ: {failures.Count} condición(es) no coinciden con el baseline.");
}

async Task<GuardResult> RunNormalizedSnapshotAsync(string connectionString, GuardOptions options)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    var snapshot = await CaptureAsync(connection);
    var failures = ValidateIdentity(snapshot, options.ExpectedDatabase).ToList();

    foreach (var migration in new[]
    {
        AlignMigration,
        SuggestionMigration,
        CompleteHistoricalMigration,
        ClassificationGovernanceMigration
    })
    {
        if (!snapshot.AppliedMigrations.Contains(migration, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add($"No está registrada la migración normalizada {migration}.");
        }
    }

    if (!snapshot.Schema.IsCompleteNormalizedV2)
    {
        failures.Add("El contrato normalizado V2 está incompleto.");
    }

    AddZeroRequirement(failures, snapshot.Blockers.RequestsWithInvalidEquipmentUnit, "solicitudes con unidad inexistente");
    AddZeroRequirement(failures, snapshot.Blockers.MaintenancesWithInvalidRequest, "mantenimientos con solicitud inexistente");
    AddZeroRequirement(failures, snapshot.Blockers.RequestEquipmentUnitOrphans, "relaciones solicitud-unidad huérfanas");
    AddZeroRequirement(failures, snapshot.Blockers.MaintenanceRequestOrphans, "relaciones mantenimiento-solicitud huérfanas");
    AddZeroRequirement(failures, snapshot.Blockers.DisabledOrUntrustedConstraints, "restricciones deshabilitadas o no confiables");

    var checkConstraintRows = await CountCheckConstraintViolationsAsync(connection);
    AddZeroRequirement(failures, checkConstraintRows, "filas reportadas por DBCC CHECKCONSTRAINTS");

    var manifest = new MigrationManifest(
        ManifestVersion: 1,
        Phase: "snapshot",
        CapturedAtUtc: DateTime.UtcNow,
        Database: snapshot.Database,
        ServerFingerprint: snapshot.ServerFingerprint,
        Engine: snapshot.Engine,
        AppliedMigrations: snapshot.AppliedMigrations,
        ExpectedPendingMigrations: [],
        RowCounts: snapshot.RowCounts,
        Schema: snapshot.Schema,
        Blockers: snapshot.Blockers,
        ChecksPassed: failures.Count == 0,
        Failures: failures);

    await WriteJsonAsync(options.OutputPath, manifest);
    return new GuardResult(failures.Count == 0,
        failures.Count == 0
            ? "SNAPSHOT NORMALIZADO APROBADO: identidad, esquema, relaciones y restricciones son consistentes."
            : $"SNAPSHOT NORMALIZADO BLOQUEADO: {failures.Count} condición(es) requieren atención.");
}

async Task<DatabaseSnapshot> CaptureAsync(SqlConnection connection)
{
    var database = connection.Database;
    var serverName = connection.DataSource;
    var serverFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serverName.Trim().ToUpperInvariant())));

    var engine = await ReadEngineAsync(connection);
    var migrations = await ReadMigrationsAsync(connection);
    var counts = await ReadRowCountsAsync(connection);
    var schema = await ReadSchemaAsync(connection);
    var blockers = await ReadBlockersAsync(connection);
    return new DatabaseSnapshot(database, serverFingerprint, engine, migrations, counts, schema, blockers);
}

async Task<EngineInfo> ReadEngineAsync(SqlConnection connection)
{
    const string sql = """
        SELECT CONVERT(int, SERVERPROPERTY('EngineEdition')),
               CONVERT(nvarchar(128), SERVERPROPERTY('Edition')),
               CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion')),
               CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Status')),
               CONVERT(nvarchar(60), DATABASEPROPERTYEX(DB_NAME(), 'Updateability'));
        """;
    await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    return new EngineInfo(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4));
}

async Task<List<string>> ReadMigrationsAsync(SqlConnection connection)
{
    const string sql = "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;";
    await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
    await using var reader = await command.ExecuteReaderAsync();
    var result = new List<string>();
    while (await reader.ReadAsync()) result.Add(reader.GetString(0));
    return result;
}

async Task<Dictionary<string, long>> ReadRowCountsAsync(SqlConnection connection)
{
    const string sql = """
        SELECT t.name, SUM(p.rows)
        FROM sys.tables t
        JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
        GROUP BY t.name
        ORDER BY t.name;
        """;
    await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
    await using var reader = await command.ExecuteReaderAsync();
    var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    while (await reader.ReadAsync()) result[reader.GetString(0)] = reader.GetInt64(1);
    return result;
}

async Task<SchemaInfo> ReadSchemaAsync(SqlConnection connection)
{
    const string sql = """
        SELECT
          CASE WHEN COL_LENGTH('dbo.Equipments','CatalogCode') IS NOT NULL THEN 1 ELSE 0 END,
          CASE WHEN COL_LENGTH('dbo.Requests','Suggestion') IS NOT NULL THEN 1 ELSE 0 END,
          CASE WHEN OBJECT_ID('dbo.Articles','U') IS NOT NULL THEN 1 ELSE 0 END,
          CASE WHEN OBJECT_ID('dbo.ImportSourceRows','U') IS NOT NULL THEN 1 ELSE 0 END,
          CASE WHEN OBJECT_ID('dbo.RequestEquipmentUnits','U') IS NOT NULL THEN 1 ELSE 0 END,
          CASE WHEN OBJECT_ID('dbo.MaintenanceRequests','U') IS NOT NULL THEN 1 ELSE 0 END,
          CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Equipments') AND name='IX_Equipments_CatalogCode' AND is_unique=1 AND has_filter=1) THEN 1 ELSE 0 END,
          CASE WHEN EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Requests') AND name='Suggestion' AND max_length=4000 AND is_nullable=1) THEN 1 ELSE 0 END,
          CASE WHEN EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Requests') AND name='RequestDate' AND is_nullable=1)
                 AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Verifications') AND name='ObservedEquipmentStatus' AND is_nullable=1)
                 AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.DepartureItems') AND name='ArticleId' AND is_nullable=1)
                 AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.MaintenancePlans') AND name='HistoricalSourceKey' AND is_nullable=1)
                 AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.MaintenancePlans') AND name='ResponsiblePersonId' AND is_nullable=1)
               THEN 1 ELSE 0 END,
          CASE WHEN OBJECT_ID('dbo.EquipmentClassificationDecisions','U') IS NOT NULL
                 AND COL_LENGTH('dbo.Equipments','ClassificationReviewStatus') IS NOT NULL
                 AND COL_LENGTH('dbo.Equipments','OtherClassificationDetail') IS NOT NULL
                 AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.EquipmentClassificationDecisions') AND name='IX_EquipmentClassificationDecisions_DecisionKey' AND is_unique=1)
               THEN 1 ELSE 0 END;
        """;
    await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    var values = Enumerable.Range(0, 10).Select(index => reader.GetInt32(index) == 1).ToArray();
    return new SchemaInfo(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8], values[9]);
}

async Task<BlockerInfo> ReadBlockersAsync(SqlConnection connection)
{
    var departureItemsWithoutUnit = await ScalarAsync(connection, "SELECT COUNT_BIG(*) FROM dbo.DepartureItems WHERE EquipmentUnitId IS NULL;");
    var invalidRequestUnit = await ScalarAsync(connection, "SELECT COUNT_BIG(*) FROM dbo.Requests r LEFT JOIN dbo.EquipmentUnits u ON u.Id=r.EquipmentUnitId WHERE r.EquipmentUnitId IS NOT NULL AND u.Id IS NULL;");
    var invalidMaintenanceRequest = await ScalarAsync(connection, "SELECT COUNT_BIG(*) FROM dbo.Maintenances m LEFT JOIN dbo.Requests r ON r.Id=m.RequestId WHERE m.RequestId IS NOT NULL AND r.Id IS NULL;");
    var eligibleRequests = await ScalarAsync(connection, "SELECT COUNT_BIG(*) FROM dbo.Requests WHERE EquipmentUnitId IS NOT NULL;");
    var eligibleMaintenances = await ScalarAsync(connection, "SELECT COUNT_BIG(*) FROM dbo.Maintenances WHERE RequestId IS NOT NULL;");
    var untrusted = await ScalarAsync(connection, "SELECT (SELECT COUNT_BIG(*) FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) + (SELECT COUNT_BIG(*) FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1);");
    var requestOrphans = await ConditionalScalarAsync(connection, "dbo.RequestEquipmentUnits", "SELECT COUNT_BIG(*) FROM dbo.RequestEquipmentUnits l LEFT JOIN dbo.Requests r ON r.Id=l.RequestId LEFT JOIN dbo.EquipmentUnits u ON u.Id=l.EquipmentUnitId WHERE r.Id IS NULL OR u.Id IS NULL;");
    var maintenanceOrphans = await ConditionalScalarAsync(connection, "dbo.MaintenanceRequests", "SELECT COUNT_BIG(*) FROM dbo.MaintenanceRequests l LEFT JOIN dbo.Maintenances m ON m.Id=l.MaintenanceId LEFT JOIN dbo.Requests r ON r.Id=l.RequestId WHERE m.Id IS NULL OR r.Id IS NULL;");
    return new BlockerInfo(departureItemsWithoutUnit, invalidRequestUnit, invalidMaintenanceRequest, eligibleRequests, eligibleMaintenances, untrusted, requestOrphans, maintenanceOrphans);
}

async Task<long> ConditionalScalarAsync(SqlConnection connection, string table, string sql) =>
    await ObjectExistsAsync(connection, table) ? await ScalarAsync(connection, sql) : 0;

async Task<bool> ObjectExistsAsync(SqlConnection connection, string objectName)
{
    await using var command = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@name, 'U') IS NULL THEN 0 ELSE 1 END;", connection);
    command.Parameters.AddWithValue("@name", objectName);
    return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
}

async Task<long> ScalarAsync(SqlConnection connection, string sql)
{
    await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
    return Convert.ToInt64(await command.ExecuteScalarAsync());
}

async Task<long> CountCheckConstraintViolationsAsync(SqlConnection connection)
{
    await using var command = new SqlCommand("DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;", connection) { CommandTimeout = 300 };
    await using var reader = await command.ExecuteReaderAsync();
    long rows = 0;
    do
    {
        while (await reader.ReadAsync()) rows++;
    } while (await reader.NextResultAsync());
    return rows;
}

static IEnumerable<string> ValidateIdentity(DatabaseSnapshot snapshot, string expectedDatabase)
{
    if (!string.Equals(snapshot.Database, expectedDatabase, StringComparison.OrdinalIgnoreCase))
        yield return $"Base inesperada: se esperaba {expectedDatabase} y se abrió {snapshot.Database}.";
    if (!string.Equals(snapshot.Engine.Status, "ONLINE", StringComparison.OrdinalIgnoreCase))
        yield return $"La base no está ONLINE: {snapshot.Engine.Status}.";
    if (!string.Equals(snapshot.Engine.Updateability, "READ_WRITE", StringComparison.OrdinalIgnoreCase))
        yield return $"La base no está READ_WRITE: {snapshot.Engine.Updateability}.";
}

static void AddZeroRequirement(List<string> failures, long value, string label)
{
    if (value != 0) failures.Add($"Se encontraron {value} {label}.");
}

static void RequireCount(IReadOnlyDictionary<string, long> counts, string table, long expected, List<string> failures)
{
    if (!counts.TryGetValue(table, out var actual)) failures.Add($"No existe la tabla {table}.");
    else if (actual != expected) failures.Add($"Conteo inesperado en {table}: esperado={expected}, actual={actual}.");
}

async Task WriteJsonAsync<T>(string path, T value)
{
    var fullPath = Path.GetFullPath(path);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(value, jsonOptions));
}

static string Redact(string message)
{
    var passwordIndex = message.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
    return passwordIndex < 0 ? message : message[..passwordIndex] + "Password=<redacted>";
}

internal enum GuardMode { Preflight, Postflight, Snapshot }

internal sealed record GuardOptions(GuardMode Mode, string OutputPath, string? BaselinePath, string ExpectedDatabase, bool ShowHelp)
{
    public static GuardOptions Parse(string[] args)
    {
        if (args.Length == 0 || args.Any(arg => arg is "--help" or "-h"))
            return new GuardOptions(GuardMode.Preflight, string.Empty, null, GuardConstants.DefaultExpectedDatabase, true);
        var mode = args[0].ToLowerInvariant() switch
        {
            "preflight" => GuardMode.Preflight,
            "postflight" => GuardMode.Postflight,
            "snapshot" => GuardMode.Snapshot,
            _ => throw new ArgumentException("El primer argumento debe ser preflight, postflight o snapshot.")
        };
        string? output = null, baseline = null;
        var expectedDatabase = GuardConstants.DefaultExpectedDatabase;
        for (var index = 1; index < args.Length; index++)
        {
            string Value()
            {
                if (++index >= args.Length) throw new ArgumentException($"{args[index - 1]} requiere un valor.");
                return args[index];
            }
            switch (args[index])
            {
                case "--output": output = Value(); break;
                case "--baseline": baseline = Value(); break;
                case "--expected-database": expectedDatabase = Value(); break;
                default: throw new ArgumentException($"Opción desconocida: {args[index]}");
            }
        }
        if (string.IsNullOrWhiteSpace(output)) throw new ArgumentException("Falta --output <archivo.json>.");
        return new GuardOptions(mode, Path.GetFullPath(output), baseline is null ? null : Path.GetFullPath(baseline), expectedDatabase, false);
    }

    public static void PrintHelp() => Console.WriteLine("""
        Guardas de migración de esquema V2 para SQL Server.

        Uso:
          dotnet run --project Tools/CloudMigrationGuard -- preflight --output <manifest.json> [--expected-database db65393]
          dotnet run --project Tools/CloudMigrationGuard -- postflight --baseline <manifest.json> --output <report.json> [--expected-database db65393]
          dotnet run --project Tools/CloudMigrationGuard -- snapshot --output <manifest.json> --expected-database <base-normalizada>

        La conexión se obtiene de ConnectionStrings__DefaultConnection o de User Secrets del proyecto web.
        Los archivos de evidencia nunca contienen la cadena de conexión ni el nombre del servidor.
        snapshot es de solo lectura y certifica una base que ya contiene el contrato normalizado V2 completo.
        """);
}

internal static class ConnectionStringResolver
{
    public static string Resolve()
    {
        var environment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(environment)) return environment;

        var assembly = typeof(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext).Assembly;
        var attribute = assembly.GetCustomAttribute<Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute>();
        if (attribute is null) throw new InvalidOperationException("El proyecto web no declara UserSecretsId.");
        var secretsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", attribute.UserSecretsId, "secrets.json");
        if (!File.Exists(secretsPath)) throw new InvalidOperationException("No existe el archivo de User Secrets.");
        using var document = JsonDocument.Parse(File.ReadAllText(secretsPath));
        if (document.RootElement.TryGetProperty("ConnectionStrings:DefaultConnection", out var flat))
            return flat.GetString() ?? throw new InvalidOperationException("DefaultConnection está vacío.");
        if (document.RootElement.TryGetProperty("ConnectionStrings", out var group)
            && group.TryGetProperty("DefaultConnection", out var nested))
            return nested.GetString() ?? throw new InvalidOperationException("DefaultConnection está vacío.");
        throw new InvalidOperationException("No existe ConnectionStrings:DefaultConnection en User Secrets.");
    }
}

internal static class GuardConstants
{
    public const string DefaultExpectedDatabase = "db65393";
}

internal sealed record GuardResult(bool Success, string Message);
internal sealed record EngineInfo(int EngineEdition, string Edition, string ProductVersion, string Status, string Updateability);
internal sealed record SchemaInfo(bool CatalogCode, bool Suggestion, bool Articles, bool ImportSourceRows, bool RequestEquipmentUnits, bool MaintenanceRequests, bool CatalogCodeUniqueIndex, bool SuggestionDefinition, bool CompleteHistoricalModel, bool ClassificationGovernance)
{
    public int V2ObjectsPresent => new[] { CatalogCode, Suggestion, Articles, ImportSourceRows, RequestEquipmentUnits, MaintenanceRequests }.Count(value => value);
    public bool IsCompleteV2 => V2ObjectsPresent == 6 && CatalogCodeUniqueIndex && SuggestionDefinition;
    public bool IsCompleteNormalizedV2 => IsCompleteV2 && CompleteHistoricalModel && ClassificationGovernance;
}
internal sealed record BlockerInfo(long DepartureItemsWithoutEquipmentUnit, long RequestsWithInvalidEquipmentUnit, long MaintenancesWithInvalidRequest, long RequestsEligibleForBridgeBackfill, long MaintenancesEligibleForBridgeBackfill, long DisabledOrUntrustedConstraints, long RequestEquipmentUnitOrphans, long MaintenanceRequestOrphans);
internal sealed record DatabaseSnapshot(string Database, string ServerFingerprint, EngineInfo Engine, List<string> AppliedMigrations, Dictionary<string, long> RowCounts, SchemaInfo Schema, BlockerInfo Blockers);
internal sealed record MigrationManifest(int ManifestVersion, string Phase, DateTime CapturedAtUtc, string Database, string ServerFingerprint, EngineInfo Engine, List<string> AppliedMigrations, List<string> ExpectedPendingMigrations, Dictionary<string, long> RowCounts, SchemaInfo Schema, BlockerInfo Blockers, bool ChecksPassed, List<string> Failures);
internal sealed record MigrationPostflightReport(int ManifestVersion, string Phase, DateTime CapturedAtUtc, string Database, string ServerFingerprint, List<string> AppliedMigrations, Dictionary<string, long> RowCounts, SchemaInfo Schema, BlockerInfo Blockers, string BaselinePath, bool ChecksPassed, List<string> Failures);
