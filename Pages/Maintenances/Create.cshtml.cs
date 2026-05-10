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

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
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

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false, int? managementPlanId = null)
        {
            Input = new InputModel
            {
                ScheduledDate = DateTime.UtcNow,
                MaintenanceType = isWizard ? MaintenanceType.Preventivo : MaintenanceType.Otros,
                CostDetails = new List<CostDetail>(),
                Tasks = new List<MaintenanceTask>
                {
                    new MaintenanceTask { Description = "Limpieza y Desinfección de Componentes" },
                    new MaintenanceTask { Description = "Calibración y Ajuste de Sistema" },
                    new MaintenanceTask { Description = "Pruebas de Esfuerzo y Carga Operativa" },
                    new MaintenanceTask { Description = "Revisión Final de Seguridad y Cierre" }
                }
            };

            if (managementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.Maintenance).ThenInclude(m => m!.CostDetails)
                    .Include(p => p.Maintenance).ThenInclude(m => m!.Tasks)
                    .FirstOrDefaultAsync(p => p.Id == managementPlanId.Value);
                if (plan != null)
                {
                    ViewData["CurrentPhaseInt"] = (int)plan.CurrentPhase;
                    ViewData["ManagementId"] = plan.ManagementId;
                    var mgmt = await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == plan.ManagementId);
                    ViewData["IsCorrective"] = mgmt?.Type == ManagementType.Corrective;
                    if (plan.RequestId.HasValue)
                    {
                        Input.RequestId = plan.RequestId;
                    }
                    if (plan.Maintenance != null)
                    {
                        ApplyMaintenanceToInput(plan.Maintenance);
                    }
                    if (!equipmentUnitId.HasValue && plan.EquipmentUnitId.HasValue)
                    {
                        equipmentUnitId = plan.EquipmentUnitId;
                    }
                }
            }

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

                    ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == Input.FacultyId).ToListAsync(), "Id", "Name", Input.LaboratoryId);
                    ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == Input.LaboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", Input.EquipmentUnitId);
                }
            }

            ViewData["IsWizard"] = isWizard;
            ManagementPlanId = managementPlanId;



            LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId, Input.TechnicianId, Input.RequestId);
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

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

            [Required(ErrorMessage = "El tipo de mantenimiento es obligatorio")]
            [Display(Name = "Tipo de Mantenimiento")]
            public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Otros;

            [Display(Name = "Técnico Responsable")]
            public int? TechnicianId { get; set; }

            [Display(Name = "Solicitud Relacionada")]
            public int? RequestId { get; set; }

            [Display(Name = "Descripción del Trabajo")]
            public string? Description { get; set; }

            [Required]
            [Display(Name = "Fecha Programada")]
            [DataType(DataType.Date)]
            public DateTime ScheduledDate { get; set; } = DateTime.UtcNow;

            [Display(Name = "Fecha Inicio Real")]
            [DataType(DataType.DateTime)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "Fecha Fin Real")]
            [DataType(DataType.DateTime)]
            public DateTime? EndDate { get; set; }

            [Display(Name = "Costo Real Total")]
            public decimal ActualCost { get; set; }

            [Display(Name = "Observaciones")]
            public string? Observations { get; set; }

            [Display(Name = "Recomendaciones")]
            public string? Recommendations { get; set; }

            [Display(Name = "Estado Inicial")]
            public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

            public List<CostDetail> CostDetails { get; set; } = new();

            public int CompletionPercentage { get; set; } = 0;
            public List<MaintenanceTask> Tasks { get; set; } = new();
            
            // Legacy steps (to be removed once fully migrated if needed)
            public bool Step1_Cleaning { get; set; } = false;
            public bool Step2_Calibration { get; set; } = false;
            public bool Step3_Testing { get; set; } = false;
            public bool Step4_FinalReview { get; set; } = false;

            [DataType(DataType.Date)]
            [Display(Name = "Fecha Sugerida de Próximo Mantenimiento")]
            public DateTime? SuggestedNextMaintenanceDate { get; set; }

            [Display(Name = "Nivel de Satisfacción")]
            public int? SatisfactionLevel { get; set; }
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

        public async Task<IActionResult> OnPostDraftAsync(bool isWizard = false)
        {
            return await SaveMaintenanceAsync(isWizard, saveDraft: true);
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            return await SaveMaintenanceAsync(isWizard, saveDraft: false);
        }

        private async Task<IActionResult> SaveMaintenanceAsync(bool isWizard, bool saveDraft)
        {
            // ─────────────────────────────────────────────────────────────
            // WIZARD PRE-VALIDATION: reconstruir valores ANTES de ModelState
            // Los selects disabled no postean → FacultyId/LaboratoryId/
            // EquipmentUnitId/MaintenanceType llegan como 0 → [Required] falla.
            // ─────────────────────────────────────────────────────────────
            ManagementPlan? wizardPlan = null;
            Management? currentMgmt = null;

            if (ManagementPlanId.HasValue)
            {
                wizardPlan = await _context.ManagementPlans
                    .AsTracking()
                    .Include(p => p.Management)
                    .Include(p => p.Maintenance).ThenInclude(m => m!.CostDetails)
                    .Include(p => p.Maintenance).ThenInclude(m => m!.Tasks)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Laboratory)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                    .FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);

                if (wizardPlan != null)
                {
                    currentMgmt = wizardPlan.Management;

                    if (wizardPlan.EquipmentUnitId.HasValue)
                    {
                        Input.EquipmentUnitId = wizardPlan.EquipmentUnitId.Value;
                        Input.LaboratoryId = wizardPlan.EquipmentUnit?.LaboratoryId ?? Input.LaboratoryId;
                        Input.FacultyId = wizardPlan.EquipmentUnit?.Laboratory?.FacultyId ?? Input.FacultyId;
                    }

                    if (isWizard)
                    {
                        Input.MaintenanceType = MaintenanceType.Preventivo;
                    }

                    if (wizardPlan.RequestId.HasValue)
                    {
                        Input.RequestId = wizardPlan.RequestId;
                    }

                    // Limpiar keys que el servidor acaba de reconstruir (disabled inputs NO postean)
                    ModelState.Remove("Input.FacultyId");
                    ModelState.Remove("Input.LaboratoryId");
                    ModelState.Remove("Input.EquipmentUnitId");
                    ModelState.Remove("Input.MaintenanceType");
                }
            }

            if (!isWizard && !ManagementPlanId.HasValue)
            {
                currentMgmt = await _managementContext.GetCurrentManagementAsync();
            }

            // Restaurar ViewData wizard para re-render en caso de error
            if (wizardPlan != null)
            {
                ViewData["IsWizard"] = true;
                ViewData["CurrentPhaseInt"] = (int)wizardPlan.CurrentPhase;
                ViewData["ManagementId"] = wizardPlan.ManagementId;
                ViewData["ManagementPlanId"] = wizardPlan.Id;
                var mgmt = await _context.Managements.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == wizardPlan.ManagementId);
                ViewData["IsCorrective"] = mgmt?.Type == ManagementType.Corrective;
            }

            if (saveDraft)
            {
                ClearDraftModelState();
            }

            if (Input.StartDate.HasValue && Input.EndDate.HasValue)
            {
                if (Input.EndDate < Input.StartDate)
                    ModelState.AddModelError("Input.EndDate", "La fecha de finalización no puede ser anterior al inicio.");
            }

            if (isWizard && !saveDraft && !Input.TechnicianId.HasValue)
            {
                ModelState.AddModelError("Input.TechnicianId", "El técnico responsable es obligatorio en el flujo de mantenimiento. Asígnelo para que L-3 pueda derivar el responsable de la salida.");
            }

            if (!saveDraft && Input.Status == MaintenanceStatus.Completed && Input.CompletionPercentage != 100)
            {
                ModelState.AddModelError("Input.CompletionPercentage", "Para marcar el mantenimiento como Completado, el avance debe estar al 100%.");
            }

            var equipmentUnit = await _context.EquipmentUnits.Include(u => u.Equipment).AsTracking().FirstOrDefaultAsync(u => u.Id == Input.EquipmentUnitId);
            if (equipmentUnit == null)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad física no existe.");
            }
            else if (equipmentUnit.CurrentStatus == EquipmentStatus.OnLoan)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "No se puede realizar mantenimiento a un equipo que actualmente está en préstamo.");
            }
            else if (equipmentUnit.LaboratoryId == null)
            {
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad física no tiene laboratorio asignado. Asigne un laboratorio al equipo antes de registrar el mantenimiento.");
            }

            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Input.CostDetails")).ToList())
            {
                ModelState.Remove(key);
            }

            Input.CostDetails = Input.CostDetails?
                .Where(d => !string.IsNullOrWhiteSpace(d.Concept))
                .ToList() ?? new List<CostDetail>();

            foreach (var detail in Input.CostDetails)
            {
                detail.Quantity = detail.Quantity <= 0 ? 1 : detail.Quantity;
                detail.UnitPrice = detail.UnitPrice < 0 ? 0 : detail.UnitPrice;
            }

            decimal totalCosts = Input.CostDetails?.Sum(d => d.Subtotal) ?? 0;
            Input.ActualCost = totalCosts;

            Input.Tasks = NormalizeTasks(Input.Tasks);
            Input.CompletionPercentage = CalculateCompletionPercentage(Input.Tasks);

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .SelectMany(ms => ms.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? $"{ms.Key}: valor inválido." : e.ErrorMessage))
                    .Distinct()
                    .Take(4)
                    .ToList();

                TempData.Error(errors.Count > 0
                    ? "No se pudo registrar el mantenimiento: " + string.Join(" ", errors)
                    : "No se pudo registrar el mantenimiento. Revise los campos obligatorios.");

                LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                return Page();
            }

            try
            {
                currentMgmt ??= await _managementContext.GetCurrentManagementAsync();
                if (currentMgmt == null)
                {
                    TempData.Warning("No hay una gestion activa disponible. Por favor active una gestion institucional para registrar el mantenimiento.");
                    LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId);
                    return Page();
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                var maintenance = wizardPlan?.Maintenance ?? new Maintenance
                {
                    CreatedDate = DateTime.UtcNow,
                    CostDetails = new(),
                    Tasks = new()
                };

                var currentUser = await _userManager.GetUserAsync(User);
                if (maintenance.Id == 0)
                {
                    maintenance.CreatedById = currentUser?.Id;
                    _context.Maintenances.Add(maintenance);
                }
                else
                {
                    maintenance.LastModifiedDate = DateTime.UtcNow;
                    maintenance.ModifiedById = currentUser?.Id;
                }

                SyncMaintenanceScalarFields(maintenance, currentMgmt.Id, saveDraft);

                if (maintenance.Id == 0)
                {
                    await _context.SaveChangesAsync();
                }

                SyncMaintenanceChildren(maintenance);

                if (!saveDraft && equipmentUnit != null && equipmentUnit.CurrentStatus != EquipmentStatus.UnderMaintenance)
                {
                    var lastHistory = await _context.EquipmentStateHistories
                        .Where(h => h.EquipmentUnitId == equipmentUnit.Id && h.EndDate == null)
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
                        EquipmentUnitId = equipmentUnit.Id,
                        Status = EquipmentStatus.UnderMaintenance,
                        StartDate = DateTime.UtcNow,
                        Reason = "Ingreso a proceso de mantenimiento."
                    };
                    _context.EquipmentStateHistories.Add(newHistory);

                    equipmentUnit.CurrentStatus = EquipmentStatus.UnderMaintenance;
                    _context.EquipmentUnits.Update(equipmentUnit);
                }

                await _context.SaveChangesAsync();

                if (wizardPlan != null)
                {
                    wizardPlan.MaintenanceId = maintenance.Id;
                    if (saveDraft)
                    {
                        MarkMaintenanceDraft(wizardPlan);
                    }
                    else
                    {
                        wizardPlan.CurrentPhase = WizardPhase.Exit;
                        wizardPlan.CurrentState = WizardEquipmentState.AwaitingDeparture;
                        ClearDraft(wizardPlan);
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                if (!saveDraft)
                {
                    await TryCreateMaintenanceNotificationAsync(maintenance, equipmentUnit);
                }

                TempData.Success(saveDraft
                    ? $"Borrador de mantenimiento para '{equipmentUnit?.Equipment?.Name}' guardado correctamente."
                    : $"Mantenimiento para '{equipmentUnit?.Equipment?.Name}' guardado correctamente.");

                if (wizardPlan != null && isWizard)
                {
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = saveDraft ? 3 : 4, SelectedLabId = Input.LaboratoryId, ManagementId = currentMgmt.Id });
                }

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData.Error($"Error al guardar el registro: {ex.Message}");
                LoadLists(Input.FacultyId, Input.LaboratoryId, Input.EquipmentUnitId, Input.TechnicianId, Input.RequestId);
                return Page();
            }
        }

        private void ClearDraftModelState()
        {
            ModelState.Remove("Input.TechnicianId");
            ModelState.Remove("Input.Description");
            ModelState.Remove("Input.ScheduledDate");
            ModelState.Remove("Input.StartDate");
            ModelState.Remove("Input.EndDate");
            ModelState.Remove("Input.Observations");
            ModelState.Remove("Input.Recommendations");
            ModelState.Remove("Input.SuggestedNextMaintenanceDate");
            ModelState.Remove("Input.SatisfactionLevel");
            ModelState.Remove("Input.CostDetails");
            ModelState.Remove("Input.Tasks");
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Input.CostDetails[") || k.StartsWith("Input.Tasks[")).ToList())
            {
                ModelState.Remove(key);
            }
        }

        private void ApplyMaintenanceToInput(Maintenance maintenance)
        {
            Input.MaintenanceType = maintenance.MaintenanceType;
            Input.TechnicianId = maintenance.TechnicianId;
            Input.RequestId = maintenance.RequestId;
            Input.Description = maintenance.Description;
            Input.ScheduledDate = maintenance.ScheduledDate ?? DateTime.UtcNow;
            Input.StartDate = maintenance.StartDate;
            Input.EndDate = maintenance.EndDate;
            Input.ActualCost = maintenance.ActualCost ?? 0m;
            Input.Observations = maintenance.Observations;
            Input.Recommendations = maintenance.Recommendations;
            Input.Status = maintenance.Status;
            Input.CompletionPercentage = maintenance.CompletionPercentage;
            Input.Step1_Cleaning = maintenance.Step1_Cleaning;
            Input.Step2_Calibration = maintenance.Step2_Calibration;
            Input.Step3_Testing = maintenance.Step3_Testing;
            Input.Step4_FinalReview = maintenance.Step4_FinalReview;
            Input.SuggestedNextMaintenanceDate = maintenance.SuggestedNextMaintenanceDate;
            Input.SatisfactionLevel = maintenance.SatisfactionLevel.HasValue ? (int)maintenance.SatisfactionLevel.Value : null;
            Input.CostDetails = maintenance.CostDetails?.OrderBy(c => c.Id).ToList() ?? new List<CostDetail>();
            Input.Tasks = maintenance.Tasks?.Where(t => !t.IsDeleted).OrderBy(t => t.Id).ToList() ?? new List<MaintenanceTask>();
        }

        private void SyncMaintenanceScalarFields(Maintenance maintenance, int managementId, bool saveDraft)
        {
            maintenance.EquipmentUnitId = Input.EquipmentUnitId;
            maintenance.ManagementId = managementId;
            maintenance.MaintenanceType = Input.MaintenanceType;
            maintenance.TechnicianId = Input.TechnicianId;
            maintenance.RequestId = Input.RequestId;
            maintenance.Description = Input.Description?.Clean();
            maintenance.ScheduledDate = Input.ScheduledDate;
            maintenance.StartDate = Input.StartDate;
            maintenance.EndDate = Input.EndDate;
            maintenance.ActualCost = Input.ActualCost;
            maintenance.Observations = Input.Observations?.Clean();
            maintenance.Recommendations = Input.Recommendations?.Clean();
            maintenance.SuggestedNextMaintenanceDate = Input.SuggestedNextMaintenanceDate;
            maintenance.SatisfactionLevel = (MaintenanceSatisfaction?)Input.SatisfactionLevel;
            maintenance.Status = saveDraft ? MaintenanceStatus.InProgress : Input.Status;
            maintenance.CompletionPercentage = Input.CompletionPercentage;
            maintenance.Step1_Cleaning = Input.Tasks.ElementAtOrDefault(0)?.IsCompleted ?? false;
            maintenance.Step2_Calibration = Input.Tasks.ElementAtOrDefault(1)?.IsCompleted ?? false;
            maintenance.Step3_Testing = Input.Tasks.ElementAtOrDefault(2)?.IsCompleted ?? false;
            maintenance.Step4_FinalReview = Input.Tasks.ElementAtOrDefault(3)?.IsCompleted ?? false;

        }

        private void SyncMaintenanceChildren(Maintenance maintenance)
        {
            SyncCostDetails(maintenance, Input.CostDetails ?? new List<CostDetail>());
            SyncTasks(maintenance, Input.Tasks ?? new List<MaintenanceTask>());
        }

        private static List<MaintenanceTask> NormalizeTasks(List<MaintenanceTask>? tasks)
        {
            return (tasks ?? new List<MaintenanceTask>())
                .Where(t => !t.IsDeleted && (t.Id > 0 || !string.IsNullOrWhiteSpace(t.Description)))
                .Select(t =>
                {
                    t.Description = t.Description?.Trim() ?? string.Empty;
                    return t;
                })
                .ToList();
        }

        private static int CalculateCompletionPercentage(List<MaintenanceTask> tasks)
        {
            if (tasks.Count == 0) return 0;
            return (int)Math.Round(tasks.Count(t => t.IsCompleted) * 100m / tasks.Count);
        }

        private void SyncCostDetails(Maintenance maintenance, List<CostDetail> inputDetails)
        {
            var inputIds = inputDetails.Where(d => d.Id > 0).Select(d => d.Id).ToHashSet();
            foreach (var existing in maintenance.CostDetails.Where(d => d.Id > 0 && !inputIds.Contains(d.Id)).ToList())
            {
                existing.MaintenanceId = null;
                existing.Maintenance = null;
                existing.LastModifiedDate = DateTime.UtcNow;
            }

            foreach (var detail in inputDetails)
            {
                detail.Concept = detail.Concept.Trim();
                detail.Quantity = detail.Quantity <= 0 ? 1 : detail.Quantity;
                detail.UnitPrice = detail.UnitPrice < 0 ? 0 : detail.UnitPrice;
                detail.MaintenanceId = maintenance.Id;
                detail.Maintenance = maintenance;
                detail.RequestId = null;

                var existing = maintenance.CostDetails.FirstOrDefault(d => d.Id == detail.Id && d.Id != 0);
                if (existing != null)
                {
                    existing.Concept = detail.Concept;
                    existing.Description = detail.Description?.Clean();
                    existing.Quantity = detail.Quantity;
                    existing.UnitOfMeasure = detail.UnitOfMeasure;
                    existing.UnitPrice = detail.UnitPrice;
                    existing.Category = detail.Category;
                    existing.Provider = detail.Provider;
                    existing.InvoiceNumber = detail.InvoiceNumber;
                    existing.LastModifiedDate = DateTime.UtcNow;
                }
                else
                {
                    detail.CreatedDate = DateTime.UtcNow;
                    maintenance.CostDetails.Add(detail);
                }
            }
        }

        private static void SyncTasks(Maintenance maintenance, List<MaintenanceTask> inputTasks)
        {
            var inputIds = inputTasks.Where(t => t.Id > 0).Select(t => t.Id).ToHashSet();
            foreach (var existing in maintenance.Tasks.Where(t => t.Id > 0 && !t.IsDeleted && !inputIds.Contains(t.Id)).ToList())
            {
                existing.IsDeleted = true;
                existing.IsCompleted = false;
            }

            foreach (var task in inputTasks)
            {
                var existing = maintenance.Tasks.FirstOrDefault(t => t.Id == task.Id && t.Id != 0);
                if (existing != null)
                {
                    existing.Description = task.Description;
                    existing.IsCompleted = task.IsCompleted;
                    existing.IsDeleted = false;
                }
                else if (!string.IsNullOrWhiteSpace(task.Description))
                {
                    maintenance.Tasks.Add(new MaintenanceTask
                    {
                        Description = task.Description,
                        IsCompleted = task.IsCompleted,
                        IsDeleted = false
                    });
                }
            }
        }

        private static void MarkMaintenanceDraft(ManagementPlan plan)
        {
            plan.IsDraft = true;
            plan.DraftPhase = WizardPhase.Maintenance;
            plan.DraftSavedAt = DateTime.UtcNow;
            plan.DraftSummary = "Borrador L-8 guardado con planificación parcial de mantenimiento.";
            plan.CurrentPhase = WizardPhase.Maintenance;
            plan.CurrentState = WizardEquipmentState.AwaitingMaintenance;
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

        private async Task TryCreateMaintenanceNotificationAsync(Maintenance maintenance, EquipmentUnit? equipmentUnit)
        {
            if (!maintenance.ScheduledDate.HasValue || !maintenance.TechnicianId.HasValue)
                return;

            var daysUntil = (maintenance.ScheduledDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
            if (daysUntil < 0 || daysUntil > 7)
                return;

            try
            {
                var technician = await _context.People
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == maintenance.TechnicianId.Value);

                if (string.IsNullOrWhiteSpace(technician?.Email))
                    return;

                var recipient = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == technician.Email);

                if (recipient == null)
                    return;

                _context.Notifications.Add(new Notification
                {
                    UserId = recipient.Id,
                    Title = "Mantenimiento Próximo a Vencer",
                    Message = $"El mantenimiento de la unidad {equipmentUnit?.InventoryNumber} debe realizarse el {maintenance.ScheduledDate.Value:dd/MM/yyyy}.",
                    ActionUrl = $"/Maintenances/Details/{maintenance.Id}",
                    IconClass = "fas fa-exclamation-triangle text-warning",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
            catch
            {
                // La notificación no forma parte del guardado crítico de L-8.
            }
        }

        private void LoadLists(int facultyId = 0, int labId = 0, int equipUnitId = 0, int? technicianId = null, int? requestId = null)
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name), "Id", "Name", facultyId);

            if (facultyId > 0)
            {
                ViewData["LaboratoryId"] = new SelectList(_context.Laboratories
                    .Where(l => l.FacultyId == facultyId)
                    .ToList(), "Id", "Name", labId);
            }
            else
            {
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            if (labId > 0)
            {
                ViewData["EquipmentUnitId"] = new SelectList(_context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Where(u => u.LaboratoryId == labId)
                    .Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" })
                    .ToList(), "Id", "Name", equipUnitId);
            }
            else
            {
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            // Fix: FullName is NotMapped and causes LINQ translation errors in TPT.
            var people = _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .ToList();

            var technicianList = people
                .Select(p => new { 
                    Id = p.Id, 
                    Name = p is Intern i ? i.Name : (p is Extern e ? e.Name : "Técnico #" + p.Id) 
                })
                .OrderBy(x => x.Name)
                .ToList();

            ViewData["TechnicianId"] = new SelectList(technicianList, "Id", "Name", technicianId);

            ViewData["MaintenanceType"] = EnumHelper.GetStatusSelectList<MaintenanceType>();
            ViewData["ServiceType"] = EnumHelper.GetStatusSelectList<ServiceType>();
            var requests = _context.Requests
                .Include(r => r.Laboratory)
                .OrderByDescending(r => r.CreatedDate)
                .Take(20)
                .ToList()
                .Select(r => new {
                    Id = r.Id,
                    DisplayText = $"#{r.Id} - {r.Laboratory?.Name} ({r.CreatedDate:dd/MM}): " + (r.Description.Length > 40 ? r.Description.Substring(0, 40) + "..." : r.Description)
                });
            ViewData["RequestId"] = new SelectList(requests, "Id", "DisplayText", requestId);
        }
    }
}
