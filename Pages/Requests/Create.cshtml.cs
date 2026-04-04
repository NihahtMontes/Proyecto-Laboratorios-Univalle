using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementContext;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, 
            UserManager<User> userManager,
            IManagementContextService managementContext)
        {
            _context = context;
            _userManager = userManager;
            _managementContext = managementContext;
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false)
        {
            await LoadLists();

            if (equipmentUnitId.HasValue)
            {
                var unit = await _context.EquipmentUnits
                    .Include(u => u.Laboratory)
                    .FirstOrDefaultAsync(u => u.Id == equipmentUnitId.Value);

                if (unit != null)
                {
                    Input.EquipmentUnitId = unit.Id;
                    Input.LaboratoryId = unit.LaboratoryId ?? 0;
                    Input.FacultyId = unit.Laboratory?.FacultyId ?? 0;

                    // Forzar carga de listas para que el Select2 muestre los valores
                    ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == Input.FacultyId).ToListAsync(), "Id", "Name", Input.LaboratoryId);
                    ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == Input.LaboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", Input.EquipmentUnitId);
                }
            }

            if (isWizard)
            {
                Input.Observations = @"1. Desconexión del cable de la alimentación eléctrica para mantenimiento preventivo/correctivo 12 horas antes.
2. Limpieza y desinfección interna con productos no abrasivos.
3. Limpieza externa de condensador, serpentín, evaporador y retiro de polvo y grasas adheridas.
4. Verificación de presión del refrigerante.
5. Revisión de fugas y/o microfugas en serpentín.
6. Revisión de formaciones de hielo y condensaciones superficiales no esporádicas.
7. Control de temperatura y termostatos según norma.
8. Revisión de puertas y sellos de goma (empaques).
9. Limpieza de drenajes de deshielo.
10. Verificación del funcionamiento de ventiladores.
11. Mantenimiento eléctrico: inspección de cableado, terminales, protecciones eléctricas, etc.
12. Lubricación de partes móviles.
13. Mantenimiento con personal externo capacitado.";
            }

            ViewData["IsWizard"] = isWizard;
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        public class InputModel
        {
            public RequestType Type { get; set; } = RequestType.Technical;
            [Required(ErrorMessage = "La facultad es obligatoria")]
            public int FacultyId { get; set; }
            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            public int LaboratoryId { get; set; }
            [Required(ErrorMessage = "La unidad física es obligatoria")]
            public int EquipmentUnitId { get; set; }
            [Required(ErrorMessage = "La descripción del fallo es obligatoria")]
            public string Description { get; set; } = string.Empty;
            public string? Observations { get; set; }
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;
            public string? EstimatedRepairTime { get; set; }
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            if (!ModelState.IsValid)
            {
                await LoadLists();
                return Page();
            }

            var unit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
            var currentMgmt = await _managementContext.GetCurrentManagementAsync();
            if (currentMgmt == null)
            {
                TempData["Warning"] = "No se ha detectado una gestión activa. Debe activar un periodo de gestión o aplicar las migraciones de base de datos para poder registrar solicitudes.";
                await LoadLists();
                return Page();
            }

            var request = new Request
            {
                Type = RequestType.Technical,
                ManagementId = currentMgmt.Id,
                LaboratoryId = Input.LaboratoryId,
                EquipmentId = unit?.EquipmentId ?? 0,
                EquipmentUnitId = Input.EquipmentUnitId,
                Description = Input.Description.Clean()!,
                Priority = Input.Priority,
                Observations = Input.Observations?.Clean(),
                EstimatedRepairTime = Input.EstimatedRepairTime?.Clean(),
                Status = RequestStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null) { request.CreatedById = currentUser.Id; request.RequestedById = currentUser.Id; }

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans.FindAsync(ManagementPlanId.Value);
                if (plan != null)
                {
                    plan.RequestId = request.Id;
                    plan.CurrentPhase = WizardPhase.Maintenance;
                    plan.CurrentState = WizardEquipmentState.AwaitingMaintenance;
                    await _context.SaveChangesAsync();
                }
            }

            TempData.Success($"Solicitud técnica L-7 registrada exitosamente.");

            if (isWizard)
            {
                // Al ser Wizard, el sistema entiende que ya se cumplió el paso de Solicitud (Paso 2)
                return RedirectToPage("/Wizard/Index", new { Step = 3, SelectedLabId = Input.LaboratoryId });
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name).ToListAsync(), "Id", "Name");
            if (Input.FacultyId == 0) ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            if (Input.LaboratoryId == 0) ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }

        // Handlers para AJAX (Asegúrate de que existan en tu controlador o aquí)
        public async Task<JsonResult> OnGetLaboratoriesByFaculty(int facultyId)
        {
            var labs = await _context.Laboratories.Where(l => l.FacultyId == facultyId).Select(l => new { id = l.Id, name = l.Name }).ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLab(int laboratoryId)
        {
            var units = await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == laboratoryId).Select(u => new { id = u.Id, name = u.Equipment.Name + " (" + u.InventoryNumber + ")" }).ToListAsync();
            return new JsonResult(units);
        }
    }
}