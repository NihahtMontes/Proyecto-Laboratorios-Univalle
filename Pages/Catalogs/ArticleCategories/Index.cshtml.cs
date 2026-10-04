using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.ArticleCategories
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<ArticleCategory> Categories { get; set; } = new PaginatedList<ArticleCategory>(new List<ArticleCategory>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            // ArticleCategory no tiene query filter global; excluimos eliminados manualmente.
            var query = _context.ArticleCategories
                .AsNoTracking()
                .Where(c => c.Status != GeneralStatus.Eliminado);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(term) || c.Code.ToLower().Contains(term));
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(c => c.Status == StatusFilter.Value);
            }

            Categories = await PaginatedList<ArticleCategory>.CreateAsync(query.OrderBy(c => c.Name), pageIndex ?? 1, 20);
        }
    }
}