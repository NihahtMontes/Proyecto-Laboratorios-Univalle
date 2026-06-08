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

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
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

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        /// <summary>Lista de puntos de control cargados desde la BD para renderizar la UI dinámica.</summary>
        public List<VerificationCheckItem> CheckItems { get; set; } = [];

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            public int EquipmentUnitId { get; set; }

            [DataType(DataType.Date)]
            public DateTime Date { get; set; } = DateTime.Today;

            /// <summary>
            /// Resultados dinámicos: key = CheckItemId, value = VerificationResult.
            /// Se bindea como Input.Results[id] desde el formulario.
            /// </summary>
            public Dictionary<int, VerificationResult> Results { get; set; } = [];

            [Display(Name = "Fallas o problemas del equipo")]
            public List<string> FaultDescriptions { get; set; } = new();

            [Display(Name = "Observaciones (Fallas o problemas del equipo)")]
            public string? Observations { get; set; }

            public VerificationStatus Status { get; set; } = VerificationStatus.Draft;
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, int? returnFacultyId = null, int? returnLaboratoryId = null, bool isWizard = false)
        {
            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.Verification).ThenInclude(v => v!.CheckResults)
                    .Include(p => p.Verification).ThenInclude(v => v!.Faults)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                ViewData["CurrentPhaseInt"] = (int)(plan?.CurrentPhase ?? WizardPhase.Verification);
                ViewData["ManagementId"] = plan?.ManagementId ?? ManagementId;
                ManagementId = plan?.ManagementId ?? ManagementId;

                if (plan?.Verification != null)
                {
                    Input.EquipmentUnitId = plan.Verification.EquipmentUnitId;
                    Input.Date = plan.Verification.Date;
                    Input.Observations = plan.Verification.Observations;
                    Input.Status = plan.Verification.Status;
                    Input.Results = plan.Verification.CheckResults.ToDictionary(r => r.CheckItemId, r => r.Result);
                    Input.FaultDescriptions = plan.Verification.Faults?.Where(f => !f.IsDeleted).Select(f => f.Description).ToList() ?? new();
                    equipmentUnitId = Input.EquipmentUnitId; // Para cargar combos
                }
            }

            if (equipmentUnitId.HasValue)
            {
                Input.EquipmentUnitId = equipmentUnitId.Value;
                var unit = await _context.EquipmentUnits
                    .Include(u => u.Laboratory)
                    .FirstOrDefaultAsync(u => u.Id == equipmentUnitId.Value);

                if (unit != null)
                {
                    Input.LaboratoryId = unit.LaboratoryId ?? 0;
                    if (unit.Laboratory != null)
                        Input.FacultyId = unit.Laboratory.FacultyId;
                }
            }

            LoadCheckItems();
            LoadLists();

            ViewData["ReturnFacultyId"] = returnFacultyId;
            ViewData["ReturnLaboratoryId"] = returnLaboratoryId;
            ViewData["IsWizard"] = isWizard;
            ViewData["ManagementId"] = ViewData["ManagementId"] ?? ManagementId;
            return Page();
        }

        // AJAX handlers
        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLabAsync(int laboratoryId)
        {
            var units = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .Select(u => new { id = u.Id, name = u.Equipment.Name + " (" + u.InventoryNumber + ")" })
                .ToListAsync();
            return new JsonResult(units);
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            return await SaveVerificationAsync(isWizard, saveDraft: false);
        }

        public async Task<IActionResult> OnPostDraftAsync(bool isWizard = false)
        {
            return await SaveVerificationAsync(isWizard, saveDraft: true);
        }

        private async Task<IActionResult> SaveVerificationAsync(bool isWizard, bool saveDraft)
        {
            // Recarga los checks para que la UI renderice bien si hay validación fallida
            LoadCheckItems();

            if (saveDraft)
            {
                ModelState.Remove("Input.FacultyId");
                ModelState.Remove("Input.LaboratoryId");
                ModelState.Remove("Input.EquipmentUnitId");
            }

            if (!ModelState.IsValid)
            {
                LoadLists();
                ViewData["IsWizard"] = isWizard;
                ViewData["ManagementId"] = ManagementId;
                return Page();
            }

            // Leer condición física actual del equipo
            var equipmentUnit = await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
            var physicalCondition = equipmentUnit?.PhysicalCondition ?? PhysicalCondition.Excellent;

            // Obtener gestión activa
            var currentMgmt = ManagementId.HasValue
                ? await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value)
                : await _managementContext.GetCurrentManagementAsync();

            if (currentMgmt == null)
            {
                TempData.Warning("No se ha detectado una gestión activa. Por favor, asegúrese de haber aplicado las migraciones de base de datos o de activar un periodo de gestión para poder registrar la verificación.");
                LoadLists();
                ViewData["IsWizard"] = isWizard;
                ViewData["ManagementId"] = ManagementId;
                return Page();
            }

            // Determinar si hay fallas (si el usuario escribió observaciones de problemas en la lista dinámica)
            bool hasFailures = Input.FaultDescriptions != null && Input.FaultDescriptions.Any(f => !string.IsNullOrWhiteSpace(f));

            var user = await _userManager.GetUserAsync(User);

            var plan = ManagementPlanId.HasValue 
                ? await _context.ManagementPlans
                    .Include(p => p.Verification).ThenInclude(v => v!.Faults)
                    .Include(p => p.Verification).ThenInclude(v => v!.CheckResults)
                    .AsTracking()
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value) 
                : null;

            Verification verification;
            bool isNew = plan?.Verification == null;

            if (isNew)
            {
                verification = new Verification
                {
                    EquipmentUnitId = Input.EquipmentUnitId,
                    ManagementId = currentMgmt.Id,
                    Date = Input.Date,
                    Observations = hasFailures ? string.Join(" | ", Input.FaultDescriptions?.Where(f => !string.IsNullOrWhiteSpace(f)) ?? Enumerable.Empty<string>()) : null,
                    PhysicalCondition = physicalCondition,
                    Status = saveDraft ? VerificationStatus.Draft : (hasFailures ? VerificationStatus.WithObservations : VerificationStatus.Completed),
                    CreatedDate = DateTime.UtcNow,
                    CreatedById = user?.Id,
                    Faults = Input.FaultDescriptions?
                        .Where(f => !string.IsNullOrWhiteSpace(f))
                        .Select(f => new VerificationFault 
                        { 
                            Description = f, 
                            CreatedDate = DateTime.UtcNow, 
                            CreatedById = user?.Id 
                        })
                        .ToList() ?? new List<VerificationFault>()
                };
                _context.Verifications.Add(verification);
            }
            else
            {
                verification = plan!.Verification!;
                verification.EquipmentUnitId = Input.EquipmentUnitId;
                verification.Date = Input.Date;
                verification.Observations = hasFailures ? string.Join(" | ", Input.FaultDescriptions?.Where(f => !string.IsNullOrWhiteSpace(f)) ?? Enumerable.Empty<string>()) : null;
                verification.PhysicalCondition = physicalCondition;
                verification.Status = saveDraft ? VerificationStatus.Draft : (hasFailures ? VerificationStatus.WithObservations : VerificationStatus.Completed);
                verification.LastModifiedDate = DateTime.UtcNow;
                verification.ModifiedById = user?.Id;

                // Soft delete existing faults
                if (verification.Faults != null)
                {
                    foreach (var fault in verification.Faults)
                        fault.IsDeleted = true;
                }
                else
                {
                    verification.Faults = new List<VerificationFault>();
                }

                var activeFaults = Input.FaultDescriptions?.Where(f => !string.IsNullOrWhiteSpace(f)).ToList() ?? new();
                foreach (var desc in activeFaults)
                {
                    var existing = verification.Faults.FirstOrDefault(f => f.Description == desc);
                    if (existing != null)
                    {
                        existing.IsDeleted = false;
                        existing.LastModifiedDate = DateTime.UtcNow;
                        existing.ModifiedById = user?.Id;
                    }
                    else
                    {
                        verification.Faults.Add(new VerificationFault { Description = desc, CreatedDate = DateTime.UtcNow, CreatedById = user?.Id });
                    }
                }
                
                // Limpiar resultados anteriores
                if (verification.CheckResults != null && verification.CheckResults.Any())
                {
                    _context.VerificationCheckResults.RemoveRange(verification.CheckResults);
                }
            }

            await _context.SaveChangesAsync();

            // Guardar resultados individuales de cada check
            foreach (var (checkItemId, result) in Input.Results)
            {
                _context.VerificationCheckResults.Add(new VerificationCheckResult
                {
                    VerificationId = verification.Id,
                    CheckItemId = checkItemId,
                    Result = result
                });
            }
            await _context.SaveChangesAsync();

            // Actualizar management plan
            if (plan != null)
            {
                plan.VerificationId = verification.Id;

                if (saveDraft)
                {
                    MarkVerificationDraft(plan);
                }
                else if (hasFailures)
                {
                    plan.CurrentPhase = WizardPhase.TechnicalRequest;
                    plan.CurrentState = WizardEquipmentState.AwaitingRequest;
                    ClearDraft(plan);
                }
                else
                {
                    plan.CurrentPhase = WizardPhase.Verification;
                    plan.CurrentState = WizardEquipmentState.VerifiedGood;
                    ClearDraft(plan);
                }

                await _context.SaveChangesAsync();
            }

            if (isWizard)
            {
                if (saveDraft)
                {
                    TempData.Success($"Borrador L-6 guardado correctamente.");
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = 1, SelectedLabId = Input.LaboratoryId, ManagementId = currentMgmt.Id });
                }

                if (hasFailures)
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = 2, SelectedLabId = Input.LaboratoryId, ManagementId = currentMgmt.Id });

                return RedirectToPage("/Index", new { ShowWizard = true, Step = 1, SelectedLabId = Input.LaboratoryId, ManagementId = currentMgmt.Id });
            }

            TempData.Success(saveDraft ? "Borrador guardado correctamente." : "Verificación registrada correctamente.");
            return RedirectToPage("./Index");
        }

        private static void MarkVerificationDraft(ManagementPlan plan)
        {
            plan.IsDraft = true;
            plan.DraftPhase = WizardPhase.Verification;
            plan.DraftSavedAt = DateTime.UtcNow;
            plan.DraftSummary = "Borrador L-6 guardado con verificación parcial.";
            plan.CurrentPhase = WizardPhase.Verification;
            plan.CurrentState = WizardEquipmentState.PendingVerification;
            plan.PlanStatus = ManagementPlanStatus.InProgress;
            plan.LastModifiedDate = DateTime.UtcNow;
        }

        private static void ClearDraft(ManagementPlan plan)
        {
            plan.IsDraft = false;
            plan.DraftPhase = null;
            plan.DraftSavedAt = null;
            plan.DraftSummary = null;
            plan.LastModifiedDate = DateTime.UtcNow;
        }

        private void LoadCheckItems()
        {
            CheckItems = _context.VerificationCheckItems
                .Where(c => c.IsActive)
                .OrderBy(c => c.Order)
                .ToList();
        }

        private void LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name), "Id", "Name", Input.FacultyId);

            if (Input.FacultyId > 0)
            {
                var labs = _context.Laboratories
                    .Where(l => l.FacultyId == Input.FacultyId && l.Status == GeneralStatus.Activo)
                    .OrderBy(l => l.Name).ToList();
                ViewData["LaboratoryId"] = new SelectList(labs, "Id", "Name", Input.LaboratoryId);
            }
            else
            {
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            if (Input.LaboratoryId > 0)
            {
                var units = _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Where(u => u.LaboratoryId == Input.LaboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                    .Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" })
                    .ToList();
                ViewData["EquipmentUnitId"] = new SelectList(units, "Id", "Name", Input.EquipmentUnitId);
            }
            else
            {
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }
    }
}
