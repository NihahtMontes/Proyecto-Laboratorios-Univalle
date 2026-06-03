using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.AssetView
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class UnitsModel : PageModel
    {
        private const int PageSize = 20;
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public UnitsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public EquipmentTypeClassification Classification { get; set; } = EquipmentTypeClassification.Otro;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        public string ClassificationName => EnumHelper.GetDisplayName(Classification);

        public PaginatedList<EquipmentUnit> Units { get; set; } = new(new List<EquipmentUnit>(), 0, 1, PageSize);

        public async Task OnGetAsync()
        {
            await LoadUnitsAsync(PageIndex ?? 1);
        }

        public async Task<IActionResult> OnPostDeleteUnitAsync(int id)
        {
            var unit = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .AsTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null)
            {
                TempData.Warning("La unidad fisica ya no esta disponible.");
                return RedirectToPage(new { classification = Classification, searchTerm = SearchTerm, pageIndex = PageIndex });
            }

            unit.CurrentStatus = EquipmentStatus.Deleted;
            unit.LastModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData.Success($"Unidad {unit.Equipment?.Name ?? "Equipo"} ({unit.InventoryNumber}) dada de baja correctamente.");
            return RedirectToPage(new { classification = Classification, searchTerm = SearchTerm, pageIndex = PageIndex });
        }

        private async Task LoadUnitsAsync(int pageIndex)
        {
            var query = _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Include(u => u.Laboratory)
                .Where(u => u.Equipment != null &&
                            u.Equipment.Category == EquipmentCategory.Equipment &&
                            u.Equipment.TypeClassification == Classification);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.Equipment!.Name.ToLower().Contains(term) ||
                    (u.SerialNumber != null && u.SerialNumber.ToLower().Contains(term)));
            }

            Units = await PaginatedList<EquipmentUnit>.CreateAsync(
                query.OrderBy(u => u.Equipment!.Name).ThenBy(u => u.InventoryNumber),
                pageIndex,
                PageSize);
        }
    }
}
