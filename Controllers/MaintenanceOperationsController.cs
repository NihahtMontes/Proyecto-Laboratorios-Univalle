using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using TechnicalRequest = Proyecto_Laboratorios_Univalle.Models.Request;

namespace Proyecto_Laboratorios_Univalle.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api")]
    public class MaintenanceOperationsController : ControllerBase
    {
        private const int CriticalToleranceDays = 7;
        private const int RequiredTechnicalChecklistItems = 13;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public MaintenanceOperationsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("notificaciones/equipos")]
        public async Task<IActionResult> GetEquipmentNotifications([FromQuery] int? managementId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var today = DateTime.UtcNow.Date;
            var plans = ActivePlans(managementId);

            var unread = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.ActionUrl, n.IconClass, n.CreatedAt))
                .ToListAsync();

            var cards = new[]
            {
                new EquipmentStatusCardDto("retraso", "Equipos con mantenimiento retrasado",
                    await plans.CountAsync(p => p.PlannedDate.HasValue && p.PlannedDate.Value.Date < today)),
                new EquipmentStatusCardDto("vencido", "Equipos criticos vencidos",
                    await plans.CountAsync(p => p.PlannedDate.HasValue && p.PlannedDate.Value.Date < today.AddDays(-CriticalToleranceDays))),
                new EquipmentStatusCardDto("l7", "Requerimientos tecnicos pendientes",
                    await plans.CountAsync(p => p.CurrentPhase == WizardPhase.TechnicalRequest)),
                new EquipmentStatusCardDto("l48-planeado", "Cronograma planificado",
                    await plans.CountAsync(p => p.PlannedWeek.HasValue && !p.ExecutedWeek.HasValue)),
                new EquipmentStatusCardDto("l48-ejecutado", "Cronograma ejecutado",
                    await plans.CountAsync(p => p.ExecutedWeek.HasValue))
            };

            return Ok(new { cards, notifications = unread });
        }

        [HttpGet("equipos")]
        public async Task<IActionResult> GetEquipmentByStatus([FromQuery] string? estado = null, [FromQuery] int? managementId = null)
        {
            var today = DateTime.UtcNow.Date;
            IQueryable<ManagementPlan> query = ActivePlans(managementId)
                .Include(p => p.EquipmentUnit)!.ThenInclude(u => u!.Equipment)
                .Include(p => p.EquipmentUnit)!.ThenInclude(u => u!.Laboratory);

            query = estado?.Trim().ToLowerInvariant() switch
            {
                "retraso" => query.Where(p => p.PlannedDate.HasValue && p.PlannedDate.Value.Date < today),
                "vencido" => query.Where(p => p.PlannedDate.HasValue && p.PlannedDate.Value.Date < today.AddDays(-CriticalToleranceDays)),
                _ => query
            };

            var result = await query
                .OrderBy(p => p.PlannedDate)
                .Select(p => new EquipmentStatusDto(
                    p.Id,
                    p.EquipmentUnitId,
                    p.EquipmentUnit!.InventoryNumber,
                    p.EquipmentUnit.Equipment!.Name,
                    p.EquipmentUnit.Laboratory != null ? p.EquipmentUnit.Laboratory.Name : "N/A",
                    p.PlannedDate,
                    p.PlanStatus.ToString()))
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost("requerimientos-tecnicos")]
        public async Task<IActionResult> CreateTechnicalRequest([FromBody] TechnicalRequestCreateDto input)
        {
            if (!Enum.IsDefined(input.Priority))
                return BadRequest("Debe seleccionar una prioridad valida para la solicitud L-7.");

            if (string.IsNullOrWhiteSpace(input.Description))
                return BadRequest("La descripcion del problema es obligatoria.");

            if (!input.TechnicalChecklistConfirmed || input.TechnicalChecklistItems.Count < RequiredTechnicalChecklistItems)
                return BadRequest("Debe confirmar el checklist tecnico obligatorio de 13 puntos.");

            var managementExists = await _context.Managements
                .AsNoTracking()
                .AnyAsync(m => m.Id == input.ManagementId);

            if (!managementExists)
                return BadRequest("La gestion indicada no existe o no esta activa.");

            var unit = await _context.EquipmentUnits
                .AsNoTracking()
                .Include(u => u.Equipment)
                .FirstOrDefaultAsync(u => u.Id == input.EquipmentUnitId);

            if (unit == null || unit.Equipment == null)
                return BadRequest("La unidad de equipo indicada no existe.");

            if (!unit.LaboratoryId.HasValue)
                return BadRequest("La unidad de equipo no tiene laboratorio asignado.");

            var hasPendingRequest = await _context.Requests
                .AsNoTracking()
                .AnyAsync(r => r.Type == RequestType.Technical
                    && r.ManagementId == input.ManagementId
                    && r.EquipmentUnitId == input.EquipmentUnitId
                    && r.Status == RequestStatus.Pending);

            if (hasPendingRequest)
                return Conflict("Ya existe una solicitud L-7 pendiente para este equipo en la misma gestion.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var request = new TechnicalRequest
            {
                ManagementId = input.ManagementId,
                LaboratoryId = unit.LaboratoryId.Value,
                EquipmentId = unit.EquipmentId,
                EquipmentUnitId = unit.Id,
                Type = RequestType.Technical,
                Description = input.Description.Trim(),
                Priority = input.Priority,
                Observations = input.Observations?.Trim(),
                EstimatedRepairTime = input.EstimatedRepairTime?.Trim(),
                Status = RequestStatus.Pending,
                RequestedById = currentUser?.Id,
                CreatedById = currentUser?.Id,
                CreatedDate = DateTime.UtcNow
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            if (input.ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans
                    .AsTracking()
                    .FirstOrDefaultAsync(p => p.Id == input.ManagementPlanId.Value
                        && p.ManagementId == input.ManagementId
                        && p.EquipmentUnitId == input.EquipmentUnitId);

                if (plan != null)
                {
                    plan.RequestId = request.Id;
                    plan.CurrentPhase = WizardPhase.Maintenance;
                    plan.CurrentState = WizardEquipmentState.AwaitingMaintenance;
                    plan.PlanStatus = ManagementPlanStatus.InProgress;
                    await _context.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();
            return Created($"/api/requerimientos-tecnicos/{request.Id}", new { request.Id });
        }

        [HttpGet("equipos/{id:int}/requerimientos")]
        public async Task<IActionResult> GetEquipmentTechnicalRequests(int id)
        {
            var result = await _context.Requests
                .AsNoTracking()
                .Where(r => r.EquipmentUnitId == id && r.Type == RequestType.Technical)
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new TechnicalRequestDto(
                    r.Id,
                    r.ManagementId,
                    r.Description,
                    r.Priority.ToString(),
                    r.Status.ToString(),
                    r.Observations,
                    r.EstimatedRepairTime,
                    r.CreatedDate))
                .ToListAsync();

            return Ok(result);
        }

        [HttpPut("requerimientos-tecnicos/{id:int}")]
        public async Task<IActionResult> UpdateTechnicalRequest(int id, [FromBody] TechnicalRequestUpdateDto input)
        {
            if (!Enum.IsDefined(input.Priority))
                return BadRequest("Debe seleccionar una prioridad valida para la solicitud L-7.");

            if (string.IsNullOrWhiteSpace(input.Description))
                return BadRequest("La descripcion del problema es obligatoria.");

            var request = await _context.Requests
                .AsTracking()
                .FirstOrDefaultAsync(r => r.Id == id && r.Type == RequestType.Technical);

            if (request == null) return NotFound();

            if (request.Status != RequestStatus.Pending)
                return BadRequest("Solo se puede editar una solicitud L-7 antes de su aprobacion o asignacion.");

            var currentUser = await _userManager.GetUserAsync(User);
            request.Description = input.Description.Trim();
            request.Priority = input.Priority;
            request.Observations = input.Observations?.Trim();
            request.EstimatedRepairTime = input.EstimatedRepairTime?.Trim();
            request.ModifiedById = currentUser?.Id;
            request.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { request.Id });
        }

        [HttpGet("cronograma")]
        public async Task<IActionResult> GetSchedule([FromQuery] string estado = "todos", [FromQuery] int? managementId = null)
        {
            var query = ScheduleQuery(managementId);

            query = estado.Trim().ToLowerInvariant() switch
            {
                "planeado" => query.Where(p => p.PlannedWeek.HasValue && !p.ExecutedWeek.HasValue),
                "progreso" => query.Where(p => p.Maintenance != null && p.Maintenance.Status == MaintenanceStatus.InProgress),
                "ejecutado" => query.Where(p => p.ExecutedWeek.HasValue || (p.Maintenance != null && p.Maintenance.Status == MaintenanceStatus.Completed)),
                "externo" => query.Where(p => p.Maintenance != null && p.Maintenance.ServiceType == ServiceType.External),
                _ => query
            };

            var result = await query
                .OrderBy(p => p.PlannedWeek)
                .ThenBy(p => p.PlannedDate)
                .Select(p => new ScheduleItemDto(
                    p.Id,
                    p.ManagementId,
                    p.EquipmentUnitId,
                    p.EquipmentUnit != null ? p.EquipmentUnit.InventoryNumber : null,
                    p.EquipmentUnit != null && p.EquipmentUnit.Equipment != null ? p.EquipmentUnit.Equipment.Name : "N/A",
                    p.PlannedWeek,
                    p.ExecutedWeek,
                    p.PlannedDate,
                    p.Maintenance != null ? p.Maintenance.Status.ToString() : p.PlanStatus.ToString(),
                    p.Maintenance != null && p.Maintenance.ServiceType == ServiceType.External))
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("cronograma/semanal")]
        public async Task<IActionResult> GetWeeklySchedule([FromQuery] int? managementId = null)
        {
            var items = await ScheduleQuery(managementId)
                .Select(p => new
                {
                    p.Id,
                    p.PlannedWeek,
                    p.ExecutedWeek,
                    IsExternal = p.Maintenance != null && p.Maintenance.ServiceType == ServiceType.External,
                    IsInProgress = p.Maintenance != null && p.Maintenance.Status == MaintenanceStatus.InProgress
                })
                .ToListAsync();

            var result = items
                .SelectMany(p => new[]
                {
                    new { Week = p.PlannedWeek, Kind = "planeado", p.IsExternal, p.IsInProgress },
                    new { Week = p.ExecutedWeek, Kind = "ejecutado", p.IsExternal, p.IsInProgress }
                })
                .Where(p => p.Week.HasValue)
                .GroupBy(p => p.Week!.Value)
                .OrderBy(g => g.Key)
                .Select(g => new WeeklyScheduleDto(
                    $"S{g.Key}",
                    g.Key,
                    g.Count(x => x.Kind == "planeado"),
                    g.Count(x => x.Kind == "ejecutado"),
                    g.Count(x => x.IsInProgress),
                    g.Count(x => x.IsExternal)))
                .ToList();

            return Ok(result);
        }

        [HttpPost("cronograma/{id:int}/validar-estado")]
        public async Task<IActionResult> ValidateScheduleState(int id, [FromBody] ValidateScheduleStateDto input)
        {
            var plan = await _context.ManagementPlans
                .AsTracking()
                .Include(p => p.Maintenance)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null) return NotFound();

            var normalizedState = input.Estado.Trim().ToLowerInvariant();
            if (normalizedState == "ejecutado")
            {
                var executedWeek = input.Semana ?? plan.ExecutedWeek;

                if (!plan.PlannedWeek.HasValue)
                    return BadRequest("Debe planificar primero antes de marcar como ejecutado.");

                if (!executedWeek.HasValue)
                    return BadRequest("Debe indicar la semana ejecutada.");

                if (executedWeek.Value < plan.PlannedWeek.Value)
                    return BadRequest("La semana ejecutada no puede ser menor que la semana planificada.");

                var hasClosureEvidence = plan.RequestId.HasValue
                    || !string.IsNullOrWhiteSpace(plan.DocumentReference)
                    || !string.IsNullOrWhiteSpace(input.DocumentReference)
                    || !string.IsNullOrWhiteSpace(plan.Maintenance?.Description);

                if (!hasClosureEvidence)
                    return BadRequest("Debe adjuntar informe tecnico de cierre o tener requerimiento origen.");

                if (input.ApplyChange)
                {
                    plan.ExecutedWeek = executedWeek.Value;
                    if (!string.IsNullOrWhiteSpace(input.DocumentReference))
                    {
                        plan.DocumentReference = input.DocumentReference.Trim();
                    }
                }
            }

            if (input.ApplyChange)
                await _context.SaveChangesAsync();

            return Ok(new { valid = true });
        }

        private IQueryable<ManagementPlan> ActivePlans(int? managementId)
        {
            var query = _context.ManagementPlans
                .AsNoTracking()
                .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.EquipmentUnitId != null);

            if (managementId.HasValue)
                query = query.Where(p => p.ManagementId == managementId.Value);

            return query;
        }

        private IQueryable<ManagementPlan> ScheduleQuery(int? managementId)
        {
            IQueryable<ManagementPlan> query = _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.EquipmentUnit)!.ThenInclude(u => u!.Equipment)
                .Include(p => p.Maintenance)
                .Where(p => p.EquipmentUnitId != null);

            if (managementId.HasValue)
                query = query.Where(p => p.ManagementId == managementId.Value);

            return query;
        }

        public sealed record EquipmentStatusCardDto(string Key, string Title, int Count);
        public sealed record NotificationDto(int Id, string Title, string Message, string? Url, string? Icon, DateTime CreatedAt);
        public sealed record EquipmentStatusDto(int PlanId, int? EquipmentUnitId, string? InventoryNumber, string EquipmentName, string LaboratoryName, DateTime? PlannedDate, string Status);
        public sealed record TechnicalRequestDto(int Id, int ManagementId, string Description, string Priority, string Status, string? Observations, string? EstimatedRepairTime, DateTime CreatedDate);
        public sealed record ScheduleItemDto(int Id, int ManagementId, int? EquipmentUnitId, string? InventoryNumber, string EquipmentName, int? PlannedWeek, int? ExecutedWeek, DateTime? PlannedDate, string Status, bool IsExternal);
        public sealed record WeeklyScheduleDto(string WeekLabel, int WeekNumber, int Planned, int Executed, int InProgress, int External);

        public sealed class TechnicalRequestCreateDto
        {
            public int ManagementId { get; set; }
            public int EquipmentUnitId { get; set; }
            public int? ManagementPlanId { get; set; }
            public string Description { get; set; } = string.Empty;
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;
            public string? Observations { get; set; }
            public string? EstimatedRepairTime { get; set; }
            public bool TechnicalChecklistConfirmed { get; set; }
            public List<string> TechnicalChecklistItems { get; set; } = new();
        }

        public sealed class TechnicalRequestUpdateDto
        {
            public string Description { get; set; } = string.Empty;
            public RequestPriority Priority { get; set; } = RequestPriority.Medium;
            public string? Observations { get; set; }
            public string? EstimatedRepairTime { get; set; }
        }

        public sealed class ValidateScheduleStateDto
        {
            public string Estado { get; set; } = string.Empty;
            public int? Semana { get; set; }
            public string? DocumentReference { get; set; }
            public bool ApplyChange { get; set; }
        }
    }
}
