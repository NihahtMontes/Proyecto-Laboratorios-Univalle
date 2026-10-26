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
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentUnits
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IManagementContextService _managementService;
        private readonly IEquipmentStateHistoryService _stateHistory;

        public CreateModel(ApplicationDbContext context, IManagementContextService managementService, IEquipmentStateHistoryService stateHistory)
        {
            _context = context;
            _managementService = managementService;
            _stateHistory = stateHistory;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        /// <summary>
        /// Pantalla de origen (p. ej. el detalle del conflicto de material): Guardar y Cancelar vuelven ahí.
        /// Solo se aceptan rutas locales.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        /// <summary>ReturnUrl validado (local) o null si no se indicó o no es seguro.</summary>
        public string? SafeReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

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
            public PhysicalCondition PhysicalCondition { get; set; } = PhysicalCondition.Excellent;

            [DataType(DataType.Date)]
            [Display(Name = "Fecha de Adquisición")]
            public DateTime? AcquisitionDate { get; set; } = DateTime.Today;

            [DataType(DataType.Date)]
            [Display(Name = "Fecha de Fabricación")]
            public DateTime? ManufacturingDate { get; set; }

            [Range(0d, 999999999d, ErrorMessage = "El precio de adquisición debe estar entre 0 y 999.999.999.")]
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

            // Normalizar ANTES de comprobar duplicados (mismo criterio que Edit):
            // la comparación y el guardado usan el valor limpio.
            Input.InventoryNumber = Input.InventoryNumber.Clean();
            Input.SerialNumber = Input.SerialNumber?.Clean();
            Input.Notes = Input.Notes?.Clean();

            // Tras colapsar espacios internos el inventario puede quedar bajo el mínimo.
            if (Input.InventoryNumber.Length < 3)
            {
                ModelState.AddModelError("Input.InventoryNumber", "El inventario debe tener al menos 3 caracteres");
            }

            // ── Validaciones de integridad referencial (evitan error 500 por FK inexistente) ──
            if (!await _context.Equipments.AnyAsync(e => e.Id == Input.EquipmentId))
            {
                ModelState.AddModelError("Input.EquipmentId", "El modelo de equipo seleccionado no existe.");
            }

            var laboratoryExists = await _context.Laboratories.AnyAsync(l => l.Id == Input.LaboratoryId);
            if (!laboratoryExists)
            {
                ModelState.AddModelError("Input.LaboratoryId", "El laboratorio seleccionado no existe.");
            }
            else if (!await _context.Laboratories.AnyAsync(l => l.Id == Input.LaboratoryId && l.FacultyId == Input.FacultyId))
            {
                ModelState.AddModelError("Input.LaboratoryId", "El laboratorio seleccionado no pertenece a la facultad indicada.");
            }

            if (Input.CareerId.HasValue && !await _context.Careers.AnyAsync(c => c.Id == Input.CareerId))
            {
                ModelState.AddModelError("Input.CareerId", "La carrera seleccionada no existe.");
            }

            ValidateUnitDates(Input.AcquisitionDate, Input.ManufacturingDate, "Input");

            // QA 2026-10-18: una unidad nueva no puede nacer «Eliminada» ni «En Préstamo» (sin salida L-3 que la respalde).
            if (Input.CurrentStatus == EquipmentStatus.Deleted)
                ModelState.AddModelError("Input.CurrentStatus", "No se puede registrar una unidad con estado «Eliminado».");
            else if (Input.CurrentStatus == EquipmentStatus.OnLoan)
                ModelState.AddModelError("Input.CurrentStatus", "Una unidad nueva no puede registrarse «En Préstamo»: el préstamo se registra con una salida de equipos.");

            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

            // Validar que el número de inventario sea único (ya normalizado)
            var existing = await _context.EquipmentUnits
                .AnyAsync(u => u.InventoryNumber == Input.InventoryNumber && u.CurrentStatus != EquipmentStatus.Deleted);

            if (existing)
            {
                ModelState.AddModelError("Input.InventoryNumber", "Este número de inventario ya está registrado.");
                LoadLists();
                return Page();
            }

            var currentMgmt = await _managementService.GetCurrentManagementAsync();
            if (currentMgmt == null)
            {
                TempData.Warning("No se ha detectado una gestión activa o el esquema de base de datos no está actualizado. Por favor, asegúrese de aplicar las migraciones o de activar un periodo de gestión para poder registrar activos.");
                LoadLists();
                return Page();
            }

            var unit = new EquipmentUnit
            {
                EquipmentId = Input.EquipmentId!.Value,
                // CORRECCIÓN: Se agrega .Value para convertir de int? a int, ya validado por [Required]
                LaboratoryId = Input.LaboratoryId!.Value,
                CareerId = Input.CareerId,
                InventoryNumber = Input.InventoryNumber,
                SerialNumber = Input.SerialNumber,
                Notes = Input.Notes,
                CurrentStatus = Input.CurrentStatus,
                PhysicalCondition = Input.PhysicalCondition,
                AcquisitionDate = Input.AcquisitionDate,
                ManufacturingDate = Input.ManufacturingDate,
                AcquisitionValue = Input.AcquisitionValue,
                ManagementId = currentMgmt.Id
            };

            _context.EquipmentUnits.Add(unit);
            await _context.SaveChangesAsync();

            // Guardar primer registro del historial de estado (texto visible del enum en español)
            // El tramo inicial empieza en el alta (CreatedDate de la unidad) y queda vigente.
            await _stateHistory.EnsureInitialAsync(unit, $"Alta inicial en sistema. Condición: {EnumHelper.GetDisplayName(unit.PhysicalCondition)}");
            await _context.SaveChangesAsync();

            var equipmentName = await _context.Equipments
                .Where(e => e.Id == unit.EquipmentId)
                .Select(e => e.Name)
                .FirstOrDefaultAsync() ?? "Equipo";

            TempData.Success($"Equipo {equipmentName} ({unit.InventoryNumber}) fue creado exitosamente.");
            if (SafeReturnUrl is string returnUrl)
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToPage("/Equipment/Details", new { id = unit.EquipmentId });
        }

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
