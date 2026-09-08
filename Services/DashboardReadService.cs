using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public interface IDashboardReadService
    {
        Task<DashboardReadResult> LoadAsync(
            int managementId,
            ManagementType managementType,
            DashboardReadFilter filter,
            bool includeWizard,
            int? selectedLabId,
            int currentStep = 0,
            int currentPage = 1);
    }

    public class DashboardReadFilter
    {
        public int? LabFilterId { get; set; }
        public int? CategoryFilterId { get; set; }
        public int? TechFilterId { get; set; }
        public string? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
        public string? SerialNumber { get; set; }
        public string? InventoryNumber { get; set; }
    }

    public class DashboardReadResult
    {
        public int TotalActivos { get; set; }
        public int EquiposTerminados { get; set; }
        public int TotalVencidos { get; set; }
        public double GlobalProgress { get; set; }
        public int CountL6 { get; set; }
        public int CountL7 { get; set; }
        public int CountL8 { get; set; }
        public int CountSalida { get; set; }
        public int CountDesembolso { get; set; }
        public int CountPendientes { get; set; }
        public int CountBuenos { get; set; }
        public Dictionary<string, int> TopEquipmentTypes { get; set; } = new();
        public Dictionary<string, int> TopGroups { get; set; } = new();
        public Dictionary<string, int> TopLaboratories { get; set; } = new();
        public IList<ManagementPlan> ManagementPlans { get; set; } = new List<ManagementPlan>();
        public IList<DashboardOverduePlanDto> OverduePlans { get; set; } = new List<DashboardOverduePlanDto>();
        public List<ManagementPlan> ActivePlans { get; set; } = new();
        public List<ManagementPlan> Step1Plans { get; set; } = new();
        public List<ManagementPlan> Step2Plans { get; set; } = new();
        public List<ManagementPlan> Step3Plans { get; set; } = new();
        public List<ManagementPlan> Step4Plans { get; set; } = new();
        public List<ManagementPlan> Step5Plans { get; set; } = new();
        public List<ManagementPlan> Step6Plans { get; set; } = new();
        public List<ManagementPlan> CompletedPlans { get; set; } = new();

        public int ActiveTotalItems { get; set; }
        public int Step1TotalItems { get; set; }
        public int Step2TotalItems { get; set; }
        public int Step3TotalItems { get; set; }
        public int Step4TotalItems { get; set; }
        public int Step5TotalItems { get; set; }
        public int Step6TotalItems { get; set; }
        public int CompletedTotalItems { get; set; }

        public List<Step1LabSummary> Step1Labs { get; set; } = new();
    }

    public class Step1LabSummary
    {
        public int LabId { get; set; }
        public string LabName { get; set; } = string.Empty;
        public int PendingCount { get; set; }
    }

    public class DashboardOverduePlanDto
    {
        public int Id { get; set; }
        public DateTime? PlannedDate { get; set; }
        public ManagementPlanStatus PlanStatus { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string LaboratoryName { get; set; } = string.Empty;
    }

    public class DashboardReadService : IDashboardReadService
    {
        private readonly ApplicationDbContext _context;

        public DashboardReadService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardReadResult> LoadAsync(
            int managementId,
            ManagementType managementType,
            DashboardReadFilter filter,
            bool includeWizard,
            int? selectedLabId,
            int currentStep = 0,
            int currentPage = 1)
        {
            var result = new DashboardReadResult();

            var statsQuery = _context.ManagementPlans
                .AsNoTracking()
                .Where(p => p.ManagementId == managementId && p.EquipmentUnit != null);

            var allStats = await statsQuery.Select(p => new
            {
                p.PlanStatus,
                p.CurrentPhase,
                p.CurrentState,
                p.PlannedDate,
                p.VerificationId,
                p.IsDraft,
                p.DraftPhase
            }).ToListAsync();

            result.TotalActivos = allStats.Count;
            result.EquiposTerminados = allStats.Count(p => IsCompletedPlan(p.PlanStatus, p.CurrentState));

            var now = DateTime.Now;
            result.CountBuenos = allStats.Count(p => p.CurrentState == WizardEquipmentState.VerifiedGood);
            result.TotalVencidos = allStats.Count(p => IsActivePipelinePlan(p.PlanStatus, p.CurrentState)
                && p.PlannedDate.HasValue
                && p.PlannedDate < now);
            result.GlobalProgress = result.TotalActivos > 0
                ? Math.Round((double)(result.CountBuenos + result.EquiposTerminados) / result.TotalActivos * 100, 1)
                : 0;

            result.CountPendientes = allStats.Count(p => IsActivePipelinePlan(p.PlanStatus, p.CurrentState)
                && p.CurrentState >= WizardEquipmentState.AwaitingRequest);

            result.CountL6 = managementType == ManagementType.Corrective
                ? 0
                : allStats.Count(p => IsActivePipelinePlan(p.PlanStatus, p.CurrentState)
                    && p.CurrentPhase == WizardPhase.Verification
                    && (p.CurrentState == WizardEquipmentState.PendingVerification || (p.IsDraft && p.DraftPhase == WizardPhase.Verification)));
            result.CountL7 = allStats.Count(p => IsActivePipelinePlan(p.PlanStatus, p.CurrentState) && p.CurrentPhase == WizardPhase.TechnicalRequest);
            result.CountL8 = allStats.Count(p => IsActivePipelinePlan(p.PlanStatus, p.CurrentState) && p.CurrentPhase == WizardPhase.Maintenance);
            result.CountSalida = allStats.Count(p => IsActivePipelinePlan(p.PlanStatus, p.CurrentState) && p.CurrentPhase == WizardPhase.Exit);
            result.CountDesembolso = allStats.Count(p => IsActivePipelinePlan(p.PlanStatus, p.CurrentState) && p.CurrentPhase == WizardPhase.Disbursement);

            var dataQuery = _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Equipment)
                .Include(p => p.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                .Include(p => p.Verification).ThenInclude(v => v!.CheckResults)
                .Include(p => p.TechnicalRequest)
                .Include(p => p.AcquisitionRequest)
                .Include(p => p.Departure)
                .Where(p => p.ManagementId == managementId && p.EquipmentUnit != null);

            await LoadEquipmentChartsAsync(result, managementId);
            result.OverduePlans = await LoadOverduePlansAsync(dataQuery, now);
            result.ManagementPlans = await LoadCronogramaAsync(dataQuery, filter);

            if (includeWizard)
            {
                await LoadWizardPlansAsync(result, dataQuery, filter, selectedLabId, currentStep, currentPage);
            }

            return result;
        }

        private async Task LoadEquipmentChartsAsync(DashboardReadResult result, int managementId)
        {
            var equipmentStats = await _context.ManagementPlans
                .AsNoTracking()
                .Where(p => p.ManagementId == managementId
                    && p.EquipmentUnit != null
                    && p.EquipmentUnit.Equipment != null
                    && p.EquipmentUnit.Equipment.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed)
                .Select(p => new
                {
                    Category = p.EquipmentUnit!.Equipment!.Category,
                    TypeClass = p.EquipmentUnit!.Equipment!.TypeClassification,
                    UtensilType = p.EquipmentUnit!.Equipment!.UtensilType,
                    OtherDetail = p.EquipmentUnit!.Equipment!.OtherClassificationDetail,
                    LabName = p.EquipmentUnit!.Laboratory != null
                        ? ((p.EquipmentUnit.Laboratory.Code ?? "") + " - " + p.EquipmentUnit.Laboratory.Name)
                        : "N/A"
                })
                .ToListAsync();

            result.TopEquipmentTypes = equipmentStats
                .GroupBy(p =>
                    p.Category == EquipmentCategory.Equipment && p.TypeClass.HasValue
                        ? EnumHelper.GetDisplayName(p.TypeClass.Value)
                        : p.Category == EquipmentCategory.Utensil && p.UtensilType.HasValue
                            ? EnumHelper.GetDisplayName(p.UtensilType.Value)
                            : p.OtherDetail ?? "Otro confirmado")
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());

            result.TopGroups = equipmentStats
                .GroupBy(p => EnumHelper.GetDisplayName(p.Category))
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());

            result.TopLaboratories = equipmentStats
                .Where(p => p.LabName != "N/A")
                .GroupBy(p => p.LabName)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private static async Task<IList<DashboardOverduePlanDto>> LoadOverduePlansAsync(
            IQueryable<ManagementPlan> dataQuery,
            DateTime now)
        {
            return await dataQuery
                .Where(p => p.CurrentState != WizardEquipmentState.VerifiedGood
                    && p.CurrentState != WizardEquipmentState.Completed
                    && p.PlanStatus != ManagementPlanStatus.Completed
                    && p.PlannedDate.HasValue
                    && p.PlannedDate < now)
                .OrderBy(p => p.PlannedDate)
                .Select(p => new DashboardOverduePlanDto
                {
                    Id = p.Id,
                    PlannedDate = p.PlannedDate,
                    PlanStatus = p.PlanStatus,
                    EquipmentName = p.EquipmentUnit!.Equipment!.Name,
                    LaboratoryName = p.EquipmentUnit.Laboratory != null ? p.EquipmentUnit.Laboratory.Name : "N/A"
                })
                .ToListAsync();
        }

        private static async Task<IList<ManagementPlan>> LoadCronogramaAsync(
            IQueryable<ManagementPlan> dataQuery,
            DashboardReadFilter filter)
        {
            var cronogramaQuery = dataQuery;

            if (filter.LabFilterId.HasValue)
            {
                cronogramaQuery = cronogramaQuery.Where(p => p.EquipmentUnit!.LaboratoryId == filter.LabFilterId.Value);
            }

            if (filter.CategoryFilterId.HasValue)
            {
                cronogramaQuery = cronogramaQuery.Where(p => p.EquipmentUnit!.Equipment!.Category == (EquipmentCategory)filter.CategoryFilterId.Value);
            }

            if (filter.TechFilterId.HasValue)
            {
                cronogramaQuery = cronogramaQuery.Where(p => p.Maintenance!.TechnicianId == filter.TechFilterId.Value);
            }

            if (!string.IsNullOrEmpty(filter.StatusFilter) && filter.StatusFilter != "Todos")
            {
                if (filter.StatusFilter == "Externo")
                {
                    cronogramaQuery = cronogramaQuery.Where(p => p.Maintenance!.ServiceType == ServiceType.External);
                }
                else if (Enum.TryParse<ManagementPlanStatus>(filter.StatusFilter, out var statusEnum))
                {
                    cronogramaQuery = cronogramaQuery.Where(p => p.PlanStatus == statusEnum);
                }
            }

            return await cronogramaQuery
                .Where(p => p.PlannedDate.HasValue)
                .OrderBy(p => p.PlannedDate)
                .Take(10)
                .ToListAsync();
        }

        private static async Task LoadWizardPlansAsync(
            DashboardReadResult result,
            IQueryable<ManagementPlan> dataQuery,
            DashboardReadFilter filter,
            int? selectedLabId,
            int currentStep,
            int currentPage)
        {
            var wizardQuery = dataQuery;

            // --- Filtro por laboratorio ---
            if (selectedLabId.HasValue)
            {
                wizardQuery = wizardQuery.Where(p => p.EquipmentUnit!.LaboratoryId == selectedLabId.Value);
            }

            // --- Filtros de texto ---
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                wizardQuery = wizardQuery.Where(p => p.EquipmentUnit!.Equipment!.Name.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(filter.SerialNumber))
            {
                var serial = filter.SerialNumber.Trim();
                wizardQuery = wizardQuery.Where(p => (p.EquipmentUnit!.SerialNumber ?? string.Empty).Contains(serial));
            }

            if (!string.IsNullOrWhiteSpace(filter.InventoryNumber))
            {
                var inv = filter.InventoryNumber.Trim();
                wizardQuery = wizardQuery.Where(p => p.EquipmentUnit!.InventoryNumber.Contains(inv));
            }

            // --- Cargar todos los planes filtrados en memoria ---
            var allPlans = await wizardQuery.ToListAsync();
            result.ActivePlans = allPlans;

            // --- Particionar por paso (totales para paginación) ---
            var step1All = allPlans
                .Where(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.Verification
                    && (p.CurrentState == WizardEquipmentState.PendingVerification || (p.IsDraft && p.DraftPhase == WizardPhase.Verification)))
                .ToList();
            var step2All = allPlans.Where(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.TechnicalRequest).ToList();
            var step3All = allPlans.Where(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.Maintenance).ToList();
            var step4All = allPlans.Where(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.Exit).ToList();
            var step5All = allPlans.Where(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.Kardex).ToList();
            var step6All = allPlans.Where(p => IsActivePipelinePlan(p) && p.CurrentPhase == WizardPhase.Disbursement).ToList();
            var completedAll = allPlans
                .Where(p => IsCompletedPlan(p))
                .OrderByDescending(p => p.LastModifiedDate ?? p.CreatedDate)
                .ToList();

            // --- Totales ---
            result.ActiveTotalItems = allPlans.Count;
            result.Step1TotalItems = step1All.Count;
            result.Step1Labs = step1All
                .Where(p => p.EquipmentUnit?.Laboratory != null)
                .GroupBy(p => new { Id = p.EquipmentUnit!.LaboratoryId!.Value, Name = p.EquipmentUnit!.Laboratory!.Name })
                .Select(g => new Step1LabSummary
                {
                    LabId = g.Key.Id,
                    LabName = g.Key.Name,
                    PendingCount = g.Count()
                })
                .OrderBy(l => l.LabName)
                .ToList();
            result.Step2TotalItems = step2All.Count;
            result.Step3TotalItems = step3All.Count;
            result.Step4TotalItems = step4All.Count;
            result.Step5TotalItems = step5All.Count;
            result.Step6TotalItems = step6All.Count;
            result.CompletedTotalItems = completedAll.Count;

            // --- Paginación: solo el paso actual ---
            const int pageSize = 5;
            var skip = (currentPage - 1) * pageSize;

            switch (currentStep)
            {
                case 0:
                    result.ActivePlans = allPlans.Skip(skip).Take(pageSize).ToList();
                    break;
                case 1:
                    result.Step1Plans = step1All.Skip(skip).Take(pageSize).ToList();
                    break;
                case 2:
                    result.Step2Plans = step2All.Skip(skip).Take(pageSize).ToList();
                    break;
                case 3:
                    result.Step3Plans = step3All.Skip(skip).Take(pageSize).ToList();
                    break;
                case 4:
                    result.Step4Plans = step4All.Skip(skip).Take(pageSize).ToList();
                    break;
                case 5:
                    result.Step5Plans = step5All.Skip(skip).Take(pageSize).ToList();
                    break;
                case 6:
                    result.Step6Plans = step6All.Skip(skip).Take(pageSize).ToList();
                    break;
                case 7:
                    result.CompletedPlans = completedAll.Skip(skip).Take(pageSize).ToList();
                    break;
            }
        }

        private static bool IsCompletedPlan(ManagementPlan plan)
        {
            return IsCompletedPlan(plan.PlanStatus, plan.CurrentState);
        }

        private static bool IsCompletedPlan(ManagementPlanStatus planStatus, WizardEquipmentState currentState)
        {
            return currentState == WizardEquipmentState.Completed ||
                   planStatus == ManagementPlanStatus.Completed;
        }

        private static bool IsActivePipelinePlan(ManagementPlan plan)
        {
            return IsActivePipelinePlan(plan.PlanStatus, plan.CurrentState);
        }

        private static bool IsActivePipelinePlan(ManagementPlanStatus planStatus, WizardEquipmentState currentState)
        {
            return currentState != WizardEquipmentState.VerifiedGood &&
                   !IsCompletedPlan(planStatus, currentState);
        }
    }
}
