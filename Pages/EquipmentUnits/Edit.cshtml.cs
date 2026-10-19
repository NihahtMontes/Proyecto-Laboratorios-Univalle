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
            [MinLength(3, ErrorMessage = "El inventario debe tener al menos 3 caracteres")]
            [StringLength(50, ErrorMessage = "El inventario no puede superar 50 caracteres")]
            [Display(Name = "Número de Inventario")]
            public string InventoryNumber { get; set; } = string.Empty;

            [StringLength(100, ErrorMessage = "El número de serie no puede superar 100 caracteres")]
            [Display(Name = "Número de Serie")]
            public string? SerialNumber { get; set; }

            [StringLength(2000, ErrorMessage = "Las observaciones no pueden superar 2000 caracteres")]
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

            [Range(0d, 999999999d, ErrorMessage = "El precio de adquisición debe estar entre 0 y 999.999.999.")]
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

            // Tras colapsar espacios internos el inventario puede quedar bajo el mínimo.
            if (EquipmentUnit.InventoryNumber.Length < 3)
            {
                ModelState.AddModelError("EquipmentUnit.InventoryNumber", "El inventario debe tener al menos 3 caracteres");
            }

            // ── Validaciones de integridad referencial (evitan error 500 por FK inexistente) ──
            if (!await _context.Equipments.AnyAsync(e => e.Id == EquipmentUnit.EquipmentId))
            {
                ModelState.AddModelError("EquipmentUnit.EquipmentId", "El modelo de equipo seleccionado no existe.");
            }

            if (EquipmentUnit.LaboratoryId.HasValue
                && !await _context.Laboratories.AnyAsync(l => l.Id == EquipmentUnit.LaboratoryId))
            {
                ModelState.AddModelError("EquipmentUnit.LaboratoryId", "El laboratorio seleccionado no existe.");
            }

            if (EquipmentUnit.CareerId.HasValue
                && !await _context.Careers.AnyAsync(c => c.Id == EquipmentUnit.CareerId))
            {
                ModelState.AddModelError("EquipmentUnit.CareerId", "La carrera seleccionada no existe.");
            }

            ValidateUnitDates(EquipmentUnit.AcquisitionDate, EquipmentUnit.ManufacturingDate, "EquipmentUnit");

            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

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

            // Estados que no se asignan a mano desde este formulario.
            if (dbUnit.CurrentStatus != EquipmentUnit.CurrentStatus)
            {
                if (EquipmentUnit.CurrentStatus == EquipmentStatus.OnLoan)
                {
                    ModelState.AddModelError("EquipmentUnit.CurrentStatus", "El estado \"En Préstamo\" se asigna automáticamente al registrar una salida (L-3); no puede establecerse manualmente.");
                }
                else if (EquipmentUnit.CurrentStatus == EquipmentStatus.Deleted)
                {
                    ModelState.AddModelError("EquipmentUnit.CurrentStatus", "El estado \"Eliminado\" solo se gestiona con la opción \"Dar de baja\" de la unidad.");
                }
                else if (EquipmentUnit.CurrentStatus == EquipmentStatus.PorConfirmar && dbUnit.CurrentStatus != EquipmentStatus.PorConfirmar)
                {
                    ModelState.AddModelError("EquipmentUnit.CurrentStatus", "El estado \"Por confirmar\" corresponde a datos históricos importados y no puede asignarse manualmente.");
                }

                if (!ModelState.IsValid)
                {
                    LoadLists();
                    return Page();
                }
            }

            if (dbUnit.CurrentStatus != EquipmentUnit.CurrentStatus || dbUnit.PhysicalCondition != EquipmentUnit.PhysicalCondition)
            {
                stateChanged = true;
                var previousCondition = dbUnit.PhysicalCondition.HasValue
                    ? EnumHelper.GetDisplayName(dbUnit.PhysicalCondition.Value)
                    : "Sin confirmar";
                var newCondition = EquipmentUnit.PhysicalCondition.HasValue
                    ? EnumHelper.GetDisplayName(EquipmentUnit.PhysicalCondition.Value)
                    : "Sin confirmar";
                stateChangeMessage = $"Estado: {EnumHelper.GetDisplayName(dbUnit.CurrentStatus)} -> {EnumHelper.GetDisplayName(EquipmentUnit.CurrentStatus)}. Físico: {previousCondition} -> {newCondition}";

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
                    Reason = TruncateHistoryReason("Actualización manual de estado/condición. " + stateChangeMessage)
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

        /// <summary>Longitud máxima de Reason en EquipmentStateHistory (StringLength 500 en BD).</summary>
        private const int HistoryReasonMaxLength = 500;

        /// <summary>Trunca con seguridad el motivo del historial para no violar el máximo de BD.</summary>
        private static string TruncateHistoryReason(string reason) =>
            reason.Length <= HistoryReasonMaxLength ? reason : reason[..HistoryReasonMaxLength];

        /// <summary>
        /// Valida coherencia y rango de las fechas de la unidad: ninguna anterior a 1900-01-01
        /// ni posterior a hoy, y la fabricación no puede ser posterior a la adquisición.
        /// </summary>
        private void ValidateUnitDates(DateTime? acquisitionDate, DateTime? manufacturingDate, string prefix)
        {
            var today = DateTime.Today;
            var minDate = new DateTime(1900, 1, 1);

            if (acquisitionDate.HasValue)
            {
                var date = acquisitionDate.Value.Date;
                if (date < minDate || date > today)
                {
                    ModelState.AddModelError($"{prefix}.AcquisitionDate", "La fecha de adquisición debe estar entre 01/01/1900 y la fecha actual.");
                }
            }

            if (manufacturingDate.HasValue)
            {
                var date = manufacturingDate.Value.Date;
                if (date < minDate || date > today)
                {
                    ModelState.AddModelError($"{prefix}.ManufacturingDate", "La fecha de fabricación debe estar entre 01/01/1900 y la fecha actual.");
                }
            }

            if (acquisitionDate.HasValue && manufacturingDate.HasValue
                && manufacturingDate.Value.Date > acquisitionDate.Value.Date)
            {
                ModelState.AddModelError($"{prefix}.ManufacturingDate", "La fecha de fabricación no puede ser posterior a la fecha de adquisición.");
            }
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
