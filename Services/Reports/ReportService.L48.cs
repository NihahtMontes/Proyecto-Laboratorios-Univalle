using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public partial class ReportService
    {
        // Plantilla L48.xlsx (RE-10-LAB-270): cada equipo ocupa un bloque de 2 filas (Planeado / Real) desde la fila 13;
        // A-E, O y P están combinadas por bloque; G-N son las 8 semanas (4 del primer mes y 4 del segundo).
        private const int L48FirstRow = 13;
        private const int L48TemplateBlocks = 13;          // filas 13-38
        private const int L48FirstWeekColumn = 7;          // G

        public async Task<byte[]> GenerateL48GanttExcel(int managementId, int labId)
        {
            var management = await _context.Managements
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == managementId)
                ?? throw new KeyNotFoundException("Gestión no encontrada.");

            var labName = await _context.Laboratories
                .Where(l => l.Id == labId)
                .Select(l => l.Name)
                .FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(labName)) throw new KeyNotFoundException("Laboratorio no encontrado.");

            var plans = await _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                .Include(p => p.TechnicalRequest)
                .Where(p => p.ManagementId == managementId && p.EquipmentUnit!.LaboratoryId == labId)
                .OrderBy(p => p.EquipmentUnit!.InventoryNumber)
                .ToListAsync();

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "L48.xlsx");
            if (!File.Exists(templatePath)) throw new FileNotFoundException("Plantilla L-48 no encontrada.");

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0];
            var isCorrective = management.Type == ManagementType.Corrective;

            var processLabel = isCorrective
                ? "CORRECTIVO"
                : management.Type == ManagementType.PorConfirmar ? "POR CONFIRMAR" : "PREVENTIVO";
            var period = management.Semester is 1 or 2
                ? $"{(management.Semester == 1 ? "I" : "II")}/{management.Year}"
                : management.Code;
            worksheet.Cells["A5"].Value = $"PLAN DE MANTENIMIENTO {processLabel} EQUIPOS DE LABORATORIO GESTIÓN {period}";
            // Semanas del semestre (mismo criterio que el cronograma: I = junio-julio, II = diciembre-enero).
            worksheet.Cells["G11"].Value = management.Semester == 1 ? "JUNIO" : "DICIEMBRE";
            worksheet.Cells["K11"].Value = management.Semester == 1 ? "JULIO" : "ENERO";

            // Más equipos que bloques de la plantilla: se agregan bloques con el estilo del último y sus celdas combinadas.
            var blocks = Math.Max(L48TemplateBlocks, plans.Count);
            if (plans.Count > L48TemplateBlocks)
            {
                var extraRows = (plans.Count - L48TemplateBlocks) * 2;
                var lastTemplateRow = L48FirstRow + L48TemplateBlocks * 2;   // 39
                worksheet.InsertRow(lastTemplateRow, extraRows, lastTemplateRow - 2);
                for (var block = L48TemplateBlocks; block < plans.Count; block++)
                {
                    var top = L48FirstRow + block * 2;
                    foreach (var col in new[] { 1, 2, 3, 4, 5, 15, 16 })
                        worksheet.Cells[top, col, top + 1, col].Merge = true;
                }
            }

            // Limpiar los datos de ejemplo de la plantilla (solo valores; los bloques combinados se respetan).
            var lastRow = L48FirstRow + blocks * 2 - 1;
            for (var top = L48FirstRow; top <= lastRow; top += 2)
            {
                foreach (var col in new[] { 1, 2, 3, 4, 5, 15, 16 })
                    worksheet.Cells[top, col].Value = null;          // celda superior del bloque combinado
                for (var c = 6; c < L48FirstWeekColumn + 8; c++)
                {
                    worksheet.Cells[top, c].Value = null;
                    worksheet.Cells[top + 1, c].Value = null;
                }
            }
            for (var r = L48FirstRow; r <= lastRow; r++)
                for (var c = L48FirstWeekColumn; c < L48FirstWeekColumn + 8; c++)
                    SetL48Fill(worksheet.Cells[r, c], 255, 255, 255);

            for (var i = 0; i < plans.Count; i++)
            {
                var plan = plans[i];
                var unit = plan.EquipmentUnit;
                var maintenance = plan.Maintenance;
                var top = L48FirstRow + i * 2;

                worksheet.Cells[top, 1].Value = i + 1;
                worksheet.Cells[top, 2].Value = labName;
                worksheet.Cells[top, 3].Value = $"{unit?.Equipment?.Name}, inv {unit?.InventoryNumber}";
                worksheet.Cells[top, 4].Value = plan.TechnicalRequest?.Description
                    ?? (isCorrective ? "Mantenimiento correctivo" : "Mantenimiento preventivo");
                worksheet.Cells[top, 5].Value = maintenance?.ServiceType == ServiceType.External ? "E" : "I";
                worksheet.Cells[top, 6].Value = "Planeado";
                worksheet.Cells[top + 1, 6].Value = "Real";
                worksheet.Cells[top, 15].Value = maintenance == null ? "0%" : $"{maintenance.CompletionPercentage}%";
                worksheet.Cells[top, 16].Value = maintenance?.Technician?.FullName ?? "—";

                // Semana planificada (azul) y ejecutada (verde); en curso sin semana ejecutada → amarillo en la planificada.
                if (plan.PlannedWeek is >= 1 and <= 8)
                    SetL48Fill(worksheet.Cells[top, L48FirstWeekColumn + plan.PlannedWeek.Value - 1], 51, 153, 255);
                if (plan.ExecutedWeek is >= 1 and <= 8)
                    SetL48Fill(worksheet.Cells[top + 1, L48FirstWeekColumn + plan.ExecutedWeek.Value - 1], 52, 236, 52);
                else if (maintenance?.Status == MaintenanceStatus.InProgress && plan.PlannedWeek is >= 1 and <= 8)
                    SetL48Fill(worksheet.Cells[top + 1, L48FirstWeekColumn + plan.PlannedWeek.Value - 1], 255, 255, 102);
            }

            return SavePackage(package);
        }

        // Colores de la leyenda de la plantilla (Planificado FF3399FF, En proceso FFFFFF66, Ejecutado FF34EC34), sin System.Drawing.
        private static void SetL48Fill(ExcelRange cell, int r, int g, int b)
        {
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(255, r, g, b);
        }
    }
}
