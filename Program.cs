using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
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

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se configuró ConnectionStrings:DefaultConnection. Use User Secrets en desarrollo o variables de entorno en despliegue.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        sqlServerOptions.CommandTimeout(120);
    })
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

builder.Services.AddIdentity<User, IdentityRole<int>>(options => {
    options.SignIn.RequireConfirmedAccount = false;
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
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = cookieSecurePolicy;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".ProyectoUnivalle.Auth.vUniversal";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/Error";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(5);
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = ".ProyectoUnivalle.Antiforgery.vUniversal";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.HeaderName = "RequestVerificationToken";
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
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
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
builder.Services.AddScoped<IEquipmentClassificationDecisionService, EquipmentClassificationDecisionService>();
builder.Services.AddScoped<ICatalogClassificationBatchService, CatalogClassificationBatchService>();
builder.Services.AddScoped<DatabaseErrorHandler>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

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

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler("/Error");
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return Task.CompletedTask;
    });

    await next();
});

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
            if (!app.Environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "La aplicacion automatica de migraciones solo puede habilitarse en Development.");
            }

            Console.WriteLine(">>> APLICANDO MIGRACIONES DE EF <<<");
            await db.Database.MigrateAsync();
        }
        else
        {
            Console.WriteLine(">>> AUTO-MIGRATE DESACTIVADO: Saltando migraciones de EF <<<");
        }

        if (runSeed)
        {
            if (!app.Environment.IsDevelopment())
            {
                throw new InvalidOperationException("La semilla de demostración solo puede ejecutarse en Development.");
            }

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
        throw;
    }
}

// TEMPORARY: Reset admin password to login locally
using (var resetScope = app.Services.CreateScope())
{
    var userManager = resetScope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var adminUser = await userManager.FindByEmailAsync("admin@univalle.edu"); // Assuming this is the email
    if (adminUser != null)
    {
        adminUser.Status = Proyecto_Laboratorios_Univalle.Models.Enums.GeneralStatus.Activo;
        adminUser.LockoutEnd = null;
        adminUser.AccessFailedCount = 0;
        await userManager.UpdateAsync(adminUser);

        var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
        await userManager.ResetPasswordAsync(adminUser, token, "Admin123!");
        Console.WriteLine("====================================================");
        Console.WriteLine(">>> ÉXITO: Contraseña de admin@univalle.edu reseteada a Admin123! <<<");
        Console.WriteLine("====================================================");
    }
    else
    {
        Console.WriteLine("====================================================");
        Console.WriteLine(">>> ADVERTENCIA: El usuario admin@univalle.edu NO EXISTE en la BD. Buscando alternativas... <<<");
        var firstUser = await userManager.Users.FirstOrDefaultAsync();
        if (firstUser != null)
        {
            firstUser.Status = Proyecto_Laboratorios_Univalle.Models.Enums.GeneralStatus.Activo;
            firstUser.LockoutEnd = null;
            firstUser.AccessFailedCount = 0;
            await userManager.UpdateAsync(firstUser);
            var token = await userManager.GeneratePasswordResetTokenAsync(firstUser);
            await userManager.ResetPasswordAsync(firstUser, token, "Admin123!");
            Console.WriteLine($">>> ÉXITO: Contraseña de {firstUser.Email} reseteada a Admin123! Usa este correo para entrar. <<<");
        }
        Console.WriteLine("====================================================");
    }
}

app.Run();
