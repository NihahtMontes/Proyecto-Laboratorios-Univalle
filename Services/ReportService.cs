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
        Task<byte[]> GenerateSolicitudMantenimientoExcel(int requestId);
        Task<byte[]> GenerateSolicitudAdquisicionExcel(int requestId);
        Task<byte[]> GenerateReport(int requestId);
        Task<byte[]> GenerateL6Report(int laboratoryId, int managementId, int personId);
        Task<byte[]> GenerateL7Report(int laboratoryId, int managementId, int personId, List<Request> solicitudes);
        Task<byte[]> GenerateL8Report(int laboratoryId, int managementId, int personId, List<Maintenance> historial);
        Task<byte[]> GenerateL3Report(int laboratoryId, int managementId, int personId, List<Departure> salidas);
    }

    public class ReportService : IReportService
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
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<byte[]> GenerateL6Report(int laboratoryId, int managementId, int personId)
        {
            try
            {
                var lab = await _context.Laboratories.Include(l => l.Faculty).FirstOrDefaultAsync(l => l.Id == laboratoryId);
                var mgmt = await _context.Managements.FirstOrDefaultAsync(m => m.Id == managementId);
                var person = await _context.People.FirstOrDefaultAsync(p => p.Id == personId);

                var templatePath = Path.Combine(_env.WebRootPath, "templates", "L-6 VERIFICACION ESTADO DE EQUIPOS POR LABORATORI (V4).xlsx");
                if (!File.Exists(templatePath)) throw new FileNotFoundException($"No se encontró la plantilla L-6");

                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];

                worksheet.Cells["A5"].Value = mgmt?.Code ?? "";
                worksheet.Cells["B7"].Value = lab?.Name?.ToUpper() ?? "";

                string nombreCompleto = person != null && person.FullName != "Ficha de Persona"
                    ? person.FullName
                    : (person?.Email ?? "SIN ASIGNAR");

                worksheet.Cells["B8"].Value = nombreCompleto.ToUpper();
                worksheet.Cells["B9"].Value = lab?.Faculty?.Name?.ToUpper() ?? "ISI-UNIVALLE";
                worksheet.Cells["B10"].Value = DateTime.Now.ToString("dd/MM/yyyy");

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en L-6");
                throw;
            }
        }

        public async Task<byte[]> GenerateL7Report(int laboratoryId, int managementId, int personId, List<Request> solicitudes)
        {
            try
            {
                var lab = await _context.Laboratories.Include(l => l.Faculty).FirstOrDefaultAsync(l => l.Id == laboratoryId);
                var mgmt = await _context.Managements.FirstOrDefaultAsync(m => m.Id == managementId);
                var person = await _context.People.FirstOrDefaultAsync(p => p.Id == personId);

                var templatePath = Path.Combine(_env.WebRootPath, "templates", "L-7 SOLICITUD MANTENIMIENTO EQUIPOS DE LABORATORI(V4).xlsx");
                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];

                worksheet.Cells["A5"].Value = mgmt?.Code ?? "";
                worksheet.Cells["B10"].Value = lab?.Name?.ToUpper() ?? "";
                worksheet.Cells["D10"].Value = DateTime.Now.ToString("dd/MM/yyyy");

                string responsable = person != null && person.FullName != "Ficha de Persona" ? person.FullName : person?.Email;
                worksheet.Cells["B11"].Value = (responsable ?? "SIN ASIGNAR").ToUpper();

                int startRow = 23;
                for (int i = 0; i < solicitudes.Count; i++)
                {
                    var req = solicitudes[i];
                    int currentRow = startRow + i;
                    if (currentRow > 45) break;

                    worksheet.Cells[$"B{currentRow}"].Value = $"{req.Equipment?.Name} (Inv: {req.EquipmentUnit?.InventoryNumber})";
                    worksheet.Cells[$"A{currentRow}"].Value = req.Description;
                }

                return package.GetAsByteArray();
            }
            catch (Exception ex) { _logger.LogError(ex, "Error en L-7"); throw; }
        }

        public async Task<byte[]> GenerateL8Report(int laboratoryId, int managementId, int personId, List<Maintenance> historial)
        {
            try
            {
                var lab = await _context.Laboratories.Include(l => l.Faculty).FirstOrDefaultAsync(l => l.Id == laboratoryId);
                var mgmt = await _context.Managements.FirstOrDefaultAsync(m => m.Id == managementId);
                var person = await _context.People.FirstOrDefaultAsync(p => p.Id == personId);

                var templatePath = Path.Combine(_env.WebRootPath, "templates", "FORM L-8 KARDEX MANTENIMIENTO.xlsx");
                if (!File.Exists(templatePath)) throw new FileNotFoundException("Plantilla L-8 no encontrada.");

                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];

                var primero = historial.FirstOrDefault();
                worksheet.Cells["B9"].Value = primero?.EquipmentUnit?.Equipment?.Name?.ToUpper() ?? "EQUIPO GENERAL";
                worksheet.Cells["B10"].Value = lab?.Name?.ToUpper() ?? "";
                worksheet.Cells["B11"].Value = primero?.EquipmentUnit?.Equipment?.Brand ?? "";
                worksheet.Cells["B12"].Value = primero?.EquipmentUnit?.Equipment?.Model ?? "";
                worksheet.Cells["B13"].Value = primero?.EquipmentUnit?.SerialNumber ?? "";
                worksheet.Cells["B14"].Value = primero?.EquipmentUnit?.InventoryNumber ?? "";

                int startRow = 16;
                for (int i = 0; i < historial.Count; i++)
                {
                    var m = historial[i];
                    int currentRow = startRow + i;
                    if (currentRow > 45) break;

                    worksheet.Cells[$"A{currentRow}"].Value = m.MaintenanceDate.ToString("dd/MM/yyyy");
                    worksheet.Cells[$"B{currentRow}"].Value = m.Description ?? "MANTENIMIENTO REALIZADO";
                    worksheet.Cells[$"C{currentRow}"].Value = m.Technician?.FullName ?? "PERSONAL TÉCNICO UNIVALLE";
                    worksheet.Cells[$"D{currentRow}"].Value = m.SuggestedNextMaintenanceDate.HasValue
                        ? m.SuggestedNextMaintenanceDate.Value.ToString("dd/MM/yyyy")
                        : m.MaintenanceDate.AddMonths(6).ToString("dd/MM/yyyy");
                    worksheet.Cells[$"E{currentRow}"].Value = m.ActualCost ?? 0;
                    worksheet.Cells[$"F{currentRow}"].Value = m.Observations ?? "";
                }

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte L-8");
                throw;
            }
        }

        public async Task<byte[]> GenerateL3Report(int laboratoryId, int managementId, int personId, List<Departure> salidas)
        {
            try
            {
                var lab = await _context.Laboratories.Include(l => l.Faculty).FirstOrDefaultAsync(l => l.Id == laboratoryId);
                var mgmt = await _context.Managements.FirstOrDefaultAsync(m => m.Id == managementId);
                var person = await _context.People.FirstOrDefaultAsync(p => p.Id == personId);

                var templatePath = Path.Combine(_env.WebRootPath, "templates", "Formulario L3 de Laboratorios.xlsx");
                if (!File.Exists(templatePath)) throw new FileNotFoundException("Plantilla L-3 no encontrada.");

                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];

                // --- ENCABEZADO ---
                worksheet.Cells["D12"].Value = DateTime.Now.ToString("dd/MM/yyyy");
                worksheet.Cells["M12"].Value = mgmt?.Code ?? "";
                worksheet.Cells["B7"].Value = lab?.Faculty?.Name?.ToUpper() ?? "UNIVALLE";
                worksheet.Cells["B12"].Value = lab?.Name?.ToUpper() ?? "";

                string responsableExcel = person != null && person.FullName != "Ficha de Persona" ? person.FullName : person?.Email;
                worksheet.Cells["M10"].Value = (responsableExcel ?? "SIN ASIGNAR").ToUpper();

                // --- CUERPO DINÁMICO (TIEMPO REAL) ---
                int startRow = 18;
                for (int i = 0; i < salidas.Count; i++)
                {
                    var s = salidas[i];
                    int currentRow = startRow + i;
                    if (currentRow > 50) break;

                    // Extraemos los datos del equipo y el código (Lo que ves en tu tabla)
                    string nombreEquipo = s.EquipmentUnit?.Equipment?.Name ?? "EQUIPO / SALIDA #" + s.Id;
                    string nroInventario = s.EquipmentUnit?.InventoryNumber ?? "S/N";
                    string solicitante = s.Borrower?.FullName ?? "N/A";

                    // Escribimos en el Excel: Columna B (Detalle)
                    worksheet.Cells[$"B{currentRow}"].Value = $"{nombreEquipo} (Inv: {nroInventario}) - Solicitado por: {solicitante}";

                    // Cantidad y Unidad
                    worksheet.Cells[$"H{currentRow}"].Value = 1;
                    worksheet.Cells[$"K{currentRow}"].Value = "PZA";
                }

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando L-3");
                throw;
            }
        }

        public async Task<byte[]> GenerateReport(int requestId)
        {
            var type = await _context.Requests.Where(r => r.Id == requestId).Select(r => r.Type).FirstOrDefaultAsync();
            return type == RequestType.Purchasing ? await GenerateSolicitudAdquisicionExcel(requestId) : await GenerateSolicitudMantenimientoExcel(requestId);
        }

        public async Task<byte[]> GenerateSolicitudAdquisicionExcel(int requestId)
        {
            try
            {
                var request = await _context.Requests.Include(r => r.Laboratory).ThenInclude(l => l.Faculty).Include(r => r.RequestedBy).Include(r => r.CostDetails).FirstOrDefaultAsync(r => r.Id == requestId);
                var templatePath = Path.Combine(_env.WebRootPath, "templates", "solicitud_mantenimiento_template2.xlsx");
                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];
                worksheet.Cells["D6"].Value = request?.Laboratory?.Faculty?.Name?.ToUpper();
                worksheet.Cells["D7"].Value = $"{request?.Laboratory?.Code} - {request?.Laboratory?.Name}".ToUpper();
                worksheet.Cells["D8"].Value = request?.RequestedBy?.FullName?.ToUpper();
                worksheet.Cells["Q8"].Value = request?.Id.ToString();
                return package.GetAsByteArray();
            }
            catch (Exception ex) { throw; }
        }

        public async Task<byte[]> GenerateSolicitudMantenimientoExcel(int requestId)
        {
            try
            {
                var request = await _context.Requests.Include(r => r.Laboratory).Include(r => r.Equipment).Include(r => r.EquipmentUnit).FirstOrDefaultAsync(r => r.Id == requestId);
                var templatePath = Path.Combine(_env.WebRootPath, "templates", "solicitud_mantenimiento_template.xlsx");
                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];
                worksheet.Cells["D4"].Value = $"RE-10-LAB-{request?.Id:D3}";
                worksheet.Cells["B11"].Value = request?.Equipment?.Name?.ToUpper();
                worksheet.Cells["B12"].Value = (request?.EquipmentUnit?.Laboratory?.Name ?? request?.Laboratory?.Name ?? "").ToUpper();
                return package.GetAsByteArray();
            }
            catch (Exception ex) { throw; }
        }
    }
}