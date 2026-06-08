using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateSolicitudMantenimientoExcel(Request request);
        Task<byte[]> GenerateSolicitudAdquisicionExcel(Request request);
        Task<byte[]> GenerateReport(int requestId);
        Task<byte[]> GenerateL8KardexExcel(int unitId, int? managementPlanId = null, int? managementId = null);
        Task<byte[]> GenerateL48GanttExcel(int managementId, int labId);
        Task<byte[]> GenerateL6VerificacionExcel(int managementId, int labId, string responsable, DateTime? sessionDate = null);
        Task<byte[]> GenerateL3SalidaExcel(int departureId);
    }

    public partial class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<ReportService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        private static byte[] SavePackage(ExcelPackage package)
        {
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateReport(int requestId)
        {
            var request = await _context.Requests
                .AsNoTracking()
                .Include(r => r.Laboratory).ThenInclude(l => l!.Faculty)
                .Include(r => r.Equipment).ThenInclude(e => e!.City)
                .Include(r => r.Equipment).ThenInclude(e => e!.Country)
                .Include(r => r.RequestedBy)
                .Include(r => r.EquipmentUnit).ThenInclude(u => u!.Laboratory).ThenInclude(l => l!.Faculty)
                .Include(r => r.CostDetails)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) throw new Exception("Solicitud no encontrada.");

            return request.Type == RequestType.Purchasing
                ? await GenerateSolicitudAdquisicionExcel(request)
                : await GenerateSolicitudMantenimientoExcel(request);
        }

    }
}
