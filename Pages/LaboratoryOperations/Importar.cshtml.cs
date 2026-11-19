using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.PlanningImport;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Asistente para cargar la planificación del semestre desde Excel (solo SuperAdmin por ahora):
/// 1) descargar la plantilla y subir el archivo, 2) revisar fila por fila y decidir qué hacer con las prácticas que ya
/// están cargadas o tienen problemas, 3) confirmar (todo o nada). El archivo leído y las decisiones viven en caché
/// del servidor durante una hora, bajo una clave propia del usuario.
/// </summary>
[Authorize(Roles = AuthorizationHelper.RoleSuperAdmin)]
public class ImportarModel : PageModel
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    private readonly IPlanningImportService _import;
    private readonly IAcademicManagementService _managements;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ImportarModel> _logger;

    public ImportarModel(IPlanningImportService import, IAcademicManagementService managements, IMemoryCache cache,
        ILogger<ImportarModel> logger)
    {
        _import = import;
        _managements = managements;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Lo que se guarda entre pasos: el archivo leído, la gestión y las decisiones por fila.</summary>
    private sealed class Draft
    {
        public int ManagementId { get; init; }
        public required ImportFile File { get; init; }
        public Dictionary<int, string> Decisions { get; } = [];
    }

    [BindProperty(SupportsGet = true)] public int? ManagementId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Token { get; set; }
    /// <summary>Filtro visual de la lista: todas, problemas, decidir, nuevas, avisos, omitidas.</summary>
    [BindProperty(SupportsGet = true)] public string? Ver { get; set; }
    [BindProperty] public IFormFile? Upload { get; set; }

    public AcademicManagementSummary? Management { get; private set; }
    public IReadOnlyList<AcademicManagementSummary> Managements { get; private set; } = [];
    public ImportAnalysis? Analysis { get; private set; }
    public bool HasCalendar { get; private set; } = true;

    private string CacheKey(string token) => $"PlanImport:{User.FindFirstValue(ClaimTypes.NameIdentifier)}:{token}";

    private Draft? LoadDraft() =>
        !string.IsNullOrWhiteSpace(Token) && _cache.TryGetValue(CacheKey(Token), out Draft? draft) ? draft : null;

    private void SaveDraft(string token, Draft draft) =>
        _cache.Set(CacheKey(token), draft, new MemoryCacheEntryOptions { SlidingExpiration = Lifetime });

    private async Task LoadManagementAsync(int? managementId)
    {
        Managements = (await _managements.ListAsync()).Where(m => m.IsEditable).ToList();
        Management = Managements.FirstOrDefault(m => m.Id == managementId)
            ?? Managements.FirstOrDefault(m => m.IsCurrent)
            ?? Managements.FirstOrDefault();
        ManagementId = Management?.Id;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var draft = LoadDraft();
        if (Token != null && draft == null)
        {
            TempData.Warning("La revisión expiró. Suba el archivo otra vez.");
            return RedirectToPage(new { managementId = ManagementId });
        }
        await LoadManagementAsync(draft?.ManagementId ?? ManagementId);
        if (draft != null && Management != null)
        {
            try
            {
                Analysis = await _import.AnalyzeAsync(Management.Id, draft.File, draft.Decisions);
            }
            catch (ImportFileException ex)
            {
                TempData.Error(ex.Message);
                return RedirectToPage(new { managementId = ManagementId });
            }
        }
        return Page();
    }

    /// <summary>Plantilla de la gestión, con las listas desplegables tomadas del sistema en este momento.</summary>
    public async Task<IActionResult> OnGetTemplateAsync()
    {
        await LoadManagementAsync(ManagementId);
        if (Management == null)
        {
            TempData.Error("No hay una gestión editable para la que descargar la plantilla.");
            return RedirectToPage();
        }
        var bytes = await _import.BuildTemplateAsync(Management.Id);
        return File(bytes, ContentType, $"Planificacion_{Management.Code}_plantilla.xlsx");
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        await LoadManagementAsync(ManagementId);
        if (Management == null)
        {
            TempData.Error("Elija una gestión editable.");
            return RedirectToPage();
        }
        if (Upload == null || Upload.Length == 0)
        {
            TempData.Error("Seleccione el archivo Excel (.xlsx) de la planificación.");
            return RedirectToPage(new { managementId = Management.Id });
        }
        if (!Upload.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || Upload.Length > 10 * 1024 * 1024)
        {
            TempData.Error("El archivo debe ser .xlsx y pesar menos de 10 MB.");
            return RedirectToPage(new { managementId = Management.Id });
        }

        ImportFile file;
        try
        {
            await using var stream = Upload.OpenReadStream();
            file = _import.ReadFile(stream, Path.GetFileName(Upload.FileName));
        }
        catch (ImportFileException ex)
        {
            TempData.Error(ex.Message);
            return RedirectToPage(new { managementId = Management.Id });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo leer el Excel de planificación {File}", Upload.FileName);
            TempData.Error("No se pudo leer el archivo. Verifique que sea la plantilla de planificación en formato .xlsx.");
            return RedirectToPage(new { managementId = Management.Id });
        }

        var token = Guid.NewGuid().ToString("N");
        SaveDraft(token, new Draft { ManagementId = Management.Id, File = file });
        return RedirectToPage(new { token });
    }

    /// <summary>
    /// Guarda las decisiones. <paramref name="d"/> trae la decisión de cada fila mostrada (clave = número de fila);
    /// <paramref name="bulk"/> aplica una decisión a todo un grupo de filas de una vez.
    /// </summary>
    public async Task<IActionResult> OnPostDecideAsync(Dictionary<int, string>? d, string? bulk)
    {
        var draft = LoadDraft();
        if (draft == null)
        {
            TempData.Warning("La revisión expiró. Suba el archivo otra vez.");
            return RedirectToPage(new { managementId = ManagementId });
        }

        foreach (var (rowNumber, value) in d ?? [])
        {
            if (value is ImportDecisions.Skip or ImportDecisions.Overwrite) draft.Decisions[rowNumber] = value;
            else draft.Decisions.Remove(rowNumber);
        }

        if (!string.IsNullOrEmpty(bulk))
        {
            var analysis = await _import.AnalyzeAsync(draft.ManagementId, draft.File, draft.Decisions);
            switch (bulk)
            {
                case "overwriteAll":
                    foreach (var row in analysis.Rows.Where(r => r.Action == ImportRowAction.NeedsDecision && r.CanOverwrite))
                        draft.Decisions[row.RowNumber] = ImportDecisions.Overwrite;
                    break;
                case "skipExisting":
                    foreach (var row in analysis.Rows.Where(r => r.Action == ImportRowAction.NeedsDecision))
                        draft.Decisions[row.RowNumber] = ImportDecisions.Skip;
                    break;
                case "skipProblems":
                    foreach (var row in analysis.Rows.Where(r => r.Action == ImportRowAction.Blocked))
                        draft.Decisions[row.RowNumber] = ImportDecisions.Skip;
                    break;
                case "reset":
                    draft.Decisions.Clear();
                    break;
            }
        }
        SaveDraft(Token!, draft);
        return RedirectToPage(new { token = Token, ver = Ver });
    }

    /// <summary>Descarga el mismo archivo con las columnas Resultado y Qué corregir.</summary>
    public async Task<IActionResult> OnGetResultAsync()
    {
        var draft = LoadDraft();
        if (draft == null)
        {
            TempData.Warning("La revisión expiró. Suba el archivo otra vez.");
            return RedirectToPage(new { managementId = ManagementId });
        }
        var analysis = await _import.AnalyzeAsync(draft.ManagementId, draft.File, draft.Decisions);
        var name = Path.GetFileNameWithoutExtension(draft.File.FileName);
        return File(_import.BuildResultWorkbook(analysis), ContentType, $"{name}_revisado.xlsx");
    }

    public async Task<IActionResult> OnPostApplyAsync()
    {
        var draft = LoadDraft();
        if (draft == null)
        {
            TempData.Warning("La revisión expiró. Suba el archivo otra vez.");
            return RedirectToPage(new { managementId = ManagementId });
        }

        var result = await _import.ApplyAsync(draft.ManagementId, draft.File, draft.Decisions);
        if (!result.Ok)
        {
            TempData.Error(result.Message);
            return RedirectToPage(new { token = Token });
        }

        _cache.Remove(CacheKey(Token!));
        TempData.Success(result.Message);
        _logger.LogInformation("Planificación del semestre cargada: lote {Batch}, {Created} nuevas, {Overwritten} sobrescritas, por {User}",
            result.BatchCode, result.Created, result.Overwritten, User.Identity?.Name);
        return RedirectToPage("./Grilla", new
        {
            managementId = draft.ManagementId,
            laboratoryId = result.FirstLaboratoryId,
            date = result.FirstDate?.ToString("yyyy-MM-dd")
        });
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        var draft = LoadDraft();
        if (Token != null) _cache.Remove(CacheKey(Token));
        await Task.CompletedTask;
        return RedirectToPage(new { managementId = draft?.ManagementId ?? ManagementId });
    }
}
