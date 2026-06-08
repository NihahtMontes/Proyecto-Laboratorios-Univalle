using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.AssetView
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class EnvironmentUnitsModel : PageModel
    {
        private const int PageSize = 20;
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public EnvironmentUnitsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int LaboratoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        public string LaboratoryDisplayName { get; set; } = string.Empty;
        public PaginatedList<EquipmentUnit> Units { get; set; } = new(new List<EquipmentUnit>(), 0, 1, PageSize);

        public async Task<IActionResult> OnGetAsync()
        {
            var laboratory = await _context.Laboratories
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == LaboratoryId);

            if (laboratory == null)
            {
                TempData.Warning("El ambiente solicitado ya no esta disponible.");
                return RedirectToPage("./Environments");
            }

            LaboratoryDisplayName = LaboratoryDisplayHelper.Format(laboratory);
            await LoadUnitsAsync(PageIndex ?? 1);
            return Page();
        }

        private async Task LoadUnitsAsync(int pageIndex)
        {
            var query = _context.EquipmentUnits
                .AsNoTracking()
                .Include(u => u.Equipment)
                .Include(u => u.Laboratory)
                .Where(u => u.LaboratoryId == LaboratoryId && u.Equipment != null);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.Equipment!.Name.ToLower().Contains(term) ||
                    u.InventoryNumber.ToLower().Contains(term) ||
                    (u.SerialNumber != null && u.SerialNumber.ToLower().Contains(term)));
            }

            Units = await PaginatedList<EquipmentUnit>.CreateAsync(
                query.OrderBy(u => u.Equipment!.Name).ThenBy(u => u.InventoryNumber),
                pageIndex,
                PageSize);
        }
    }
}
