using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<User> _userManager;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public MaintenanceType MaintenanceType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var maintenancetype = await _context.MaintenanceTypes.FirstOrDefaultAsync(m => m.Id == id);
            
            if (maintenancetype == null) return NotFound();
            
            MaintenanceType = maintenancetype;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var typeToUpdate = await _context.MaintenanceTypes.FindAsync(MaintenanceType.Id);
            if (typeToUpdate == null) return NotFound();

            // Mapping and Normalization
            typeToUpdate.Name = MaintenanceType.Name.Clean();
            typeToUpdate.Description = MaintenanceType.Description?.Clean();
            typeToUpdate.LastModifiedDate = DateTime.Now;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                typeToUpdate.ModifiedById = currentUser.Id;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Categoría técnica '{typeToUpdate.Name}' actualizada correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MaintenanceTypeExists(MaintenanceType.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private bool MaintenanceTypeExists(int id)
        {
            return _context.MaintenanceTypes.Any(e => e.Id == id);
        }
    }
}
