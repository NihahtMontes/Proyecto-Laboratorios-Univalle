using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        /// <summary>
        /// Obtiene el nombre completo del usuario que está imprimiendo el reporte.
        /// </summary>
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
        public async Task<IActionResult> DownloadL6(int labId)
        {
            try
            {
                var responsable = await GetCurrentUserFullName();
                var bytes = await _reportService.GenerateL6VerificacionExcel(labId, responsable);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Verificacion_L6_Lab_{labId}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRASH en GenerateL6. LabId={LabId}", labId);
                return StatusCode(500, $"Error: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            }
        }

        [HttpGet("download/l3")]
        public async Task<IActionResult> DownloadL3(int unitId)
        {
            try
            {
                var bytes = await _reportService.GenerateL3SalidaExcel(unitId);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Salida_L3_Equipo_{unitId}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generando L3: {ex.Message}");
            }
        }

        [HttpGet("download/l8")]
        public async Task<IActionResult> DownloadL8(int unitId)
        {
            try
            {
                var bytes = await _reportService.GenerateL8KardexExcel(unitId);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Kardex_L8_Equipo_{unitId}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generando L8: {ex.Message}");
            }
        }

        [HttpGet("download/l48")]
        public async Task<IActionResult> DownloadL48(int labId)
        {
            try
            {
                var bytes = await _reportService.GenerateL48GanttExcel(labId);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Plan_Gantt_L48_Lab_{labId}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generando L48: {ex.Message}");
            }
        }

        [HttpGet("download/l7")]
        public async Task<IActionResult> DownloadL7(int unitId)
        {
            // Buscamos la última solicitud técnica para este equipo CON sus includes
            var lastRequest = await _context.Requests
                .AsNoTracking()
                .Include(r => r.Laboratory)
                .Include(r => r.Equipment)
                    .ThenInclude(e => e!.City)
                .Include(r => r.Equipment)
                    .ThenInclude(e => e!.Country)
                .Include(r => r.RequestedBy)
                .Include(r => r.EquipmentUnit)
                .Where(r => r.EquipmentUnitId == unitId)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (lastRequest == null)
                return NotFound("Este equipo no tiene solicitudes L-7 registradas.");

            try
            {
                var bytes = await _reportService.GenerateSolicitudMantenimientoExcel(lastRequest);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Solicitud_L7_{lastRequest.Id}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generando L7: {ex.Message}");
            }
        }

        [HttpGet("download/adquisicion")]
        public async Task<IActionResult> DownloadAdquisicion(int unitId)
        {
            var lastRequest = await _context.Requests
                .AsNoTracking()
                .Include(r => r.Laboratory)
                    .ThenInclude(l => l!.Faculty)
                .Include(r => r.RequestedBy)
                .Include(r => r.CostDetails)
                .Where(r => r.EquipmentUnitId == unitId
                         && r.Type == Models.Enums.RequestType.Purchasing)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (lastRequest == null)
                return NotFound("Este equipo no tiene solicitudes de adquisición.");

            try
            {
                var bytes = await _reportService.GenerateSolicitudAdquisicionExcel(lastRequest);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Adquisicion_{lastRequest.Id}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generando Adquisición: {ex.Message}");
            }
        }
    }
}
