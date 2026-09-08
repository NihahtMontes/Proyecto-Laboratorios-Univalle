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

var arguments = ParseArguments(args);
if (arguments.ContainsKey("help"))
{
    PrintUsage();
    return 0;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<BootstrapMarker>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("No existe ConnectionStrings:DefaultConnection en User Secrets o variables de entorno.");
    return 2;
}

var connection = new SqlConnectionStringBuilder(connectionString);
if (!string.Equals(connection.InitialCatalog, "DB_Laboratorios_Univalle", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("SEGURIDAD: el bootstrap solo puede ejecutarse contra DB_Laboratorios_Univalle.");
    return 3;
}

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<ICurrentUserService, BootstrapCurrentUserService>();
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
    Console.Error.WriteLine("La base oficial tiene migraciones pendientes. No se creará el administrador.");
    return 4;
}

var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

if (arguments.ContainsKey("check"))
{
    Console.WriteLine("Bootstrap verificado: conexión oficial, esquema, UserManager y RoleManager disponibles.");
    return 0;
}

var hasSuperAdmin = await db.Users.IgnoreQueryFilters().AnyAsync(user => user.Role == UserRole.SuperAdmin)
    || await db.UserRoles.AnyAsync(link => db.Roles.Any(role => role.Id == link.RoleId && role.Name == AuthorizationHelper.RoleSuperAdmin));
if (hasSuperAdmin)
{
    Console.Error.WriteLine("Ya existe un SuperAdmin. El bootstrap se niega a crear un segundo usuario.");
    return 5;
}

var userName = RequiredValue(arguments, "username", "Usuario");
var email = RequiredValue(arguments, "email", "Correo");
var firstName = RequiredValue(arguments, "first-name", "Nombres");
var lastName = RequiredValue(arguments, "last-name", "Primer apellido");
var identityCard = RequiredValue(arguments, "identity-card", "Cédula de identidad");
var phone = RequiredValue(arguments, "phone", "Teléfono");

var password = ReadSecret("Contraseña (mínimo 12 caracteres): ");
var confirmation = ReadSecret("Repita la contraseña: ");
if (!string.Equals(password, confirmation, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Las contraseñas no coinciden.");
    return 6;
}

foreach (var roleName in AuthorizationHelper.ManagedIdentityRoles)
{
    if (!await roleManager.RoleExistsAsync(roleName))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
        if (!roleResult.Succeeded)
        {
            Console.Error.WriteLine($"No se pudo crear el rol {roleName}: {FormatErrors(roleResult)}");
            return 7;
        }
    }
}

var user = new User
{
    UserName = userName,
    Email = email,
    EmailConfirmed = true,
    PhoneNumber = phone,
    PhoneNumberConfirmed = true,
    FirstName = firstName,
    LastName = lastName,
    IdentityCard = identityCard,
    Role = UserRole.SuperAdmin,
    Status = GeneralStatus.Activo,
    Position = "Administrador del sistema",
    Department = "Administración",
    CreatedDate = DateTime.UtcNow
};

var createResult = await userManager.CreateAsync(user, password);
password = string.Empty;
confirmation = string.Empty;
if (!createResult.Succeeded)
{
    Console.Error.WriteLine($"No se pudo crear el SuperAdmin: {FormatErrors(createResult)}");
    return 8;
}

var roleSyncResult = await userManager.SynchronizeManagedRoleAsync(user, UserRole.SuperAdmin);
if (!roleSyncResult.Succeeded)
{
    await userManager.DeleteAsync(user);
    Console.Error.WriteLine($"No se pudo sincronizar el rol; se revirtió el usuario: {FormatErrors(roleSyncResult)}");
    return 9;
}

Console.WriteLine($"SuperAdmin creado correctamente: {user.UserName}. La contraseña no fue mostrada ni almacenada por la herramienta.");
return 0;

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
        if (string.Equals(key, "help", StringComparison.OrdinalIgnoreCase))
        {
            result[key] = null;
            continue;
        }

        result[key] = index + 1 < values.Length && !values[index + 1].StartsWith("--", StringComparison.Ordinal)
            ? values[++index]
            : null;
    }

    return result;
}

static string RequiredValue(IReadOnlyDictionary<string, string?> values, string key, string prompt)
{
    if (values.TryGetValue(key, out var supplied) && !string.IsNullOrWhiteSpace(supplied))
    {
        return supplied.Trim();
    }

    Console.Write($"{prompt}: ");
    var entered = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(entered))
    {
        throw new InvalidOperationException($"{prompt} es obligatorio.");
    }

    return entered.Trim();
}

static string ReadSecret(string prompt)
{
    Console.Write(prompt);
    var buffer = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return new string(buffer.ToArray());
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (buffer.Count > 0)
            {
                buffer.RemoveAt(buffer.Count - 1);
            }
            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            buffer.Add(key.KeyChar);
        }
    }
}

static string FormatErrors(IdentityResult result) =>
    string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));

static void PrintUsage()
{
    Console.WriteLine("dotnet run --project Tools/AdminBootstrap -- --username admin --email correo --first-name Nombre --last-name Apellido --identity-card CI --phone Telefono");
    Console.WriteLine("Use --check para validar la inicialización sin crear ni modificar usuarios.");
    Console.WriteLine("La contraseña se solicita de forma interactiva y no se muestra.");
}

sealed class BootstrapMarker;

sealed class BootstrapCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
}
