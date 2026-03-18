using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Reporting;
using QuestPDF.Infrastructure;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;

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
// Esto permite que las cookies funcionen incluso si el navegador detecta navegación cruzada o mixta (HTTP/HTTPS).
// Es la solución estándar para problemas de "Schemeful Same-Site".

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
    options.LoginPath = "/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".ProyectoUnivalle.Antiforgery.vUniversal";
    options.Cookie.SameSite = SameSiteMode.Lax; 
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IVerificationReportService, VerificationReportService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<DatabaseErrorHandler>();
builder.Services.AddScoped<DataMigrationService>();

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    // Browser Link se deshabilita vía appsettings.Development.json o configuración de VS,
    // pero asegurarnos de NO llamarlo aqui ayuda.
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCookiePolicy(); // Middleware crítico

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// INICIALIZACIÓN Y SEMILLA DE BASE DE DATOS
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Aplicar migraciones pendientes antes de sembrar
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