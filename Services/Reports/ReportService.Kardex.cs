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
        public async Task<byte[]> GenerateL8KardexExcel(int unitId, int? managementPlanId = null, int? managementId = null)
        {
            if (managementPlanId.HasValue)
            {
                var planUnitId = await _context.ManagementPlans
                    .AsNoTracking()
                    .Where(p => p.Id == managementPlanId.Value && p.EquipmentUnitId.HasValue)
                    .Select(p => p.EquipmentUnitId!.Value)
                    .FirstOrDefaultAsync();

                if (planUnitId == 0) throw new Exception("Plan de gestión no encontrado para L-8.");
                unitId = planUnitId;
            }

            var unit = await _context.EquipmentUnits
                .Where(u => u.Id == unitId)
                .Select(u => new {
                    EquipmentName = u.Equipment != null ? u.Equipment.Name : "",
                    LabName = u.Laboratory != null ? u.Laboratory.Name : "",
                    EquipmentBrand = u.Equipment != null ? u.Equipment.Brand : "",
                    EquipmentModel = u.Equipment != null ? u.Equipment.Model : "",
                    u.SerialNumber,
                    u.InventoryNumber
                })
                .FirstOrDefaultAsync();

            if (unit == null) throw new Exception("Equipment Unit not found");

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "L8.xlsx");
            if (!File.Exists(templatePath)) throw new FileNotFoundException("Plantilla L-8 no encontrada.");

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0];

            // LIMPIEZA SEGURA (sin Merge=false - causa crash 0xffffffff)
            for (int r = 15; r <= 50; r++)
                for (int c = 1; c <= 6; c++)
                    try { worksheet.Cells[r, c].Value = null; } catch { }

            // HEADER
            worksheet.Cells["B8"].Value = unit.EquipmentName?.ToUpper();
            worksheet.Cells["B9"].Value = unit.LabName?.ToUpper();
            worksheet.Cells["B10"].Value = unit.EquipmentBrand?.ToUpper();
            worksheet.Cells["B11"].Value = unit.EquipmentModel?.ToUpper();
            worksheet.Cells["B12"].Value = unit.SerialNumber?.ToUpper();
            worksheet.Cells["B13"].Value = unit.InventoryNumber;

            // CUERPO (Iterando mantenimientos completados)
            int startRow = 15;

            var plansQuery = _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.Maintenance)
                    .ThenInclude(m => m!.Technician)
                .Where(p => p.EquipmentUnitId == unitId && p.Maintenance != null);

            if (managementPlanId.HasValue)
            {
                plansQuery = plansQuery.Where(p => p.Id == managementPlanId.Value);
            }
            else
            {
                plansQuery = plansQuery.Where(p => p.Maintenance!.Status == MaintenanceStatus.Completed);
                if (managementId.HasValue)
                {
                    plansQuery = plansQuery.Where(p => p.ManagementId == managementId.Value);
                }
            }

            var plansWithMaintenance = await plansQuery
                .OrderBy(p => p.Maintenance!.EndDate)
                .ToListAsync();

            foreach (var plan in plansWithMaintenance)
            {
                var m = plan.Maintenance!;
                worksheet.Cells[startRow, 1].Value = m.EndDate?.ToString("dd/MM/yyyy");
                worksheet.Cells[startRow, 2].Value = m.Description ?? "Mantenimiento Preventivo";
                worksheet.Cells[startRow, 3].Value = m.Technician?.FullName ?? "MANTENIMIENTO INTERNO";
                worksheet.Cells[startRow, 4].Value = m.SuggestedNextMaintenanceDate?.ToString("dd/MM/yyyy");
                worksheet.Cells[startRow, 5].Value = m.ActualCost;
                worksheet.Cells[startRow, 5].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[startRow, 6].Value = m.Recommendations ?? "Sin observaciones";

                var maxLength = Math.Max(m.Description?.Length ?? 0, m.Recommendations?.Length ?? 0);
                if (maxLength > 30) worksheet.Row(startRow).Height = Math.Max(25, (maxLength / 30.0) * 15);

                startRow++;
            }

            return SavePackage(package);
        }

    }
}
