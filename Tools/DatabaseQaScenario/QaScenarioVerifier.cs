using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace Proyecto_Laboratorios_Univalle.Tools.DatabaseQaScenario;

internal sealed class QaScenarioVerifier
{
    private const string RestoreDatabase = "DB_Laboratorios_Univalle_SCENARIOS_RESTORE_QA";
    private const string BackupDirectory = @"C:\ProgramData\LaboratoriosUnivalle\QA-DATA-001\Backups";
    private readonly string _sourceConnection;
    private readonly string _targetConnection;
    private readonly string _sourceDatabase;
    private readonly string _targetDatabase;

    public QaScenarioVerifier(
        string sourceConnection,
        string targetConnection,
        string sourceDatabase,
        string targetDatabase)
    {
        _sourceConnection = sourceConnection;
        _targetConnection = targetConnection;
        _sourceDatabase = sourceDatabase;
        _targetDatabase = targetDatabase;
    }

    public async Task<QaVerificationResult> VerifyAsync(QaProfile profile)
    {
        if (profile == QaProfile.Stress)
            throw new InvalidOperationException("El perfil stress requiere una base independiente y autorización específica.");

        MemoryGuard.EnsureAvailable(false);
        var checks = new List<QaCheckResult>();
        var failures = new List<string>();

        async Task CheckAsync(string name, Func<Task<string?>> test)
        {
            try
            {
                var problem = await test();
                var passed = problem is null;
                checks.Add(new QaCheckResult(name, passed, problem ?? "Aprobado"));
                if (!passed) failures.Add($"{name}: {problem}");
            }
            catch (Exception exception)
            {
                checks.Add(new QaCheckResult(name, false, $"{exception.GetType().Name}: {exception.Message}"));
                failures.Add($"{name}: {exception.Message}");
            }
        }

        await CheckAsync("aislamiento de bases", async () =>
        {
            await using var source = new SqlConnection(_sourceConnection);
            await using var target = new SqlConnection(_targetConnection);
            await source.OpenAsync();
            await target.OpenAsync();
            return source.Database == _sourceDatabase
                   && target.Database == _targetDatabase
                   && target.Database.EndsWith("_QA", StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(source.Database, target.Database, StringComparison.OrdinalIgnoreCase)
                ? null
                : "la fuente o el destino no coincide con el contrato QA-DATA-001";
        });

        var sourceCounts = await QaScenarioService.ReadCountsAsync(_sourceConnection);
        var targetCounts = await QaScenarioService.ReadCountsAsync(_targetConnection);
        var expected = ExpectedCounts(profile, sourceCounts["__EFMigrationsHistory"]);
        foreach (var pair in expected)
        {
            await CheckAsync($"conteo {pair.Key}", () => Task.FromResult(
                targetCounts.GetValueOrDefault(pair.Key) == pair.Value
                    ? null
                    : $"esperado {pair.Value}, obtenido {targetCounts.GetValueOrDefault(pair.Key)}"));
        }

        await CheckAsync("catálogos únicos y completos", async () =>
        {
            var values = await QueryPairAsync(_targetConnection,
                "SELECT COUNT_BIG(*),COUNT_BIG(DISTINCT CatalogCode) FROM dbo.Equipments WHERE CatalogCode IS NOT NULL;");
            return values.First == 107 && values.Second == 107 ? null : $"conteos {values.First}/{values.Second}";
        });

        await CheckAsync("inventarios únicos y completos", async () =>
        {
            var values = await QueryPairAsync(_targetConnection,
                "SELECT COUNT_BIG(*),COUNT_BIG(DISTINCT InventoryNumber) FROM dbo.EquipmentUnits;");
            return values.First == 556 && values.Second == 556 ? null : $"conteos {values.First}/{values.Second}";
        });

        await CheckAsync("restricciones habilitadas y confiables", async () =>
        {
            var count = await QueryScalarAsync<long>(_targetConnection, """
                SELECT
                    (SELECT COUNT_BIG(*) FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) +
                    (SELECT COUNT_BIG(*) FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1);
                """);
            return count == 0 ? null : $"existen {count} restricciones deshabilitadas o no confiables";
        });

        await CheckAsync("DBCC CHECKCONSTRAINTS", () => DbccReturnsNoRowsAsync(
            _targetConnection, "DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;"));
        await CheckAsync("DBCC CHECKDB", () => DbccReturnsNoRowsAsync(
            _targetConnection, $"DBCC CHECKDB([{_targetDatabase}]) WITH NO_INFOMSGS;"));

        await ExpectConstraintFailureAsync(
            "CHECK costo con padre exclusivo",
            "INSERT dbo.CostDetails(RequestId,MaintenanceId,Concept,Quantity,UnitPrice,Category,CreatedDate) VALUES(NULL,NULL,N'QA-INVALID',1,1,1,SYSUTCDATETIME());",
            CheckAsync);
        await ExpectConstraintFailureAsync(
            "CHECK detalle L-3 con referencia exclusiva",
            "DECLARE @DepartureId int=(SELECT TOP(1) Id FROM dbo.Departures ORDER BY Id); INSERT dbo.DepartureItems(DepartureId,EquipmentUnitId,ArticleId,ProductName,Quantity,IsRemoved,CreatedDate) VALUES(@DepartureId,NULL,NULL,N'QA-INVALID',1,0,SYSUTCDATETIME());",
            CheckAsync);
        await ExpectConstraintFailureAsync(
            "índice único de inventario",
            "DECLARE @First int=(SELECT TOP(1) Id FROM dbo.EquipmentUnits ORDER BY Id); DECLARE @Last int=(SELECT TOP(1) Id FROM dbo.EquipmentUnits ORDER BY Id DESC); UPDATE dbo.EquipmentUnits SET InventoryNumber=(SELECT InventoryNumber FROM dbo.EquipmentUnits WHERE Id=@First) WHERE Id=@Last;",
            CheckAsync);
        await ExpectConstraintFailureAsync(
            "relación solicitud-unidad no duplicable",
            "INSERT dbo.RequestEquipmentUnits(RequestId,EquipmentUnitId,IsLegacyPrimary,IsActive,CreatedById,CreatedDate) SELECT TOP(1) RequestId,EquipmentUnitId,IsLegacyPrimary,IsActive,CreatedById,CreatedDate FROM dbo.RequestEquipmentUnits ORDER BY Id;",
            CheckAsync);

        await CheckAsync("rollback transaccional", VerifyRollbackAsync);
        await CheckAsync("concurrencia optimista RowVersion", VerifyRowVersionAsync);
        await CheckAsync("cinco lecturas concurrentes controladas", VerifyConcurrentReadsAsync);
        await CheckAsync("datos marcados como sintéticos", async () =>
        {
            var count = await QueryScalarAsync<long>(_targetConnection, """
                SELECT COUNT_BIG(*) FROM dbo.ImportBatches
                WHERE Code IN(N'QA-DATA-001-FOUNDATION-V1',N'QA-DATA-001-FUNCTIONAL-V1')
                  AND SourceType=N'SyntheticQA'
                  AND (Notes LIKE N'%promov%' OR Notes LIKE N'%prohibida%');
                """);
            return count == 2 ? null : "los dos lotes QA no quedaron identificados como sintéticos/no promovibles";
        });

        return new QaVerificationResult(
            failures.Count == 0,
            DateTime.UtcNow,
            checks,
            failures);
    }

    public async Task<QaBenchmarkResult> BenchmarkAsync()
    {
        MemoryGuard.EnsureAvailable(false);
        var definitions = new[]
        {
            new BenchmarkDefinition("resumen operativo", """
                SELECT eu.CurrentStatus,COUNT_BIG(*) Total
                FROM dbo.EquipmentUnits eu
                GROUP BY eu.CurrentStatus;
                """, 750d),
            new BenchmarkDefinition("búsqueda de inventario", """
                SELECT TOP(100) eu.InventoryNumber,e.Name,l.Name LaboratoryName
                FROM dbo.EquipmentUnits eu
                JOIN dbo.Equipments e ON e.Id=eu.EquipmentId
                LEFT JOIN dbo.Laboratories l ON l.Id=eu.LaboratoryId
                WHERE e.Name LIKE N'%a%' OR eu.InventoryNumber LIKE N'%1%'
                ORDER BY eu.InventoryNumber;
                """, 750d),
            new BenchmarkDefinition("solicitudes recientes", """
                SELECT TOP(100) r.Id,COALESCE(r.RequestDate,r.CreatedDate) RequestDate,r.Status,
                    COUNT(link.Id) AffectedUnits
                FROM dbo.Requests r
                LEFT JOIN dbo.RequestEquipmentUnits link ON link.RequestId=r.Id AND link.IsActive=1
                GROUP BY r.Id,r.RequestDate,r.CreatedDate,r.Status
                ORDER BY COALESCE(r.RequestDate,r.CreatedDate) DESC,r.Id DESC;
                """, 750d),
            new BenchmarkDefinition("mantenimientos y participantes", """
                SELECT TOP(100) m.Id,m.Status,eu.InventoryNumber,
                    COUNT(DISTINCT mr.RequestId) LinkedRequests,COUNT(DISTINCT mp.PersonId) Participants
                FROM dbo.Maintenances m
                JOIN dbo.EquipmentUnits eu ON eu.Id=m.EquipmentUnitId
                LEFT JOIN dbo.MaintenanceRequests mr ON mr.MaintenanceId=m.Id AND mr.IsActive=1
                LEFT JOIN dbo.MaintenanceParticipants mp ON mp.MaintenanceId=m.Id AND mp.IsActive=1
                GROUP BY m.Id,m.Status,eu.InventoryNumber,m.ScheduledDate
                ORDER BY m.ScheduledDate DESC,m.Id DESC;
                """, 750d),
            new BenchmarkDefinition("reporte de costos por gestión", """
                SELECT mg.Code,COUNT(DISTINCT m.Id) Maintenances,
                    SUM(cd.Quantity*cd.UnitPrice) TotalCost
                FROM dbo.Managements mg
                LEFT JOIN dbo.Maintenances m ON m.ManagementId=mg.Id
                LEFT JOIN dbo.CostDetails cd ON cd.MaintenanceId=m.Id
                GROUP BY mg.Code
                ORDER BY mg.Code;
                """, 2000d)
        };

        var results = new List<QaBenchmarkQueryResult>();
        foreach (var definition in definitions)
        {
            for (var warmup = 0; warmup < 3; warmup++)
                await ExecuteAndConsumeAsync(_targetConnection, definition.Sql);

            var durations = new List<double>(20);
            for (var iteration = 0; iteration < 20; iteration++)
            {
                MemoryGuard.EnsureAvailable(false);
                var watch = Stopwatch.StartNew();
                await ExecuteAndConsumeAsync(_targetConnection, definition.Sql);
                watch.Stop();
                durations.Add(watch.Elapsed.TotalMilliseconds);
            }

            durations.Sort();
            var p50 = Percentile(durations, 0.50);
            var p95 = Percentile(durations, 0.95);
            results.Add(new QaBenchmarkQueryResult(
                definition.Name,
                durations.Count,
                Math.Round(p50, 3),
                Math.Round(p95, 3),
                definition.BudgetMs,
                p95 <= definition.BudgetMs));
        }

        var concurrentWatch = Stopwatch.StartNew();
        await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => ExecuteAndConsumeAsync(_targetConnection, definitions[2].Sql)));
        concurrentWatch.Stop();
        var concurrentMs = Math.Round(concurrentWatch.Elapsed.TotalMilliseconds, 3);

        return new QaBenchmarkResult(
            results.All(result => result.Passed) && concurrentMs <= 2000,
            DateTime.UtcNow,
            3,
            20,
            5,
            concurrentMs,
            2000,
            results);
    }

    public async Task<QaBackupResult> CreateAndRestoreBackupAsync()
    {
        MemoryGuard.EnsureAvailable(false);
        if (!string.Equals(_targetDatabase, "DB_Laboratorios_Univalle_SCENARIOS_QA", StringComparison.Ordinal))
            throw new InvalidOperationException("El backup solo admite el destino exacto QA-DATA-001.");

        var masterConnection = QaConnections.Build("master");
        var dataRoot = await QueryScalarAsync<string>(masterConnection,
            "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'));");
        var logRoot = await QueryScalarAsync<string>(masterConnection,
            "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'));");
        if (string.IsNullOrWhiteSpace(dataRoot) || string.IsNullOrWhiteSpace(logRoot))
            throw new InvalidOperationException("SQL Server no informó las rutas predeterminadas de datos/log.");

        Directory.CreateDirectory(BackupDirectory);

        var backupPath = Path.Combine(
            BackupDirectory,
            $"DB_Laboratorios_Univalle_SCENARIOS_QA_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak");

        await ExecuteNonQueryAsync(masterConnection, $"""
            BACKUP DATABASE [{_targetDatabase}]
            TO DISK=@BackupPath
            WITH COPY_ONLY,CHECKSUM,COMPRESSION,INIT,STATS=10;
            RESTORE VERIFYONLY FROM DISK=@BackupPath WITH CHECKSUM;
            """, 900, new SqlParameter("@BackupPath", SqlDbType.NVarChar, 4000) { Value = backupPath });

        var logicalFiles = await ReadLogicalFilesAsync(masterConnection, backupPath);
        var dataLogical = logicalFiles.FirstOrDefault(file => file.Type == "D")?.LogicalName
            ?? throw new InvalidOperationException("El backup no contiene archivo de datos.");
        var logLogical = logicalFiles.FirstOrDefault(file => file.Type == "L")?.LogicalName
            ?? throw new InvalidOperationException("El backup no contiene archivo de log.");
        var restoreDataPath = Path.Combine(dataRoot, RestoreDatabase + ".mdf");
        var restoreLogPath = Path.Combine(logRoot, RestoreDatabase + "_log.ldf");

        await DropRestoreDatabaseAsync(masterConnection);
        try
        {
            await ExecuteNonQueryAsync(masterConnection, $"""
                RESTORE DATABASE [{RestoreDatabase}]
                FROM DISK=@BackupPath
                WITH MOVE @DataLogical TO @DataPath,
                     MOVE @LogLogical TO @LogPath,
                     CHECKSUM,RECOVERY,REPLACE,STATS=10;
                """, 900,
                new SqlParameter("@BackupPath", SqlDbType.NVarChar, 4000) { Value = backupPath },
                new SqlParameter("@DataLogical", SqlDbType.NVarChar, 128) { Value = dataLogical },
                new SqlParameter("@LogLogical", SqlDbType.NVarChar, 128) { Value = logLogical },
                new SqlParameter("@DataPath", SqlDbType.NVarChar, 4000) { Value = restoreDataPath },
                new SqlParameter("@LogPath", SqlDbType.NVarChar, 4000) { Value = restoreLogPath });

            var restoredConnection = QaConnections.Build(RestoreDatabase);
            var originalCounts = await QaScenarioService.ReadCountsAsync(_targetConnection);
            var restoredCounts = await QaScenarioService.ReadCountsAsync(restoredConnection);
            if (!originalCounts.OrderBy(pair => pair.Key).SequenceEqual(restoredCounts.OrderBy(pair => pair.Key)))
                throw new InvalidOperationException("La restauración no conserva los conteos del backup.");
            var checkDbProblem = await DbccReturnsNoRowsAsync(
                restoredConnection, $"DBCC CHECKDB([{RestoreDatabase}]) WITH NO_INFOMSGS;");
            var checkConstraintsProblem = await DbccReturnsNoRowsAsync(
                restoredConnection, "DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;");
            if (checkDbProblem is not null || checkConstraintsProblem is not null)
                throw new InvalidOperationException(checkDbProblem ?? checkConstraintsProblem);
        }
        finally
        {
            await DropRestoreDatabaseAsync(masterConnection);
        }

        var info = new FileInfo(backupPath);
        return new QaBackupResult(
            backupPath,
            QaHash.FileSha256(backupPath),
            info.Length,
            true,
            true,
            DateTime.UtcNow);
    }

    public async Task<QaBackupResult> ReadLatestBackupAsync()
    {
        var latest = new DirectoryInfo(BackupDirectory)
            .GetFiles("DB_Laboratorios_Univalle_SCENARIOS_QA_*.bak")
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault()
            ?? throw new FileNotFoundException("No existe un backup QA-DATA-001.");
        return new QaBackupResult(
            latest.FullName,
            QaHash.FileSha256(latest.FullName),
            latest.Length,
            true,
            true,
            latest.LastWriteTimeUtc);
    }

    private static Dictionary<string, long> ExpectedCounts(QaProfile profile, long migrations)
    {
        var functional = profile == QaProfile.Functional;
        return new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            ["__EFMigrationsHistory"] = migrations,
            ["Faculties"] = 2,
            ["Careers"] = 4,
            ["Laboratories"] = 14,
            ["Equipments"] = 107,
            ["EquipmentUnits"] = 556,
            ["Users"] = 3,
            ["People"] = 30,
            ["Articles"] = 30,
            ["Managements"] = 7,
            ["EquipmentClassificationDecisions"] = 107,
            ["Verifications"] = functional ? 1668 : 90,
            ["VerificationFaults"] = functional ? 669 : 36,
            ["Requests"] = functional ? 669 : 36,
            ["RequestEquipmentUnits"] = functional ? 834 : 45,
            ["Maintenances"] = functional ? 333 : 18,
            ["MaintenanceRequests"] = functional ? 498 : 27,
            ["MaintenanceParticipants"] = functional ? 444 : 24,
            ["MaintenanceTasks"] = functional ? 999 : 54,
            ["CostDetails"] = functional ? 834 : 45,
            ["EquipmentStateHistories"] = functional ? 1668 : 90,
            ["Departures"] = functional ? 111 : 6,
            ["DepartureItems"] = functional ? 222 : 12,
            ["MaintenancePlans"] = functional ? 278 : 15,
            ["ManagementPlans"] = functional ? 1668 : 90,
            ["ImportBatches"] = 3
        };
    }

    private async Task ExpectConstraintFailureAsync(
        string name,
        string sql,
        Func<string, Func<Task<string?>>, Task> checkAsync)
    {
        await checkAsync(name, async () =>
        {
            await using var connection = new SqlConnection(_targetConnection);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = (SqlTransaction)transaction;
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
                await transaction.RollbackAsync();
                return "SQL Server aceptó una escritura inválida";
            }
            catch (SqlException exception) when (exception.Number is 547 or 2601 or 2627)
            {
                await transaction.RollbackAsync();
                return null;
            }
        });
    }

    private async Task<string?> VerifyRollbackAsync()
    {
        var before = await QueryScalarAsync<long>(_targetConnection, "SELECT COUNT_BIG(*) FROM dbo.Articles;");
        await using (var connection = new SqlConnection(_targetConnection))
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = connection.CreateCommand();
            command.Transaction = (SqlTransaction)transaction;
            command.CommandText = """
                INSERT dbo.Articles(Code,Name,Category,UnitOfMeasure,Status,CreatedDate)
                VALUES(N'QA-ROLLBACK-TEMP',N'Artículo rollback QA',N'QA',N'UNIDAD',0,SYSUTCDATETIME());
                """;
            await command.ExecuteNonQueryAsync();
            await transaction.RollbackAsync();
        }
        var after = await QueryScalarAsync<long>(_targetConnection, "SELECT COUNT_BIG(*) FROM dbo.Articles;");
        return before == after ? null : $"el conteo cambió de {before} a {after}";
    }

    private async Task<string?> VerifyRowVersionAsync()
    {
        await using var connection = new SqlConnection(_targetConnection);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            int id;
            byte[] rowVersion;
            await using (var read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText = "SELECT TOP(1) Id,RowVersion FROM dbo.Maintenances ORDER BY Id;";
                await using var reader = await read.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return "no existe un mantenimiento para probar RowVersion";
                id = reader.GetInt32(0);
                rowVersion = (byte[])reader[1];
            }

            var first = await ConditionalMaintenanceUpdateAsync(connection, transaction, id, rowVersion, "QA-ROWVERSION-FIRST");
            var stale = await ConditionalMaintenanceUpdateAsync(connection, transaction, id, rowVersion, "QA-ROWVERSION-STALE");
            await transaction.RollbackAsync();
            return first == 1 && stale == 0 ? null : $"escrituras afectadas: primera={first}, obsoleta={stale}";
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<int> ConditionalMaintenanceUpdateAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int id,
        byte[] rowVersion,
        string observations)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE dbo.Maintenances SET Observations=@Observations WHERE Id=@Id AND RowVersion=@RowVersion;";
        command.Parameters.Add(new SqlParameter("@Observations", SqlDbType.NVarChar, 1000) { Value = observations });
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
        command.Parameters.Add(new SqlParameter("@RowVersion", SqlDbType.Timestamp, 8) { Value = rowVersion });
        return await command.ExecuteNonQueryAsync();
    }

    private async Task<string?> VerifyConcurrentReadsAsync()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            QueryScalarAsync<long>(_targetConnection, """
                SELECT COUNT_BIG(*) FROM dbo.Maintenances m
                JOIN dbo.EquipmentUnits u ON u.Id=m.EquipmentUnitId
                JOIN dbo.Equipments e ON e.Id=u.EquipmentId;
                """)));
        return results.Distinct().Count() == 1 && results[0] > 0
            ? null
            : "las lecturas concurrentes no devolvieron un resultado estable";
    }

    private async Task DropRestoreDatabaseAsync(string masterConnection)
    {
        if (!RestoreDatabase.EndsWith("_QA", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Nombre de restauración no autorizado.");
        await ExecuteNonQueryAsync(masterConnection, $"""
            IF DB_ID(N'{RestoreDatabase}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{RestoreDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{RestoreDatabase}];
            END;
            """, 120);
    }

    private static async Task<IReadOnlyList<BackupLogicalFile>> ReadLogicalFilesAsync(
        string masterConnection,
        string backupPath)
    {
        var result = new List<BackupLogicalFile>();
        await using var connection = new SqlConnection(masterConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 120;
        command.CommandText = "RESTORE FILELISTONLY FROM DISK=@BackupPath;";
        command.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 4000) { Value = backupPath });
        await using var reader = await command.ExecuteReaderAsync();
        var logicalOrdinal = reader.GetOrdinal("LogicalName");
        var typeOrdinal = reader.GetOrdinal("Type");
        while (await reader.ReadAsync())
            result.Add(new BackupLogicalFile(reader.GetString(logicalOrdinal), reader.GetString(typeOrdinal)));
        return result;
    }

    private static async Task<string?> DbccReturnsNoRowsAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 600;
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? "DBCC reportó incidencias" : null;
    }

    private static async Task ExecuteAndConsumeAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 30;
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            while (await reader.ReadAsync())
            {
                for (var index = 0; index < reader.FieldCount; index++)
                    _ = reader.GetValue(index);
            }
        } while (await reader.NextResultAsync());
    }

    private static async Task ExecuteNonQueryAsync(
        string connectionString,
        string sql,
        int timeout,
        params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandTimeout = timeout;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> QueryScalarAsync<T>(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 120;
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync();
        if (value is null or DBNull)
            throw new InvalidOperationException("La consulta escalar no devolvió un valor.");
        return (T)Convert.ChangeType(value, typeof(T))!;
    }

    private static async Task<(long First, long Second)> QueryPairAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return (0, 0);
        return (reader.GetInt64(0), reader.GetInt64(1));
    }

    private static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        var index = (int)Math.Ceiling(percentile * values.Count) - 1;
        return values[Math.Clamp(index, 0, values.Count - 1)];
    }

    private sealed record BenchmarkDefinition(string Name, string Sql, double BudgetMs);
    private sealed record BackupLogicalFile(string LogicalName, string Type);
}

internal sealed record QaCheckResult(string Name, bool Passed, string Detail);

internal sealed record QaVerificationResult(
    bool Passed,
    DateTime GeneratedAtUtc,
    IReadOnlyList<QaCheckResult> Checks,
    IReadOnlyList<string> Failures);

internal sealed record QaBenchmarkQueryResult(
    string Name,
    int Iterations,
    double P50Milliseconds,
    double P95Milliseconds,
    double BudgetMilliseconds,
    bool Passed);

internal sealed record QaBenchmarkResult(
    bool Passed,
    DateTime GeneratedAtUtc,
    int WarmupIterations,
    int MeasuredIterations,
    int ConcurrentReaders,
    double ConcurrentReadMilliseconds,
    double ConcurrentReadBudgetMilliseconds,
    IReadOnlyList<QaBenchmarkQueryResult> Queries);

internal sealed record QaBackupResult(
    string Path,
    string Sha256,
    long SizeBytes,
    bool VerifyOnlyPassed,
    bool RestoreTestPassed,
    DateTime VerifiedAtUtc);
