using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.Drawing;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public partial class ReportService
    {
        public async Task<byte[]> GenerateL48GanttExcel(int managementId, int labId)
        {
            var management = await _context.Managements
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == managementId);
            if (management == null) throw new Exception("Gestión no encontrada.");

            var labName = await _context.Laboratories
                .Where(l => l.Id == labId)
                .Select(l => l.Name)
                .FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(labName)) throw new Exception("Laboratory not found");

            var plans = await _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                .Where(p => p.ManagementId == managementId && p.EquipmentUnit!.LaboratoryId == labId)
                .OrderBy(p => p.EquipmentUnit!.InventoryNumber)
                .ToListAsync();

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "L48.xlsx");
            if (!File.Exists(templatePath)) throw new FileNotFoundException("Plantilla L-48 no encontrada.");

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0];

            var processLabel = management.Type == ManagementType.Corrective ? "CORRECTIVO" : "PREVENTIVO";
            worksheet.Cells["A5"].Value = $"PLAN DE MANTENIMIENTO {processLabel} EQUIPOS DE LABORATORIO GESTIÓN {management.Code}";
            for (int r = 13; r <= 60; r++)
                for (int c = 1; c <= 16; c++)
                    try { worksheet.Cells[r, c].Value = null; } catch { }

            int currentRow = 13;
            int itemIndex = 1;

            foreach (var plan in plans)
            {
                var u = plan.EquipmentUnit;
                var m = plan.Maintenance;

                worksheet.Cells[currentRow, 1].Value = itemIndex;
                worksheet.Cells[currentRow, 2].Value = u?.Equipment?.Name?.ToUpper();
                worksheet.Cells[currentRow, 3].Value = u?.InventoryNumber;
                worksheet.Cells[currentRow, 4].Value = m?.MaintenanceType.ToString() ?? "Preventivo";
                worksheet.Cells[currentRow, 5].Value = m?.Technician?.FullName ?? "Interno";
                worksheet.Cells[currentRow, 6].Value = m?.ScheduledDate?.ToString("dd/MM/yyyy");
                worksheet.Cells[currentRow, 7].Value = m?.StartDate?.ToString("dd/MM/yyyy");
                worksheet.Cells[currentRow, 8].Value = m?.EndDate?.ToString("dd/MM/yyyy");
                worksheet.Cells[currentRow, 9].Value = m?.Status.ToString();
                worksheet.Cells[currentRow, 10].Value = m?.Observations;

                // Gantt coloring: months Jan-Dec → cols 11-22 (but template may only go to 16)
                if (m?.ScheduledDate != null)
                {
                    int monthCol = 10 + m.ScheduledDate.Value.Month;
                    if (monthCol <= 22)
                    {
                        try 
                        {
                            var bgColor = System.Drawing.Color.LightBlue; // Planificado (Default)
                            
                            if (m.Status == MaintenanceStatus.Completed)
                            {
                                bgColor = System.Drawing.Color.LightGreen; // Completado
                                // Usar el mes de finalización real si existe
                                if (m.EndDate.HasValue) monthCol = 10 + m.EndDate.Value.Month;
                            }
                            else if (m.Status == MaintenanceStatus.InProgress)
                            {
                                bgColor = System.Drawing.Color.LightYellow; // En Proceso
                            }

                            if (monthCol <= 22)
                            {
                                worksheet.Cells[currentRow, monthCol].Style.Fill.SetBackground(bgColor);
                            }
                        } 
                        catch { }
                    }
                }

                currentRow++;
                itemIndex++;
            }

            return SavePackage(package);
        }

    }
}
