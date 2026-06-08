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
        public async Task<byte[]> GenerateL6VerificacionExcel(int managementId, int labId, string responsable = "Sistema", DateTime? sessionDate = null)
        {
            // L-6 GENERADO DESDE CERO (SIN TEMPLATE)
            // Worksheets.Add(sourceSheet.Name, sourceSheet) copia estilos XML del template
            // L6V2.xlsx que corrompen el paquete. GetAsByteArray() crashea (0xffffffff).

            var labName = await _context.Laboratories
                .AsNoTracking()
                .Where(l => l.Id == labId)
                .Select(l => l.Name)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(labName)) throw new Exception("Laboratorio no encontrado.");

            var management = await _context.Managements
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == managementId);
            if (management == null) throw new Exception("Gestión no encontrada.");
            if (management.Type == ManagementType.Corrective) throw new Exception("L-6 solo aplica al proceso preventivo.");

            // BATCH QUERIES (3 totales, sin N+1)
            var equipmentData = sessionDate.HasValue
                ? await _context.Verifications
                    .AsNoTracking()
                    .Where(v => v.ManagementId == managementId
                             && v.EquipmentUnit != null
                             && v.EquipmentUnit.LaboratoryId == labId
                             && v.Date.Date == sessionDate.Value.Date
                             && v.Status != VerificationStatus.Annulled)
                    .OrderBy(v => v.EquipmentUnit!.InventoryNumber)
                    .Select(v => new
                    {
                        Id = v.EquipmentUnit!.Id,
                        EquipmentName = v.EquipmentUnit.Equipment != null ? v.EquipmentUnit.Equipment.Name : "",
                        Brand = v.EquipmentUnit.Equipment != null ? v.EquipmentUnit.Equipment.Brand : "",
                        v.EquipmentUnit.InventoryNumber
                    })
                    .Distinct()
                    .ToListAsync()
                : await _context.ManagementPlans
                    .AsNoTracking()
                    .Where(p => p.ManagementId == managementId && p.EquipmentUnit != null && p.EquipmentUnit.LaboratoryId == labId)
                    .OrderBy(p => p.EquipmentUnit!.InventoryNumber)
                    .Select(p => new
                    {
                        Id = p.EquipmentUnit!.Id,
                        EquipmentName = p.EquipmentUnit.Equipment != null ? p.EquipmentUnit.Equipment.Name : "",
                        Brand = p.EquipmentUnit.Equipment != null ? p.EquipmentUnit.Equipment.Brand : "",
                        p.EquipmentUnit.InventoryNumber
                    })
                    .ToListAsync();

            var unitIds = equipmentData.Select(e => e.Id).ToList();

            var allVerifications = await _context.Verifications
                .AsNoTracking()
                .Where(v => v.ManagementId == managementId && unitIds.Contains(v.EquipmentUnitId) && v.Status != VerificationStatus.Annulled)
                .OrderByDescending(v => v.Date)
                .ToListAsync();

            if (sessionDate.HasValue)
            {
                allVerifications = allVerifications
                    .Where(v => v.Date.Date == sessionDate.Value.Date)
                    .ToList();
            }

            var lastVerifications = allVerifications
                .GroupBy(v => v.EquipmentUnitId)
                .ToDictionary(g => g.Key, g => g.First());

            var lastVIds = lastVerifications.Values.Select(v => v.Id).ToList();
            var allFaults = await _context.VerificationFaults
                .AsNoTracking()
                .Where(vf => !vf.IsDeleted && lastVIds.Contains(vf.VerificationId))
                .Select(vf => new { vf.VerificationId, vf.Description })
                .ToListAsync();

            var faultsByVerification = allFaults
                .GroupBy(f => f.VerificationId)
                .ToDictionary(g => g.Key, g => g.Select(f => f.Description).ToList());

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "L6V2.xlsx");
            if (!File.Exists(templatePath))
            {
                _logger.LogWarning("Plantilla L6V2.xlsx no encontrada.");
                throw new FileNotFoundException("Plantilla L-6 no encontrada en wwwroot/templates/L6V2.xlsx");
            }

            using var package = new ExcelPackage(new FileInfo(templatePath));
            
            // Buscar si existe una hoja con el nombre del laboratorio, si no, usar la primera
            var ws = package.Workbook.Worksheets.FirstOrDefault(w => w.Name.Equals(labName, StringComparison.OrdinalIgnoreCase)) 
                     ?? package.Workbook.Worksheets[0];

            // Eliminar todas las demás hojas para que el reporte sea limpio y exclusivo del laboratorio solicitado
            var sheetsToDelete = package.Workbook.Worksheets
                .Where(w => w.Name != ws.Name)
                .Select(w => w.Name)
                .ToList();
                
            foreach (var sName in sheetsToDelete)
            {
                package.Workbook.Worksheets.Delete(sName);
            }

            // Renombrar la hoja resultante para asegurar consistencia
            string safeLabName = string.Join("_", (labName ?? "Laboratorio").Split(Path.GetInvalidFileNameChars()));
            if (safeLabName.Length > 31) safeLabName = safeLabName.Substring(0, 31); // Excel tab names limit
            ws.Name = safeLabName;

            // METADATOS
            for (int r = 1; r <= 15; r++)
            {
                for (int c = 1; c <= 8; c++)
                {
                    var text = ws.Cells[r, c].Text?.ToUpper()?.Trim() ?? "";

                    if (text == "LABORATORIO" || text == "LABORATORIO:")
                        ws.Cells[r, c + 2].Value = labName?.ToUpper();
                        
                    if (text == "RESPONSABLE" || text == "RESPONSABLE:")
                        ws.Cells[r, c + 2].Value = responsable?.ToUpper();
                        
                    if (text.StartsWith("FECHA VERIFICACION") || text.StartsWith("FECHA VERIFICACIÓN"))
                        ws.Cells[r, c + 2].Value = (sessionDate ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm");
                        
                    if (text.StartsWith("GESTION") || text.StartsWith("GESTIÓN"))
                        ws.Cells[r, c].Value = $"GESTION {management.Code}";
                }
            }   

            // HEADER DINAMICO
            int headerRow = 12;
            for (int r = 8; r <= 15; r++)
            {
                if (ws.Cells[r, 2].Text?.ToUpper().Contains("DESCRIPCI") == true)
                {
                    headerRow = r;
                    break;
                }
            }

            int colDesc = 2, colInv = 3, colMarca = 4, colEstado = 5, colObs = 6;
            for (int c = 1; c <= 8; c++)
            {
                var text = ws.Cells[headerRow, c].Text?.ToUpper() ?? "";
                if (text.Contains("DESCRIPCI")) colDesc = c;
                else if (text.Contains("INV")) colInv = c;
                else if (text.Contains("MARCA")) colMarca = c;
                else if (text.Contains("ESTADO")) colEstado = c;
                else if (text.Contains("OBSERVACION")) colObs = c;
            }

            int startRow = headerRow + 1;

            // Clear dummy data
            for (int r = startRow; r <= startRow + 30; r++)
            {
                for (int c = 1; c <= 8; c++)
                {
                    ws.Cells[r, c].Value = null;
                }
            }

            // Datos
            int row = startRow;
            int item = 1;
            bool alternate = false;

            foreach (var eq in equipmentData)
            {
                var condition = "N/A";
                var observationsText = "";

                if (lastVerifications.TryGetValue(eq.Id, out var lastV))
                {
                    condition = lastV.PhysicalCondition switch
                    {
                        PhysicalCondition.Excellent => "Excelente",
                        PhysicalCondition.Good => "Bueno",
                        PhysicalCondition.Regular => "Regular",
                        PhysicalCondition.Bad => "Malo",
                        PhysicalCondition.Decommissioned => "Baja",
                        _ => lastV.PhysicalCondition.ToString()
                    };
                    observationsText = lastV.Observations ?? "";

                    if (faultsByVerification.TryGetValue(lastV.Id, out var faultList) && faultList.Count > 0)
                        observationsText = string.Join("; ", faultList);
                }
                if (string.IsNullOrEmpty(observationsText)) observationsText = "Sin observaciones";

                ws.Cells[row, 1].Value = item;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[row, colDesc].Value = eq.EquipmentName?.ToUpper();
                ws.Cells[row, colInv].Value = eq.InventoryNumber;
                
                ws.Cells[row, colEstado].Value = condition.ToUpper();
                ws.Cells[row, colEstado].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                if (condition.ToUpper() == "MALO" || condition.ToUpper() == "BAJA")
                    ws.Cells[row, colEstado].Style.Font.Color.SetColor(System.Drawing.Color.Red);

                ws.Cells[row, colMarca].Value = eq.Brand?.ToUpper() ?? "S/M";
                ws.Cells[row, colMarca].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[row, colObs].Value = observationsText.ToUpper();

                var rowBg = alternate ? System.Drawing.Color.FromArgb(242, 246, 252) : System.Drawing.Color.White;
                for (int c = 1; c <= 6; c++)
                {
                    ws.Cells[row, c].Style.Fill.SetBackground(rowBg);
                    ws.Cells[row, c].Style.Border.BorderAround(ExcelBorderStyle.Thin, System.Drawing.Color.FromArgb(180, 180, 180));
                    ws.Cells[row, c].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    ws.Cells[row, c].Style.WrapText = true;
                }

                // Auto height based on text length
                ws.Row(row).Height = Math.Max(20, (observationsText.Length / 40.0) * 15);

                row++;
                item++;
                alternate = !alternate;
            }

            return SavePackage(package);
        }


    }
}
