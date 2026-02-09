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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

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
    options.MinimumSameSitePolicy = SameSiteMode.None; // Permisivo
    options.Secure = CookieSecurePolicy.Always;      // Requerido si usamos None
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".ProyectoUnivalle.Auth.vUniversal"; 
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None; // Fundamental para evitar el error
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/Login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".ProyectoUnivalle.Antiforgery.vUniversal";
    options.Cookie.SameSite = SameSiteMode.None; // Fundamental para evitar el error
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IVerificationReportService, VerificationReportService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<DatabaseErrorHandler>();

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
        Console.WriteLine(">>> INICIANDO SEMILLA DE BASE DE DATOS <<<");
        await DbInitializer.SeedAsync(services);
        Console.WriteLine(">>> SEMILLA DE BASE DE DATOS COMPLETADA CON ÉXITO <<<");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al sembrar la base de datos.");
        Console.WriteLine($">>> ERROR CRÍTICO EN SEMILLA: {ex.Message} <<<");
    }
}

app.Run();
        