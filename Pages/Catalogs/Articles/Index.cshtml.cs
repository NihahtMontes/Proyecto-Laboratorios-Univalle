using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Articles
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<Article> Articles { get; set; } = new PaginatedList<Article>(new List<Article>(), 0, 1, 20);

        public List<SelectListItem> CategoryOptions { get; set; } = new();

        /// <summary>Cantidad de artículos con revisión PorConfirmar (calculada con el resto de filtros aplicados, pero sin el propio filtro de revisión).</summary>
        public int PendingReviewCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ArticleCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DataReviewStatus? ReviewStatusFilter { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            // Article tiene query filter global (Status != Eliminado).
            var baseQuery = _context.Articles
                .AsNoTracking()
                .Include(a => a.ArticleCategory)
                .Include(a => a.MeasurementUnit)
                .Where(a => a.Status != GeneralStatus.Eliminado);

            // Filtro de texto
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                baseQuery = baseQuery.Where(a => a.Name.ToLower().Contains(term) || a.Code.ToLower().Contains(term));
            }

            if (StatusFilter.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.Status == StatusFilter.Value);
            }

            if (ArticleCategoryId.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.ArticleCategoryId == ArticleCategoryId.Value);
            }

            // Filtro de revisión (Todos/Confirmado/Por confirmar)
            if (ReviewStatusFilter.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.ReviewStatus == ReviewStatusFilter.Value);
            }

            // Contador de Por confirmar (calculado sobre los mismos filtros de categoría/estado/texto, sin filtro de revisión).
            PendingReviewCount = await baseQuery.CountAsync(a => a.ReviewStatus == DataReviewStatus.PorConfirmar);

            Articles = await PaginatedList<Article>.CreateAsync(
                baseQuery.OrderBy(a => a.Name),
                pageIndex ?? 1,
                20);

            // Opciones de filtro de categoría.
            CategoryOptions = await _context.ArticleCategories
                .AsNoTracking()
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }
    }
}