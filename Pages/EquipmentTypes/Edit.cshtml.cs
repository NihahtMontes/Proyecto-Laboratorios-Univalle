using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentTypes
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
        public EquipmentType EquipmentType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipmenttype = await _context.EquipmentTypes.FirstOrDefaultAsync(m => m.Id == id);
            
            if (equipmenttype == null) return NotFound();
            
            EquipmentType = equipmenttype;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var typeToUpdate = await _context.EquipmentTypes.FindAsync(EquipmentType.Id);
            if (typeToUpdate == null) return NotFound();

            // Mapping and Normalization
            typeToUpdate.Name = EquipmentType.Name.Clean();
            typeToUpdate.Description = EquipmentType.Description?.Clean();
            typeToUpdate.RequiresCalibration = EquipmentType.RequiresCalibration;
            typeToUpdate.MaintenanceFrequencyMonths = EquipmentType.MaintenanceFrequencyMonths;
            typeToUpdate.LastModifiedDate = DateTime.UtcNow;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                typeToUpdate.ModifiedById = currentUser.Id;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Categoría '{typeToUpdate.Name}' actualizada correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipmentTypeExists(EquipmentType.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private bool EquipmentTypeExists(int id)
        {
            return _context.EquipmentTypes.Any(e => e.Id == id);
        }
    }
}
