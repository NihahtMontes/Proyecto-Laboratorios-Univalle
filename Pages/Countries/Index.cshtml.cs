using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Countries
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<Country> Countries { get; set; } = new PaginatedList<Country>(new List<Country>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            var query = _context.Countries
                .Include(c => c.CreatedBy)
                .Include(c => c.ModifiedBy)
                .Where(c => c.Status != GeneralStatus.Eliminado);

            // Term Filter
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(term));
            }

            // Status Filter
            if (StatusFilter.HasValue)
            {
                query = query.Where(c => c.Status == StatusFilter.Value);
            }

            Countries = await PaginatedList<Country>.CreateAsync(query.OrderBy(c => c.Name), pageIndex ?? 1, 20);
        }
    }
}
