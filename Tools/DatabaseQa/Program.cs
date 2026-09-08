using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using System.Reflection;
using System.Text.Json;

QaOptions qaOptions;
try
{
    qaOptions = QaOptions.Parse(args);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    QaOptions.PrintHelp();
    return 2;
}

var connectionString = ConnectionStringResolver.Resolve();
var parsedConnection = new SqlConnectionStringBuilder(connectionString);
if (!parsedConnection.InitialCatalog.EndsWith("_QA", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("SEGURIDAD: las pruebas de escritura solo se ejecutan en una base cuyo nombre termina en _QA.");
    return 3;
}

var baseline = QaBaseline.Load(qaOptions.BaselinePath);

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    .Options;

var failures = new List<string>();
await using var context = CreateContext(options);

await CheckAsync("migraciones pendientes", async () =>
    (await context.Database.GetPendingMigrationsAsync()).Any() ? "hay migraciones pendientes" : null);
await CheckCountAsync("Equipments", context.Equipments.IgnoreQueryFilters(), baseline.Expected("Equipments", 107));
await CheckCountAsync("EquipmentUnits", context.EquipmentUnits.IgnoreQueryFilters(), baseline.Expected("EquipmentUnits", 556));
await CheckCountAsync("Verifications", context.Verifications.IgnoreQueryFilters(), baseline.Expected("Verifications", 1020));
await CheckCountAsync("Requests", context.Requests.IgnoreQueryFilters(), baseline.Expected("Requests", 520));
await CheckCountAsync("Maintenances", context.Maintenances.IgnoreQueryFilters(), baseline.Expected("Maintenances", 157));
await CheckCountAsync("ManagementPlans", context.ManagementPlans.IgnoreQueryFilters(), baseline.Expected("ManagementPlans", 517));
await CheckCountAsync("HistoricalVerificationQuarantines", context.HistoricalVerificationQuarantines, baseline.Expected("HistoricalVerificationQuarantines", 562));
await CheckCountAsync("MaintenanceParticipants", context.MaintenanceParticipants.IgnoreQueryFilters(), baseline.Expected("MaintenanceParticipants", 157));
await CheckCountAsync("VerificationCheckResults", context.VerificationCheckResults.IgnoreQueryFilters(), baseline.Expected("VerificationCheckResults", 0));
await CheckCountAsync("MaintenanceTasks", context.MaintenanceTasks.IgnoreQueryFilters(), baseline.Expected("MaintenanceTasks", 0));
await CheckCountAsync("RequestEquipmentUnits", context.RequestEquipmentUnits.IgnoreQueryFilters(), baseline.RequestBridgeRows);
await CheckCountAsync("MaintenanceRequests", context.MaintenanceRequests.IgnoreQueryFilters(), baseline.MaintenanceBridgeRows);
await CheckCountAsync("Articles", context.Articles.IgnoreQueryFilters(), 0);
await CheckCountAsync("ImportSourceRows", context.ImportSourceRows.IgnoreQueryFilters(), 0);

await CheckAsync("ubicaciones pendientes explícitas", async () =>
{
    var units = await context.EquipmentUnits.IgnoreQueryFilters()
        .CountAsync(unit => unit.LaboratoryId == null && unit.LocationResolutionStatus == LocationResolutionStatus.Pending);
    var requests = await context.Requests.IgnoreQueryFilters()
        .CountAsync(request => request.LaboratoryId == null && request.LocationResolutionStatus == LocationResolutionStatus.Pending);
    var expectedUnits = baseline.Expected("EquipmentUnits", 556);
    var expectedRequests = baseline.Expected("Requests", 520);
    return units == expectedUnits && requests == expectedRequests ? null : $"unidades={units}/{expectedUnits}, solicitudes={requests}/{expectedRequests}";
});

await CheckAsync("hash de la plantilla", async () =>
{
    var hash = await context.ImportBatches.Select(batch => batch.SourceSha256).SingleAsync();
    return hash == "FAEAF00640BD4AF05D78C22B3B5FB7E105260BF8AF8A4B5E8FC2FAD4A78484A4"
        ? null
        : $"hash inesperado: {hash}";
});

await VerifyDependentQueryFiltersAsync();
await ExpectConstraintFailureAsync(
    "CostDetail exige exactamente un padre",
    "INSERT INTO dbo.CostDetails (Concept, Quantity, UnitPrice, Category, CreatedDate) VALUES (N'QA inválido', 1, 0, 0, SYSUTCDATETIME());");
await ExpectConstraintFailureAsync(
    "Maintenance rechaza avance > 100",
    "UPDATE dbo.Maintenances SET CompletionPercentage = 101 WHERE Id = (SELECT MIN(Id) FROM dbo.Maintenances);");
await ExpectConstraintFailureAsync(
    "ManagementPlan rechaza unidad duplicada en una gestión",
    "INSERT INTO dbo.ManagementPlans (ManagementId, EquipmentUnitId, CurrentPhase, CurrentState, PlanStatus, IsDraft, CreatedDate) SELECT TOP (1) ManagementId, EquipmentUnitId, CurrentPhase, CurrentState, PlanStatus, IsDraft, SYSUTCDATETIME() FROM dbo.ManagementPlans WHERE EquipmentUnitId IS NOT NULL;");
await ExpectConstraintFailureAsync(
    "MaintenanceParticipant rechaza dos responsables primarios",
    "INSERT INTO dbo.MaintenanceParticipants (MaintenanceId, PersonId, Role, IsPrimary, IsActive, AssignedAt) SELECT TOP (1) MaintenanceId, PersonId, 1, 1, 1, SYSUTCDATETIME() FROM dbo.MaintenanceParticipants WHERE IsPrimary = 1 AND IsActive = 1;");
await VerifyOptimisticConcurrencyAsync();

await CheckAsync("FK/CHECK confiables", async () =>
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT (SELECT COUNT(*) FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) + (SELECT COUNT(*) FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1);";
    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
    return count == 0 ? null : $"restricciones no confiables/deshabilitadas={count}";
});

if (failures.Count > 0)
{
    Console.Error.WriteLine($"QA FALLÓ ({failures.Count}):");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    return 1;
}

Console.WriteLine("QA COMPLETADO: lecturas, filtros, restricciones y concurrencia pasaron en la base QA.");
return 0;

ApplicationDbContext CreateContext(DbContextOptions<ApplicationDbContext> dbOptions) =>
    new(dbOptions, new QaCurrentUserService());

async Task CheckCountAsync<T>(string name, IQueryable<T> query, long expected) where T : class
{
    await CheckAsync($"cantidad {name}", async () =>
    {
        var actual = await query.LongCountAsync();
        return actual == expected ? null : $"esperado={expected}, actual={actual}";
    });
}

async Task CheckAsync(string name, Func<Task<string?>> assertion)
{
    try
    {
        var error = await assertion();
        if (error is null)
        {
            Console.WriteLine($"OK  {name}");
        }
        else
        {
            failures.Add($"{name}: {error}");
        }
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.GetType().Name}: {ex.Message}");
    }
}

async Task VerifyDependentQueryFiltersAsync()
{
    await CheckAsync("filtro dependiente VerificationFault", async () =>
    {
        await using var filterContext = CreateContext(options);
        await using var transaction = await filterContext.Database.BeginTransactionAsync();
        var verification = await filterContext.Verifications.IgnoreQueryFilters().AsTracking()
            .FirstAsync(item => item.Faults!.Any());
        var faultId = await filterContext.VerificationFaults.IgnoreQueryFilters()
            .Where(fault => fault.VerificationId == verification.Id)
            .Select(fault => fault.Id)
            .FirstAsync();
        verification.Status = VerificationStatus.Annulled;
        await filterContext.SaveChangesAsync();
        filterContext.ChangeTracker.Clear();
        var visible = await filterContext.VerificationFaults.AnyAsync(fault => fault.Id == faultId);
        await transaction.RollbackAsync();
        return visible ? "la falla siguió visible al anular su verificación" : null;
    });

    await CheckAsync("filtro dependiente MaintenanceParticipant", async () =>
    {
        await using var filterContext = CreateContext(options);
        await using var transaction = await filterContext.Database.BeginTransactionAsync();
        var maintenance = await filterContext.Maintenances.IgnoreQueryFilters().AsTracking()
            .FirstAsync(item => item.Participants.Any());
        var participantId = await filterContext.MaintenanceParticipants.IgnoreQueryFilters()
            .Where(participant => participant.MaintenanceId == maintenance.Id)
            .Select(participant => participant.Id)
            .FirstAsync();
        maintenance.Status = MaintenanceStatus.Cancelled;
        await filterContext.SaveChangesAsync();
        filterContext.ChangeTracker.Clear();
        var visible = await filterContext.MaintenanceParticipants.AnyAsync(participant => participant.Id == participantId);
        await transaction.RollbackAsync();
        return visible ? "el participante siguió visible al cancelar su mantenimiento" : null;
    });

    await CheckAsync("filtro dependiente ManagementPlan", async () =>
    {
        await using var filterContext = CreateContext(options);
        await using var transaction = await filterContext.Database.BeginTransactionAsync();
        var plan = await filterContext.ManagementPlans.IgnoreQueryFilters().FirstAsync();
        var management = await filterContext.Managements.IgnoreQueryFilters().AsTracking()
            .SingleAsync(item => item.Id == plan.ManagementId);
        management.Status = ManagementStatus.Deleted;
        await filterContext.SaveChangesAsync();
        filterContext.ChangeTracker.Clear();
        var visible = await filterContext.ManagementPlans.AnyAsync(item => item.Id == plan.Id);
        await transaction.RollbackAsync();
        return visible ? "el plan siguió visible al eliminar lógicamente su gestión" : null;
    });
}

async Task ExpectConstraintFailureAsync(string name, string sql)
{
    await CheckAsync(name, async () =>
    {
        await using var constraintContext = CreateContext(options);
        await using var transaction = await constraintContext.Database.BeginTransactionAsync();
        try
        {
            await constraintContext.Database.ExecuteSqlRawAsync(sql);
            await transaction.RollbackAsync();
            return "SQL Server aceptó una escritura inválida";
        }
        catch (SqlException ex) when (ex.Number is 547 or 2601 or 2627)
        {
            await transaction.RollbackAsync();
            return null;
        }
    });
}

async Task VerifyOptimisticConcurrencyAsync()
{
    await CheckAsync("concurrencia RowVersion", async () =>
    {
        string? originalObservations = null;
        int maintenanceId = 0;
        await using var firstContext = CreateContext(options);
        await using var secondContext = CreateContext(options);
        try
        {
            var firstCopy = await firstContext.Maintenances.IgnoreQueryFilters().AsTracking().FirstAsync();
            var staleCopy = await secondContext.Maintenances.IgnoreQueryFilters().AsTracking()
                .SingleAsync(item => item.Id == firstCopy.Id);
            maintenanceId = firstCopy.Id;
            originalObservations = firstCopy.Observations;

            firstCopy.Observations = $"QA-CONCURRENCY-{Guid.NewGuid():N}";
            await firstContext.SaveChangesAsync();

            staleCopy.Observations = $"QA-STALE-{Guid.NewGuid():N}";
            try
            {
                await secondContext.SaveChangesAsync();
                return "una escritura obsoleta no produjo DbUpdateConcurrencyException";
            }
            catch (DbUpdateConcurrencyException)
            {
                return null;
            }
        }
        finally
        {
            if (maintenanceId > 0)
            {
                await using var restoreContext = CreateContext(options);
                var row = await restoreContext.Maintenances.IgnoreQueryFilters().AsTracking()
                    .SingleAsync(item => item.Id == maintenanceId);
                row.Observations = originalObservations;
                await restoreContext.SaveChangesAsync();
            }
        }
    });
}

sealed class QaCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
}

sealed record QaOptions(string BaselinePath)
{
    public static QaOptions Parse(string[] args)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h")
        {
            PrintHelp();
            Environment.Exit(0);
        }

        if (args.Length != 2 || args[0] != "--baseline" || string.IsNullOrWhiteSpace(args[1]))
            throw new ArgumentException("DatabaseQa requiere --baseline <manifest.json>.");
        var path = Path.GetFullPath(args[1]);
        if (!File.Exists(path)) throw new ArgumentException($"No existe el baseline: {path}");
        return new QaOptions(path);
    }

    public static void PrintHelp() => Console.WriteLine("""
        Uso:
          dotnet run --project Tools/DatabaseQa -- --baseline <preflight.json>

        La conexión se obtiene de ConnectionStrings__DefaultConnection o User Secrets y debe apuntar
        obligatoriamente a una base cuyo nombre termine en _QA.
        """);
}

sealed class QaBaseline
{
    private readonly Dictionary<string, long> _rowCounts;

    private QaBaseline(Dictionary<string, long> rowCounts, long requestBridgeRows, long maintenanceBridgeRows)
    {
        _rowCounts = rowCounts;
        RequestBridgeRows = requestBridgeRows;
        MaintenanceBridgeRows = maintenanceBridgeRows;
    }

    public long RequestBridgeRows { get; }
    public long MaintenanceBridgeRows { get; }
    public long Expected(string table, long fallback) => _rowCounts.TryGetValue(table, out var value) ? value : fallback;

    public static QaBaseline Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        if (!root.TryGetProperty("checksPassed", out var passed) || !passed.GetBoolean())
            throw new InvalidDataException("El baseline no está aprobado.");

        var counts = root.GetProperty("rowCounts").EnumerateObject()
            .ToDictionary(item => item.Name, item => item.Value.GetInt64(), StringComparer.OrdinalIgnoreCase);
        var blockers = root.GetProperty("blockers");
        return new QaBaseline(
            counts,
            blockers.GetProperty("requestsEligibleForBridgeBackfill").GetInt64(),
            blockers.GetProperty("maintenancesEligibleForBridgeBackfill").GetInt64());
    }
}

static class ConnectionStringResolver
{
    public static string Resolve()
    {
        var environment = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(environment)) return environment;

        var assembly = typeof(ApplicationDbContext).Assembly;
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
