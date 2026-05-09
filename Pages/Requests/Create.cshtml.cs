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

            var currentMgmt = await ResolveManagementAsync();
            var isCorrective = currentMgmt?.Type == ManagementType.Corrective;
            ViewData["IsCorrective"] = isCorrective;
            ViewData["ManagementId"] = currentMgmt?.Id;

            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.Verification).ThenInclude(v => v!.Faults)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                ViewData["CurrentPhaseInt"] = (int)(plan?.CurrentPhase ?? WizardPhase.TechnicalRequest);

                if (plan?.Verification != null && !string.IsNullOrWhiteSpace(plan.Verification.Observations))
                {
                    Input.Description = plan.Verification.Observations;
                }

                // Exponer IDs de fases previas para la sección de referencia vinculada
                if (plan?.VerificationId != null)
                {
                    ViewData["LinkedVerificationId"] = plan.VerificationId;
                }
                
                if (!equipmentUnitId.HasValue && plan?.EquipmentUnitId != null)
                {
                    equipmentUnitId = plan.EquipmentUnitId;
                }
            }

            if (equipmentUnitId.HasValue)
            {
                var unit = await _context.EquipmentUnits
                    .Include(u => u.Laboratory)
                    .Include(u => u.Equipment).ThenInclude(e => e.Notes)
                    .FirstOrDefaultAsync(u => u.Id == equipmentUnitId.Value);

                if (unit != null)
                {
                    Input.EquipmentUnitId = unit.Id;
                    Input.LaboratoryId = unit.LaboratoryId ?? 0;
                    Input.FacultyId = unit.Laboratory?.FacultyId ?? 0;

                    ViewData["EquipmentNotes"] = unit.Equipment?.Notes?.Select(n => n.Note).ToList();
                    ViewData["CurrentEquipmentUnitId"] = unit.Id;

                    // Forzar carga de listas para que el Select2 muestre los valores
                    ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == Input.FacultyId).ToListAsync(), "Id", "Name", Input.LaboratoryId);
                    ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == Input.LaboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", Input.EquipmentUnitId);
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

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

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
            ManagementPlan? wizardPlan = null;
            Management? currentMgmt = null;

            if (ManagementPlanId.HasValue)
            {
                wizardPlan = await _context.ManagementPlans
                    .AsTracking()
                    .Include(p => p.Management)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Laboratory)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                if (wizardPlan == null)
                {
                    TempData.Error($"No se pudo resolver el plan del wizard para L-7. ManagementPlanId recibido: {ManagementPlanId.Value}.");
                    await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                    ViewData["IsWizard"] = isWizard;
                    return Page();
                }

                currentMgmt = wizardPlan.Management;

                if (wizardPlan.EquipmentUnitId.HasValue)
                {
                    Input.EquipmentUnitId = wizardPlan.EquipmentUnitId.Value;
                    Input.LaboratoryId = wizardPlan.EquipmentUnit?.LaboratoryId ?? Input.LaboratoryId;
                    Input.FacultyId = wizardPlan.EquipmentUnit?.Laboratory?.FacultyId ?? Input.FacultyId;
                }

                ModelState.Remove("Input.FacultyId");
                ModelState.Remove("Input.LaboratoryId");
                ModelState.Remove("Input.EquipmentUnitId");
            }

            currentMgmt ??= await ResolveManagementAsync();
            var isCorrective = currentMgmt?.Type == ManagementType.Corrective;

            ViewData["IsWizard"] = isWizard;
            ViewData["IsCorrective"] = isCorrective;
            ViewData["ManagementId"] = currentMgmt?.Id ?? ManagementId;
            ViewData["CurrentPhaseInt"] = (int)(wizardPlan?.CurrentPhase ?? WizardPhase.TechnicalRequest);

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .SelectMany(ms => ms.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? $"{ms.Key}: valor inválido." : e.ErrorMessage))
                    .Distinct()
                    .Take(4)
                    .ToList();

                TempData.Error(errors.Count > 0
                    ? "No se pudo registrar la solicitud L-7: " + string.Join(" ", errors)
                    : "No se pudo registrar la solicitud L-7. Revise los campos obligatorios.");

                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            if (currentMgmt == null)
            {
                TempData.Warning("No se ha detectado una gestión activa. Debe activar un periodo de gestión para registrar solicitudes.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            var unit = await _context.EquipmentUnits
                .AsTracking()
                .FirstOrDefaultAsync(u => u.Id == Input.EquipmentUnitId);

            if (unit == null)
            {
                TempData.Error("No se pudo registrar la solicitud L-7: la unidad física no existe.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            if (unit.LaboratoryId == null)
            {
                TempData.Error("No se pudo registrar la solicitud L-7: la unidad física no tiene laboratorio asignado.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = new Request
                {
                    Type = RequestType.Technical,
                    ManagementId = currentMgmt.Id,
                    LaboratoryId = Input.LaboratoryId,
                    EquipmentId = unit.EquipmentId,
                    EquipmentUnitId = Input.EquipmentUnitId,
                    Description = Input.Description.Clean()!,
                    Priority = Input.Priority,
                    Observations = Input.Observations?.Clean()?.Length > 500 ? Input.Observations.Clean()?.Substring(0, 497) + "..." : Input.Observations?.Clean(),
                    EstimatedRepairTime = Input.EstimatedRepairTime?.Clean(),
                    Status = RequestStatus.Pending,
                    CreatedDate = DateTime.UtcNow
                };

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    request.CreatedById = currentUser.Id;
                    request.RequestedById = currentUser.Id;
                }

                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                if (wizardPlan != null)
                {
                    wizardPlan.RequestId = request.Id;
                    wizardPlan.CurrentPhase = WizardPhase.Maintenance;
                    wizardPlan.CurrentState = WizardEquipmentState.AwaitingMaintenance;
                    await _context.SaveChangesAsync();
                }
                else if (currentMgmt.Type == ManagementType.Corrective)
                {
                    wizardPlan = new ManagementPlan
                    {
                        ManagementId = currentMgmt.Id,
                        EquipmentUnitId = Input.EquipmentUnitId,
                        CurrentPhase = WizardPhase.Maintenance,
                        CurrentState = WizardEquipmentState.AwaitingMaintenance,
                        PlanStatus = ManagementPlanStatus.Pending,
                        RequestId = request.Id
                    };
                    _context.ManagementPlans.Add(wizardPlan);
                    await _context.SaveChangesAsync();
                }
                else if (isWizard)
                {
                    throw new InvalidOperationException("El flujo wizard preventivo requiere un ManagementPlanId válido para avanzar de L-7 a L-8.");
                }

                await transaction.CommitAsync();

                TempData.Success("Solicitud técnica L-7 registrada exitosamente.");

                if (isWizard)
                {
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = 3, SelectedLabId = Input.LaboratoryId, ManagementId = currentMgmt.Id });
                }

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                TempData.Error($"Error al registrar la solicitud L-7: {detail}");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }
        }

        private async Task LoadLists(int facultyId = 0, int labId = 0, int equipmentUnitId = 0)
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name)
                .ToListAsync(), "Id", "Name", facultyId);

            if (facultyId > 0)
            {
                ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories
                    .Where(l => l.FacultyId == facultyId)
                    .OrderBy(l => l.Name)
                    .ToListAsync(), "Id", "Name", labId);
            }
            else
            {
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            if (labId > 0)
            {
                ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Where(u => u.LaboratoryId == labId)
                    .Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" })
                    .ToListAsync(), "Id", "Name", equipmentUnitId);
            }
            else
            {
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        // Handlers para AJAX (Asegúrate de que existan en tu controlador o aquí)
        public async Task<JsonResult> OnGetLaboratoriesByFaculty(int facultyId)
        {
            var labs = await _context.Laboratories.Where(l => l.FacultyId == facultyId).Select(l => new { id = l.Id, name = l.Name }).ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLab(int laboratoryId)
        {
            var units = await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == laboratoryId).Select(u => new { id = u.Id, name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync();
            return new JsonResult(units);
        }

        private async Task<Management?> ResolveManagementAsync()
        {
            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                if (plan != null)
                    return await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == plan.ManagementId);
            }
            if (ManagementId.HasValue)
                return await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value);
            return await _managementContext.GetCurrentManagementAsync();
        }
    }
}
