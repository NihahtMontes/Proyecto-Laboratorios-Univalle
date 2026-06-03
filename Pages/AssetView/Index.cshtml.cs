using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.AssetView
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public int EquipmentUnitsCount { get; set; }
        public int UtensilUnitsCount { get; set; }
        public int OtherUnitsCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ActiveTab { get; set; }

        public bool ShowInventoryTab => string.Equals(ActiveTab, "Inventario", StringComparison.OrdinalIgnoreCase);

        public async Task OnGetAsync()
        {
            var units = _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.Equipment != null);

            EquipmentUnitsCount = await units.CountAsync(u => u.Equipment!.Category == EquipmentCategory.Equipment);
            UtensilUnitsCount = await units.CountAsync(u => u.Equipment!.Category == EquipmentCategory.Utensil);
            OtherUnitsCount = await units.CountAsync(u => u.Equipment!.Category == EquipmentCategory.Other);
        }
    }
}
