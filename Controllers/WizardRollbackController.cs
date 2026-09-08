using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class WizardRollbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WizardRollbackController> _logger;

        public WizardRollbackController(
            ApplicationDbContext context,
            ILogger<WizardRollbackController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("redo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RedoPhase([FromBody] RollbackRequestDto request)
        {
            if (request == null || request.PlanId <= 0 ||
                !Enum.IsDefined(typeof(WizardPhase), request.TargetPhase))
            {
                return BadRequest("Solicitud inválida.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var plan = await _context.ManagementPlans
                    .AsTracking()
                    .Include(p => p.Management)
                    .Include(p => p.EquipmentUnit)
                    .Include(p => p.Verification)
                    .Include(p => p.TechnicalRequest)
                    .Include(p => p.Maintenance)
                    .Include(p => p.Departure)
                    .Include(p => p.KardexHistory)
                    .Include(p => p.AcquisitionRequest)
                    .FirstOrDefaultAsync(p => p.Id == request.PlanId);

                if (plan == null)
                    return NotFound("Plan no encontrado.");

                if (plan.IsReadOnly)
                    return BadRequest("La gestión está cerrada, no se pueden alterar los registros.");

                if (request.TargetPhase >= plan.CurrentPhase)
                    return BadRequest("Solo se puede rehacer una fase anterior a la fase actual.");

                if (plan.Management?.Type == ManagementType.Corrective &&
                    request.TargetPhase == WizardPhase.Verification)
                {
                    return BadRequest("La verificación L-6 no forma parte de una gestión correctiva.");
                }

                CancelDownstreamRecords(plan, request.TargetPhase);
                RepositionPlan(plan, request.TargetPhase);
                RestoreEquipmentState(plan, request.TargetPhase);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "Progreso revertido con éxito." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "No se pudo revertir el plan {PlanId} a la fase {TargetPhase}",
                    request.PlanId, request.TargetPhase);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo rehacer la fase. Intente nuevamente o contacte al administrador.");
            }
        }

        private void CancelDownstreamRecords(ManagementPlan plan, WizardPhase targetPhase)
        {
            if (targetPhase <= WizardPhase.Disbursement && plan.AcquisitionRequest != null)
            {
                plan.AcquisitionRequest.Status = RequestStatus.Cancelled;
                plan.AcquisitionRequestId = null;
            }

            if (targetPhase <= WizardPhase.Kardex && plan.KardexHistory != null)
            {
                if (!plan.KardexHistory.EndDate.HasValue)
                    plan.KardexHistory.EndDate = DateTime.UtcNow;

                plan.KardexHistoryId = null;
            }

            if (targetPhase <= WizardPhase.Exit && plan.Departure != null)
            {
                plan.Departure.Status = LoanStatus.Cancelled;
                plan.DepartureId = null;
            }

            if (targetPhase <= WizardPhase.Maintenance && plan.Maintenance != null)
            {
                plan.Maintenance.Status = MaintenanceStatus.Cancelled;
                plan.MaintenanceId = null;
            }

            if (targetPhase <= WizardPhase.TechnicalRequest && plan.TechnicalRequest != null)
            {
                plan.TechnicalRequest.Status = RequestStatus.Cancelled;
                plan.RequestId = null;
            }

            if (targetPhase <= WizardPhase.Verification && plan.Verification != null)
            {
                plan.Verification.Status = VerificationStatus.Annulled;
                plan.VerificationId = null;
            }
        }

        private static void RepositionPlan(ManagementPlan plan, WizardPhase targetPhase)
        {
            plan.CurrentPhase = targetPhase;
            plan.CurrentState = targetPhase switch
            {
                WizardPhase.Verification => WizardEquipmentState.PendingVerification,
                WizardPhase.TechnicalRequest => WizardEquipmentState.AwaitingRequest,
                WizardPhase.Maintenance => WizardEquipmentState.AwaitingMaintenance,
                WizardPhase.Exit => WizardEquipmentState.AwaitingDeparture,
                WizardPhase.Kardex => WizardEquipmentState.AwaitingKardex,
                WizardPhase.Disbursement => WizardEquipmentState.AwaitingDisbursement,
                _ => WizardEquipmentState.PendingVerification
            };

            plan.PlanStatus = ManagementPlanStatus.Pending;
            plan.IsDraft = false;
            plan.DraftPhase = null;
            plan.DraftSavedAt = null;
            plan.DraftSummary = null;
        }

        private void RestoreEquipmentState(ManagementPlan plan, WizardPhase targetPhase)
        {
            if (plan.EquipmentUnit == null)
                return;

            var restoredStatus = targetPhase switch
            {
                WizardPhase.TechnicalRequest => EquipmentStatus.OutOfService,
                WizardPhase.Maintenance => EquipmentStatus.UnderMaintenance,
                _ => EquipmentStatus.Operational
            };

            plan.EquipmentUnit.CurrentStatus = restoredStatus;

            if (plan.KardexHistoryId == null && targetPhase <= WizardPhase.Kardex)
            {
                _context.EquipmentStateHistories.Add(new EquipmentStateHistory
                {
                    EquipmentUnitId = plan.EquipmentUnit.Id,
                    Status = restoredStatus,
                    StartDate = DateTime.UtcNow,
                    Reason = $"Estado restaurado al rehacer la fase {targetPhase} del plan {plan.Id}.",
                    CreatedDate = DateTime.UtcNow
                });
            }
        }
    }

    public class RollbackRequestDto
    {
        public int PlanId { get; set; }
        public WizardPhase TargetPhase { get; set; }
    }
}
