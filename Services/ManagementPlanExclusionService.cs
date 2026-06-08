using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public interface IManagementPlanExclusionService
    {
        Task<ManagementPlanExclusionResult> ExcludeUnitAsync(int managementId, int equipmentUnitId, int? userId = null);
        Task<Dictionary<int, bool>> GetPlanHistoryMapAsync(int managementId, IEnumerable<int> equipmentUnitIds);
    }

    public class ManagementPlanExclusionResult
    {
        public bool Excluded { get; set; }
        public bool HadHistory { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ManagementPlanExclusionService : IManagementPlanExclusionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManagementPlanExclusionService> _logger;

        public ManagementPlanExclusionService(ApplicationDbContext context, ILogger<ManagementPlanExclusionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Dictionary<int, bool>> GetPlanHistoryMapAsync(int managementId, IEnumerable<int> equipmentUnitIds)
        {
            var unitIds = equipmentUnitIds.Distinct().ToList();
            if (!unitIds.Any())
            {
                return new Dictionary<int, bool>();
            }

            var plans = await _context.ManagementPlans
                .AsNoTracking()
                .Where(p => p.ManagementId == managementId
                    && p.EquipmentUnitId.HasValue
                    && unitIds.Contains(p.EquipmentUnitId.Value))
                .Select(p => new
                {
                    UnitId = p.EquipmentUnitId!.Value,
                    HasHistory = p.VerificationId.HasValue
                        || p.RequestId.HasValue
                        || p.MaintenanceId.HasValue
                        || p.DepartureId.HasValue
                        || p.KardexHistoryId.HasValue
                        || p.AcquisitionRequestId.HasValue
                        || p.IsDraft
                })
                .ToListAsync();

            return plans
                .GroupBy(p => p.UnitId)
                .ToDictionary(g => g.Key, g => g.Any(p => p.HasHistory));
        }

        public async Task<ManagementPlanExclusionResult> ExcludeUnitAsync(int managementId, int equipmentUnitId, int? userId = null)
        {
            var plan = await _context.ManagementPlans
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync(p => p.ManagementId == managementId && p.EquipmentUnitId == equipmentUnitId);

            if (plan == null)
            {
                return new ManagementPlanExclusionResult
                {
                    Excluded = false,
                    Message = "La unidad no estaba planificada en esta gestion."
                };
            }

            var now = DateTime.UtcNow;
            var hadHistory = HasPlanHistory(plan);

            if (plan.VerificationId.HasValue)
            {
                await CancelVerificationAsync(plan.VerificationId.Value, userId, now);
            }

            if (plan.RequestId.HasValue)
            {
                await CancelRequestAsync(plan.RequestId.Value, userId, now);
            }

            if (plan.MaintenanceId.HasValue)
            {
                await CancelMaintenanceAsync(plan.MaintenanceId.Value, userId, now);
            }

            if (plan.DepartureId.HasValue)
            {
                await CancelDepartureAsync(plan.DepartureId.Value, userId, now);
            }

            if (plan.AcquisitionRequestId.HasValue)
            {
                await CancelRequestAsync(plan.AcquisitionRequestId.Value, userId, now);
            }

            await RestoreUnitIfWorkflowOwnedAsync(equipmentUnitId, userId, now);

            plan.EquipmentUnitId = null;
            plan.VerificationId = null;
            plan.RequestId = null;
            plan.MaintenanceId = null;
            plan.DepartureId = null;
            plan.KardexHistoryId = null;
            plan.AcquisitionRequestId = null;
            plan.IsDraft = false;
            plan.DraftPhase = null;
            plan.DraftSavedAt = null;
            plan.DraftSummary = null;
            plan.PlannedWeek = null;
            plan.ExecutedWeek = null;
            plan.CurrentPhase = WizardPhase.Verification;
            plan.CurrentState = WizardEquipmentState.PendingVerification;
            plan.PlanStatus = ManagementPlanStatus.Completed;
            plan.LastModifiedDate = now;
            plan.ModifiedById = userId;
            plan.Notes = AppendExclusionNote(plan.Notes, equipmentUnitId, hadHistory, now);

            _logger.LogInformation(
                "Unidad {EquipmentUnitId} excluida logicamente de la gestion {ManagementId}. Historial asociado: {HadHistory}",
                equipmentUnitId,
                managementId,
                hadHistory);

            return new ManagementPlanExclusionResult
            {
                Excluded = true,
                HadHistory = hadHistory,
                Message = hadHistory
                    ? "Se excluyo la unidad y se cancelaron logicamente sus registros del wizard."
                    : "Se excluyo la unidad del plan."
            };
        }

        private static bool HasPlanHistory(ManagementPlan plan)
        {
            return plan.VerificationId.HasValue
                || plan.RequestId.HasValue
                || plan.MaintenanceId.HasValue
                || plan.DepartureId.HasValue
                || plan.KardexHistoryId.HasValue
                || plan.AcquisitionRequestId.HasValue
                || plan.IsDraft;
        }

        private async Task CancelVerificationAsync(int verificationId, int? userId, DateTime now)
        {
            var verification = await _context.Verifications
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync(v => v.Id == verificationId);

            if (verification != null)
            {
                verification.Status = VerificationStatus.Annulled;
                verification.ModifiedById = userId;
                verification.LastModifiedDate = now;
                verification.Observations = AppendNote(verification.Observations, "Verificacion anulada por exclusion del activo del plan L-48.", now);
            }

            var faults = await _context.VerificationFaults
                .IgnoreQueryFilters()
                .AsTracking()
                .Where(f => f.VerificationId == verificationId)
                .ToListAsync();

            foreach (var fault in faults)
            {
                fault.IsDeleted = true;
                fault.ModifiedById = userId;
                fault.LastModifiedDate = now;
            }
        }

        private async Task CancelRequestAsync(int requestId, int? userId, DateTime now)
        {
            var request = await _context.Requests
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return;
            }

            request.Status = RequestStatus.Cancelled;
            request.ModifiedById = userId;
            request.LastModifiedDate = now;
            request.RejectionReason = AppendNote(request.RejectionReason, "Solicitud cancelada por exclusion del activo del plan L-48.", now);
        }

        private async Task CancelMaintenanceAsync(int maintenanceId, int? userId, DateTime now)
        {
            var maintenance = await _context.Maintenances
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Id == maintenanceId);

            if (maintenance != null)
            {
                maintenance.Status = MaintenanceStatus.Cancelled;
                maintenance.ModifiedById = userId;
                maintenance.LastModifiedDate = now;
                maintenance.Observations = AppendNote(maintenance.Observations, "Mantenimiento cancelado por exclusion del activo del plan L-48.", now);
            }

            var tasks = await _context.MaintenanceTasks
                .IgnoreQueryFilters()
                .AsTracking()
                .Where(t => t.MaintenanceId == maintenanceId)
                .ToListAsync();

            foreach (var task in tasks)
            {
                task.IsDeleted = true;
            }
        }

        private async Task CancelDepartureAsync(int departureId, int? userId, DateTime now)
        {
            var departure = await _context.Departures
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync(d => d.Id == departureId);

            if (departure != null)
            {
                departure.Status = LoanStatus.Cancelled;
                departure.ModifiedById = userId;
                departure.LastModifiedDate = now;
                departure.ReturnObservations = AppendNote(departure.ReturnObservations, "Salida cancelada por exclusion del activo del plan L-48.", now);
            }

            var items = await _context.DepartureItems
                .IgnoreQueryFilters()
                .AsTracking()
                .Where(i => i.DepartureId == departureId)
                .ToListAsync();

            foreach (var item in items)
            {
                item.IsRemoved = true;
                item.ModifiedById = userId;
                item.LastModifiedDate = now;
            }
        }

        private async Task RestoreUnitIfWorkflowOwnedAsync(int equipmentUnitId, int? userId, DateTime now)
        {
            var unit = await _context.EquipmentUnits
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync(u => u.Id == equipmentUnitId);

            if (unit == null)
            {
                return;
            }

            if (unit.CurrentStatus is EquipmentStatus.UnderMaintenance or EquipmentStatus.InRepair or EquipmentStatus.OnLoan)
            {
                unit.CurrentStatus = EquipmentStatus.Operational;
                unit.ModifiedById = userId;
                unit.LastModifiedDate = now;
                unit.Notes = AppendNote(unit.Notes, "Estado restaurado por exclusion del activo del plan L-48.", now);
            }
        }

        private static string AppendExclusionNote(string? current, int equipmentUnitId, bool hadHistory, DateTime now)
        {
            var suffix = hadHistory ? "con cancelacion logica de registros asociados" : "sin registros asociados";
            return AppendNote(current, $"Unidad fisica ID {equipmentUnitId} excluida de la planificacion L-48 ({suffix}).", now);
        }

        private static string AppendNote(string? current, string note, DateTime now)
        {
            var stampedNote = $"[{now:yyyy-MM-dd HH:mm}] {note}";
            if (string.IsNullOrWhiteSpace(current))
            {
                return stampedNote;
            }

            return current.Length > 350
                ? current[..350] + Environment.NewLine + stampedNote
                : current + Environment.NewLine + stampedNote;
        }
    }
}
