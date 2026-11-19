using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using System.Security.Claims;
using AssetViewIndexModel = Proyecto_Laboratorios_Univalle.Pages.AssetView.IndexModel;
using DashboardIndexModel = Proyecto_Laboratorios_Univalle.Pages.IndexModel;
using GestionesIndexModel = Proyecto_Laboratorios_Univalle.Pages.Managements.IndexModel;

namespace Laboratorios.Tests;

/// <summary>
/// Inicio de la aplicación (navegación): /Index sin asistente y el Supervisor aterrizan en el
/// nuevo Inicio (/AssetView/Index); el Administrador que regresa con ManagementId de una gestión
/// activa sigue llegando al panel de su gestión. Cubre también los handlers CurrentPreventive /
/// CurrentCorrective de Gestiones y el handler de indicadores del Inicio.
/// </summary>
public class InicioPageTests
{
    // ═══════════════ (a) /Index sin asistente ═══════════════

    [Fact]
    public async Task Index_SinManagementId_Admin_RedirigeAlNuevoInicio()
    {
        using var db = new TestDb();
        var page = DashboardPage(db);
        page.HttpContext.User = Principal(AuthorizationHelper.RoleAdministrator);

        var result = await page.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/AssetView/Index", redirect.PageName);
    }

    [Fact]
    public async Task Index_AdminConManagementIdActivo_RedirigeAlPanelDeLaGestion()
    {
        using var db = new TestDb();
        var page = DashboardPage(db);
        page.HttpContext.User = Principal(AuthorizationHelper.RoleAdministrator);
        page.ManagementId = TestDb.ManagementId; // Activa por defecto en la siembra.

        var result = await page.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Managements/Details", redirect.PageName);
        Assert.Equal(TestDb.ManagementId, redirect.RouteValues!["id"]);
        Assert.Equal("dashboard", redirect.RouteValues!["ActiveTab"]);
    }

    [Fact]
    public async Task Index_Supervisor_RedirigeAlNuevoInicio()
    {
        using var db = new TestDb();
        var page = DashboardPage(db);
        page.HttpContext.User = Principal(AuthorizationHelper.RoleSupervisor);
        page.ManagementId = TestDb.ManagementId; // Aunque traiga contexto de gestión, el Supervisor va al Inicio.

        var result = await page.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/AssetView/Index", redirect.PageName);
    }

    [Fact]
    public async Task Index_AdminConManagementIdInexistente_RedirigeAlNuevoInicio()
    {
        using var db = new TestDb();
        var page = DashboardPage(db);
        page.HttpContext.User = Principal(AuthorizationHelper.RoleAdministrator);
        page.ManagementId = 999; // No existe: no hay gestión activa a la que volver.

        var result = await page.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/AssetView/Index", redirect.PageName);
    }

    // ═══════════════ (b) Gestiones: handlers de gestión activa ═══════════════

    [Fact]
    public async Task CurrentPreventive_ConGestionActiva_AbreElPanel()
    {
        using var db = new TestDb();
        var preventive = new Management { Id = 5, Code = "2026-1", Type = ManagementType.Preventive, Status = ManagementStatus.Active };
        var page = GestionesPage(db, new StubManagementContext(preventive: preventive));

        var result = await page.OnGetCurrentPreventiveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Details", redirect.PageName);
        Assert.Equal(5, redirect.RouteValues!["id"]);
        Assert.Equal("dashboard", redirect.RouteValues!["ActiveTab"]);
    }

    [Fact]
    public async Task CurrentPreventive_SinGestionActiva_VuelveALaListaConAviso()
    {
        using var db = new TestDb();
        var page = GestionesPage(db, new StubManagementContext(preventive: null));

        var result = await page.OnGetCurrentPreventiveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Index", redirect.PageName);
        Assert.Equal(ManagementType.Preventive.ToString(), redirect.RouteValues!["Type"]);
        Assert.Equal("No hay una gestión preventiva activa.", page.TempData["InfoMessage"] as string);
    }

    [Fact]
    public async Task CurrentCorrective_ConGestionActiva_AbreElPanel()
    {
        using var db = new TestDb();
        var corrective = new Management { Id = 7, Code = "CORR-2026-0", Type = ManagementType.Corrective, Status = ManagementStatus.Active };
        var page = GestionesPage(db, new StubManagementContext(corrective: corrective));

        var result = await page.OnGetCurrentCorrectiveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Details", redirect.PageName);
        Assert.Equal(7, redirect.RouteValues!["id"]);
        Assert.Equal("dashboard", redirect.RouteValues!["ActiveTab"]);
    }

    [Fact]
    public async Task CurrentCorrective_SinGestionActiva_VuelveALaListaCorrectiva()
    {
        using var db = new TestDb();
        var page = GestionesPage(db, new StubManagementContext(corrective: null));

        var result = await page.OnGetCurrentCorrectiveAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Index", redirect.PageName);
        Assert.Equal(ManagementType.Corrective.ToString(), redirect.RouteValues!["Type"]);
        Assert.NotNull(page.TempData["WarningMessage"] as string);
    }

    // ═══════════════ (c) Inicio: indicadores con caché ═══════════════

    [Fact]
    public async Task Indicadores_DevuelveLosSieteGraficosYCacheaElResultado()
    {
        using var db = new TestDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var page = Prepare(new AssetViewIndexModel(
            db.Context,
            new StubManagementContext(),
            new SidebarCountsService(db.Context, NullLogger<SidebarCountsService>.Instance),
            new FakeCurrentUser(),
            cache));
        page.HttpContext.User = Principal(AuthorizationHelper.RoleAdministrator);

        var first = Assert.IsType<JsonResult>(await page.OnGetIndicadoresAsync());
        var payload = Assert.IsType<Proyecto_Laboratorios_Univalle.Pages.AssetView.InicioIndicadoresPayload>(first.Value);
        Assert.Equal(7, payload.Charts.Count);
        Assert.Contains(payload.Charts, chart => chart.Key == "category" && chart.Type == "donut");
        Assert.Contains(payload.Charts, chart => chart.Key == "environment" && chart.Type == "bar");

        var second = Assert.IsType<JsonResult>(await page.OnGetIndicadoresAsync());
        Assert.Same(payload, second.Value); // 5 minutos de caché: misma instancia.
    }

    // ═══════════════ Soporte ═══════════════

    /// <summary>Inicializa PageContext con ViewData/ModelState, HttpContext mínimo y TempData en memoria.</summary>
    private static TPage Prepare<TPage>(TPage page) where TPage : PageModel
    {
        page.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
        page.TempData = new TempDataDictionary(page.HttpContext, new StubTempDataProvider());
        return page;
    }

    private static ClaimsPrincipal Principal(params string[] roles)
    {
        var identity = new ClaimsIdentity(roles.Select(role => new Claim(ClaimTypes.Role, role)), "Test");
        return new ClaimsPrincipal(identity);
    }

    private static DashboardIndexModel DashboardPage(TestDb db) => Prepare(new DashboardIndexModel(
        db.Context,
        new UserManager<User>(new StubUserStore(), null!, null!, null!, null!, null!, null!, null!, NullLogger<UserManager<User>>.Instance),
        NullLogger<DashboardIndexModel>.Instance,
        new StubManagementContext(),
        new StubDashboardReadService(),
        new StubManagementActivationService(),
        new StubStateHistory()));

    private static GestionesIndexModel GestionesPage(TestDb db, StubManagementContext managementContext) => Prepare(
        new GestionesIndexModel(
            db.Context,
            managementContext,
            new StubManagementActivationService(),
            new StubPurge(),
            NullLogger<GestionesIndexModel>.Instance));

    private sealed class StubManagementContext : IManagementContextService
    {
        private readonly Management? _preventive;
        private readonly Management? _corrective;

        public StubManagementContext(Management? preventive = null, Management? corrective = null)
        {
            _preventive = preventive;
            _corrective = corrective;
        }

        public Task<Management?> GetCurrentManagementAsync(ManagementType? type = null)
            => Task.FromResult(type == ManagementType.Corrective ? _corrective : _preventive);

        public void InvalidateCache() { }
    }

    private sealed class StubDashboardReadService : IDashboardReadService
    {
        public Task<DashboardReadResult> LoadAsync(
            int managementId,
            ManagementType managementType,
            DashboardReadFilter filter,
            bool includeWizard,
            int? selectedLabId,
            int currentStep = 0,
            int currentPage = 1)
            => Task.FromResult(new DashboardReadResult());
    }

    private sealed class StubManagementActivationService : IManagementActivationService
    {
        public Task<ManagementActivationResult> ActivateAsync(Management management)
            => Task.FromResult(new ManagementActivationResult { ActiveManagement = management });

        public Task<ManagementActivationResult> ReconcileActiveAsync(ManagementType type, int? preferredManagementId = null)
            => Task.FromResult(new ManagementActivationResult());
    }

    private sealed class StubStateHistory : IEquipmentStateHistoryService
    {
        public Task<EquipmentStateHistory> ChangeStatusAsync(EquipmentUnit unit, EquipmentStatus newStatus, string? reason,
            DateTime? at = null, int? userId = null, bool splitIfSameStatus = false)
            => Task.FromResult(new EquipmentStateHistory());

        public Task<EquipmentStateHistory> EnsureInitialAsync(EquipmentUnit unit, string reason, int? userId = null)
            => Task.FromResult(new EquipmentStateHistory());
    }

    private sealed class StubPurge : ICourseGroupPurgeService
    {
        public Task<int> PurgeManagementAsync(int managementId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> DeleteGroupAsync(int courseGroupId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<int> PurgeDeletedAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    private sealed class StubTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    /// <summary>UserStore mínimo: solo se necesita GetUserAsync, que devuelve null sin claims.</summary>
    private sealed class StubUserStore : IUserStore<User>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.UserName);
        public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(User user, string? normalizedUserName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<User?>(null);
        public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<User?>(null);
    }
}
