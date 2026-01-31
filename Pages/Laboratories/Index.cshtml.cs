using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Laboratories
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Laboratory> Laboratories { get; set; } = default!;
        public IList<Faculty> Faculties { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            // Query Base Laboratorios
            var labQuery = _context.Laboratories
                .Include(l => l.CreatedBy)
                .Include(l => l.Faculty)
                .Include(l => l.ModifiedBy)
                .Where(l => l.Status != GeneralStatus.Eliminado);

            // Query Base Facultades
            var facQuery = _context.Faculties
                .Include(f => f.CreatedBy)
                .Include(f => f.ModifiedBy)
                .Where(f => f.Status != GeneralStatus.Eliminado);

            // Filtro por término (Global)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                
                // Filtro Labs
                labQuery = labQuery.Where(l => l.Name.ToLower().Contains(term) || 
                                       l.Code.ToLower().Contains(term) ||
                                       (l.Faculty != null && l.Faculty.Name.ToLower().Contains(term)));

                // Filtro Facultades
                facQuery = facQuery.Where(f => f.Name.ToLower().Contains(term) || 
                                               (f.Code != null && f.Code.ToLower().Contains(term)));
            }

            // Filtro por Estado (Global)
            if (StatusFilter.HasValue)
            {
                labQuery = labQuery.Where(l => l.Status == StatusFilter.Value);
                facQuery = facQuery.Where(f => f.Status == StatusFilter.Value);
            }

            Laboratories = await labQuery.ToListAsync();
            Faculties = await facQuery.ToListAsync();
        }
    }
}
