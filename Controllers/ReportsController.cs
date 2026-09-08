using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportService reportService,
            ApplicationDbContext context,
            ICurrentUserService currentUser,
            ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        private async Task<string> GetCurrentUserFullName()
        {
            if (!_currentUser.UserId.HasValue) return "Sistema";
            var user = await _context.Users
                .Where(u => u.Id == _currentUser.UserId.Value)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync();

            return user != null
                ? $"{user.FirstName} {user.LastName}".Trim()
                : "Sistema";
        }

        [HttpGet("download/l6")]
        public async Task<IActionResult> DownloadL6(int managementId, int labId, DateTime? date = null)
        {
            try
            {
                var management = await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == managementId);
                if (management == null) return NotFound("Gestión no encontrada.");
                if (management.Type == ManagementType.Corrective) return BadRequest("L-6 solo aplica al proceso preventivo.");

                var responsable = await GetCurrentUserFullName();
                var bytes = await _reportService.GenerateL6VerificacionExcel(managementId, labId, responsable, date);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    date.HasValue
                        ? $"Verificacion_L6_{management.Code}_Lab_{labId}_{date.Value:yyyyMMdd}.xlsx"
                        : $"Verificacion_L6_{management.Code}_Lab_{labId}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar L-6. ManagementId={ManagementId}; LabId={LabId}", managementId, labId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-6. Intente nuevamente o contacte al administrador.");
            }
        }

        [HttpGet("download/l3")]
        public async Task<IActionResult> DownloadL3(int? departureId, int? unitId, int? managementId)
        {
            try
            {
                var resolvedDepartureId = departureId;

                if (!resolvedDepartureId.HasValue && unitId.HasValue)
                {
                    var departureQuery = _context.Departures
                        .AsNoTracking()
                        .Where(d => d.EquipmentUnitId == unitId.Value);

                    if (managementId.HasValue)
                    {
                        departureQuery = departureQuery.Where(d => d.ManagementId == managementId.Value);
                    }

                    resolvedDepartureId = await departureQuery
                        .OrderByDescending(d => d.CreatedDate)
                        .Select(d => (int?)d.Id)
                        .FirstOrDefaultAsync();
                }

                if (!resolvedDepartureId.HasValue)
                    return NotFound("Este equipo no tiene salidas L-3 registradas en la gestión seleccionada.");

                var bytes = await _reportService.GenerateL3SalidaExcel(resolvedDepartureId.Value);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Salida_L3_{resolvedDepartureId.Value}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo generar L-3. DepartureId={DepartureId}; UnitId={UnitId}; ManagementId={ManagementId}",
                    departureId, unitId, managementId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-3. Intente nuevamente o contacte al administrador.");
            }
        }

        [HttpGet("download/l8")]
        public async Task<IActionResult> DownloadL8(int? managementPlanId, int? unitId, int? managementId)
        {
            try
            {
                if (!managementPlanId.HasValue && !unitId.HasValue)
                    return BadRequest("Debe indicar managementPlanId o unitId.");

                var bytes = await _reportService.GenerateL8KardexExcel(unitId ?? 0, managementPlanId, managementId);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Kardex_L8_{managementPlanId ?? unitId}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo generar L-8. ManagementPlanId={ManagementPlanId}; UnitId={UnitId}; ManagementId={ManagementId}",
                    managementPlanId, unitId, managementId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-8. Intente nuevamente o contacte al administrador.");
            }
        }

        [HttpGet("download/l48")]
        public async Task<IActionResult> DownloadL48(int managementId, int labId)
        {
            try
            {
                var management = await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == managementId);
                if (management == null) return NotFound("Gestión no encontrada.");

                var bytes = await _reportService.GenerateL48GanttExcel(managementId, labId);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Plan_Gantt_L48_{management.Code}_Lab_{labId}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar L-48. ManagementId={ManagementId}; LabId={LabId}", managementId, labId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-48. Intente nuevamente o contacte al administrador.");
            }
        }

        [HttpGet("download/l7")]
        public async Task<IActionResult> DownloadL7(int? requestId, int? unitId, int? managementId)
        {
            try
            {
                var requestQuery = _context.Requests
                    .AsNoTracking()
                    .Include(r => r.Laboratory)
                    .Include(r => r.Equipment)
                        .ThenInclude(e => e!.City)
                    .Include(r => r.Equipment)
                        .ThenInclude(e => e!.Country)
                    .Include(r => r.RequestedBy)
                    .Include(r => r.RequestedByPerson)
                    .Include(r => r.EquipmentUnit)
                        .ThenInclude(eu => eu!.Laboratory)
                    .Where(r => r.Type == RequestType.Technical);

                if (requestId.HasValue)
                {
                    requestQuery = requestQuery.Where(r => r.Id == requestId.Value);
                }
                else if (unitId.HasValue)
                {
                    requestQuery = requestQuery.Where(r => r.EquipmentUnitId == unitId.Value);
                    if (managementId.HasValue)
                    {
                        requestQuery = requestQuery.Where(r => r.ManagementId == managementId.Value);
                    }
                }
                else
                {
                    return BadRequest("Debe indicar requestId o unitId para L-7.");
                }

                var request = await requestQuery
                    .OrderByDescending(r => r.RequestDate ?? r.CreatedDate)
                    .FirstOrDefaultAsync();

                if (request == null)
                    return NotFound("Este equipo no tiene solicitudes L-7 registradas en la gestión seleccionada.");

                var bytes = await _reportService.GenerateSolicitudMantenimientoExcel(request);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Solicitud_L7_{request.Id}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo generar L-7. RequestId={RequestId}; UnitId={UnitId}; ManagementId={ManagementId}",
                    requestId, unitId, managementId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-7. Intente nuevamente o contacte al administrador.");
            }
        }

        [HttpGet("download/adquisicion")]
        public async Task<IActionResult> DownloadAdquisicion(int? requestId, int? unitId, int? managementId)
        {
            try
            {
                var requestQuery = _context.Requests
                    .AsNoTracking()
                    .Include(r => r.Laboratory)
                        .ThenInclude(l => l!.Faculty)
                    .Include(r => r.RequestedBy)
                    .Include(r => r.RequestedByPerson)
                    .Include(r => r.CostDetails)
                    .Where(r => r.Type == RequestType.Purchasing);

                if (requestId.HasValue)
                {
                    requestQuery = requestQuery.Where(r => r.Id == requestId.Value);
                }
                else if (unitId.HasValue)
                {
                    requestQuery = requestQuery.Where(r => r.EquipmentUnitId == unitId.Value);
                    if (managementId.HasValue)
                    {
                        requestQuery = requestQuery.Where(r => r.ManagementId == managementId.Value);
                    }
                }
                else
                {
                    return BadRequest("Debe indicar requestId o unitId para L-12.");
                }

                var request = await requestQuery
                    .OrderByDescending(r => r.RequestDate ?? r.CreatedDate)
                    .FirstOrDefaultAsync();

                if (request == null)
                    return NotFound("Este equipo no tiene solicitudes de adquisición en la gestión seleccionada.");

                var bytes = await _reportService.GenerateSolicitudAdquisicionExcel(request);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Adquisicion_{request.Id}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo generar L-12. RequestId={RequestId}; UnitId={UnitId}; ManagementId={ManagementId}",
                    requestId, unitId, managementId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-12. Intente nuevamente o contacte al administrador.");
            }
        }
    }
}
