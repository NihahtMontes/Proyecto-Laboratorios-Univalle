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
        public List<SelectListItem> ArticleTypeOptions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ArticleCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public ArticleType? ArticleTypeFilter { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            // Article tiene query filter global (Status != Eliminado).
            var query = _context.Articles
                .AsNoTracking()
                .Include(a => a.ArticleCategory)
                .Include(a => a.MeasurementUnit)
                .Where(a => a.Status != GeneralStatus.Eliminado);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(term) || a.Code.ToLower().Contains(term));
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(a => a.Status == StatusFilter.Value);
            }

            if (ArticleCategoryId.HasValue)
            {
                query = query.Where(a => a.ArticleCategoryId == ArticleCategoryId.Value);
            }

            if (ArticleTypeFilter.HasValue)
            {
                query = query.Where(a => a.ArticleType == ArticleTypeFilter.Value);
            }

            // Cargar opciones de filtro (categorías activas).
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

            ArticleTypeOptions = Enum.GetValues<ArticleType>()
                .Select(t => new SelectListItem
                {
                    Value = ((int)t).ToString(),
                    Text = EnumHelper.GetDisplayName(t)
                })
                .ToList();

            Articles = await PaginatedList<Article>.CreateAsync(query.OrderBy(a => a.Name), pageIndex ?? 1, 20);
        }
    }
}