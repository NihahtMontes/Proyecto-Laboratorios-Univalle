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
        public IList<Proyecto_Laboratorios_Univalle.Models.Career> Careers { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            // Base Queries for Laboratories and Faculties (Excluding deleted)
            var labQuery = _context.Laboratories
                .Include(l => l.CreatedBy)
                .Include(l => l.Faculty)
                .Include(l => l.ModifiedBy)
                .Where(l => l.Status != GeneralStatus.Eliminado);

            var facQuery = _context.Faculties
                .Include(f => f.CreatedBy)
                .Include(f => f.ModifiedBy)
                .Where(f => f.Status != GeneralStatus.Eliminado);

            var carQuery = _context.Careers
                .Include(c => c.CreatedBy)
                .Include(c => c.Facultad)
                .Include(c => c.ModifiedBy)
                .Where(c => c.Status != GeneralStatus.Eliminado);

            // Apply term filter (Global search across labs and faculties)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                
                // Laboratories Filter: Search in name, code, and faculty name
                labQuery = labQuery.Where(l => l.Name.ToLower().Contains(term) || 
                                       l.Code.ToLower().Contains(term) ||
                                       (l.Faculty != null && l.Faculty.Name.ToLower().Contains(term)));

                // Faculties Filter: Search in name and institutional code
                facQuery = facQuery.Where(f => f.Name.ToLower().Contains(term) || 
                                               (f.Code != null && f.Code.ToLower().Contains(term)));

                // Careers Filter: Search in name and faculty name
                carQuery = carQuery.Where(c => c.Name.ToLower().Contains(term) ||
                                               (c.Facultad != null && c.Facultad.Name.ToLower().Contains(term)));
            }

            // Apply status filter 
            if (StatusFilter.HasValue)
            {
                labQuery = labQuery.Where(l => l.Status == StatusFilter.Value);
                facQuery = facQuery.Where(f => f.Status == StatusFilter.Value);
                carQuery = carQuery.Where(c => c.Status == StatusFilter.Value);
            }

            // Ordering for both collections
            Laboratories = await labQuery.OrderBy(l => l.Name).ToListAsync();
            Faculties = await facQuery.OrderBy(f => f.Name).ToListAsync();
            Careers = await carQuery.OrderBy(c => c.Name).ToListAsync();
        }
    }
}
