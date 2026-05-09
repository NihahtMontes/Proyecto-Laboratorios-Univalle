using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DeleteModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Maintenance Maintenance { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.Maintenances
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenance == null)
            {
                return NotFound();
            }
            else
            {
                Maintenance = maintenance;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.Maintenances
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (maintenance != null)
            {
                var equipmentName = maintenance.EquipmentUnit?.Equipment?.Name;
                
                // Soft delete: cancelar el mantenimiento en lugar de eliminar físicamente
                maintenance.Status = Models.Enums.MaintenanceStatus.Cancelled;
                await _context.SaveChangesAsync();
                
                TempData.Success(NotificationHelper.Maintenances.Deleted(equipmentName));
            }

            return RedirectToPage("./Index");
        }
    }
}
