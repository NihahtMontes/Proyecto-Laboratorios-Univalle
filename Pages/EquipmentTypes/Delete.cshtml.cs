using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentTypes
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
        public EquipmentType EquipmentType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipmentType = await _context.EquipmentTypes
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipmentType == null) return NotFound();
            
            EquipmentType = equipmentType;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipmentType = await _context.EquipmentTypes.FindAsync(id);
            if (equipmentType == null) return NotFound();

            // Perform Hard Delete (Admin privileged action)
            try
            {
                _context.EquipmentTypes.Remove(equipmentType);
                await _context.SaveChangesAsync();
                TempData.Success($"La categoría '{equipmentType.Name}' ha sido eliminada permanentemente del sistema.");
            }
            catch (DbUpdateException)
            {
                // Most likely due to foreign key constraint (equipments linked to this type)
                TempData.Error("No se pudo eliminar la categoría porque existen equipos vinculados a ella. Considere editarla o reasignar los equipos primero.");
                return RedirectToPage("./Details", new { id = id });
            }

            return RedirectToPage("./Index");
        }
    }
}
