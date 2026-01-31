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
            if (id == null)
            {
                return NotFound();
            }

            var maintenancetype = await _context.MaintenanceTypes
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenancetype == null)
            {
                return NotFound();
            }
            else
            {
                MaintenanceType = maintenancetype;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenancetype = await _context.MaintenanceTypes
                .Include(mt => mt.Maintenances)
                .FirstOrDefaultAsync(mt => mt.Id == id);

            if (maintenancetype != null)
            {
                if (maintenancetype.Maintenances.Any())
                {
                    TempData.Error($"No se puede eliminar '{maintenancetype.Name}' porque tiene {maintenancetype.Maintenances.Count} mantenimientos asociados.");
                    return RedirectToPage("/Maintenances/Index", null, "tipos");
                }

                MaintenanceType = maintenancetype;
                _context.MaintenanceTypes.Remove(MaintenanceType);
                await _context.SaveChangesAsync();
                TempData.Success($"Tipo de mantenimiento '{maintenancetype.Name}' eliminado correctamente.");
            }

            return RedirectToPage("/Maintenances/Index", null, "tipos");
        }
    }
}
