using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentUnits
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EquipmentUnit EquipmentUnit { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipmentunit = await _context.EquipmentUnits.FirstOrDefaultAsync(m => m.Id == id);
            if (equipmentunit == null) return NotFound();

            EquipmentUnit = equipmentunit;
            LoadLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

            // Normalization
            EquipmentUnit.InventoryNumber = EquipmentUnit.InventoryNumber.Clean();
            EquipmentUnit.SerialNumber = EquipmentUnit.SerialNumber?.Clean();
            EquipmentUnit.Notes = EquipmentUnit.Notes?.Clean();

            // Validar que el número de inventario sea único (excluyendo el actual)
            var existing = await _context.EquipmentUnits
                .AnyAsync(u => u.InventoryNumber == EquipmentUnit.InventoryNumber 
                               && u.Id != EquipmentUnit.Id
                               && u.CurrentStatus != EquipmentStatus.Deleted);
            
            if (existing)
            {
                ModelState.AddModelError("EquipmentUnit.InventoryNumber", "Este número de inventario ya está registrado en otra unidad.");
                LoadLists();
                return Page();
            }

            _context.Attach(EquipmentUnit).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipmentUnitExists(EquipmentUnit.Id)) return NotFound();
                else throw;
            }

            TempData.Success("Cambios guardados correctamente.");
            return RedirectToPage("/Equipment/Details", new { id = EquipmentUnit.EquipmentId });
        }

        private void LoadLists()
        {
            ViewData["EquipmentId"] = new SelectList(_context.Equipments.OrderBy(e => e.Name), "Id", "Name", EquipmentUnit.EquipmentId);
            
            var labs = _context.Laboratories
                .Include(l => l.Faculty)
                .Where(l => l.Status == GeneralStatus.Activo || l.Id == EquipmentUnit.LaboratoryId)
                .OrderBy(l => l.Faculty.Name)
                .ThenBy(l => l.Name);
            ViewData["LaboratoryId"] = new SelectList(labs, "Id", "Name", EquipmentUnit.LaboratoryId, "Faculty.Name");
        }

        private bool EquipmentUnitExists(int id)
        {
            return _context.EquipmentUnits.Any(e => e.Id == id);
        }
    }
}
