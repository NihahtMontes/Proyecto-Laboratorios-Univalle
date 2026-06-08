using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Reporting;
using QuestPDF.Infrastructure;
using OfficeOpenXml;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("ProyectoLaboratoriosUnivalle");

// DIAGNÓSTICO: Capturador de crash global a nivel OS
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    try
    {
        var ex = e.ExceptionObject as Exception;
        var message = $"{DateTime.Now:O}: {ex?.GetType().Name} - {ex?.Message}\n{ex?.StackTrace}\n\n";
        File.WriteAllText($"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log", message);
    }
    catch { }
};

// DEBUG: SameSite=None Fix
Console.WriteLine(">>> CARGANDO CONFIGURACIÓN 'SAME-SITE: NONE' (ULTRA COMPATIBLE) <<<");

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        sqlServerOptions.CommandTimeout(120);
    })
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<User, IdentityRole<int>>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    // RELAXED PASSWORD POLICY
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ESTRATEGIA DEFINITIVA: SameSite=None + Secure=Always
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax; // Lax es ideal para funcionar sin SSL localmente
    options.Secure = CookieSecurePolicy.SameAsRequest; // Usa Secure en HTTPS, no usa Secure en HTTP
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".ProyectoUnivalle.Auth.vUniversal";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".ProyectoUnivalle.Antiforgery.vUniversal";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// AÑADIDO: Configuración de Sesiones para el Wizard (Módulo TX-1)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".ProyectoUnivalle.WizardSession";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // <-- Añadido para asegurar compatibilidad de sesión
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IVerificationReportService, VerificationReportService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDashboardReadService, DashboardReadService>();
builder.Services.AddScoped<IManagementContextService, ManagementContextService>();
builder.Services.AddScoped<IManagementActivationService, ManagementActivationService>();
builder.Services.AddScoped<IManagementPlanExclusionService, ManagementPlanExclusionService>();
builder.Services.AddScoped<DatabaseErrorHandler>();

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
builder.WebHost.UseSetting("BrowserLink:Enabled", "false");

builder.Services.AddControllers();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AddPageRoute("/Requests/Details", "Requests/Details/{id:int}");
    options.Conventions.AddPageRoute("/Requests/Details", "Requests/Details/{id:int}/{*extra}");
    options.Conventions.AddPageRoute("/Maintenances/Details", "Maintenances/Details/{id:int}");
    options.Conventions.AddPageRoute("/Maintenances/Details", "Maintenances/Details/{id:int}/{*extra}");
});

// ==============================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    // COMENTADO: HSTS obliga al navegador a usar HTTPS estricto. Se desactiva para permitir HTTP puro.
    // app.UseHsts(); 
}

app.UseExceptionHandler("/Error");

// COMENTADO: Redirección HTTPS desactivada. Si entra por HTTP, se queda en HTTP.
// app.UseHttpsRedirection(); 

app.UseStaticFiles();
app.UseSession();
app.UseRouting();

// SE ELIMINÓ LA DUPLICACIÓN DE CÓDIGO (Tenías StaticFiles, Session y Routing declarados dos veces)

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// INICIALIZACIÓN Y SEMILLA DE BASE DE DATOS
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var config = services.GetRequiredService<IConfiguration>();
        bool autoMigrate = config.GetValue<bool>("Database:AutoMigrate", false);
        bool runSeed = config.GetValue<bool>("Database:RunSeed", false);

        if (autoMigrate)
        {
            Console.WriteLine(">>> APLICANDO MIGRACIONES DE EF <<<");
            await db.Database.MigrateAsync();
        }
        else
        {
            Console.WriteLine(">>> AUTO-MIGRATE DESACTIVADO: Saltando migraciones de EF <<<");
        }

        if (runSeed)
        {
            Console.WriteLine(">>> INICIANDO SEMILLA DE BASE DE DATOS <<<");
            await DbInitializer.SeedAsync(services);
            Console.WriteLine(">>> SEMILLA DE BASE DE DATOS COMPLETADA CON ÉXITO <<<");
        }
        else
        {
            Console.WriteLine(">>> RUN-SEED DESACTIVADO: Saltando semilla de base de datos <<<");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al aplicar migraciones o sembrar la base de datos.");
        Console.WriteLine($">>> ERROR CRÍTICO EN MIGRACIONES/SEMILLA: {ex.Message} <<<");
    }
}

app.Run();