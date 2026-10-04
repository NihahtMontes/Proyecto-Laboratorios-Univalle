using Proyecto_Laboratorios_Univalle.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Stock
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<StockRowViewModel> StockRows { get; set; } = new List<StockRowViewModel>();

        public SelectList LaboratoryOptions { get; set; } = new SelectList(Enumerable.Empty<SelectListItem>());

        [BindProperty(SupportsGet = true)]
        public int? LaboratoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        /// <summary>Identificador del laboratorio efectivamente seleccionado (post-resolución de ECONO por defecto).</summary>
        public int? SelectedLaboratoryId { get; set; }

        /// <summary>Etiqueta visible del laboratorio seleccionado.</summary>
        public string? SelectedLaboratoryDisplay { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Dropdown: laboratorios activos, ordenados por código para que ECONO salga primero.
            var laboratories = await _context.Laboratories
                .AsNoTracking()
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Code)
                .ThenBy(l => l.Name)
                .Select(l => new { l.Id, DisplayName = (l.Code ?? string.Empty) + " - " + l.Name })
                .ToListAsync();

            LaboratoryOptions = new SelectList(laboratories, "Id", "DisplayName", LaboratoryFilter);

            // Selección por defecto: si el usuario no eligió laboratorio, usar ECONO si existe.
            if (!LaboratoryFilter.HasValue)
            {
                LaboratoryFilter = await _context.Laboratories
                    .AsNoTracking()
                    .Where(l => l.Code == "ECONO")
                    .Select(l => (int?)l.Id)
                    .FirstOrDefaultAsync();
            }

            if (!LaboratoryFilter.HasValue || LaboratoryFilter.Value <= 0)
            {
                // Sin laboratorio seleccionado no hay existencias que mostrar.
                SelectedLaboratoryId = null;
                SelectedLaboratoryDisplay = null;
                StockRows = new List<StockRowViewModel>();
                return Page();
            }

            SelectedLaboratoryId = LaboratoryFilter.Value;

            var labInfo = await _context.Laboratories
                .AsNoTracking()
                .Where(l => l.Id == LaboratoryFilter.Value)
                .Select(l => new { l.Code, l.Name })
                .FirstOrDefaultAsync();

            if (labInfo == null)
            {
                SelectedLaboratoryDisplay = null;
                StockRows = new List<StockRowViewModel>();
                return Page();
            }

            SelectedLaboratoryDisplay = string.IsNullOrWhiteSpace(labInfo.Code)
                ? labInfo.Name
                : $"{labInfo.Code} - {labInfo.Name}";

            // Artículos activos (el query filter global de Article ya excluye Eliminado).
            var articlesQuery = _context.Articles
                .AsNoTracking()
                .Include(a => a.MeasurementUnit)
                .Where(a => a.Status == GeneralStatus.Activo);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                articlesQuery = articlesQuery.Where(a =>
                    a.Name.ToLower().Contains(term) ||
                    a.Code.ToLower().Contains(term));
            }

            var articles = await articlesQuery
                .OrderBy(a => a.Name)
                .ThenBy(a => a.Code)
                .Select(a => new
                {
                    a.Id,
                    a.Code,
                    a.Name,
                    a.UnitOfMeasure,
                    MeasurementUnitCode = a.MeasurementUnit != null ? a.MeasurementUnit.Code : null,
                    MeasurementUnitName = a.MeasurementUnit != null ? a.MeasurementUnit.Name : null
                })
                .ToListAsync();

            // LEFT JOIN lógico: cargar el stock existente del laboratorio y mapear por artículo.
            var stock = await _context.ArticleStocks
                .AsNoTracking()
                .Where(s => s.LaboratoryId == LaboratoryFilter.Value)
                .ToDictionaryAsync(s => s.ArticleId, s => new { s.AvailableQuantity, s.UpdatedOn });

            StockRows = articles
                .Select(a =>
                {
                    var hasRow = stock.TryGetValue(a.Id, out var existing);
                    return new StockRowViewModel
                    {
                        ArticleId = a.Id,
                        ArticleCode = a.Code,
                        ArticleName = a.Name,
                        UnitOfMeasure = !string.IsNullOrWhiteSpace(a.MeasurementUnitName) ? a.MeasurementUnitName! : a.UnitOfMeasure,
                        UnitCode = a.MeasurementUnitCode,
                        AvailableQuantity = hasRow ? existing!.AvailableQuantity : 0m,
                        UpdatedOn = hasRow ? existing!.UpdatedOn : null,
                        HasStockRow = hasRow
                    };
                })
                .ToList();

            return Page();
        }

        public class StockRowViewModel
        {
            public int ArticleId { get; set; }
            public string ArticleCode { get; set; } = string.Empty;
            public string ArticleName { get; set; } = string.Empty;
            public string UnitOfMeasure { get; set; } = string.Empty;
            public string? UnitCode { get; set; }
            public decimal AvailableQuantity { get; set; }
            public DateTime? UpdatedOn { get; set; }
            public bool HasStockRow { get; set; }
        }
    }
}