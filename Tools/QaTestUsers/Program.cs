using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

// Crea las cuentas de prueba de QA (una por rol) en una COPIA de la base.
// - La conexion se lee de la variable de entorno QA_USERS_CONNECTION (solo del proceso).
// - --expected-server y --expected-database deben coincidir exactamente con esa conexion.
// - Se niega a operar sobre la base REAL, la oficial o una base del sistema.
// - Las contrasenas se generan al azar, se escriben SOLO en --output (fuera de cualquier
//   repositorio Git) y nunca se muestran en la consola.

var arguments = ParseArguments(args);
if (arguments.ContainsKey("help") || args.Length == 0)
{
    PrintUsage();
    return 0;
}

var connectionString = Environment.GetEnvironmentVariable("QA_USERS_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Falta la variable de entorno QA_USERS_CONNECTION.");
    return 2;
}

var connection = new SqlConnectionStringBuilder(connectionString);
if (!arguments.TryGetValue("expected-server", out var expectedServer) || string.IsNullOrWhiteSpace(expectedServer) ||
    !arguments.TryGetValue("expected-database", out var expectedDatabase) || string.IsNullOrWhiteSpace(expectedDatabase))
{
    Console.Error.WriteLine("SEGURIDAD: indique --expected-server y --expected-database.");
    return 3;
}

var forbiddenDatabases = new[] { "DB_Laboratorios_Univalle_REAL", "DB_Laboratorios_Univalle", "master", "model", "msdb", "tempdb" };
if (forbiddenDatabases.Contains(connection.InitialCatalog, StringComparer.OrdinalIgnoreCase) ||
    !string.Equals(connection.DataSource, expectedServer.Trim(), StringComparison.OrdinalIgnoreCase) ||
    !string.Equals(connection.InitialCatalog, expectedDatabase.Trim(), StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("SEGURIDAD: la conexión no coincide con el destino declarado o apunta a una base protegida (REAL, oficial o del sistema).");
    return 3;
}

if (!arguments.TryGetValue("output", out var outputPath) || string.IsNullOrWhiteSpace(outputPath) || !Path.IsPathRooted(outputPath))
{
    Console.Error.WriteLine("Indique --output con una ruta absoluta para el archivo privado de credenciales.");
    return 4;
}

outputPath = Path.GetFullPath(outputPath);
if (IsInsideGitRepository(outputPath))
{
    Console.Error.WriteLine("SEGURIDAD: el archivo de credenciales no puede quedar dentro de un repositorio Git.");
    return 4;
}

arguments.TryGetValue("site-url", out var siteUrl);
var resetPasswords = arguments.ContainsKey("reset-passwords");

var accounts = new[]
{
    new QaAccount("admin.qa", UserRole.Administrador, "Administrador", "QA0001", "70000001", "Probador QA - Administrador"),
    new QaAccount("supervisor.qa", UserRole.Supervisor, "Supervisor", "QA0002", "70000002", "Probador QA - Supervisor"),
    new QaAccount("cierre.qa", UserRole.PracticeClosure, "Cierre", "QA0003", "70000003", "Probador QA - Cierre de practicas"),
};

var configuration = new ConfigurationBuilder().Build();
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<ICurrentUserService, ToolCurrentUserService>();
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
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
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

await using var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
if ((await db.Database.GetPendingMigrationsAsync()).Any())
{
    Console.Error.WriteLine("La base tiene migraciones pendientes. No se crearán usuarios.");
    return 5;
}

var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

foreach (var roleName in AuthorizationHelper.ManagedIdentityRoles)
{
    if (!await roleManager.RoleExistsAsync(roleName))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
        if (!roleResult.Succeeded)
        {
            Console.Error.WriteLine($"No se pudo crear el rol {roleName}: {FormatErrors(roleResult)}");
            return 6;
        }
    }
}

var rows = new List<(QaAccount Account, string Password, string Outcome)>();
foreach (var account in accounts)
{
    var existing = await userManager.FindByNameAsync(account.UserName);
    if (existing is not null)
    {
        if (!resetPasswords)
        {
            Console.WriteLine($"{account.UserName}: ya existía; contraseña sin cambios (use --reset-passwords para regenerarla).");
            rows.Add((account, "(sin cambios)", "ya existía"));
            continue;
        }

        var newPassword = GeneratePassword();
        var removeResult = await userManager.RemovePasswordAsync(existing);
        var addResult = removeResult.Succeeded ? await userManager.AddPasswordAsync(existing, newPassword) : removeResult;
        var syncResult = addResult.Succeeded ? await userManager.SynchronizeManagedRoleAsync(existing, account.Role) : addResult;
        if (!syncResult.Succeeded)
        {
            Console.Error.WriteLine($"{account.UserName}: no se pudo regenerar la contraseña: {FormatErrors(syncResult)}");
            return 7;
        }

        await userManager.SetLockoutEndDateAsync(existing, null);
        await userManager.ResetAccessFailedCountAsync(existing);
        Console.WriteLine($"{account.UserName}: contraseña regenerada.");
        rows.Add((account, newPassword, "contraseña regenerada"));
        continue;
    }

    var password = GeneratePassword();
    var user = new User
    {
        UserName = account.UserName,
        Email = $"{account.UserName}@qa.univalle.test",
        EmailConfirmed = true,
        PhoneNumber = account.Phone,
        PhoneNumberConfirmed = true,
        FirstName = "Probador",
        LastName = account.LastName,
        IdentityCard = account.IdentityCard,
        Role = account.Role,
        Status = GeneralStatus.Activo,
        Position = account.Position,
        Department = "QA",
        CreatedDate = DateTime.UtcNow
    };

    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
        Console.Error.WriteLine($"{account.UserName}: no se pudo crear: {FormatErrors(createResult)}");
        return 8;
    }

    var roleSync = await userManager.SynchronizeManagedRoleAsync(user, account.Role);
    if (!roleSync.Succeeded)
    {
        await userManager.DeleteAsync(user);
        Console.Error.WriteLine($"{account.UserName}: no se pudo asignar el rol; se revirtió el usuario: {FormatErrors(roleSync)}");
        return 9;
    }

    Console.WriteLine($"{account.UserName}: creado con rol {account.Role}.");
    rows.Add((account, password, "creado"));
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
var markdown = new StringBuilder();
markdown.AppendLine("# Credenciales de probadores QA (PRIVADO: no subir a Git ni a chats grupales)");
markdown.AppendLine();
markdown.AppendLine($"- Generado: {DateTime.Now:yyyy-MM-dd HH:mm} (hora local)");
markdown.AppendLine($"- Base: `{connection.InitialCatalog}` en `{connection.DataSource}`");
if (!string.IsNullOrWhiteSpace(siteUrl))
{
    markdown.AppendLine($"- Sitio: {siteUrl}");
}
markdown.AppendLine("- Entregar cada cuenta solo a su probador, por canal privado. Tras 5 intentos fallidos la cuenta se bloquea 15 minutos.");
markdown.AppendLine();
markdown.AppendLine("| Usuario | Rol en la app | Contraseña | Resultado |");
markdown.AppendLine("|---|---|---|---|");
foreach (var row in rows)
{
    markdown.AppendLine($"| `{row.Account.UserName}` | {RoleLabel(row.Account.Role)} | `{row.Password}` | {row.Outcome} |");
}

await File.WriteAllTextAsync(outputPath, markdown.ToString(), new UTF8Encoding(false));
Console.WriteLine($"Credenciales escritas en el archivo privado: {outputPath} (no se muestran aquí).");
return 0;

static string GeneratePassword()
{
    const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    const string lower = "abcdefghijkmnpqrstuvwxyz";
    const string digits = "23456789";
    const string symbols = "!#%*+-=?@";
    var all = upper + lower + digits + symbols;
    var chars = new List<char>
    {
        upper[RandomNumberGenerator.GetInt32(upper.Length)],
        lower[RandomNumberGenerator.GetInt32(lower.Length)],
        digits[RandomNumberGenerator.GetInt32(digits.Length)],
        symbols[RandomNumberGenerator.GetInt32(symbols.Length)],
    };
    while (chars.Count < 16)
    {
        chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
    }

    for (var index = chars.Count - 1; index > 0; index--)
    {
        var swap = RandomNumberGenerator.GetInt32(index + 1);
        (chars[index], chars[swap]) = (chars[swap], chars[index]);
    }

    return new string(chars.ToArray());
}

static bool IsInsideGitRepository(string path)
{
    var directory = new DirectoryInfo(Path.GetDirectoryName(path)!);
    while (directory is not null)
    {
        if (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git")))
        {
            return true;
        }

        directory = directory.Parent;
    }

    return false;
}

static string RoleLabel(UserRole role) => role switch
{
    UserRole.Administrador => "Administrador",
    UserRole.Supervisor => "Supervisor",
    UserRole.PracticeClosure => "Encargado de cierre de prácticas",
    UserRole.SuperAdmin => "Super Administrador",
    _ => role.ToString()
};

static Dictionary<string, string?> ParseArguments(string[] values)
{
    var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < values.Length; index++)
    {
        var value = values[index];
        if (!value.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = value[2..];
        result[key] = index + 1 < values.Length && !values[index + 1].StartsWith("--", StringComparison.Ordinal)
            ? values[++index]
            : null;
    }

    return result;
}

static string FormatErrors(IdentityResult result) =>
    string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));

static void PrintUsage()
{
    Console.WriteLine("Crea admin.qa, supervisor.qa y cierre.qa en una COPIA de la base (nunca en REAL).");
    Console.WriteLine("  set QA_USERS_CONNECTION=<cadena de la copia>");
    Console.WriteLine("  dotnet run --project Tools/QaTestUsers -- --expected-server SERVIDOR --expected-database BASE --output C:\\ruta\\fuera\\del\\repo\\CREDENCIALES.md [--site-url URL] [--reset-passwords]");
    Console.WriteLine("Las contraseñas se escriben solo en --output y no se muestran.");
}

sealed record QaAccount(string UserName, UserRole Role, string LastName, string IdentityCard, string Phone, string Position);

sealed class ToolCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
}
