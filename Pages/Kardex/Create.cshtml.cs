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
using System.Security.Claims;

namespace Proyecto_Laboratorios_Univalle.Pages.Kardex
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        public string EquipmentTitle { get; set; } = "Equipo";
        public string LaboratoryTitle { get; set; } = "Laboratorio";
        public string InventoryTitle { get; set; } = "S/N";
        public DateTime? DepartureDate { get; set; }
        public int? LinkedDepartureId { get; set; }
        public int? LinkedMaintenanceId { get; set; }

        public class InputModel
        {
            public int MaintenanceId { get; set; }
            public int EquipmentUnitId { get; set; }
            public int FacultyId { get; set; }
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "El tipo de mantenimiento es obligatorio")]
            [Display(Name = "Tipo de Mantenimiento")]
            public MaintenanceType MaintenanceType { get; set; }

            [Required(ErrorMessage = "El técnico responsable es obligatorio para cerrar Kardex")]
            [Display(Name = "Técnico Responsable")]
            public int? TechnicianId { get; set; }

            [Required(ErrorMessage = "La fecha programada es obligatoria")]
            [DataType(DataType.Date)]
            public DateTime? ScheduledDate { get; set; }

            [Required(ErrorMessage = "La fecha real de inicio es obligatoria")]
            [DataType(DataType.DateTime)]
            public DateTime? StartDate { get; set; }

            [Required(ErrorMessage = "La fecha real de finalización es obligatoria")]
            [DataType(DataType.DateTime)]
            public DateTime? EndDate { get; set; }

            [Required(ErrorMessage = "La fecha real de devolución L-3 es obligatoria")]
            [DataType(DataType.Date)]
            public DateTime? ActualReturnDate { get; set; }

            [Required(ErrorMessage = "La descripción del trabajo realizado es obligatoria")]
            [StringLength(2000)]
            public string Description { get; set; } = string.Empty;

            [Required(ErrorMessage = "El próximo mantenimiento sugerido es obligatorio")]
            [DataType(DataType.Date)]
            public DateTime? SuggestedNextMaintenanceDate { get; set; }

            [Required(ErrorMessage = "El nivel de satisfacción es obligatorio")]
            public MaintenanceSatisfaction? SatisfactionLevel { get; set; }

            [Required(ErrorMessage = "Las recomendaciones son obligatorias")]
            [StringLength(1000)]
            public string? Recommendations { get; set; }

            [Required(ErrorMessage = "Las observaciones finales son obligatorias")]
            [StringLength(1000)]
            public string? Observations { get; set; }

            public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Completed;
            public decimal ActualCost { get; set; }
            public int CompletionPercentage { get; set; }
            public bool Step1_Cleaning { get; set; }
            public bool Step2_Calibration { get; set; }
            public bool Step3_Testing { get; set; }
            public bool Step4_FinalReview { get; set; }
            public List<CostDetail> CostDetails { get; set; } = new();
            public List<MaintenanceTask> Tasks { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync(int? equipmentUnitId = null, bool isWizard = false, int? managementPlanId = null)
        {
            ManagementPlanId = managementPlanId;
            ViewData["IsWizard"] = isWizard;

            var plan = await ResolvePlanAsync(equipmentUnitId);
            if (plan == null)
            {
                TempData.Error("No se encontró el plan de gestión para cerrar Kardex.");
                return RedirectToPage("/Index");
            }

            PopulateInputFromPlan(plan);
            PopulateViewData(plan);
            await LoadListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDraftAsync(bool isWizard = false)
        {
            var prepared = await PreparePostAsync(isWizard, clearFinalValidation: true);
            if (prepared.Plan == null) return prepared.Result!;

            var plan = prepared.Plan;

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                SyncPartialMaintenance(plan, markCompleted: false);
                MarkDraft(plan, "Borrador Kardex guardado con avance parcial del cierre técnico.");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData.Success("Borrador de Kardex guardado. Puede continuar después sin avanzar a desembolso.");

                if (isWizard)
                {
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = 5, SelectedLabId = Input.LaboratoryId, ManagementId = plan.ManagementId });
                }

                return RedirectToPage("/Index", new { ManagementId = plan.ManagementId });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                TempData.Error($"Error al guardar borrador de Kardex: {detail}");
                await LoadListsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCloseAsync(bool isWizard = false)
        {
            var prepared = await PreparePostAsync(isWizard, clearFinalValidation: false);
            if (prepared.Plan == null) return prepared.Result!;

            var plan = prepared.Plan;
            var validationMessages = ValidateClosure(plan);

            if (!ModelState.IsValid)
            {
                var failedKeys = validationMessages
                    .Concat(ModelState
                    .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                    .SelectMany(ms => ms.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? $"{ms.Key}: valor inválido." : e.ErrorMessage)))
                    .Distinct()
                    .ToList();

                TempData.Error("No se pudo cerrar Kardex: " + string.Join(" | ", failedKeys));
                await LoadListsAsync();
                return Page();
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var maintenance = plan.Maintenance!;
                var departure = plan.Departure;
                var unit = plan.EquipmentUnit!;
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                SyncPartialMaintenance(plan, markCompleted: true);

                if (departure != null)
                {
                    departure.Status = LoanStatus.Returned;
                    departure.ActualReturnDate = Input.ActualReturnDate;
                    departure.ReturnObservations = "Equipo devuelto y validado en cierre Kardex.";
                    departure.LastModifiedDate = DateTime.UtcNow;
                    if (int.TryParse(userIdString, out var duid)) departure.ModifiedById = duid;
                }

                var historyDate = Input.ActualReturnDate!.Value;
                var lastHistory = await _context.EquipmentStateHistories
                    .Where(h => h.EquipmentUnitId == unit.Id && h.EndDate == null)
                    .OrderByDescending(h => h.StartDate)
                    .AsTracking()
                    .FirstOrDefaultAsync();

                var existingKardexHistory = plan.KardexHistoryId.HasValue
                    ? await _context.EquipmentStateHistories
                        .AsTracking()
                        .FirstOrDefaultAsync(h => h.Id == plan.KardexHistoryId.Value)
                    : null;

                if (lastHistory != null && lastHistory.Id != existingKardexHistory?.Id)
                {
                    lastHistory.EndDate = historyDate;
                }

                var kardexHistory = existingKardexHistory ?? new EquipmentStateHistory
                {
                    EquipmentUnitId = unit.Id,
                    Status = EquipmentStatus.Operational,
                    StartDate = historyDate,
                    Reason = BuildKardexReason(maintenance),
                    CreatedDate = DateTime.UtcNow,
                    CreatedById = int.TryParse(userIdString, out var huid) ? huid : null
                };

                if (existingKardexHistory == null)
                {
                    _context.EquipmentStateHistories.Add(kardexHistory);
                }
                else
                {
                    kardexHistory.Status = EquipmentStatus.Operational;
                    kardexHistory.StartDate = historyDate;
                    kardexHistory.EndDate = null;
                    kardexHistory.Reason = BuildKardexReason(maintenance);
                }

                unit.CurrentStatus = EquipmentStatus.Operational;

                await _context.SaveChangesAsync();

                plan.KardexHistoryId = kardexHistory.Id;
                plan.CurrentPhase = WizardPhase.Disbursement;
                plan.CurrentState = WizardEquipmentState.AwaitingDisbursement;
                plan.PlanStatus = ManagementPlanStatus.InProgress;
                ClearDraft(plan);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData.Success("Mantenimiento concluido correctamente. El registro quedó guardado en el Kardex histórico y el equipo avanza a Desembolso.");

                if (isWizard)
                {
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = 6, SelectedLabId = Input.LaboratoryId, ManagementId = plan.ManagementId });
                }

                return RedirectToPage("/Index", new { ManagementId = plan.ManagementId });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                TempData.Error($"Error al cerrar Kardex: {detail}");
                await LoadListsAsync();
                return Page();
            }
        }

        private async Task<(ManagementPlan? Plan, IActionResult? Result)> PreparePostAsync(bool isWizard, bool clearFinalValidation)
        {
            var plan = await ResolvePlanAsync(Input.EquipmentUnitId);
            if (plan == null)
            {
                TempData.Error($"No se pudo resolver el plan de Kardex. ManagementPlanId recibido: {ManagementPlanId?.ToString() ?? "sin valor"}.");
                ViewData["IsWizard"] = isWizard;
                await LoadListsAsync();
                return (null, Page());
            }

            ViewData["IsWizard"] = isWizard;
            PopulateViewData(plan);
            RebuildServerTruth(plan);

            if (clearFinalValidation)
            {
                ClearClosureModelState();
            }

            Input.CostDetails = NormalizeCostDetails(Input.CostDetails);
            Input.Tasks = NormalizeTasks(Input.Tasks);
            Input.CompletionPercentage = CalculateCompletionPercentage(Input.Tasks);

            if (plan.Maintenance == null || plan.EquipmentUnit == null)
            {
                TempData.Error("No se pudo guardar Kardex: el plan no tiene mantenimiento o unidad física vinculada.");
                await LoadListsAsync();
                return (null, Page());
            }

            return (plan, null);
        }

        private void ClearClosureModelState()
        {
            ModelState.Remove("Input.TechnicianId");
            ModelState.Remove("Input.ScheduledDate");
            ModelState.Remove("Input.StartDate");
            ModelState.Remove("Input.EndDate");
            ModelState.Remove("Input.ActualReturnDate");
            ModelState.Remove("Input.Description");
            ModelState.Remove("Input.SuggestedNextMaintenanceDate");
            ModelState.Remove("Input.SatisfactionLevel");
            ModelState.Remove("Input.Recommendations");
            ModelState.Remove("Input.Observations");
            ModelState.Remove("Input.CostDetails");
            ModelState.Remove("Input.Tasks");
            RemoveModelStatePrefix("Input.CostDetails[");
            RemoveModelStatePrefix("Input.Tasks[");
        }

        private void RemoveModelStatePrefix(string prefix)
        {
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                ModelState.Remove(key);
            }
        }

        private void SyncPartialMaintenance(ManagementPlan plan, bool markCompleted)
        {
            var maintenance = plan.Maintenance!;
            var totalCosts = Input.CostDetails.Sum(d => d.Quantity * d.UnitPrice);

            maintenance.MaintenanceType = Input.MaintenanceType;
            maintenance.TechnicianId = Input.TechnicianId;
            maintenance.ScheduledDate = Input.ScheduledDate;
            maintenance.StartDate = Input.StartDate;
            maintenance.EndDate = Input.EndDate;
            maintenance.Description = Input.Description.Clean();
            maintenance.SuggestedNextMaintenanceDate = Input.SuggestedNextMaintenanceDate;
            maintenance.SatisfactionLevel = Input.SatisfactionLevel;
            maintenance.Recommendations = Input.Recommendations.Clean();
            maintenance.Observations = Input.Observations.Clean();
            maintenance.ActualCost = totalCosts;
            maintenance.Status = markCompleted ? MaintenanceStatus.Completed : MaintenanceStatus.InProgress;
            maintenance.CompletionPercentage = Input.CompletionPercentage;
            UpdateLegacySteps(maintenance, Input.Tasks);
            maintenance.LastModifiedDate = DateTime.UtcNow;

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out var uid))
            {
                maintenance.ModifiedById = uid;
            }

            SyncCostDetails(maintenance, Input.CostDetails);
            SyncTasks(maintenance, Input.Tasks);

            if (!markCompleted && plan.Departure != null && Input.ActualReturnDate.HasValue)
            {
                plan.Departure.ActualReturnDate = Input.ActualReturnDate;
                plan.Departure.LastModifiedDate = DateTime.UtcNow;
            }
        }

        private static void MarkDraft(ManagementPlan plan, string summary)
        {
            plan.IsDraft = true;
            plan.DraftPhase = WizardPhase.Kardex;
            plan.DraftSavedAt = DateTime.UtcNow;
            plan.DraftSummary = summary;
            plan.CurrentPhase = WizardPhase.Kardex;
            plan.CurrentState = WizardEquipmentState.AwaitingKardex;
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

        private async Task<ManagementPlan?> ResolvePlanAsync(int? equipmentUnitId)
        {
            IQueryable<ManagementPlan> query = _context.ManagementPlans
                .AsTracking()
                .Include(p => p.Maintenance).ThenInclude(m => m!.CostDetails)
                .Include(p => p.Maintenance).ThenInclude(m => m!.Tasks)
                .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                .Include(p => p.Departure)
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory);

            if (ManagementPlanId.HasValue)
            {
                return await query.FirstOrDefaultAsync(p => p.Id == ManagementPlanId.Value);
            }

            if (equipmentUnitId.HasValue)
            {
                return await query
                    .Where(p => p.EquipmentUnitId == equipmentUnitId.Value && p.CurrentPhase == WizardPhase.Kardex)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync();
            }

            return null;
        }

        private void PopulateInputFromPlan(ManagementPlan plan)
        {
            var maintenance = plan.Maintenance;
            var unit = plan.EquipmentUnit;
            var departure = plan.Departure;

            Input = new InputModel
            {
                MaintenanceId = maintenance?.Id ?? 0,
                EquipmentUnitId = unit?.Id ?? plan.EquipmentUnitId ?? 0,
                LaboratoryId = unit?.LaboratoryId ?? 0,
                FacultyId = unit?.Laboratory?.FacultyId ?? 0,
                MaintenanceType = maintenance?.MaintenanceType ?? MaintenanceType.Otros,
                TechnicianId = maintenance?.TechnicianId,
                ScheduledDate = maintenance?.ScheduledDate,
                StartDate = maintenance?.StartDate,
                EndDate = maintenance?.EndDate,
                ActualReturnDate = departure?.ActualReturnDate,
                Description = maintenance?.Description ?? string.Empty,
                SuggestedNextMaintenanceDate = maintenance?.SuggestedNextMaintenanceDate,
                SatisfactionLevel = maintenance?.SatisfactionLevel,
                Recommendations = maintenance?.Recommendations,
                Observations = maintenance?.Observations,
                Status = MaintenanceStatus.Completed,
                ActualCost = maintenance?.ActualCost ?? 0m,
                CompletionPercentage = maintenance?.CompletionPercentage ?? 0,
                Step1_Cleaning = maintenance?.Step1_Cleaning ?? false,
                Step2_Calibration = maintenance?.Step2_Calibration ?? false,
                Step3_Testing = maintenance?.Step3_Testing ?? false,
                Step4_FinalReview = maintenance?.Step4_FinalReview ?? false,
                CostDetails = maintenance?.CostDetails?.OrderBy(d => d.Id).ToList() ?? new List<CostDetail>(),
                Tasks = maintenance?.Tasks?.Where(t => !t.IsDeleted).OrderBy(t => t.Id).ToList() ?? new List<MaintenanceTask>()
            };

            if (!Input.Tasks.Any())
            {
                Input.Tasks = GetDefaultTasks();
            }

            Input.CompletionPercentage = CalculateCompletionPercentage(Input.Tasks);

            if (!Input.CostDetails.Any())
            {
                Input.CostDetails.Add(new CostDetail { Quantity = 1, UnitPrice = 0m, UnitOfMeasure = "Unidad" });
            }
        }

        private void PopulateViewData(ManagementPlan plan)
        {
            var unit = plan.EquipmentUnit;
            ViewData["CurrentPhaseInt"] = 5;
            ViewData["ManagementId"] = plan.ManagementId;
            ViewData["SelectedLabId"] = unit?.LaboratoryId;
            ViewData["LinkedDepartureId"] = plan.DepartureId;
            ViewData["LinkedMaintenanceId"] = plan.MaintenanceId;

            EquipmentTitle = unit?.Equipment?.Name ?? "Equipo";
            LaboratoryTitle = unit?.Laboratory?.Name ?? "Laboratorio";
            InventoryTitle = unit?.InventoryNumber ?? "S/N";
            DepartureDate = plan.Departure?.DepartureDate;
            LinkedDepartureId = plan.DepartureId;
            LinkedMaintenanceId = plan.MaintenanceId;
        }

        private void RebuildServerTruth(ManagementPlan plan)
        {
            Input.MaintenanceId = plan.MaintenanceId ?? 0;
            Input.EquipmentUnitId = plan.EquipmentUnitId ?? 0;
            Input.LaboratoryId = plan.EquipmentUnit?.LaboratoryId ?? 0;
            Input.FacultyId = plan.EquipmentUnit?.Laboratory?.FacultyId ?? 0;
            Input.Status = MaintenanceStatus.Completed;

            ModelState.Remove("Input.MaintenanceId");
            ModelState.Remove("Input.EquipmentUnitId");
            ModelState.Remove("Input.LaboratoryId");
            ModelState.Remove("Input.FacultyId");
            ModelState.Remove("Input.Status");
            ModelState.Remove("Input.ActualCost");
        }

        private List<string> ValidateClosure(ManagementPlan plan)
        {
            var messages = new List<string>();

            if (plan.Maintenance == null)
            {
                ModelState.AddModelError("Input.MaintenanceId", "El plan no tiene un mantenimiento L-8 vinculado.");
                messages.Add("El plan no tiene un mantenimiento L-8 vinculado.");
                return messages;
            }

            if (!Input.TechnicianId.HasValue)
            {
                ModelState.AddModelError("Input.TechnicianId", "Falta técnico responsable.");
                messages.Add("Falta técnico responsable.");
            }

            if (!Input.StartDate.HasValue)
            {
                ModelState.AddModelError("Input.StartDate", "Falta inicio real.");
                messages.Add("Falta inicio real.");
            }

            if (!Input.EndDate.HasValue)
            {
                ModelState.AddModelError("Input.EndDate", "Falta finalización real.");
                messages.Add("Falta finalización real.");
            }

            if (!Input.ActualReturnDate.HasValue)
            {
                ModelState.AddModelError("Input.ActualReturnDate", "Falta devolución real L-3.");
                messages.Add("Falta devolución real L-3.");
            }

            if (string.IsNullOrWhiteSpace(Input.Description))
            {
                ModelState.AddModelError("Input.Description", "Falta descripción de trabajos realizados.");
                messages.Add("Falta descripción de trabajos realizados.");
            }

            if (!Input.SuggestedNextMaintenanceDate.HasValue)
            {
                ModelState.AddModelError("Input.SuggestedNextMaintenanceDate", "Falta próximo mantenimiento sugerido.");
                messages.Add("Falta próximo mantenimiento sugerido.");
            }

            if (!Input.SatisfactionLevel.HasValue)
            {
                ModelState.AddModelError("Input.SatisfactionLevel", "Falta nivel de satisfacción.");
                messages.Add("Falta nivel de satisfacción.");
            }

            if (string.IsNullOrWhiteSpace(Input.Recommendations))
            {
                ModelState.AddModelError("Input.Recommendations", "Faltan recomendaciones.");
                messages.Add("Faltan recomendaciones.");
            }

            if (string.IsNullOrWhiteSpace(Input.Observations))
            {
                ModelState.AddModelError("Input.Observations", "Faltan observaciones finales.");
                messages.Add("Faltan observaciones finales.");
            }

            if (plan.Departure != null && Input.ActualReturnDate.HasValue && Input.ActualReturnDate.Value.Date < plan.Departure.DepartureDate.Date)
            {
                ModelState.AddModelError("Input.ActualReturnDate", "La devolución no puede ser anterior a la salida L-3.");
                messages.Add("La devolución L-3 no puede ser anterior a la fecha de salida.");
            }

            if (plan.Departure == null)
            {
                ModelState.AddModelError("Input.ActualReturnDate", "El plan no tiene salida L-3 vinculada.");
                messages.Add("El plan no tiene salida L-3 vinculada.");
            }

            if (Input.StartDate.HasValue && Input.EndDate.HasValue && Input.EndDate.Value < Input.StartDate.Value)
            {
                ModelState.AddModelError("Input.EndDate", "La finalización no puede ser anterior al inicio real.");
                messages.Add("La finalización no puede ser anterior al inicio real.");
            }

            if (Input.EndDate.HasValue && Input.SuggestedNextMaintenanceDate.HasValue && Input.SuggestedNextMaintenanceDate.Value.Date <= Input.EndDate.Value.Date)
            {
                ModelState.AddModelError("Input.SuggestedNextMaintenanceDate", "El próximo mantenimiento debe ser posterior a la fecha de finalización.");
                messages.Add("El próximo mantenimiento debe ser posterior a la finalización.");
            }

            if (!Input.CostDetails.Any())
            {
                ModelState.AddModelError("Input.CostDetails", "Debe registrar al menos un costo o insumo real.");
                messages.Add("Debe registrar al menos un costo real.");
            }
            else if (Input.CostDetails.Sum(d => d.Quantity * d.UnitPrice) <= 0)
            {
                ModelState.AddModelError("Input.CostDetails", "El costo real total debe ser mayor a 0.");
                messages.Add("El costo real total debe ser mayor a 0.");
            }

            if (!Input.Tasks.Any())
            {
                ModelState.AddModelError("Input.Tasks", "Debe registrar tareas L-48.");
                messages.Add("Debe registrar tareas L-48.");
            }
            else
            {
                if (Input.Tasks.Any(t => string.IsNullOrWhiteSpace(t.Description)))
                {
                    ModelState.AddModelError("Input.Tasks", "Todas las tareas L-48 deben tener descripción.");
                    messages.Add("Todas las tareas L-48 deben tener descripción.");
                }

                if (Input.Tasks.Any(t => !t.IsCompleted) || Input.CompletionPercentage != 100)
                {
                    ModelState.AddModelError("Input.Tasks", "Todas las tareas L-48 deben estar completadas.");
                    messages.Add("Todas las tareas L-48 deben estar completadas.");
                }
            }

            return messages;
        }

        private static List<CostDetail> NormalizeCostDetails(List<CostDetail>? details)
        {
            return (details ?? new List<CostDetail>())
                .Where(d => !string.IsNullOrWhiteSpace(d.Concept))
                .Select(d =>
                {
                    d.Concept = d.Concept.Trim();
                    d.Description = d.Description?.Trim();
                    d.Quantity = d.Quantity <= 0 ? 1 : d.Quantity;
                    d.UnitPrice = d.UnitPrice < 0 ? 0 : d.UnitPrice;
                    d.UnitOfMeasure = string.IsNullOrWhiteSpace(d.UnitOfMeasure) ? "Unidad" : d.UnitOfMeasure.Trim();
                    return d;
                })
                .ToList();
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

        private static List<MaintenanceTask> GetDefaultTasks()
        {
            return new List<MaintenanceTask>
            {
                new() { Description = "Limpieza y desinfección" },
                new() { Description = "Calibración y ajustes" },
                new() { Description = "Pruebas de funcionamiento" },
                new() { Description = "Revisión final de seguridad" }
            };
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

            foreach (var detailForm in inputDetails)
            {
                detailForm.MaintenanceId = maintenance.Id;
                detailForm.RequestId = null;

                var existingDetail = maintenance.CostDetails.FirstOrDefault(d => d.Id == detailForm.Id && d.Id != 0);
                if (existingDetail != null)
                {
                    existingDetail.Concept = detailForm.Concept;
                    existingDetail.Description = detailForm.Description;
                    existingDetail.Quantity = detailForm.Quantity;
                    existingDetail.UnitOfMeasure = detailForm.UnitOfMeasure;
                    existingDetail.UnitPrice = detailForm.UnitPrice;
                    existingDetail.Category = detailForm.Category;
                    existingDetail.Provider = detailForm.Provider;
                    existingDetail.InvoiceNumber = detailForm.InvoiceNumber;
                    existingDetail.LastModifiedDate = DateTime.UtcNow;
                }
                else
                {
                    detailForm.CreatedDate = DateTime.UtcNow;
                    maintenance.CostDetails.Add(detailForm);
                }
            }
        }

        private static void SyncTasks(Maintenance maintenance, List<MaintenanceTask> inputTasks)
        {
            var inputIds = inputTasks.Where(t => t.Id > 0).Select(t => t.Id).ToHashSet();
            foreach (var existingTask in maintenance.Tasks.Where(t => t.Id > 0 && !t.IsDeleted && !inputIds.Contains(t.Id)).ToList())
            {
                existingTask.IsDeleted = true;
                existingTask.IsCompleted = false;
            }

            foreach (var taskForm in inputTasks)
            {
                var existingTask = maintenance.Tasks.FirstOrDefault(t => t.Id == taskForm.Id && t.Id != 0);
                if (existingTask != null)
                {
                    existingTask.Description = taskForm.Description;
                    existingTask.IsCompleted = taskForm.IsCompleted;
                    existingTask.IsDeleted = false;
                }
                else if (!string.IsNullOrWhiteSpace(taskForm.Description))
                {
                    maintenance.Tasks.Add(new MaintenanceTask
                    {
                        Description = taskForm.Description,
                        IsCompleted = taskForm.IsCompleted,
                        IsDeleted = false
                    });
                }
            }
        }

        private static void UpdateLegacySteps(Maintenance maintenance, List<MaintenanceTask> tasks)
        {
            maintenance.Step1_Cleaning = tasks.ElementAtOrDefault(0)?.IsCompleted ?? false;
            maintenance.Step2_Calibration = tasks.ElementAtOrDefault(1)?.IsCompleted ?? false;
            maintenance.Step3_Testing = tasks.ElementAtOrDefault(2)?.IsCompleted ?? false;
            maintenance.Step4_FinalReview = tasks.ElementAtOrDefault(3)?.IsCompleted ?? false;
        }

        private static string BuildKardexReason(Maintenance maintenance)
        {
            return $"Kardex cierre L-8 #{maintenance.Id}: {maintenance.Description?.Trim()} | Costo Bs {(maintenance.ActualCost ?? 0m):N2}";
        }

        private async Task LoadListsAsync()
        {
            ViewData["FacultyId"] = new SelectList(await _context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name).ToListAsync(), "Id", "Name", Input.FacultyId);

            if (Input.FacultyId > 0)
                ViewData["LaboratoryId"] = new SelectList(await _context.Laboratories.Where(l => l.FacultyId == Input.FacultyId).OrderBy(l => l.Name).ToListAsync(), "Id", "Name", Input.LaboratoryId);
            else
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            if (Input.LaboratoryId > 0)
                ViewData["EquipmentUnitId"] = new SelectList(await _context.EquipmentUnits.Include(u => u.Equipment).Where(u => u.LaboratoryId == Input.LaboratoryId).Select(u => new { Id = u.Id, Name = u.Equipment!.Name + " (" + u.InventoryNumber + ")" }).ToListAsync(), "Id", "Name", Input.EquipmentUnitId);
            else
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            var technicians = await _context.People
                .Where(p => p.Status == GeneralStatus.Activo)
                .ToListAsync();

            ViewData["TechnicianId"] = new SelectList(technicians.OrderBy(p => p.FullName).Select(p => new { p.Id, p.FullName }), "Id", "FullName", Input.TechnicianId);
            ViewData["MaintenanceType"] = EnumHelper.GetStatusSelectList<MaintenanceType>();
        }
    }
}
