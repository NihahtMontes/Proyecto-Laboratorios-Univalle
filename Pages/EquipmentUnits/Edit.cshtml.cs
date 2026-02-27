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

            var dbUnit = await _context.EquipmentUnits.FirstOrDefaultAsync(u => u.Id == EquipmentUnit.Id);
            if (dbUnit == null) return NotFound();

            bool stateChanged = false;
            string stateChangeMessage = "";

            if (dbUnit.CurrentStatus != EquipmentUnit.CurrentStatus || dbUnit.PhysicalCondition != EquipmentUnit.PhysicalCondition)
            {
                stateChanged = true;
                stateChangeMessage = $"Estado: {dbUnit.CurrentStatus} -> {EquipmentUnit.CurrentStatus}. Físico: {dbUnit.PhysicalCondition} -> {EquipmentUnit.PhysicalCondition}";

                // 1. Cerrar historial anterior (si existe y sigue abierto)
                var lastHistory = await _context.EquipmentStateHistories
                    .Where(h => h.EquipmentUnitId == dbUnit.Id && h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .FirstOrDefaultAsync();
                
                if (lastHistory != null)
                {
                    lastHistory.EndDate = DateTime.Now;
                    _context.EquipmentStateHistories.Update(lastHistory);
                }

                // 2. Crear nuevo registro de historial
                var newHistory = new EquipmentStateHistory
                {
                    EquipmentUnitId = dbUnit.Id,
                    Status = EquipmentUnit.CurrentStatus,
                    StartDate = DateTime.Now,
                    Reason = "Actualización manual de estado/condición. " + stateChangeMessage
                };
                _context.EquipmentStateHistories.Add(newHistory);
            }

            // Actualizar propiedades de dbUnit
            dbUnit.EquipmentId = EquipmentUnit.EquipmentId;
            dbUnit.LaboratoryId = EquipmentUnit.LaboratoryId;
            dbUnit.CareerId = EquipmentUnit.CareerId;
            dbUnit.InventoryNumber = EquipmentUnit.InventoryNumber;
            dbUnit.SerialNumber = EquipmentUnit.SerialNumber;
            dbUnit.Notes = EquipmentUnit.Notes;
            dbUnit.CurrentStatus = EquipmentUnit.CurrentStatus;
            dbUnit.PhysicalCondition = EquipmentUnit.PhysicalCondition;
            dbUnit.AcquisitionDate = EquipmentUnit.AcquisitionDate;
            dbUnit.ManufacturingDate = EquipmentUnit.ManufacturingDate;
            dbUnit.AcquisitionValue = EquipmentUnit.AcquisitionValue;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipmentUnitExists(dbUnit.Id)) return NotFound();
                else throw;
            }

            if (stateChanged)
            {
                TempData.Success($"Cambios guardados. Se registró el cambio: {stateChangeMessage}");
            }
            else
            {
                TempData.Success("Cambios guardados correctamente sin alteración de estados clave.");
            }
            
            return RedirectToPage("/Equipment/Details", new { id = dbUnit.EquipmentId });
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
            ViewData["CareerId"] = new SelectList(_context.Careers.OrderBy(c => c.Name), "Id", "Name", EquipmentUnit.CareerId);
        }

        private bool EquipmentUnitExists(int id)
        {
            return _context.EquipmentUnits.Any(e => e.Id == id);
        }
    }
}
