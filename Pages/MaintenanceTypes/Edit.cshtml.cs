using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.MaintenanceTypes
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
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

            var maintenancetype = await _context.MaintenanceTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (maintenancetype == null)
            {
                return NotFound();
            }
            MaintenanceType = maintenancetype;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(MaintenanceType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Tipo de mantenimiento '{MaintenanceType.Name}' actualizado correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MaintenanceTypeExists(MaintenanceType.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("/Maintenances/Index", null, "tipos");
        }

        private bool MaintenanceTypeExists(int id)
        {
            return _context.MaintenanceTypes.Any(e => e.Id == id);
        }
    }
}
