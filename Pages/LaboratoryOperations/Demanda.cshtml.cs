using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Demanda agregada por artículo para las planificaciones de una gestión académica: suma
/// RequestedQuantity de los requerimientos y la compara contra el stock total institucional (todos los
/// laboratorios; para artículos de equipo, unidades físicas disponibles). Alcance "proximas": solo
/// planificaciones vigentes no finalizadas con fecha igual o posterior a hoy; "gestion": todas las de la
/// gestión que ocupan material (incluidas las finalizadas), sin filtro de fecha.
/// </summary>
public class DemandaModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IArticleAvailabilityService _availability;

    public DemandaModel(ApplicationDbContext context, IAcademicManagementService managements,
        IArticleAvailabilityService availability)
    {
        _context = context;
        _managements = managements;
        _availability = availability;
    }

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public string Order { get; set; } = "mas";

    [BindProperty(SupportsGet = true)]
    public int? LaboratoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    /// <summary>Alcance del consolidado: "proximas" (desde hoy, sin finalizadas) o "gestion" (toda la gestión).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Scope { get; set; }

    /// <summary>True cuando el alcance cubre toda la gestión (incluye finalizadas y fechas pasadas).</summary>
    public bool IsWholeManagement => string.Equals(Scope, "gestion", StringComparison.OrdinalIgnoreCase);

    public SelectList ManagementList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public IReadOnlyList<DemandRow> Rows { get; private set; } = [];
    public IReadOnlyList<string> Categories { get; private set; } = [];
    public decimal TotalRequested { get; private set; }
    public int DeficitCount { get; private set; }
    public int SurplusCount { get; private set; }
    public bool HasManagement { get; private set; }
    public AcademicManagementSummary? Management { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;

        Management = await _managements.ResolveAsync(ManagementId);
        ManagementId = Management?.Id;
        HasManagement = Management != null;
        Scope = ResolveScope(Management);

        var managements = await _managements.ListAsync();
        ManagementList = new SelectList(
            managements.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.Code} · {m.StatusText}{(m.IsCurrent ? " (actual)" : "")}"
            }),
            "Value", "Text", ManagementId);

        if (!HasManagement)
        {
            Rows = [];
            Categories = [];
            LaboratoryList = new SelectList(Enumerable.Empty<SelectListItem>());
            return;
        }

        // Laboratorios activos: alimenta el filtro de ambiente.
        var laboratories = await _context.Laboratories.AsNoTracking()
            .Where(item => item.Status == GeneralStatus.Activo)
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Name })
            .ToListAsync();
        LaboratoryList = new SelectList(laboratories, "Id", "Name", LaboratoryId);

        // Demanda agregada por artículo (RequestedQuantity es decimal). Una sola consulta,
        // acotada a las planificaciones de la gestión; los estados no ocupantes no demandan material.
        // "proximas": no finalizadas y con fecha igual o posterior a hoy. "gestion": toda la gestión.
        var demandQuery = _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.Planning!.ManagementId == Management!.Id
                        && !PlanningStatus.NonOccupying.Contains(d.Planning!.Status)
                        && (!LaboratoryId.HasValue || d.Planning!.LaboratoryId == LaboratoryId.Value));
        if (!IsWholeManagement)
            demandQuery = demandQuery
                .Where(d => d.Planning!.Status != PlanningStatus.Finalized
                            && d.Planning!.ScheduledDate >= today);
        var demandRows = await demandQuery
            .GroupBy(d => d.ArticleId)
            .Select(g => new
            {
                ArticleId = g.Key,
                Requested = g.Sum(d => (decimal?)d.RequestedQuantity) ?? 0m,
                // Planificaciones distintas que piden el artículo, no filas de requerimientos.
                PlanCount = g.Select(d => d.PlanningId).Distinct().Count()
            })
            .ToListAsync();

        var articleIds = demandRows.Select(row => row.ArticleId).Distinct().ToList();

        // Stock disponible por artículo: total institucional (null = todos los laboratorios; equipos por
        // unidades físicas disponibles).
        var stockMap = articleIds.Count == 0
            ? new Dictionary<int, ArticleStockInfo>()
            : await _availability.GetStockAsync(articleIds, null);

        // Catálogo de artículos referenciados (un lote).
        var articleMap = articleIds.Count == 0
            ? new Dictionary<int, ArticleProjection>()
            : (await _context.Articles.AsNoTracking()
                .Where(item => articleIds.Contains(item.Id))
                .Select(item => new ArticleProjection
                {
                    Id = item.Id,
                    Code = item.Code,
                    Name = item.Name,
                    Category = item.Category,
                    ReviewStatus = item.ReviewStatus
                })
                .ToListAsync())
            .ToDictionary(item => item.Id);

        var source = demandRows.Select(d =>
        {
            articleMap.TryGetValue(d.ArticleId, out var article);
            var info = stockMap.GetValueOrDefault(d.ArticleId);
            return new DemandRow
            {
                ArticleId = d.ArticleId,
                Code = article?.Code ?? $"ART-{d.ArticleId:0000}",
                Name = article?.Name ?? "Artículo sin nombre",
                Category = article?.Category ?? string.Empty,
                ReviewPending = article?.ReviewStatus == DataReviewStatus.PorConfirmar,
                Requested = d.Requested,
                Available = info?.Available,
                StockSource = info?.SourceText,
                PlanCount = d.PlanCount
            };
        }).ToList();

        Categories = source
            .Where(item => !string.IsNullOrWhiteSpace(item.Category))
            .Select(item => item.Category!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToList();

        var category = (Category ?? "todos").Trim();
        var filtered = string.Equals(category, "todos", StringComparison.OrdinalIgnoreCase)
            ? source
            : source.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

        Rows = string.Equals(Order, "menos", StringComparison.OrdinalIgnoreCase)
            ? filtered.OrderBy(item => item.Requested).ThenBy(item => item.Name).ToList()
            : filtered.OrderByDescending(item => item.Requested).ThenBy(item => item.Name).ToList();

        TotalRequested = source.Sum(item => item.Requested);
        DeficitCount = source.Count(item => item.Balance < 0);
        SurplusCount = source.Count(item => item.Balance >= 0);
    }

    /// <summary>
    /// Alcance resuelto: "proximas" | "gestion". Si viene vacío (o inválido), "proximas" cuando la gestión
    /// es la actual y editable; si no, "gestion".
    /// </summary>
    private string ResolveScope(AcademicManagementSummary? management)
    {
        var scope = (Scope ?? string.Empty).Trim();
        if (string.Equals(scope, "gestion", StringComparison.OrdinalIgnoreCase)) return "gestion";
        if (string.Equals(scope, "proximas", StringComparison.OrdinalIgnoreCase)) return "proximas";
        return management is { IsCurrent: true, IsEditable: true } ? "proximas" : "gestion";
    }

    private sealed class ArticleProjection
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Category { get; init; }
        public DataReviewStatus ReviewStatus { get; init; }
    }

    public sealed class DemandRow
    {
        public int ArticleId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public bool ReviewPending { get; init; }
        public decimal Requested { get; init; }
        /// <summary>Null cuando el artículo no tiene registro de stock (existencias ni equipo).</summary>
        public decimal? Available { get; init; }
        /// <summary>De dónde sale el stock: "Existencias" o "Unidades físicas" (null sin registro).</summary>
        public string? StockSource { get; init; }
        public decimal Balance => (Available ?? 0m) - Requested;
        public int PlanCount { get; init; }
    }
}
