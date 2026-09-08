using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

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
        public InputModel EquipmentUnit { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }
            public int? CreatedById { get; set; }
            public DateTime CreatedDate { get; set; }
            public int? ManagementId { get; set; }

            [Required(ErrorMessage = "El modelo de equipo es obligatorio")]
            [Display(Name = "Catálogo / Modelo")]
            public int? EquipmentId { get; set; }

            [Display(Name = "Laboratorio Asignado")]
            public int? LaboratoryId { get; set; }

            [Display(Name = "Carrera Propietaria")]
            public int? CareerId { get; set; }

            [Required(ErrorMessage = "El número de inventario es obligatorio")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "El inventario debe tener al menos 3 caracteres")]
            [Display(Name = "Número de Inventario")]
            public string InventoryNumber { get; set; } = string.Empty;

            [StringLength(100)]
            [Display(Name = "Número de Serie")]
            public string? SerialNumber { get; set; }

            [StringLength(2000)]
            [Display(Name = "Notas / Observaciones")]
            public string? Notes { get; set; }

            [Display(Name = "Estado Operativo")]
            public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Operational;

            [Display(Name = "Condición Física")]
            public PhysicalCondition? PhysicalCondition { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Fecha de Adquisición")]
            public DateTime? AcquisitionDate { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Fecha de Fabricación")]
            public DateTime? ManufacturingDate { get; set; }

            [Display(Name = "Precio de Adquisición")]
            public decimal? AcquisitionValue { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipmentunit = await _context.EquipmentUnits.FirstOrDefaultAsync(m => m.Id == id);
            if (equipmentunit == null) return NotFound();

            EquipmentUnit = new InputModel
            {
                Id = equipmentunit.Id,
                CreatedById = equipmentunit.CreatedById,
                CreatedDate = equipmentunit.CreatedDate,
                ManagementId = equipmentunit.ManagementId,
                EquipmentId = equipmentunit.EquipmentId,
                LaboratoryId = equipmentunit.LaboratoryId,
                CareerId = equipmentunit.CareerId,
                InventoryNumber = equipmentunit.InventoryNumber,
                SerialNumber = equipmentunit.SerialNumber,
                Notes = equipmentunit.Notes,
                CurrentStatus = equipmentunit.CurrentStatus,
                PhysicalCondition = equipmentunit.PhysicalCondition,
                AcquisitionDate = equipmentunit.AcquisitionDate,
                ManufacturingDate = equipmentunit.ManufacturingDate,
                AcquisitionValue = equipmentunit.AcquisitionValue
            };

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

            EquipmentUnit.InventoryNumber = EquipmentUnit.InventoryNumber.Clean();
            EquipmentUnit.SerialNumber = EquipmentUnit.SerialNumber?.Clean();
            EquipmentUnit.Notes = EquipmentUnit.Notes?.Clean();

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

            var dbUnit = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .AsTracking()
                .FirstOrDefaultAsync(u => u.Id == EquipmentUnit.Id);
            if (dbUnit == null) return NotFound();

            bool stateChanged = false;
            string stateChangeMessage = "";

            if (dbUnit.CurrentStatus != EquipmentUnit.CurrentStatus || dbUnit.PhysicalCondition != EquipmentUnit.PhysicalCondition)
            {
                stateChanged = true;
                var previousCondition = dbUnit.PhysicalCondition?.ToString() ?? "Sin confirmar";
                var newCondition = EquipmentUnit.PhysicalCondition?.ToString() ?? "Sin confirmar";
                stateChangeMessage = $"Estado: {dbUnit.CurrentStatus} -> {EquipmentUnit.CurrentStatus}. Físico: {previousCondition} -> {newCondition}";

                var lastHistory = await _context.EquipmentStateHistories
                    .Where(h => h.EquipmentUnitId == dbUnit.Id && h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .AsTracking()
                    .FirstOrDefaultAsync();

                if (lastHistory != null)
                {
                    lastHistory.EndDate = DateTime.UtcNow;
                    _context.EquipmentStateHistories.Update(lastHistory);
                }

                var newHistory = new EquipmentStateHistory
                {
                    EquipmentUnitId = dbUnit.Id,
                    Status = EquipmentUnit.CurrentStatus,
                    StartDate = DateTime.UtcNow,
                    Reason = "Actualización manual de estado/condición. " + stateChangeMessage
                };
                _context.EquipmentStateHistories.Add(newHistory);
            }

            dbUnit.EquipmentId = EquipmentUnit.EquipmentId!.Value;
            dbUnit.LaboratoryId = EquipmentUnit.LaboratoryId;
            dbUnit.LocationResolutionStatus = EquipmentUnit.LaboratoryId.HasValue
                ? LocationResolutionStatus.Confirmed
                : LocationResolutionStatus.Pending;
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

            var equipmentName = dbUnit.Equipment?.Name ?? "Equipo";

            if (stateChanged)
            {
                TempData.Success($"Unidad {equipmentName} ({dbUnit.InventoryNumber}): cambios guardados. {stateChangeMessage}");
            }
            else
            {
                TempData.Success($"Unidad {equipmentName} ({dbUnit.InventoryNumber}): cambios guardados sin alteración de estados clave.");
            }

            return RedirectToPage("/Equipment/Details", new { id = dbUnit.EquipmentId });
        }

        private void LoadLists()
        {
            ViewData["EquipmentId"] = new SelectList(_context.Equipments.OrderBy(e => e.Name), "Id", "Name", EquipmentUnit.EquipmentId);

            var labs = _context.Laboratories
                .Include(l => l.Faculty)
                .Where(l => l.Status == GeneralStatus.Activo || l.Id == EquipmentUnit.LaboratoryId)
                .OrderBy(l => l.Faculty != null ? l.Faculty.Name : string.Empty)
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
