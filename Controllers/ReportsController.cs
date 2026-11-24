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
        public async Task<IActionResult> DownloadL6(int managementId, int labId, DateTime? date = null, int? unitId = null)
        {
            try
            {
                var management = await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == managementId);
                if (management == null) return NotFound("Gestión no encontrada.");
                if (management.Type == ManagementType.Corrective) return BadRequest("L-6 solo aplica al proceso preventivo.");
                if (!await _context.Laboratories.AsNoTracking().AnyAsync(l => l.Id == labId))
                    return NotFound("Laboratorio no encontrado.");

                var responsable = await GetCurrentUserFullName();
                var bytes = await _reportService.GenerateL6VerificacionExcel(managementId, labId, responsable, date, unitId);
                var unitSuffix = string.Empty;
                if (unitId.HasValue)
                {
                    var inventory = await _context.EquipmentUnits.AsNoTracking()
                        .Where(u => u.Id == unitId.Value)
                        .Select(u => u.InventoryNumber)
                        .FirstOrDefaultAsync();
                    var safeInventory = new string((inventory ?? string.Empty)
                        .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
                    unitSuffix = $"_Inv_{(safeInventory.Length > 0 ? safeInventory : unitId.Value.ToString())}";
                }
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Verificacion_L6_{management.Code}_Lab_{labId}"
                    + unitSuffix
                    + (date.HasValue ? $"_{date.Value:yyyyMMdd}" : "") + ".xlsx");
            }
            catch (KeyNotFoundException)
            {
                return NotFound("No hay datos L-6 para este equipo en la gestión y laboratorio seleccionados.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar L-6. ManagementId={ManagementId}; LabId={LabId}", managementId, labId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-6. Intente nuevamente o contacte al administrador.");
            }
        }

        [HttpGet("download/l3")]
        public async Task<IActionResult> DownloadL3(int? departureId)
        {
            try
            {
                // Identificador exacto del flujo: no se adivina "la salida más reciente" del equipo.
                if (!departureId.HasValue)
                    return BadRequest("Debe indicar el departureId de la salida L-3 a descargar.");

                if (!await _context.Departures.AsNoTracking().AnyAsync(d => d.Id == departureId.Value))
                    return NotFound("Salida L-3 no encontrada.");

                var bytes = await _reportService.GenerateL3SalidaExcel(departureId.Value);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Salida_L3_{departureId.Value}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo generar L-3. DepartureId={DepartureId}",
                    departureId);
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
                if (managementPlanId.HasValue && !await _context.ManagementPlans.AsNoTracking().AnyAsync(p => p.Id == managementPlanId.Value))
                    return NotFound("Plan de mantenimiento no encontrado.");
                if (!managementPlanId.HasValue && !await _context.EquipmentUnits.AsNoTracking().AnyAsync(u => u.Id == unitId!.Value))
                    return NotFound("Unidad física no encontrada.");

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
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar L-48. ManagementId={ManagementId}; LabId={LabId}", managementId, labId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-48. Intente nuevamente o contacte al administrador.");
            }
        }

        /// <summary>
        /// Informe L-9 de reprogramaciones y reposiciones de la gestión académica. El rango opcional
        /// (from/to) filtra por la fecha original de la práctica, no por la fecha de registro.
        /// </summary>
        [HttpGet("download/l9")]
        public async Task<IActionResult> DownloadL9(int managementId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
                    return BadRequest("El rango de fechas no es válido: la fecha inicial supera la final.");

                var management = await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == managementId);
                if (management == null) return NotFound("Gestión no encontrada.");

                var bytes = await _reportService.GenerateL9ReprogramacionesExcel(managementId, from, to);
                var rango = from.HasValue || to.HasValue
                    ? $"_{from?.ToString("yyyyMMdd") ?? "inicio"}-{to?.ToString("yyyyMMdd") ?? "hoy"}"
                    : string.Empty;
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Informe_L9_{management.Code}{rango}.xlsx");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar L-9. ManagementId={ManagementId}; From={From}; To={To}", managementId, from, to);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-9. Intente nuevamente o contacte al administrador.");
            }
        }

        [HttpGet("download/l7")]
        public async Task<IActionResult> DownloadL7(int? requestId)
        {
            try
            {
                // Identificador exacto del flujo: no se adivina "la solicitud más reciente" del equipo.
                if (!requestId.HasValue)
                    return BadRequest("Debe indicar el requestId de la solicitud L-7 a descargar.");

                var request = await _context.Requests
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
                    .Where(r => r.Type == RequestType.Technical && r.Id == requestId.Value)
                    .FirstOrDefaultAsync();

                if (request == null)
                    return NotFound("Solicitud L-7 no encontrada.");

                var bytes = await _reportService.GenerateSolicitudMantenimientoExcel(request);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Solicitud_L7_{request.Id}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo generar L-7. RequestId={RequestId}",
                    requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-7. Intente nuevamente o contacte al administrador.");
            }
        }

        [HttpGet("download/adquisicion")]
        public async Task<IActionResult> DownloadAdquisicion(int? requestId)
        {
            try
            {
                // Identificador exacto del flujo: no se adivina "la solicitud más reciente" del equipo.
                if (!requestId.HasValue)
                    return BadRequest("Debe indicar el requestId de la solicitud de adquisición a descargar.");

                var request = await _context.Requests
                    .AsNoTracking()
                    .Include(r => r.Laboratory)
                        .ThenInclude(l => l!.Faculty)
                    .Include(r => r.RequestedBy)
                    .Include(r => r.RequestedByPerson)
                    .Include(r => r.CostDetails)
                    .Where(r => r.Type == RequestType.Purchasing && r.Id == requestId.Value)
                    .FirstOrDefaultAsync();

                if (request == null)
                    return NotFound("Solicitud de adquisición no encontrada.");

                var bytes = await _reportService.GenerateSolicitudAdquisicionExcel(request);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Adquisicion_{request.Id}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No se pudo generar L-12. RequestId={RequestId}",
                    requestId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "No se pudo generar el reporte L-12. Intente nuevamente o contacte al administrador.");
            }
        }
    }
}
