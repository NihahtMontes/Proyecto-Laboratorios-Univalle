using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Cities
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<City> Cities { get; set; } = default!;
        public IList<Country> Countries { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            // Loading Cities with filters
            var citiesQuery = _context.Cities
                .Include(c => c.Country)
                .Include(c => c.CreatedBy)
                .Include(c => c.ModifiedBy)
                .Where(c => c.Status != GeneralStatus.Eliminado);

            // Term Filter (City name or Country name)
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                citiesQuery = citiesQuery.Where(c => c.Name.ToLower().Contains(term) || 
                                       (c.Country != null && c.Country.Name.ToLower().Contains(term)));
            }

            // Status Filter
            if (StatusFilter.HasValue)
            {
                citiesQuery = citiesQuery.Where(c => c.Status == StatusFilter.Value);
            }

            Cities = await citiesQuery.OrderBy(c => c.Name).ToListAsync();

            // Loading Countries for the Countries tab
            Countries = await _context.Countries
                .Include(c => c.CreatedBy)
                .Include(c => c.ModifiedBy)
                .Where(c => c.Status != GeneralStatus.Eliminado)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
