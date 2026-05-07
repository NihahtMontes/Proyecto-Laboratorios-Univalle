using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WizardRollbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WizardRollbackController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("redo")]
        public async Task<IActionResult> RedoPhase([FromBody] RollbackRequestDto request)
        {
            if (request == null || request.PlanId <= 0)
                return BadRequest("Solicitud inválida.");

            var plan = await _context.ManagementPlans
                .Include(p => p.Verification)
                .Include(p => p.TechnicalRequest)
                .Include(p => p.Maintenance)
                .Include(p => p.Departure)
                .Include(p => p.AcquisitionRequest)
                .FirstOrDefaultAsync(p => p.Id == request.PlanId);

            if (plan == null) return NotFound("Plan no encontrado.");

            if (plan.IsReadOnly)
                return BadRequest("La gestión está cerrada, no se pueden alterar los registros.");

            // 1. CASCADA INVERSA: Anulación con Soft Delete
            // Se anulan los registros dependiendo de qué tan atrás quiere ir el usuario.
            
            if (request.TargetPhase <= WizardPhase.Disbursement && plan.AcquisitionRequest != null)
            {
                plan.AcquisitionRequest.Status = RequestStatus.Cancelled;
                plan.AcquisitionRequestId = null;
            }

            if (request.TargetPhase <= WizardPhase.Exit && plan.Departure != null)
            {
                plan.Departure.Status = LoanStatus.Cancelled;
                plan.DepartureId = null;
            }

            if (request.TargetPhase <= WizardPhase.Maintenance && plan.Maintenance != null)
            {
                plan.Maintenance.Status = MaintenanceStatus.Cancelled;
                plan.MaintenanceId = null;
            }

            if (request.TargetPhase <= WizardPhase.TechnicalRequest && plan.TechnicalRequest != null)
            {
                plan.TechnicalRequest.Status = RequestStatus.Cancelled;
                plan.RequestId = null;
            }

            if (request.TargetPhase <= WizardPhase.Verification && plan.Verification != null)
            {
                plan.Verification.Status = VerificationStatus.Annulled;
                plan.VerificationId = null;
            }

            // 2. REPOSICIONAMIENTO DEL WIZARD
            plan.CurrentPhase = request.TargetPhase;
            
            switch (request.TargetPhase)
            {
                case WizardPhase.Verification:
                    plan.CurrentState = WizardEquipmentState.PendingVerification;
                    break;
                case WizardPhase.TechnicalRequest:
                    plan.CurrentState = WizardEquipmentState.AwaitingRequest;
                    break;
                case WizardPhase.Maintenance:
                    plan.CurrentState = WizardEquipmentState.AwaitingMaintenance;
                    break;
                case WizardPhase.Exit:
                    plan.CurrentState = WizardEquipmentState.AwaitingDeparture;
                    break;
                case WizardPhase.Kardex:
                    plan.CurrentState = WizardEquipmentState.AwaitingKardex;
                    break;
                default:
                    plan.CurrentState = WizardEquipmentState.PendingVerification;
                    break;
            }

            plan.PlanStatus = ManagementPlanStatus.Pending;

            // 3. RESTAURAR ESTADO DEL EQUIPO
            // Si el plan tiene un EquipmentUnit asociado, revertir su CurrentStatus
            // al estado que corresponde a la fase target
            if (plan.EquipmentUnitId.HasValue)
            {
                var unit = await _context.EquipmentUnits.FindAsync(plan.EquipmentUnitId.Value);
                if (unit != null)
                {
                    unit.CurrentStatus = request.TargetPhase switch
                    {
                        WizardPhase.Verification => EquipmentStatus.Operational,
                        WizardPhase.TechnicalRequest => EquipmentStatus.OutOfService,
                        WizardPhase.Maintenance => EquipmentStatus.UnderMaintenance,
                        WizardPhase.Exit => EquipmentStatus.Operational,
                        WizardPhase.Kardex => EquipmentStatus.Operational,
                        WizardPhase.Disbursement => EquipmentStatus.Operational,
                        _ => EquipmentStatus.Operational
                    };
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Progreso revertido con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno de base de datos: {ex.Message}");
            }
        }
    }

    public class RollbackRequestDto
    {
        public int PlanId { get; set; }
        public WizardPhase TargetPhase { get; set; }
    }
}
