using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Faculties
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Faculty> Faculties { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public IActionResult OnGet()
        {
            return RedirectToPage("/Laboratories/Index", null, "faculties");
        }

        public async Task OnGetAsync()
        {
            var query = _context.Faculties
                .Include(f => f.CreatedBy)
                .Include(f => f.ModifiedBy)
                .Where(f => f.Status != GeneralStatus.Eliminado);

            // Term and Code Filter
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(f => f.Name.ToLower().Contains(term) || (f.Code != null && f.Code.ToLower().Contains(term)));
            }

            // Status Filter
            if (StatusFilter.HasValue)
            {
                query = query.Where(f => f.Status == StatusFilter.Value);
            }

            Faculties = await query.OrderBy(f => f.Name).ToListAsync();
        }
    }
}
