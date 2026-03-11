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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            [Display(Name = "Facultad")]
            public int? FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int? LaboratoryId { get; set; }

            [Required(ErrorMessage = "El modelo de equipo es obligatorio")]
            [Display(Name = "Catálogo / Modelo")]
            public int? EquipmentId { get; set; }

            [Required(ErrorMessage = "La carrera es obligatoria")]
            [Display(Name = "Carrera Propietaria")]
            public int? CareerId { get; set; }

            [Required(ErrorMessage = "El número de inventario es obligatorio")]
            [Display(Name = "Número de Inventario")]
            public string InventoryNumber { get; set; } = string.Empty;

            [Display(Name = "Número de Serie")]
            public string? SerialNumber { get; set; }

            [Display(Name = "Notas / Observaciones")]
            public string? Notes { get; set; }

            [Display(Name = "Estado Operativo")]
            public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Operational;

            [Display(Name = "Condición Física")]
            public PhysicalCondition PhysicalCondition { get; set; } = PhysicalCondition.New;

            [DataType(DataType.Date)]
            [Display(Name = "Fecha de Adquisición")]
            public DateTime? AcquisitionDate { get; set; } = DateTime.Today;

            [DataType(DataType.Date)]
            [Display(Name = "Fecha de Fabricación")]
            public DateTime? ManufacturingDate { get; set; }

            [Display(Name = "Precio de Adquisición")]
            public decimal? AcquisitionValue { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentId)
        {
            if (equipmentId.HasValue)
            {
                Input.EquipmentId = equipmentId.Value;
            }

            LoadLists();
            return Page();
        }

        // AJAX Handler
        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .OrderBy(x => x.name)
                .ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

            // Validar que el número de inventario sea único
            var existing = await _context.EquipmentUnits
                .AnyAsync(u => u.InventoryNumber == Input.InventoryNumber && u.CurrentStatus != EquipmentStatus.Deleted);
            
            if (existing)
            {
                ModelState.AddModelError("Input.InventoryNumber", "Este número de inventario ya está registrado.");
                LoadLists();
                return Page();
            }

            var unit = new EquipmentUnit
            {
                EquipmentId = Input.EquipmentId!.Value,
                LaboratoryId = Input.LaboratoryId,
                CareerId = Input.CareerId,
                InventoryNumber = Input.InventoryNumber.Clean()!,
                SerialNumber = Input.SerialNumber?.Clean(),
                Notes = Input.Notes?.Clean(),
                CurrentStatus = Input.CurrentStatus,
                PhysicalCondition = Input.PhysicalCondition,
                AcquisitionDate = Input.AcquisitionDate,
                ManufacturingDate = Input.ManufacturingDate,
                AcquisitionValue = Input.AcquisitionValue
            };

            _context.EquipmentUnits.Add(unit);
            await _context.SaveChangesAsync();

            // Guardar primer registro del historial de estado
            var initialHistory = new EquipmentStateHistory
            {
                EquipmentUnitId = unit.Id,
                Status = unit.CurrentStatus,
                StartDate = DateTime.UtcNow,
                Reason = $"Alta inicial en sistema. Condición: {unit.PhysicalCondition}"
            };
            _context.EquipmentStateHistories.Add(initialHistory);
            await _context.SaveChangesAsync();

            TempData.Success("Unidad física registrada correctamente.");
            return RedirectToPage("/Equipment/Details", new { id = unit.EquipmentId });
        }

        private void LoadLists()
        {
            ViewData["EquipmentId"] = new SelectList(_context.Equipments.OrderBy(e => e.Name), "Id", "Name", Input.EquipmentId);
            
            ViewData["FacultyId"] = new SelectList(_context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name), "Id", "Name", Input.FacultyId);
            
            // Si hay una facultad seleccionada (ej. tras un error de validación), cargamos sus laboratorios
            if (Input.FacultyId.HasValue)
            {
                var labs = _context.Laboratories
                    .Where(l => l.FacultyId == Input.FacultyId.Value && l.Status == GeneralStatus.Activo)
                    .OrderBy(l => l.Name)
                    .ToList();
                ViewData["LaboratoryId"] = new SelectList(labs, "Id", "Name", Input.LaboratoryId);
            }
            else
            {
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }
            
            ViewData["CareerId"] = new SelectList(_context.Careers.OrderBy(c => c.Name), "Id", "Name", Input.CareerId);
        }
    }
}
