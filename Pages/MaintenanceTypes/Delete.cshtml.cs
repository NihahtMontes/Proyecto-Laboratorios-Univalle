using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.MaintenanceTypes
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
        public MaintenanceType MaintenanceType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var maintenancetype = await _context.MaintenanceTypes
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenancetype == null) return NotFound();
            
            MaintenanceType = maintenancetype;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var maintenancetype = await _context.MaintenanceTypes
                .Include(mt => mt.Maintenances)
                .FirstOrDefaultAsync(mt => mt.Id == id);

            if (maintenancetype == null) return NotFound();

            // Dependency Validation
            if (maintenancetype.Maintenances != null && maintenancetype.Maintenances.Any())
            {
                TempData.Error($"No se puede eliminar la categoría '{maintenancetype.Name}' porque tiene {maintenancetype.Maintenances.Count} registros de mantenimiento asociados. Considere editarla en su lugar.");
                return RedirectToPage("./Details", new { id = maintenancetype.Id });
            }

            // Perform Hard Delete (Admin action)
            try
            {
                _context.MaintenanceTypes.Remove(maintenancetype);
                await _context.SaveChangesAsync();
                TempData.Success($"Categoría técnica '{maintenancetype.Name}' eliminada permanentemente del sistema.");
            }
            catch (Exception ex)
            {
                TempData.Error("Hubo un error al intentar eliminar el registro: " + ex.Message);
                return RedirectToPage("./Details", new { id = maintenancetype.Id });
            }

            return RedirectToPage("./Index");
        }
    }
}
