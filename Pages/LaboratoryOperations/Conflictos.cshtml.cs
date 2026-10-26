using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Grilla semanal de conflictos de material (compartida entre laboratorios): leyenda filtrable,
/// grilla de periodos x días con las marcas de cada aviso y panel lateral con el detalle y las
/// acciones de resolución. También expone "Reducir cantidad" y "Reponer día".
/// </summary>
[Authorize(Roles = AuthorizationHelper.ManagementRoles)]
public class ConflictosModel : PageModel
{
    private static readonly CultureInfo Cultura = new("es-ES");
    private static readonly string[] AdminRoles =
        AuthorizationHelper.AdminRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static readonly string[] ManagerRoles =
        AuthorizationHelper.ManagementRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private readonly IConflictGridService _grid;
    private readonly IConflictResolutionService _resolution;
    private readonly IDayRecoveryService _recoveries;

    public ConflictosModel(IConflictGridService grid, IConflictResolutionService resolution, IDayRecoveryService recoveries)
    {
        _grid = grid;
        _resolution = resolution;
        _recoveries = recoveries;
    }

    // ── Query ──
    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Week { get; set; }

    /// <summary>Fecha suelta (yyyy-MM-dd); la semana se elige por SelDate, Date o número.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Date { get; set; }

    /// <summary>Fecha del conflicto seleccionado; con ArticleId abre el panel de detalle.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SelDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ArticleId { get; set; }

    /// <summary>Filtro por tipo ("Overlap", "Shortage"…); se ignora si no es válido.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Kind { get; set; }

    /// <summary>Grilla semanal armada por el servicio (null si no hay gestión académica).</summary>
    public ConflictWeekGrid? Grid { get; private set; }

    /// <summary>Filtro de tipo ya parseado (null si Kind falta o es inválido).</summary>
    public ConflictKind? KindFilter =>
        Enum.TryParse<ConflictKind>(Kind, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) && parsed != ConflictKind.None
            ? parsed
            : null;

    /// <summary>URL actual completa (path + query): destino del regreso tras reducir una cantidad.</summary>
    public string CurrentUrl => Request.Path + Request.QueryString;

    /// <summary>El rol habilita "Reponer día" (el servicio exige además día con prácticas y no suspendido).</summary>
    public bool CanRecover => ManagerRoles.Any(User.IsInRole);

    public async Task OnGetAsync()
    {
        var rights = new ConflictUserRights(
            IsAdmin: AdminRoles.Any(User.IsInRole),
            CanManage: ManagerRoles.Any(User.IsInRole),
            IsSuperAdmin: User.IsInRole(AuthorizationHelper.RoleSuperAdmin));
        Grid = await _grid.BuildAsync(
            new ConflictGridQuery(ManagementId, Week, Date, SelDate, ArticleId, KindFilter),
            rights, CurrentUrl);
    }

    /// <summary>Reduce la cantidad pedida del artículo en una planificación y vuelve a la pantalla.</summary>
    public async Task<IActionResult> OnPostReduceQuantityAsync(int planningId, int articleId, decimal newQuantity, string? returnTo)
    {
        var result = await _resolution.ReduceQuantityAsync(planningId, articleId, newQuantity);
        if (result.Ok) TempData.Success(result.Message);
        else TempData.Error(result.Message);
        return Url.IsLocalUrl(returnTo) ? LocalRedirect(returnTo) : RedirectToPage();
    }

    /// <summary>Vista previa (GET, solo lectura) del modal "Reponer día": fecha yyyy-MM-dd.</summary>
    public async Task<IActionResult> OnGetRecoveryPreviewAsync(int managementId, string date)
    {
        if (!CanRecover)
            return new JsonResult(new DayRecoveryPreview(false, "No tiene permiso para reponer días.", default, null, 0, [], []));
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return new JsonResult(new DayRecoveryPreview(false, "Elija una fecha válida.", default, null, 0, [], []));
        return new JsonResult(await _recoveries.PreviewAsync(managementId, parsed));
    }

    /// <summary>Suspende el día elegido desde la grilla, crea la reposición y abre la grilla con los afectados.</summary>
    public async Task<IActionResult> OnPostRecoverDayAsync(int managementId, string date, string? reason)
    {
        if (!CanRecover)
        {
            TempData.Error("No tiene permiso para reponer días suspendidos.");
            return RedirectToPage(new { managementId, week = Week });
        }
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            TempData.Error("Elija una fecha válida para reponer.");
            return RedirectToPage(new { managementId, week = Week });
        }
        var result = await _recoveries.SuspendByDateAsync(managementId, parsed, reason);
        if (result.Ok)
        {
            TempData.Success(result.Message);
            return RedirectToPage("./Grilla", new { managementId, recoveryId = result.RecoveryId });
        }
        TempData.Error(result.Message);
        return RedirectToPage(new { managementId, week = Week, kind = KindFilter });
    }

    /// <summary>Línea breve de cada aviso del panel lateral, según el tipo de conflicto.</summary>
    public static string LineaBreve(ConflictItem item, CultureInfo cultura) => item.Kind switch
    {
        ConflictKind.Overlap => $"faltan {item.Shortage.ToString("0.##", cultura)} · {item.PeakText}",
        ConflictKind.Shortage => $"falta {item.Shortage.ToString("0.##", cultura)}",
        ConflictKind.Rotation => "alcanza turnando",
        _ => "sin existencias"
    };

    /// <summary>Clase Font Awesome de una acción ("mdi …" ya trae su prefijo).</summary>
    public static string IconClass(string icon) => icon.StartsWith("mdi", StringComparison.Ordinal) ? icon : $"fas {icon}";
}
