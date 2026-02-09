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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EquipmentUnit EquipmentUnit { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? equipmentId)
        {
            if (equipmentId.HasValue)
            {
                var equipment = await _context.Equipments.FindAsync(equipmentId);
                if (equipment != null)
                {
                    EquipmentUnit = new EquipmentUnit { EquipmentId = equipmentId.Value };
                }
            }

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

            // Clean data
            EquipmentUnit.InventoryNumber = EquipmentUnit.InventoryNumber.Clean();
            EquipmentUnit.SerialNumber = EquipmentUnit.SerialNumber?.Clean();
            EquipmentUnit.Notes = EquipmentUnit.Notes?.Clean();

            // Validar que el número de inventario sea único
            var existing = await _context.EquipmentUnits
                .AnyAsync(u => u.InventoryNumber == EquipmentUnit.InventoryNumber && u.CurrentStatus != EquipmentStatus.Deleted);
            
            if (existing)
            {
                ModelState.AddModelError("EquipmentUnit.InventoryNumber", "Este número de inventario ya está registrado.");
                LoadLists();
                return Page();
            }

            _context.EquipmentUnits.Add(EquipmentUnit);
            await _context.SaveChangesAsync();

            TempData.Success("Unidad física registrada correctamente.");
            return RedirectToPage("/Equipment/Details", new { id = EquipmentUnit.EquipmentId });
        }

        private void LoadLists()
        {
            ViewData["EquipmentId"] = new SelectList(_context.Equipments.OrderBy(e => e.Name), "Id", "Name");
            
            var labs = _context.Laboratories
                .Include(l => l.Faculty)
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Faculty.Name)
                .ThenBy(l => l.Name);
            ViewData["LaboratoryId"] = new SelectList(labs, "Id", "Name", null, "Faculty.Name");
        }
    }
}
