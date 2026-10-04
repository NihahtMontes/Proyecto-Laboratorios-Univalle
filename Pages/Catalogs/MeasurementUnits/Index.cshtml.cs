using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.MeasurementUnits
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<MeasurementUnit> Units { get; set; } = new PaginatedList<MeasurementUnit>(new List<MeasurementUnit>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            // MeasurementUnit no tiene query filter global; excluimos eliminados manualmente.
            var query = _context.MeasurementUnits
                .AsNoTracking()
                .Where(u => u.Status != GeneralStatus.Eliminado);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(u => u.Name.ToLower().Contains(term) || u.Code.ToLower().Contains(term));
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(u => u.Status == StatusFilter.Value);
            }

            Units = await PaginatedList<MeasurementUnit>.CreateAsync(query.OrderBy(u => u.Name), pageIndex ?? 1, 20);
        }
    }
}