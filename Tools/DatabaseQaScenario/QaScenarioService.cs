using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using System.Data;
using System.Text.Json;

namespace Proyecto_Laboratorios_Univalle.Tools.DatabaseQaScenario;

internal sealed class QaScenarioService
{
    internal const string FoundationBatchCode = "QA-DATA-001-FOUNDATION-V1";
    internal const string ScenarioBatchCode = "QA-DATA-001-FUNCTIONAL-V1";
    private readonly string _sourceDatabase;
    private readonly string _targetDatabase;
    private readonly string _sourceConnection;
    private readonly string _targetConnection;
    private readonly QaPaths _paths;

    public QaScenarioService(string sourceDatabase, string targetDatabase, QaPaths paths)
    {
        _sourceDatabase = sourceDatabase;
        _targetDatabase = targetDatabase;
        _sourceConnection = QaConnections.Build(sourceDatabase);
        _targetConnection = QaConnections.Build(targetDatabase);
        _paths = paths;
    }

    public async Task PlanAsync(QaProfile profile)
    {
        var source = await InspectSourceAsync();
        await WriteManifestAsync("Planned", profile, source, null, null, null);
        Console.WriteLine($"PLAN APROBADO: fuente={_sourceDatabase}, catálogos={source.Counts["Equipments"]}, unidades={source.Counts["EquipmentUnits"]}.");
        Console.WriteLine($"Manifiesto: {_paths.ManifestPath}");
    }

    public async Task CreateAsync(QaProfile profile)
    {
        if (profile == QaProfile.Stress)
            throw new InvalidOperationException("El perfil stress utiliza una base separada y no se crea desde este destino funcional.");

        var source = await InspectSourceAsync();
        await EnsureTargetSchemaAsync();
        var adminId = await EnsureQaUsersAsync();
        await SeedFoundationAsync(source.Fingerprint, adminId);
        var target = await ReadCountsAsync(_targetConnection);
        await WriteManifestAsync("FoundationReady", profile, source, target, null, null);
        Console.WriteLine("BASE QA CREADA: esquema, usuarios, catálogos, unidades y maestros listos.");
    }

    public async Task SeedAsync(QaProfile profile)
    {
        if (profile == QaProfile.Stress)
            throw new InvalidOperationException("El perfil stress queda implementado como opt-in, pero requiere un destino separado y una ejecución autorizada posterior.");

        await EnsureTargetReadyAsync();
        var source = await InspectSourceAsync();
        var adminId = await EnsureQaUsersAsync();
        await SeedFoundationAsync(source.Fingerprint, adminId);
        await ExecuteScenarioSeedAsync(profile, adminId);
        Console.WriteLine($"SEMILLA {profile.ToString().ToUpperInvariant()} COMPLETADA.");
    }

    public async Task VerifyAsync(QaProfile profile)
    {
        await EnsureTargetReadyAsync();
        var verifier = new QaScenarioVerifier(_sourceConnection, _targetConnection, _sourceDatabase, _targetDatabase);
        var result = await verifier.VerifyAsync(profile);
        var source = await InspectSourceAsync();
        var target = await ReadCountsAsync(_targetConnection);
        await WriteManifestAsync(result.Passed ? "Verified" : "Failed", profile, source, target, result, null);
        if (!result.Passed)
            throw new InvalidOperationException(string.Join(" | ", result.Failures));
        Console.WriteLine("VERIFICACIÓN COMPLETADA: integridad, transacciones, restricciones y concurrencia aprobadas.");
    }

    public async Task BenchmarkAsync(QaProfile profile)
    {
        await EnsureTargetReadyAsync();
        var verifier = new QaScenarioVerifier(_sourceConnection, _targetConnection, _sourceDatabase, _targetDatabase);
        var benchmark = await verifier.BenchmarkAsync();
        await File.WriteAllTextAsync(
            _paths.BenchmarkPath,
            JsonSerializer.Serialize(benchmark, JsonOptions));
        if (!benchmark.Passed)
            throw new InvalidOperationException("Uno o más benchmarks superaron el presupuesto aprobado.");
        Console.WriteLine($"BENCHMARK APROBADO: {_paths.BenchmarkPath}");
    }

    public async Task BackupAsync(QaProfile profile)
    {
        await EnsureTargetReadyAsync();
        var verifier = new QaScenarioVerifier(_sourceConnection, _targetConnection, _sourceDatabase, _targetDatabase);
        var backup = await verifier.CreateAndRestoreBackupAsync();
        var source = await InspectSourceAsync();
        var target = await ReadCountsAsync(_targetConnection);
        await WriteManifestAsync("BackupVerified", profile, source, target, null, backup);
        Console.WriteLine($"BACKUP RESTAURABLE APROBADO: {backup.Path}");
        Console.WriteLine($"SHA-256: {backup.Sha256}");
    }

    public async Task CopyCredentialsAsync()
    {
        await EnsureTargetReadyAsync();
        await QaCredentialStore.CopyToClipboardAsync(QaCredentialStore.GetPassword());
        Console.WriteLine("Contraseña QA copiada al portapapeles. No fue impresa.");
        Console.WriteLine("Usuarios: qa.superadmin, qa.administrador, qa.supervisor");
    }

    public async Task RunAllAsync(QaProfile profile)
    {
        if (profile == QaProfile.Stress)
            throw new InvalidOperationException("La ejecución all no inicia automáticamente el perfil stress.");

        var sourceBefore = await InspectSourceAsync();
        await PlanAsync(profile);
        await CreateAsync(profile);
        await SeedAsync(profile);
        var firstCounts = await ReadCountsAsync(_targetConnection);
        await SeedAsync(profile);
        var secondCounts = await ReadCountsAsync(_targetConnection);
        if (!firstCounts.OrderBy(x => x.Key).SequenceEqual(secondCounts.OrderBy(x => x.Key)))
            throw new InvalidOperationException("La segunda semilla alteró los conteos; no es idempotente.");

        await VerifyAsync(profile);
        await BenchmarkAsync(profile);
        await BackupAsync(profile);

        var sourceAfter = await InspectSourceAsync();
        if (!string.Equals(sourceBefore.Fingerprint, sourceAfter.Fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("La huella de la base oficial cambió durante QA-DATA-001.");

        var verifier = new QaScenarioVerifier(_sourceConnection, _targetConnection, _sourceDatabase, _targetDatabase);
        var verification = await verifier.VerifyAsync(profile);
        var benchmark = await verifier.BenchmarkAsync();
        var backup = await verifier.ReadLatestBackupAsync();
        var finalCounts = await ReadCountsAsync(_targetConnection);
        await WriteManifestAsync("Completed", profile, sourceAfter, finalCounts, verification, backup, benchmark);
        Console.WriteLine("QA-DATA-001 COMPLETADO. La aplicación web no fue iniciada.");
    }

    private async Task<SourceSnapshot> InspectSourceAsync()
    {
        await using var connection = new SqlConnection(_sourceConnection);
        await connection.OpenAsync();
        if (!string.Equals(connection.Database, _sourceDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("La conexión fuente no apunta a la base oficial esperada.");

        var counts = await ReadCountsAsync(_sourceConnection);
        if (counts.GetValueOrDefault("Equipments") != 107 || counts.GetValueOrDefault("EquipmentUnits") != 556)
            throw new InvalidOperationException("La fuente no conserva los 107 catálogos y 556 unidades aprobados.");

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CONCAT(
                (SELECT STRING_AGG(CONVERT(nvarchar(max), CatalogCode), N'|') WITHIN GROUP (ORDER BY CatalogCode) FROM dbo.Equipments),
                N'||',
                (SELECT STRING_AGG(CONVERT(nvarchar(max), InventoryNumber), N'|') WITHIN GROUP (ORDER BY InventoryNumber) FROM dbo.EquipmentUnits),
                N'||',
                (SELECT STRING_AGG(CONVERT(nvarchar(max), MigrationId), N'|') WITHIN GROUP (ORDER BY MigrationId) FROM dbo.__EFMigrationsHistory));
            """;
        var material = Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
        var fingerprint = QaHash.Sha256(material);
        return new SourceSnapshot(counts, fingerprint);
    }

    private async Task EnsureTargetSchemaAsync()
    {
        if (!_targetDatabase.EndsWith("_QA", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_targetDatabase, "DB_Laboratorios_Univalle_SCENARIOS_QA", StringComparison.Ordinal))
            throw new InvalidOperationException("Destino QA no autorizado.");

        await using var context = CreateContext(_targetConnection);
        await context.Database.MigrateAsync();
        var pending = await context.Database.GetPendingMigrationsAsync();
        if (pending.Any())
            throw new InvalidOperationException("La base QA conserva migraciones pendientes.");
    }

    private async Task EnsureTargetReadyAsync()
    {
        await using var master = new SqlConnection(QaConnections.Build("master"));
        await master.OpenAsync();
        await using var command = master.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name=@database AND state=0;";
        command.Parameters.Add(new SqlParameter("@database", SqlDbType.NVarChar, 128) { Value = _targetDatabase });
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("La base QA no existe o no está ONLINE. Ejecute create.");

        await using var context = CreateContext(_targetConnection);
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException("La base QA tiene migraciones pendientes.");
    }

    private async Task<int> EnsureQaUsersAsync()
    {
        var password = QaCredentialStore.GetOrCreatePassword();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<ICurrentUserService, QaCurrentUserService>();
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseSqlServer(_targetConnection)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 4;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        foreach (var role in AuthorizationHelper.ManagedIdentityRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                EnsureIdentityResult(await roleManager.CreateAsync(new IdentityRole<int>(role)), $"crear rol {role}");
        }

        var definitions = new[]
        {
            new QaUser("qa.superadmin", "qa.superadmin@local.test", "QA", "SuperAdmin", "99000001", UserRole.SuperAdmin),
            new QaUser("qa.administrador", "qa.administrador@local.test", "QA", "Administrador", "99000002", UserRole.Administrador),
            new QaUser("qa.supervisor", "qa.supervisor@local.test", "QA", "Supervisor", "99000003", UserRole.Supervisor)
        };

        User? superAdmin = null;
        foreach (var definition in definitions)
        {
            var user = await userManager.FindByNameAsync(definition.UserName);
            if (user is null)
            {
                user = new User
                {
                    UserName = definition.UserName,
                    Email = definition.Email,
                    EmailConfirmed = true,
                    PhoneNumber = "70000000",
                    PhoneNumberConfirmed = true,
                    FirstName = definition.FirstName,
                    LastName = definition.LastName,
                    IdentityCard = definition.IdentityCard,
                    Role = definition.Role,
                    Status = GeneralStatus.Activo,
                    Position = "Usuario de pruebas QA",
                    Department = "QA-DATA-001"
                };
                EnsureIdentityResult(await userManager.CreateAsync(user, password), $"crear {definition.UserName}");
            }
            else
            {
                user.Role = definition.Role;
                user.Status = GeneralStatus.Activo;
                EnsureIdentityResult(await userManager.UpdateAsync(user), $"actualizar {definition.UserName}");
                if (!await userManager.CheckPasswordAsync(user, password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    EnsureIdentityResult(await userManager.ResetPasswordAsync(user, token, password), $"sincronizar contraseña {definition.UserName}");
                }
            }

            EnsureIdentityResult(await userManager.SynchronizeManagedRoleAsync(user, definition.Role), $"sincronizar rol {definition.UserName}");
            if (!await userManager.CheckPasswordAsync(user, password))
                throw new InvalidOperationException($"La credencial de {definition.UserName} no se pudo verificar.");
            if (definition.Role == UserRole.SuperAdmin) superAdmin = user;
        }

        password = string.Empty;
        return superAdmin?.Id ?? throw new InvalidOperationException("No se creó el SuperAdmin QA.");
    }

    private async Task SeedFoundationAsync(string sourceFingerprint, int adminId)
    {
        const string sql = """
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            DECLARE @Now datetime2 = SYSUTCDATETIME();
            DECLARE @BatchId int;
            SELECT @BatchId=Id FROM dbo.ImportBatches WHERE Code=N'QA-DATA-001-FOUNDATION-V1';
            IF @BatchId IS NULL
            BEGIN
                INSERT dbo.ImportBatches(Code,SourceType,ContractVersion,SourceName,SourceSha256,ImportedAt,Status,Notes)
                VALUES(N'QA-DATA-001-FOUNDATION-V1',N'SyntheticQA',N'qa-data-001-v1',N'DB_Laboratorios_Univalle',@SourceSha,@Now,1,N'Base sintética no promovible; fuente oficial solo lectura.');
                SET @BatchId=CONVERT(int,SCOPE_IDENTITY());
            END;

            INSERT dbo.Faculties(Name,Code,Description,Status,CreatedDate,CreatedById)
            SELECT s.Name,s.Code,s.Description,s.Status,COALESCE(s.CreatedDate,@Now),@AdminId
            FROM [DB_Laboratorios_Univalle].dbo.Faculties s
            WHERE s.Status<>2 AND NOT EXISTS(SELECT 1 FROM dbo.Faculties t WHERE ISNULL(t.Code,N'')=ISNULL(s.Code,N'') AND t.Name=s.Name);

            INSERT dbo.Careers(Name,FacultadId,Status,CreatedById,CreatedDate,Code)
            SELECT s.Name,tf.Id,s.Status,@AdminId,COALESCE(s.CreatedDate,@Now),s.Code
            FROM [DB_Laboratorios_Univalle].dbo.Careers s
            LEFT JOIN [DB_Laboratorios_Univalle].dbo.Faculties sf ON sf.Id=s.FacultadId
            LEFT JOIN dbo.Faculties tf ON ISNULL(tf.Code,N'')=ISNULL(sf.Code,N'') AND tf.Name=sf.Name
            WHERE s.Status<>2 AND NOT EXISTS(SELECT 1 FROM dbo.Careers t WHERE ISNULL(t.Code,N'')=ISNULL(s.Code,N'') AND t.Name=s.Name);

            INSERT dbo.Laboratories(FacultyId,Code,Name,[Floor],Description,Status,CreatedById,CreatedDate,CityId,[Block],Building,Room,[Type])
            SELECT tf.Id,s.Code,s.Name,s.[Floor],s.Description,s.Status,@AdminId,COALESCE(s.CreatedDate,@Now),NULL,s.[Block],s.Building,s.Room,s.[Type]
            FROM [DB_Laboratorios_Univalle].dbo.Laboratories s
            JOIN [DB_Laboratorios_Univalle].dbo.Faculties sf ON sf.Id=s.FacultyId
            JOIN dbo.Faculties tf ON ISNULL(tf.Code,N'')=ISNULL(sf.Code,N'') AND tf.Name=sf.Name
            WHERE s.Status<>2 AND s.Code<>N'PENDIENTE' AND NOT EXISTS(SELECT 1 FROM dbo.Laboratories t WHERE t.Code=s.Code);

            WHILE 1=1
            BEGIN
                ;WITH SourceEquipment AS
                (
                    SELECT s.*,ROW_NUMBER() OVER(ORDER BY s.CatalogCode,s.Id) rn
                    FROM [DB_Laboratorios_Univalle].dbo.Equipments s
                    WHERE s.Status<>2
                )
                INSERT TOP(100) dbo.Equipments(Category,UtensilType,TypeClassification,Status,ImageUrl,CountryId,CityId,Name,Brand,Model,UsefulLifeYears,Description,CreatedById,CreatedDate,ImportBatchId,CatalogCode,ClassificationReviewStatus,OtherClassificationDetail)
                SELECT
                    CASE WHEN rn%5 IN(0,1,2) THEN 0 WHEN rn%5=3 THEN 1 ELSE 2 END,
                    CASE WHEN rn%5=3 THEN CONVERT(int,(rn%10)+1) ELSE NULL END,
                    CASE WHEN rn%5 IN(0,1,2) THEN CONVERT(int,CASE WHEN rn%15>=7 THEN rn%15+1 ELSE rn%15 END) ELSE NULL END,
                    0,ImageUrl,NULL,NULL,Name,Brand,Model,UsefulLifeYears,Description,@AdminId,COALESCE(CreatedDate,@Now),@BatchId,CatalogCode,2,
                    CASE WHEN rn%5=4 THEN N'Clasificación Otro creada únicamente para pruebas QA' ELSE NULL END
                FROM SourceEquipment s
                WHERE s.CatalogCode IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Equipments t WHERE t.CatalogCode=s.CatalogCode);
                IF @@ROWCOUNT=0 BREAK;
            END;

            WHILE 1=1
            BEGIN
                ;WITH UnitSource AS
                (
                    SELECT s.*,se.CatalogCode,ROW_NUMBER() OVER(ORDER BY s.InventoryNumber,s.Id) rn
                    FROM [DB_Laboratorios_Univalle].dbo.EquipmentUnits s
                    JOIN [DB_Laboratorios_Univalle].dbo.Equipments se ON se.Id=s.EquipmentId
                    WHERE s.CurrentStatus<>99
                ), LabRows AS
                (
                    SELECT l.*,ROW_NUMBER() OVER(ORDER BY l.Code) rn,COUNT(*) OVER() total
                    FROM dbo.Laboratories l WHERE l.Status<>2
                )
                INSERT TOP(100) dbo.EquipmentUnits(ManagementId,EquipmentId,LaboratoryId,InventoryNumber,SerialNumber,CareerId,InternalLocation,AcquisitionDate,ManufacturingDate,AcquisitionValue,CurrentStatus,PhysicalCondition,Notes,CreatedById,CreatedDate,ImportBatchId,LocationResolutionStatus)
                SELECT NULL,te.Id,lr.Id,u.InventoryNumber,u.SerialNumber,career.Id,
                    CONCAT(N'QA-',RIGHT(N'000'+CONVERT(nvarchar(3),((u.rn-1)%20)+1),3)),u.AcquisitionDate,u.ManufacturingDate,u.AcquisitionValue,
                    CASE WHEN u.rn%20<14 THEN 0 WHEN u.rn%20=14 THEN 1 WHEN u.rn%20=15 THEN 2 WHEN u.rn%20=16 THEN 5 WHEN u.rn%20=17 THEN 6 WHEN u.rn%20=18 THEN 10 ELSE 3 END,
                    CASE WHEN u.rn%20<10 THEN 5 WHEN u.rn%20<14 THEN 4 WHEN u.rn%20<17 THEN 3 WHEN u.rn%20<19 THEN 2 ELSE 1 END,
                    CONCAT(N'[QA-SINTETICO] Ubicación y estado asignados solo para pruebas. ',COALESCE(u.Notes,N'')),@AdminId,COALESCE(u.CreatedDate,@Now),@BatchId,1
                FROM UnitSource u
                JOIN dbo.Equipments te ON te.CatalogCode=u.CatalogCode
                JOIN LabRows lr ON lr.rn=((u.rn-1)%lr.total)+1
                OUTER APPLY(SELECT TOP(1) c.Id FROM dbo.Careers c WHERE c.FacultadId=lr.FacultyId AND c.Status<>2 ORDER BY ABS(CHECKSUM(u.InventoryNumber,c.Id))) career
                WHERE NOT EXISTS(SELECT 1 FROM dbo.EquipmentUnits t WHERE t.InventoryNumber=u.InventoryNumber);
                IF @@ROWCOUNT=0 BREAK;
            END;

            WHILE 1=1
            BEGIN
                INSERT TOP(100) dbo.EquipmentClassificationDecisions(DecisionKey,EquipmentId,ReviewStatus,Category,TypeClassification,UtensilType,GeneralStatus,OtherDetail,EvidenceReference,ResponsibleSnapshot,DecisionDate,EffectiveFrom,ImportBatchId,RecordedByUserId,CreatedById,CreatedDate)
                SELECT CONCAT(N'QA-V1-CLASS-',e.CatalogCode),e.Id,2,e.Category,e.TypeClassification,e.UtensilType,e.Status,e.OtherClassificationDetail,N'QA-SINTETICO; no promovible',N'Equipo QA-DATA-001',@Now,NULL,@BatchId,@AdminId,@AdminId,@Now
                FROM dbo.Equipments e
                WHERE NOT EXISTS(SELECT 1 FROM dbo.EquipmentClassificationDecisions d WHERE d.EquipmentId=e.Id AND d.EffectiveTo IS NULL);
                IF @@ROWCOUNT=0 BREAK;
            END;

            COMMIT TRANSACTION;
            """;

        await ExecuteSqlAsync(_targetConnection, sql,
            new SqlParameter("@SourceSha", SqlDbType.NVarChar, 64) { Value = sourceFingerprint },
            new SqlParameter("@AdminId", SqlDbType.Int) { Value = adminId });
    }

    private async Task ExecuteScenarioSeedAsync(QaProfile profile, int adminId)
    {
        var unitLimit = profile == QaProfile.Smoke ? 30 : 556;
        var sql = QaScenarioSql.Build();
        const int batchSize = 30;
        for (var start = 0; start < unitLimit; start += batchSize)
        {
            MemoryGuard.EnsureAvailable(false);
            await ExecuteSqlAsync(_targetConnection, sql,
                new SqlParameter("@AdminId", SqlDbType.Int) { Value = adminId },
                new SqlParameter("@StartRn", SqlDbType.Int) { Value = start },
                new SqlParameter("@EndRn", SqlDbType.Int) { Value = Math.Min(start + batchSize, unitLimit) });
        }
    }

    internal static async Task<Dictionary<string, long>> ReadCountsAsync(string connectionString)
    {
        string[] tables =
        [
            "__EFMigrationsHistory", "Faculties", "Careers", "Laboratories", "Equipments", "EquipmentUnits",
            "Users", "People", "Articles", "Managements", "EquipmentClassificationDecisions", "Verifications",
            "VerificationFaults", "Requests", "RequestEquipmentUnits", "Maintenances", "MaintenanceRequests",
            "MaintenanceParticipants", "MaintenanceTasks", "CostDetails", "EquipmentStateHistories", "Departures",
            "DepartureItems", "MaintenancePlans", "ManagementPlans", "ImportBatches"
        ];
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT_BIG(*) FROM dbo.[{table}];";
            result[table] = Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        return result;
    }

    private static ApplicationDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        return new ApplicationDbContext(options, new QaCurrentUserService());
    }

    private static async Task ExecuteSqlAsync(string connectionString, string sql, params SqlParameter[] parameters)
    {
        MemoryGuard.EnsureAvailable(false);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 300;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private async Task WriteManifestAsync(
        string status,
        QaProfile profile,
        SourceSnapshot source,
        Dictionary<string, long>? target,
        QaVerificationResult? verification,
        QaBackupResult? backup,
        QaBenchmarkResult? benchmark = null)
    {
        var payload = new
        {
            contract = "qa-data-001-v1",
            status,
            generatedAtUtc = DateTime.UtcNow,
            sourceDatabase = _sourceDatabase,
            targetDatabase = _targetDatabase,
            profile = profile.ToString(),
            sourceReadOnly = true,
            sourceFingerprint = source.Fingerprint,
            sourceCounts = source.Counts,
            targetCounts = target,
            verification,
            benchmark,
            backup,
            webApplicationStarted = false,
            syntheticDataMustNeverBePromoted = true
        };
        await File.WriteAllTextAsync(_paths.ManifestPath, JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static void EnsureIdentityResult(IdentityResult result, string operation)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException($"No se pudo {operation}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed record QaUser(string UserName, string Email, string FirstName, string LastName, string IdentityCard, UserRole Role);
}

internal sealed record SourceSnapshot(Dictionary<string, long> Counts, string Fingerprint);
