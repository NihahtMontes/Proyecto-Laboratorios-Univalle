using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

const string databaseName = "DB_Laboratorios_Univalle";
const string userSecretsId = "e2ee0ec6-49ab-4006-8a26-76ab45a791b4";

var backupRoot = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Documents",
    "SQLServerBackups",
    databaseName);
var fullDirectory = Path.Combine(backupRoot, "Full");
var logDirectory = Path.Combine(backupRoot, "Log");
var keyDirectory = Path.Combine(backupRoot, "Keys");
Directory.CreateDirectory(fullDirectory);
Directory.CreateDirectory(logDirectory);
Directory.CreateDirectory(keyDirectory);

var certificateFile = Path.Combine(keyDirectory, "LaboratoriosBackupCertificate.cer");
var privateKeyFile = Path.Combine(keyDirectory, "LaboratoriosBackupCertificate.pvk");
if (File.Exists(certificateFile) || File.Exists(privateKeyFile))
{
    Console.Error.WriteLine("Ya existen archivos del certificado. No se sobrescribirán; revise el aprovisionamiento anterior.");
    return 2;
}

var certificatePassword = CreatePassword();
var adminConnectionString = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;";
try
{
    await using var connection = new SqlConnection(adminConnectionString);
    await connection.OpenAsync();
    await EnsureCertificateAsync(connection, certificatePassword, certificateFile, privateKeyFile);
    await ConfigureRecoveryAndLogicalNamesAsync(connection);
    await CreateBackupJobsAsync(connection, fullDirectory, logDirectory);

    var baselineBackup = Path.Combine(fullDirectory, $"{databaseName}-FULL-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak");
    await CreateAndVerifyInitialBackupAsync(connection, baselineBackup);
    SaveBackupSecret(certificatePassword);

    Console.WriteLine($"Backup FULL cifrado y restauración de prueba completados: {baselineBackup}");
    Console.WriteLine("Certificado y clave privada exportados en la carpeta Keys; su contraseña quedó solo en User Secrets.");
    Console.WriteLine("Jobs creados: full diario 02:00, log cada 15 minutos y restauración de prueba semanal.");
    Console.WriteLine("AGENTE_REQUERIDO: SQLSERVERAGENT debe iniciarse y configurarse como Automático desde Windows.");
    return 0;
}
finally
{
    certificatePassword = string.Empty;
}

async Task EnsureCertificateAsync(SqlConnection connection, string password, string publicPath, string privatePath)
{
    await using var command = connection.CreateCommand();
    command.CommandTimeout = 120;
    command.CommandText =
        """
        IF NOT EXISTS (SELECT 1 FROM master.sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##')
        BEGIN
            DECLARE @CreateMasterKey nvarchar(max) = N'CREATE MASTER KEY ENCRYPTION BY PASSWORD = ' + QUOTENAME(@Password, '''') + N';';
            EXEC master.sys.sp_executesql @CreateMasterKey;
        END;

        IF NOT EXISTS (SELECT 1 FROM master.sys.certificates WHERE name = N'LaboratoriosBackupCertificate')
        BEGIN
            CREATE CERTIFICATE [LaboratoriosBackupCertificate]
                WITH SUBJECT = N'Cifrado de backups de Laboratorios Univalle',
                EXPIRY_DATE = '20361231';
        END;

        DECLARE @BackupCertificate nvarchar(max) =
            N'BACKUP CERTIFICATE [LaboratoriosBackupCertificate] TO FILE = ' + QUOTENAME(@PublicPath, '''')
          + N' WITH PRIVATE KEY (FILE = ' + QUOTENAME(@PrivatePath, '''')
          + N', ENCRYPTION BY PASSWORD = ' + QUOTENAME(@Password, '''') + N');';
        EXEC master.sys.sp_executesql @BackupCertificate;
        """;
    command.Parameters.Add(new SqlParameter("@Password", System.Data.SqlDbType.NVarChar, 128) { Value = password });
    command.Parameters.Add(new SqlParameter("@PublicPath", System.Data.SqlDbType.NVarChar, 4000) { Value = publicPath });
    command.Parameters.Add(new SqlParameter("@PrivatePath", System.Data.SqlDbType.NVarChar, 4000) { Value = privatePath });
    await command.ExecuteNonQueryAsync();
}

async Task ConfigureRecoveryAndLogicalNamesAsync(SqlConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandTimeout = 120;
    command.CommandText =
        """
        ALTER DATABASE [DB_Laboratorios_Univalle] SET RECOVERY FULL;

        IF EXISTS
        (
            SELECT 1 FROM sys.master_files
            WHERE database_id = DB_ID(N'DB_Laboratorios_Univalle')
              AND name = N'DB_Laboratorios_Univalle_MIGRACION'
        )
            ALTER DATABASE [DB_Laboratorios_Univalle]
                MODIFY FILE (NAME = N'DB_Laboratorios_Univalle_MIGRACION', NEWNAME = N'DB_Laboratorios_Univalle');

        IF EXISTS
        (
            SELECT 1 FROM sys.master_files
            WHERE database_id = DB_ID(N'DB_Laboratorios_Univalle')
              AND name = N'DB_Laboratorios_Univalle_MIGRACION_log'
        )
            ALTER DATABASE [DB_Laboratorios_Univalle]
                MODIFY FILE (NAME = N'DB_Laboratorios_Univalle_MIGRACION_log', NEWNAME = N'DB_Laboratorios_Univalle_log');
        """;
    await command.ExecuteNonQueryAsync();
}

async Task CreateBackupJobsAsync(SqlConnection connection, string fullPath, string logPath)
{
    var fullCommand =
        $"""
        DECLARE @BackupFile nvarchar(4000) = N'{SqlLiteral(fullPath)}\DB_Laboratorios_Univalle-FULL-' + CONVERT(char(8), GETDATE(), 112) + N'-' + REPLACE(CONVERT(char(8), GETDATE(), 108), N':', N'') + N'.bak';
        BACKUP DATABASE [DB_Laboratorios_Univalle] TO DISK = @BackupFile
        WITH INIT, CHECKSUM, COMPRESSION, ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = [LaboratoriosBackupCertificate]), STATS = 10;
        RESTORE VERIFYONLY FROM DISK = @BackupFile WITH CHECKSUM;
        """;

    var logCommand =
        $"""
        DECLARE @BackupFile nvarchar(4000) = N'{SqlLiteral(logPath)}\DB_Laboratorios_Univalle-LOG-' + CONVERT(char(8), GETDATE(), 112) + N'-' + REPLACE(CONVERT(char(8), GETDATE(), 108), N':', N'') + N'.trn';
        BACKUP LOG [DB_Laboratorios_Univalle] TO DISK = @BackupFile
        WITH INIT, CHECKSUM, COMPRESSION, ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = [LaboratoriosBackupCertificate]), STATS = 10;
        """;

    var verifyCommand =
        """
        SET NOCOUNT ON;
        DECLARE @BackupFile nvarchar(4000);
        DECLARE @DataPath nvarchar(4000) = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000));
        DECLARE @DataFile nvarchar(4000) = @DataPath + N'DB_Laboratorios_Univalle_RESTORE_VERIFY.mdf';
        DECLARE @LogFile nvarchar(4000) = @DataPath + N'DB_Laboratorios_Univalle_RESTORE_VERIFY_log.ldf';

        SELECT TOP (1) @BackupFile = media.physical_device_name
        FROM msdb.dbo.backupset backup_set
        INNER JOIN msdb.dbo.backupmediafamily media ON media.media_set_id = backup_set.media_set_id
        WHERE backup_set.database_name = N'DB_Laboratorios_Univalle'
          AND backup_set.[type] = N'D'
          AND backup_set.is_copy_only = 0
          AND backup_set.is_damaged = 0
        ORDER BY backup_set.backup_finish_date DESC;

        IF @BackupFile IS NULL
            THROW 51020, 'No existe un backup FULL para verificar.', 1;

        BEGIN TRY
            IF DB_ID(N'DB_Laboratorios_Univalle_RESTORE_VERIFY') IS NOT NULL
            BEGIN
                ALTER DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY];
            END;

            RESTORE DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY]
            FROM DISK = @BackupFile
            WITH MOVE N'DB_Laboratorios_Univalle' TO @DataFile,
                 MOVE N'DB_Laboratorios_Univalle_log' TO @LogFile,
                 RECOVERY, CHECKSUM, REPLACE;

            DBCC CHECKDB (N'DB_Laboratorios_Univalle_RESTORE_VERIFY') WITH NO_INFOMSGS;
            ALTER DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY];
        END TRY
        BEGIN CATCH
            IF DB_ID(N'DB_Laboratorios_Univalle_RESTORE_VERIFY') IS NOT NULL
            BEGIN
                ALTER DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY];
            END;
            THROW;
        END CATCH;
        """;

    await using var command = connection.CreateCommand();
    command.CommandTimeout = 120;
    command.CommandText =
        """
        DECLARE @Owner sysname = SUSER_SNAME();

        IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'Laboratorios - Backup FULL diario')
            EXEC msdb.dbo.sp_delete_job @job_name = N'Laboratorios - Backup FULL diario';
        IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'Laboratorios - Backup LOG 15 minutos')
            EXEC msdb.dbo.sp_delete_job @job_name = N'Laboratorios - Backup LOG 15 minutos';
        IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'Laboratorios - Verificación restauración semanal')
            EXEC msdb.dbo.sp_delete_job @job_name = N'Laboratorios - Verificación restauración semanal';

        EXEC msdb.dbo.sp_add_job @job_name=N'Laboratorios - Backup FULL diario', @enabled=1, @owner_login_name=@Owner;
        EXEC msdb.dbo.sp_add_jobstep @job_name=N'Laboratorios - Backup FULL diario', @step_name=N'Backup cifrado y VERIFYONLY', @subsystem=N'TSQL', @database_name=N'master', @command=@FullCommand, @retry_attempts=2, @retry_interval=5;
        EXEC msdb.dbo.sp_add_jobschedule @job_name=N'Laboratorios - Backup FULL diario', @name=N'Laboratorios FULL 02:00', @freq_type=4, @freq_interval=1, @active_start_time=020000;
        EXEC msdb.dbo.sp_add_jobserver @job_name=N'Laboratorios - Backup FULL diario';

        EXEC msdb.dbo.sp_add_job @job_name=N'Laboratorios - Backup LOG 15 minutos', @enabled=1, @owner_login_name=@Owner;
        EXEC msdb.dbo.sp_add_jobstep @job_name=N'Laboratorios - Backup LOG 15 minutos', @step_name=N'Backup cifrado del log', @subsystem=N'TSQL', @database_name=N'master', @command=@LogCommand, @retry_attempts=2, @retry_interval=5;
        EXEC msdb.dbo.sp_add_jobschedule @job_name=N'Laboratorios - Backup LOG 15 minutos', @name=N'Laboratorios LOG cada 15 minutos', @freq_type=4, @freq_interval=1, @freq_subday_type=4, @freq_subday_interval=15, @active_start_time=000000, @active_end_time=235959;
        EXEC msdb.dbo.sp_add_jobserver @job_name=N'Laboratorios - Backup LOG 15 minutos';

        EXEC msdb.dbo.sp_add_job @job_name=N'Laboratorios - Verificación restauración semanal', @enabled=1, @owner_login_name=@Owner;
        EXEC msdb.dbo.sp_add_jobstep @job_name=N'Laboratorios - Verificación restauración semanal', @step_name=N'Restore aislado y CHECKDB', @subsystem=N'TSQL', @database_name=N'master', @command=@VerifyCommand, @retry_attempts=1, @retry_interval=10;
        EXEC msdb.dbo.sp_add_jobschedule @job_name=N'Laboratorios - Verificación restauración semanal', @name=N'Laboratorios RESTORE domingo 04:00', @freq_type=8, @freq_interval=1, @freq_recurrence_factor=1, @active_start_time=040000;
        EXEC msdb.dbo.sp_add_jobserver @job_name=N'Laboratorios - Verificación restauración semanal';
        """;
    command.Parameters.Add(new SqlParameter("@FullCommand", System.Data.SqlDbType.NVarChar, -1) { Value = fullCommand });
    command.Parameters.Add(new SqlParameter("@LogCommand", System.Data.SqlDbType.NVarChar, -1) { Value = logCommand });
    command.Parameters.Add(new SqlParameter("@VerifyCommand", System.Data.SqlDbType.NVarChar, -1) { Value = verifyCommand });
    await command.ExecuteNonQueryAsync();
}

async Task CreateAndVerifyInitialBackupAsync(SqlConnection connection, string backupFile)
{
    await using var command = connection.CreateCommand();
    command.CommandTimeout = 600;
    command.CommandText =
        """
        BACKUP DATABASE [DB_Laboratorios_Univalle] TO DISK = @BackupFile
        WITH INIT, CHECKSUM, COMPRESSION,
             ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = [LaboratoriosBackupCertificate]),
             STATS = 10;
        RESTORE VERIFYONLY FROM DISK = @BackupFile WITH CHECKSUM;

        DECLARE @DataPath nvarchar(4000) = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000));
        DECLARE @DataFile nvarchar(4000) = @DataPath + N'DB_Laboratorios_Univalle_RESTORE_VERIFY.mdf';
        DECLARE @LogFile nvarchar(4000) = @DataPath + N'DB_Laboratorios_Univalle_RESTORE_VERIFY_log.ldf';

        RESTORE DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY]
        FROM DISK = @BackupFile
        WITH MOVE N'DB_Laboratorios_Univalle' TO @DataFile,
             MOVE N'DB_Laboratorios_Univalle_log' TO @LogFile,
             RECOVERY, CHECKSUM, REPLACE;
        DBCC CHECKDB (N'DB_Laboratorios_Univalle_RESTORE_VERIFY') WITH NO_INFOMSGS;
        ALTER DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [DB_Laboratorios_Univalle_RESTORE_VERIFY];

        BACKUP LOG [DB_Laboratorios_Univalle] TO DISK = @LogBackupFile
        WITH INIT, CHECKSUM, COMPRESSION,
             ENCRYPTION (ALGORITHM = AES_256, SERVER CERTIFICATE = [LaboratoriosBackupCertificate]),
             STATS = 10;
        """;
    command.Parameters.Add(new SqlParameter("@BackupFile", System.Data.SqlDbType.NVarChar, 4000) { Value = backupFile });
    command.Parameters.Add(new SqlParameter("@LogBackupFile", System.Data.SqlDbType.NVarChar, 4000)
    {
        Value = Path.Combine(logDirectory, $"{databaseName}-LOG-{DateTime.UtcNow:yyyyMMdd-HHmmss}.trn")
    });
    await command.ExecuteNonQueryAsync();
}

string CreatePassword()
{
    var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(36))
        .Replace('+', 'x').Replace('/', 'Y').TrimEnd('=');
    return $"Aa1!{random}";
}

void SaveBackupSecret(string password)
{
    var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var secretsDirectory = Path.Combine(applicationData, "Microsoft", "UserSecrets", userSecretsId);
    var secretsPath = Path.Combine(secretsDirectory, "secrets.json");
    Directory.CreateDirectory(secretsDirectory);
    var root = File.Exists(secretsPath)
        ? JsonNode.Parse(File.ReadAllText(secretsPath)) as JsonObject ?? new JsonObject()
        : new JsonObject();
    root["Backup:CertificatePassword"] = password;
    var temporaryPath = Path.Combine(secretsDirectory, $"secrets.{Guid.NewGuid():N}.tmp");
    File.WriteAllText(temporaryPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    File.Move(temporaryPath, secretsPath, overwrite: true);
}

static string SqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);
