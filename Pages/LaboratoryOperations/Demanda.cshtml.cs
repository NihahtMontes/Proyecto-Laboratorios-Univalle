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
/// Demanda agregada por artículo para las planificaciones de una gestión académica:
/// suma RequestedQuantity de los requerimientos y compara contra el stock del Economato (laboratorio "ECONO").
/// Solo considera planificaciones vigentes (estado que ocupa material) y con fecha igual o posterior a hoy.
/// </summary>
public class DemandaModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;

    public DemandaModel(ApplicationDbContext context, IAcademicManagementService managements)
    {
        _context = context;
        _managements = managements;
    }

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public string Order { get; set; } = "mas";

    [BindProperty(SupportsGet = true)]
    public int? LaboratoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    public SelectList ManagementList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public IReadOnlyList<DemandRow> Rows { get; private set; } = [];
    public IReadOnlyList<string> Categories { get; private set; } = [];
    public decimal TotalRequested { get; private set; }
    public int DeficitCount { get; private set; }
    public int SurplusCount { get; private set; }
    public bool HasManagement { get; private set; }
    public AcademicManagementSummary? Management { get; private set; }
    public string? OriginLaboratoryCode { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;

        Management = await _managements.ResolveAsync(ManagementId);
        ManagementId = Management?.Id;
        HasManagement = Management != null;

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

        // Laboratorios activos: alimenta el filtro y la resolución del Economato.
        var laboratories = await _context.Laboratories.AsNoTracking()
            .Where(item => item.Status == GeneralStatus.Activo)
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Code, item.Name })
            .ToListAsync();
        LaboratoryList = new SelectList(laboratories, "Id", "Name", LaboratoryId);

        var econo = laboratories.FirstOrDefault(item => item.Code == "ECONO");
        var econoDbId = econo?.Id;
        OriginLaboratoryCode = econo?.Code;

        // Demanda agregada por artículo (RequestedQuantity es decimal). Una sola consulta,
        // acotada a las planificaciones de la gestión; los estados no ocupantes no demandan material.
        var demandRows = await _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.Planning!.ManagementId == Management!.Id
                        && !PlanningStatus.NonOccupying.Contains(d.Planning!.Status)
                        && d.Planning!.ScheduledDate >= today
                        && (!LaboratoryId.HasValue || d.Planning!.LaboratoryId == LaboratoryId.Value))
            .GroupBy(d => d.ArticleId)
            .Select(g => new
            {
                ArticleId = g.Key,
                Requested = g.Sum(d => (decimal?)d.RequestedQuantity) ?? 0m,
                PlanCount = g.Count()
            })
            .ToListAsync();

        var articleIds = demandRows.Select(row => row.ArticleId).Distinct().ToList();

        // Stock disponible por artículo del Economato (AvailableQuantity es decimal).
        var stockMap = (econoDbId == null || articleIds.Count == 0)
            ? new Dictionary<int, decimal>()
            : (await _context.ArticleStocks.AsNoTracking()
                .Where(s => s.LaboratoryId == econoDbId.Value && articleIds.Contains(s.ArticleId))
                .GroupBy(s => s.ArticleId)
                .Select(g => new { ArticleId = g.Key, Available = g.Sum(s => s.AvailableQuantity) })
                .ToListAsync())
            .ToDictionary(x => x.ArticleId, x => x.Available);

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
            var hasStock = stockMap.TryGetValue(d.ArticleId, out var stockValue);
            return new DemandRow
            {
                ArticleId = d.ArticleId,
                Code = article?.Code ?? $"ART-{d.ArticleId:0000}",
                Name = article?.Name ?? "Artículo sin nombre",
                Category = article?.Category ?? string.Empty,
                ReviewPending = article?.ReviewStatus == DataReviewStatus.PorConfirmar,
                Requested = d.Requested,
                Available = hasStock ? stockValue : (decimal?)null,
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
        /// <summary>Null cuando no existe ArticleStock para el artículo en el Economato.</summary>
        public decimal? Available { get; init; }
        public decimal Balance => (Available ?? 0m) - Requested;
        public int PlanCount { get; init; }
    }
}
