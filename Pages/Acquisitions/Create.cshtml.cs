using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

namespace Proyecto_Laboratorios_Univalle.Pages.Acquisitions
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementService;

        public CreateModel(ApplicationDbContext context, UserManager<User> userManager, IManagementContextService managementService)
        {
            _context = context;
            _userManager = userManager;
            _managementService = managementService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required(ErrorMessage = "La justificación es obligatoria")]
            [Display(Name = "Justificación del Requerimiento")]
            [StringLength(1000, ErrorMessage = "La justificación no puede superar los 1000 caracteres")]
            public string Description { get; set; } = string.Empty;

            [Display(Name = "Especificaciones / Observaciones (Opcional)")]
            [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres")]
            public string? Observations { get; set; }

            [Required(ErrorMessage = "El código de inversión es obligatorio")]
            [Display(Name = "Código de Inversión")]
            [StringLength(50)]
            public string InvestmentCode { get; set; } = string.Empty;

            [Required(ErrorMessage = "El centro de costos es obligatorio")]
            [Display(Name = "Centro de Costos")]
            [StringLength(100)]
            public string CostCenter { get; set; } = string.Empty;

            [Display(Name = "Referencia de Mantenimiento (Opcional)")]
            public int? MaintenanceId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false, int? managementPlanId = null)
        {
            ManagementPlanId = managementPlanId;
            ViewData["IsWizard"] = isWizard;

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
                }
            }

            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Laboratory)
                    .Include(p => p.Maintenance)
                    .Include(p => p.AcquisitionRequest).ThenInclude(r => r!.CostDetails)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
                if (plan != null)
                {
                    ApplyPlanToInput(plan);
                    ApplyAcquisitionRequestToInput(plan.AcquisitionRequest);
                    await PopulateWizardViewDataAsync(plan);
                }
            }

            await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
            return Page();
        }

        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .OrderBy(x => x.name)
                .ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLabAsync(int laboratoryId)
        {
            var units = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(u => u.Equipment!.Name)
                .ThenBy(u => u.InventoryNumber)
                .Select(u => new {
                    id = u.Id,
                    eqName = u.Equipment != null ? u.Equipment.Name : "Equipo",
                    inv = u.InventoryNumber
                })
                .ToListAsync();

            var result = units.Select(x => new {
                id = x.id,
                name = $"{x.eqName} (Inv: {x.inv})"
            });

            return new JsonResult(result);
        }

        public async Task<JsonResult> OnGetMaintenancesByUnitAsync(int unitId)
        {
            var maintenances = await _context.Maintenances
                .Where(m => m.EquipmentUnitId == unitId &&
                           (m.Status == MaintenanceStatus.Pending || m.Status == MaintenanceStatus.InProgress || m.Status == MaintenanceStatus.Scheduled || m.Status == MaintenanceStatus.Completed))
                .Select(m => new {
                    id = m.Id,
                    displayName = $"Mnt #{m.Id} - {m.ScheduledDate:dd/MM/yyyy} - {m.Status}"
                })
                .OrderByDescending(m => m.id)
                .ToListAsync();

            return new JsonResult(maintenances);
        }

        public async Task<JsonResult> OnGetMaintenanceCostsAsync(int maintenanceId)
        {
            var costs = await _context.CostDetails
                .Where(d => d.MaintenanceId == maintenanceId)
                .Select(d => new {
                    id = d.Id,
                    concept = d.Concept,
                    quantity = d.Quantity,
                    unitPrice = d.UnitPrice,
                    unitOfMeasure = d.UnitOfMeasure,
                    categoryName = d.Category.ToString()
                })
                .ToListAsync();

            return new JsonResult(costs);
        }

        public async Task<JsonResult> OnGetNextInvestmentCodeAsync(int unitId)
        {
            var unit = await _context.EquipmentUnits.Include(u => u.Equipment).FirstOrDefaultAsync(u => u.Id == unitId);
            if (unit?.Equipment == null) return new JsonResult(new { code = "" });

            var count = await _context.Requests
                .Where(r => r.EquipmentUnitId == unitId)
                .CountAsync();

            string suggestedCode = $"{unit.Equipment!.Name.Replace(" ", "").ToUpper()}-{(count + 1):D3}";
            return new JsonResult(new { code = suggestedCode });
        }

        public async Task<IActionResult> OnPostDraftAsync(bool isWizard = false)
        {
            return await SaveAcquisitionAsync(isWizard, saveDraft: true);
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            return await SaveAcquisitionAsync(isWizard, saveDraft: false);
        }

        private async Task<IActionResult> SaveAcquisitionAsync(bool isWizard, bool saveDraft)
        {
            ManagementPlan? wizardPlan = null;
            if (ManagementPlanId.HasValue)
            {
                wizardPlan = await _context.ManagementPlans
                    .AsTracking()
                    .Include(p => p.Management)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Laboratory)
                    .Include(p => p.Maintenance).ThenInclude(m => m!.CostDetails)
                    .Include(p => p.AcquisitionRequest).ThenInclude(r => r!.CostDetails)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                if (wizardPlan == null)
                {
                    TempData.Error($"No se pudo resolver el plan de gestión para L-12. ManagementPlanId recibido: {ManagementPlanId.Value}.");
                    await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                    ViewData["IsWizard"] = isWizard;
                    return Page();
                }

                ApplyPlanToInput(wizardPlan);
                ModelState.Remove("Input.FacultyId");
                ModelState.Remove("Input.LaboratoryId");
                ModelState.Remove("Input.EquipmentUnitId");
                ModelState.Remove("Input.MaintenanceId");
            }

            if (saveDraft)
            {
                ClearDraftModelState();
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .SelectMany(ms => ms.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? $"{ms.Key}: valor inválido." : e.ErrorMessage))
                    .Distinct()
                    .Take(4)
                    .ToList();
                TempData.Error(errors.Count > 0 ? "No se pudo guardar L-12: " + string.Join(" ", errors) : "No se pudo guardar L-12. Revise los campos obligatorios.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                if (wizardPlan != null) await PopulateWizardViewDataAsync(wizardPlan);
                ViewData["IsWizard"] = isWizard;
                return Page();
            }

            var unit = wizardPlan?.EquipmentUnit ?? await _context.EquipmentUnits.FindAsync(Input.EquipmentUnitId);
            if (unit == null)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad seleccionada no es válida.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                if (wizardPlan != null) await PopulateWizardViewDataAsync(wizardPlan);
                ViewData["IsWizard"] = isWizard;
                return Page();
            }

            var management = wizardPlan?.Management;
            if (management == null && ManagementId.HasValue)
            {
                management = await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value);
            }
            if (management == null && !isWizard)
            {
                management = await _managementService.GetCurrentManagementAsync(ManagementType.Preventive);
            }

            if (management == null)
            {
                TempData.Error("No hay gestión activa para registrar L-12 en este flujo.");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                if (wizardPlan != null) await PopulateWizardViewDataAsync(wizardPlan);
                ViewData["IsWizard"] = isWizard;
                return Page();
            }

            var managementId = management.Id;

            var request = wizardPlan?.AcquisitionRequest ?? new Request { CreatedDate = DateTime.UtcNow };

            request.Type = RequestType.Purchasing;
            request.ManagementId = managementId;
            request.LaboratoryId = Input.LaboratoryId;
            request.EquipmentId = unit.EquipmentId;
            request.EquipmentUnitId = unit.Id;
            request.Description = string.IsNullOrWhiteSpace(Input.Description)
                ? "Borrador L-12 pendiente de justificación."
                : Input.Description.Clean()!;
            request.Observations = Input.Observations?.Clean();
            request.InvestmentCode = Input.InvestmentCode.Clean();
            request.CostCenter = Input.CostCenter.Clean();
            request.Priority = RequestPriority.Medium;
            request.Status = RequestStatus.Pending;
            request.LastModifiedDate = request.Id == 0 ? null : DateTime.UtcNow;

            if (Input.MaintenanceId.HasValue)
            {
                var maintenanceCosts = wizardPlan?.Maintenance?.CostDetails?.ToList()
                    ?? await _context.CostDetails
                        .Where(d => d.MaintenanceId == Input.MaintenanceId.Value)
                        .ToListAsync();

                SyncRequestCosts(request, maintenanceCosts);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                if (request.Id == 0)
                {
                    request.CreatedById = currentUser.Id;
                    request.RequestedById = currentUser.Id;
                }
                else
                {
                    request.ModifiedById = currentUser.Id;
                }
            }

            if (request.Id == 0)
            {
                _context.Requests.Add(request);
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SaveChangesAsync();

                if (!saveDraft && currentUser != null)
                {
                    var notification = new Notification
                    {
                        UserId = currentUser.Id,
                        Title = "Adquisición Solicitada",
                        Message = $"Se registró correctamente tu solicitud de compra para la unidad {unit.InventoryNumber}.",
                        ActionUrl = wizardPlan != null
                            ? $"/Requests/Details/{request.Id}?IsWizard=true&ManagementId={wizardPlan.ManagementId}&ManagementType={management.Type}&Step=6"
                            : $"/Requests/Details/{request.Id}?ManagementId={management.Id}&ManagementType={management.Type}",
                        IconClass = "fas fa-shopping-cart text-success",
                        ManagementId = management.Id,
                        ManagementType = management.Type,
                        Scope = "acquisition",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                if (wizardPlan != null)
                {
                    wizardPlan.AcquisitionRequestId = request.Id;
                    if (saveDraft)
                    {
                        MarkAcquisitionDraft(wizardPlan);
                    }
                    else
                    {
                        wizardPlan.CurrentPhase = WizardPhase.Disbursement;
                        wizardPlan.CurrentState = WizardEquipmentState.Completed;
                        wizardPlan.PlanStatus = ManagementPlanStatus.Completed;
                        ClearDraft(wizardPlan);
                    }
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                TempData.Error($"Error al {(saveDraft ? "guardar borrador de" : "registrar")} adquisición: {detail}");
                await LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                if (wizardPlan != null) await PopulateWizardViewDataAsync(wizardPlan);
                ViewData["IsWizard"] = isWizard;
                return Page();
            }

            TempData.Success(saveDraft
                ? $"Borrador L-12 para '{unit.InventoryNumber}' guardado correctamente."
                : $"Solicitud de Adquisición para '{unit.InventoryNumber}' registrada exitosamente. El mantenimiento quedó completado en el wizard.");

            if (wizardPlan != null && isWizard)
            {
                return RedirectToPage("/Index", new { ShowWizard = true, Step = saveDraft ? 6 : 7, SelectedLabId = Input.LaboratoryId, ManagementId = wizardPlan.ManagementId });
            }

            return RedirectToPage("./Index");
        }

        private void ApplyPlanToInput(ManagementPlan plan)
        {
            if (plan.EquipmentUnit != null)
            {
                Input.EquipmentUnitId = plan.EquipmentUnit.Id;
                Input.LaboratoryId = plan.EquipmentUnit.LaboratoryId ?? 0;
                Input.FacultyId = plan.EquipmentUnit.Laboratory?.FacultyId ?? 0;
            }

            Input.MaintenanceId = plan.MaintenanceId;
            ManagementId = plan.ManagementId;
        }

        private void ApplyAcquisitionRequestToInput(Request? request)
        {
            if (request == null) return;

            Input.Description = request.Description == "Borrador L-12 pendiente de justificación." ? string.Empty : request.Description;
            Input.Observations = request.Observations;
            Input.InvestmentCode = request.InvestmentCode ?? string.Empty;
            Input.CostCenter = request.CostCenter ?? string.Empty;
        }

        private void ClearDraftModelState()
        {
            ModelState.Remove("Input.Description");
            ModelState.Remove("Input.InvestmentCode");
            ModelState.Remove("Input.CostCenter");
            ModelState.Remove("Input.Observations");
        }

        private static void SyncRequestCosts(Request request, IEnumerable<CostDetail> maintenanceCosts)
        {
            foreach (var existing in request.CostDetails.Where(c => c.Id > 0).ToList())
            {
                existing.RequestId = null;
                existing.Request = null;
                existing.LastModifiedDate = DateTime.UtcNow;
            }

            request.CostDetails.Clear();
            foreach (var cost in maintenanceCosts)
            {
                request.CostDetails.Add(new CostDetail
                {
                    Concept = cost.Concept,
                    Description = cost.Description,
                    Quantity = cost.Quantity,
                    UnitOfMeasure = cost.UnitOfMeasure,
                    UnitPrice = cost.UnitPrice,
                    Category = cost.Category,
                    Provider = cost.Provider,
                    InvoiceNumber = cost.InvoiceNumber,
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        private static void MarkAcquisitionDraft(ManagementPlan plan)
        {
            plan.IsDraft = true;
            plan.DraftPhase = WizardPhase.Disbursement;
            plan.DraftSavedAt = DateTime.UtcNow;
            plan.DraftSummary = "Borrador L-12 guardado con solicitud de adquisición parcial.";
            plan.CurrentPhase = WizardPhase.Disbursement;
            plan.CurrentState = WizardEquipmentState.AwaitingDisbursement;
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

        private async Task PopulateWizardViewDataAsync(ManagementPlan plan)
        {
            ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
            ViewData["ManagementId"] = plan.ManagementId;
            var mgmt = plan.Management ?? await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == plan.ManagementId);
            ViewData["IsCorrective"] = mgmt?.Type == ManagementType.Corrective;
            if (plan.DepartureId.HasValue) ViewData["LinkedDepartureId"] = plan.DepartureId.Value;
            if (plan.MaintenanceId.HasValue) ViewData["LinkedMaintenanceId"] = plan.MaintenanceId.Value;
        }

        private async Task LoadLists(int facultyId = 0, int laboratoryId = 0, int equipmentUnitId = 0)
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name).ToListAsync(), "Id", "Name", facultyId);

            if (facultyId > 0)
                ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == facultyId).ToListAsync(), "Id", "Name", laboratoryId);
            else
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            if (laboratoryId > 0)
                ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == laboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", equipmentUnitId);
            else
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}
