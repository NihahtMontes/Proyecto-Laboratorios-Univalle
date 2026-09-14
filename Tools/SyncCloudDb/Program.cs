using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

Console.WriteLine("=================================================");
Console.WriteLine(" SINCRONIZADOR QA LOCAL -> NUBE (MonsterASP) ");
Console.WriteLine("=================================================");

string sourceCs = "Server=localhost;Database=DB_Laboratorios_Univalle_SCENARIOS_QA;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
string targetCs = "Server=db65393.public.databaseasp.net;Database=db65393;User Id=db65393;Password=8g-FxG9!+4yJ;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

var currentUserService = new DummyCurrentUserService();

Console.WriteLine("\n[1/5] Aplicando Migraciones EF Core a la base de datos remota...");
var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlServer(targetCs);
using (var targetContext = new ApplicationDbContext(optionsBuilder.Options, currentUserService))
{
    await targetContext.Database.MigrateAsync();
    Console.WriteLine(" -> Migraciones aplicadas correctamente en la nube.");
}

Console.WriteLine("\n[2/5] Deshabilitando restricciones y limpiando tablas remotas...");
using (var targetConn = new SqlConnection(targetCs))
{
    await targetConn.OpenAsync();
    using (var cmd = targetConn.CreateCommand())
    {
        cmd.CommandTimeout = 180;
        cmd.CommandText = "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';";
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine(" -> Restricciones deshabilitadas en la nube.");
    }

    // Obtener lista de tablas
    var tables = new List<string>();
    using (var cmd = targetConn.CreateCommand())
    {
        cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME != '__EFMigrationsHistory';";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
    }

    foreach (var table in tables)
    {
        using var cmd = targetConn.CreateCommand();
        cmd.CommandTimeout = 180;
        cmd.CommandText = $"DELETE FROM [{table}];";
        await cmd.ExecuteNonQueryAsync();
    }
    Console.WriteLine($" -> {tables.Count} tablas limpiadas en la nube.");
}

Console.WriteLine("\n[3/5] Copiando datos de QA Local a la Nube...");
using (var sourceConn = new SqlConnection(sourceCs))
using (var targetConn = new SqlConnection(targetCs))
{
    await sourceConn.OpenAsync();
    await targetConn.OpenAsync();

    // Obtener todas las tablas ordenadas (excluyendo __EFMigrationsHistory)
    var tablesToSync = new List<string>();
    using (var cmd = sourceConn.CreateCommand())
    {
        cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' AND TABLE_NAME != '__EFMigrationsHistory' ORDER BY TABLE_NAME;";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tablesToSync.Add(reader.GetString(0));
        }
    }

    foreach (var table in tablesToSync)
    {
        using var countCmd = sourceConn.CreateCommand();
        countCmd.CommandText = $"SELECT COUNT(*) FROM [{table}];";
        long rowCount = Convert.ToInt64(await countCmd.ExecuteScalarAsync());

        if (rowCount == 0)
        {
            Console.WriteLine($" -> Tabla [{table}]: 0 filas (omitida)");
            continue;
        }

        using var selectCmd = sourceConn.CreateCommand();
        selectCmd.CommandText = $"SELECT * FROM [{table}];";
        using var dataReader = await selectCmd.ExecuteReaderAsync();

        using var bulkCopy = new SqlBulkCopy(targetConn, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls, null)
        {
            DestinationTableName = $"[{table}]",
            BulkCopyTimeout = 300,
            BatchSize = 1000
        };

        await bulkCopy.WriteToServerAsync(dataReader);
        Console.WriteLine($" -> Tabla [{table}]: {rowCount} filas copiadas con éxito.");
    }
}

Console.WriteLine("\n[4/5] Habilitando restricciones de Claves Foráneas en la Nube...");
using (var targetConn = new SqlConnection(targetCs))
{
    await targetConn.OpenAsync();
    using var cmd = targetConn.CreateCommand();
    cmd.CommandTimeout = 180;
    cmd.CommandText = "EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';";
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine(" -> Restricciones habilitadas y verificadas.");
}

Console.WriteLine("\n[5/5] Actualizando contraseña de usuarios QA...");
using (var targetContext = new ApplicationDbContext(optionsBuilder.Options, currentUserService))
{
    var hasher = new PasswordHasher<User>();
    var users = await targetContext.Users.IgnoreQueryFilters().ToListAsync();
    foreach (var user in users)
    {
        user.PasswordHash = hasher.HashPassword(user, "contraseñ@123");
        user.SecurityStamp = Guid.NewGuid().ToString();
    }
    await targetContext.SaveChangesAsync();
    Console.WriteLine($" -> Contraseña 'contraseñ@123' configurada para {users.Count} usuarios (qa.superadmin, qa.administrador, qa.supervisor).");
}

Console.WriteLine("\n=================================================");
Console.WriteLine(" ¡SINCRONIZACIÓN A LA NUBE COMPLETADA CON ÉXITO! ");
Console.WriteLine("=================================================");
return 0;

public class DummyCurrentUserService : ICurrentUserService
{
    public int? UserId => 1;
}
