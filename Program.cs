using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Reporting;
using QuestPDF.Infrastructure;
using OfficeOpenXml;

QuestPDF.Settings.License = LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);

// DEBUG: SameSite=None Fix
Console.WriteLine(">>> CARGANDO CONFIGURACIÓN 'SAME-SITE: NONE' (ULTRA COMPATIBLE) <<<");

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configuración de la base de datos SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    options.MinimumSameSitePolicy = SameSiteMode.Lax; // Cambiado a Lax para compatibilidad local
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".ProyectoUnivalle.Auth.vUniversal";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/Login"; // Ruta a la que redirige si no hay sesión
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".ProyectoUnivalle.Antiforgery.vUniversal";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// AÑADIDO: Configuración de Sesiones para el Wizard (Módulo TX-1)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".ProyectoUnivalle.WizardSession";
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IVerificationReportService, VerificationReportService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IManagementContextService, ManagementContextService>();
builder.Services.AddScoped<DatabaseErrorHandler>();
// builder.Services.AddScoped<DataMigrationService>(); // Removido: Mantenimiento de modelos a enums completado.

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;


builder.Services.AddRazorPages(options =>
{
    // Esto obliga a que CUALQUIER página pida Login por defecto
    options.Conventions.AuthorizeFolder("/");
    // Si tu página de Login está en la raíz, debes permitirle el acceso anónimo:
    options.Conventions.AllowAnonymousToPage("/Login");
});
builder.Services.AddScoped<IReportService, ReportService>();
// ==============================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// AÑADIDO: Activar Middleware de Sesiones
app.UseSession();

app.UseRouting();
app.UseCookiePolicy();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// INICIALIZACIÓN Y SEMILLA DE BASE DE DATOS
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        Console.WriteLine(">>> APLICANDO MIGRACIONES DE EF <<<");
        await db.Database.MigrateAsync();

        Console.WriteLine(">>> INICIANDO SEMILLA DE BASE DE DATOS <<<");
        await DbInitializer.SeedAsync(services);
        Console.WriteLine(">>> SEMILLA DE BASE DE DATOS COMPLETADA CON ÉXITO <<<");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al aplicar migraciones o sembrar la base de datos.");
        Console.WriteLine($">>> ERROR CRÍTICO EN MIGRACIONES/SEMILLA: {ex.Message} <<<");
    }
}

app.Run();