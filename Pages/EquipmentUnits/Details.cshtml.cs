using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentUnits
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public EquipmentUnit EquipmentUnit { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipmentunit = await _context.EquipmentUnits
                .Include(e => e.Equipment)
                    .ThenInclude(e => e!.City)
                .Include(e => e.Equipment)
                    .ThenInclude(e => e!.Country)
                .Include(e => e.Laboratory)
                    .ThenInclude(l => l!.Faculty)
                .Include(e => e.Career)
                .Include(e => e.Maintenances!)
                    .ThenInclude(m => m.Technician)
                .Include(e => e.Maintenances!)
                    .ThenInclude(m => m.MaintenanceType)
                .Include(e => e.Verifications!)
                .Include(e => e.StateHistory!)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipmentunit == null) return NotFound();
            
            EquipmentUnit = equipmentunit;
            return Page();
        }
    }
}
