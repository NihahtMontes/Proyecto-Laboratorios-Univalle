using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

const string officialDatabase = "DB_Laboratorios_Univalle";
const string loginName = "laboratorios_app";
const string userSecretsId = "e2ee0ec6-49ab-4006-8a26-76ab45a791b4";

if (args.Contains("--verify", StringComparer.OrdinalIgnoreCase))
{
    var savedConnectionString = LoadConnectionStringFromUserSecrets();
    if (string.IsNullOrWhiteSpace(savedConnectionString))
    {
        Console.Error.WriteLine("No se encontró DefaultConnection en User Secrets.");
        return 10;
    }

    await VerifyRestrictedLoginAsync(savedConnectionString);
    Console.WriteLine("Acceso SQL verificado: conexión funcional, sin ALTER, BACKUP, db_owner ni sysadmin.");
    return 0;
}

var adminConnectionString =
    "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;";

var password = CreatePassword();
try
{
    await using var adminConnection = new SqlConnection(adminConnectionString);
    await adminConnection.OpenAsync();

    await EnsureOfficialDatabaseIsReadyAsync(adminConnection);
    await EnableMixedAuthenticationOnNextRestartAsync(adminConnection);
    await CreateOrRotateLoginAsync(adminConnection, password);
    await GrantLeastPrivilegeAccessAsync(adminConnection);

    var appConnectionString = new SqlConnectionStringBuilder
    {
        DataSource = "localhost",
        InitialCatalog = officialDatabase,
        UserID = loginName,
        Password = password,
        Encrypt = true,
        TrustServerCertificate = true,
        MultipleActiveResultSets = true,
        ConnectTimeout = 15,
        ApplicationName = "ProyectoLaboratoriosUnivalle"
    }.ConnectionString;

    SaveConnectionStringToUserSecrets(appConnectionString);
    Console.WriteLine("Acceso SQL de mínimo privilegio creado y ConnectionStrings:DefaultConnection actualizado en User Secrets.");
    Console.WriteLine("La contraseña SQL fue generada internamente y no se mostró ni se escribió en el repositorio.");
    Console.WriteLine("Si el motor aun opera en modo solo Windows, reinicie MSSQLSERVER. Luego ejecute esta herramienta con --verify.");
    return 0;
}
finally
{
    password = string.Empty;
}

async Task EnsureOfficialDatabaseIsReadyAsync(SqlConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandText =
        """
        IF DB_ID(N'DB_Laboratorios_Univalle') IS NULL
            THROW 51001, 'No existe la base oficial.', 1;
        IF NOT EXISTS
        (
            SELECT 1 FROM [DB_Laboratorios_Univalle].dbo.__EFMigrationsHistory
            WHERE MigrationId = N'20260823000130_HardenOfficialDataIntegrity'
        )
            THROW 51002, 'La base oficial no tiene la migración de integridad.', 1;
        """;
    await command.ExecuteNonQueryAsync();
}

async Task EnableMixedAuthenticationOnNextRestartAsync(SqlConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandText =
        """
        EXEC master.dbo.xp_instance_regwrite
            N'HKEY_LOCAL_MACHINE',
            N'Software\Microsoft\MSSQLServer\MSSQLServer',
            N'LoginMode',
            REG_DWORD,
            2;
        """;
    await command.ExecuteNonQueryAsync();
}

async Task CreateOrRotateLoginAsync(SqlConnection connection, string password)
{
    await using var command = connection.CreateCommand();
    command.CommandText =
        """
        DECLARE @Sql nvarchar(max);
        IF SUSER_ID(N'laboratorios_app') IS NULL
            SET @Sql = N'CREATE LOGIN [laboratorios_app] WITH PASSWORD = '
                     + QUOTENAME(@Password, '''')
                     + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [DB_Laboratorios_Univalle];';
        ELSE
            SET @Sql = N'ALTER LOGIN [laboratorios_app] WITH PASSWORD = '
                     + QUOTENAME(@Password, '''')
                     + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = [DB_Laboratorios_Univalle]; ALTER LOGIN [laboratorios_app] ENABLE;';
        EXEC sys.sp_executesql @Sql;
        """;
    command.Parameters.Add(new SqlParameter("@Password", System.Data.SqlDbType.NVarChar, 128) { Value = password });
    await command.ExecuteNonQueryAsync();
}

async Task GrantLeastPrivilegeAccessAsync(SqlConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandText =
        """
        USE [DB_Laboratorios_Univalle];

        IF DATABASE_PRINCIPAL_ID(N'laboratorios_app_role') IS NULL
            CREATE ROLE [laboratorios_app_role] AUTHORIZATION [dbo];

        IF USER_ID(N'laboratorios_app') IS NULL
            CREATE USER [laboratorios_app] FOR LOGIN [laboratorios_app];

        IF IS_ROLEMEMBER(N'laboratorios_app_role', N'laboratorios_app') <> 1
            ALTER ROLE [laboratorios_app_role] ADD MEMBER [laboratorios_app];

        GRANT CONNECT TO [laboratorios_app];
        GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [laboratorios_app_role];
        GRANT EXECUTE ON SCHEMA::[dbo] TO [laboratorios_app_role];
        DENY ALTER TO [laboratorios_app_role];
        -- CONTROL contiene CONNECT en la jerarquia de permisos. Una
        -- denegacion explicita aqui impediria abrir la base aun cuando el
        -- usuario tenga GRANT CONNECT. Se revoca cualquier DENY heredado de
        -- versiones anteriores y se mantiene CONTROL sin conceder.
        REVOKE CONTROL TO [laboratorios_app_role];
        DENY TAKE OWNERSHIP TO [laboratorios_app_role];
        DENY BACKUP DATABASE TO [laboratorios_app_role];
        DENY BACKUP LOG TO [laboratorios_app_role];

        IF IS_SRVROLEMEMBER(N'sysadmin', N'laboratorios_app') = 1
            THROW 51003, 'El login de aplicación no puede ser sysadmin.', 1;
        IF IS_ROLEMEMBER(N'db_owner', N'laboratorios_app') = 1
            THROW 51004, 'El usuario de aplicación no puede ser db_owner.', 1;
        """;
    await command.ExecuteNonQueryAsync();
}

string CreatePassword()
{
    var random = RandomNumberGenerator.GetBytes(36);
    var randomText = Convert.ToBase64String(random)
        .Replace('+', 'x')
        .Replace('/', 'Y')
        .TrimEnd('=');
    return $"Aa1!{randomText}";
}

void SaveConnectionStringToUserSecrets(string connectionString)
{
    var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var secretsDirectory = Path.Combine(applicationData, "Microsoft", "UserSecrets", userSecretsId);
    var secretsPath = Path.Combine(secretsDirectory, "secrets.json");
    Directory.CreateDirectory(secretsDirectory);

    JsonObject root;
    if (File.Exists(secretsPath))
    {
        root = JsonNode.Parse(File.ReadAllText(secretsPath)) as JsonObject ?? new JsonObject();
    }
    else
    {
        root = new JsonObject();
    }

    root["ConnectionStrings:DefaultConnection"] = connectionString;
    var temporaryPath = Path.Combine(secretsDirectory, $"secrets.{Guid.NewGuid():N}.tmp");
    File.WriteAllText(temporaryPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    File.Move(temporaryPath, secretsPath, overwrite: true);
}

string? LoadConnectionStringFromUserSecrets()
{
    var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var secretsPath = Path.Combine(applicationData, "Microsoft", "UserSecrets", userSecretsId, "secrets.json");
    if (!File.Exists(secretsPath))
    {
        return null;
    }

    var root = JsonNode.Parse(File.ReadAllText(secretsPath)) as JsonObject;
    return root?["ConnectionStrings:DefaultConnection"]?.GetValue<string>();
}

async Task VerifyRestrictedLoginAsync(string connectionString)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText =
        """
        SELECT
            DB_NAME() AS DatabaseName,
            HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'SELECT') AS CanSelect,
            HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'INSERT') AS CanInsert,
            HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'UPDATE') AS CanUpdate,
            HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'DELETE') AS CanDelete,
            HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'EXECUTE') AS CanExecute,
            HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'ALTER') AS CanAlter,
            HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL') AS CanControl,
            HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'TAKE OWNERSHIP') AS CanTakeOwnership,
            HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'BACKUP DATABASE') AS CanBackup,
            IS_ROLEMEMBER('db_owner') AS IsDbOwner,
            IS_SRVROLEMEMBER('sysadmin') AS IsSysAdmin,
            (SELECT COUNT_BIG(*) FROM dbo.EquipmentUnits) AS EquipmentUnits;
        """;
    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        throw new InvalidOperationException("La verificación del login no devolvió resultados.");
    }

    var databaseName = reader.GetString(0);
    var canSelect = reader.GetInt32(1);
    var canInsert = reader.GetInt32(2);
    var canUpdate = reader.GetInt32(3);
    var canDelete = reader.GetInt32(4);
    var canExecute = reader.GetInt32(5);
    var canAlter = reader.GetInt32(6);
    var canControl = reader.GetInt32(7);
    var canTakeOwnership = reader.GetInt32(8);
    var canBackup = reader.GetInt32(9);
    var isDbOwner = reader.GetInt32(10);
    var isSysAdmin = reader.GetInt32(11);
    var equipmentUnits = reader.GetInt64(12);
    if (!string.Equals(databaseName, officialDatabase, StringComparison.OrdinalIgnoreCase)
        || canSelect != 1 || canInsert != 1 || canUpdate != 1 || canDelete != 1 || canExecute != 1
        || canAlter != 0 || canControl != 0 || canTakeOwnership != 0 || canBackup != 0
        || isDbOwner != 0 || isSysAdmin != 0 || equipmentUnits != 556)
    {
        throw new InvalidOperationException("El login no cumple el perfil de mínimo privilegio esperado.");
    }
}
