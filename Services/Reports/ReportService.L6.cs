using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public partial class ReportService
    {
        public async Task<byte[]> GenerateL6VerificacionExcel(int managementId, int labId, string responsable = "Sistema", DateTime? sessionDate = null, int? unitId = null)
        {
            // L-6 SOBRE LA PLANTILLA wwwroot/templates/L6V2.xlsx (se abre y se limpia/reescribe).
            // No se copian hojas con Worksheets.Add(nombre, hoja): duplicar el XML de estilos de
            // L6V2.xlsx corrompe el paquete y GetAsByteArray() crashea (0xffffffff).


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
                             && v.Status != VerificationStatus.Annulled
                             && (!unitId.HasValue || v.EquipmentUnitId == unitId.Value))
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
                    .Where(p => p.ManagementId == managementId && p.EquipmentUnit != null
                             && p.EquipmentUnit.LaboratoryId == labId
                             && (!unitId.HasValue || p.EquipmentUnitId == unitId.Value))
                    .OrderBy(p => p.EquipmentUnit!.InventoryNumber)
                    .Select(p => new
                    {
                        Id = p.EquipmentUnit!.Id,
                        EquipmentName = p.EquipmentUnit.Equipment != null ? p.EquipmentUnit.Equipment.Name : "",
                        Brand = p.EquipmentUnit.Equipment != null ? p.EquipmentUnit.Equipment.Brand : "",
                        p.EquipmentUnit.InventoryNumber
                    })
                    // Un mismo equipo puede tener dos planes en la gestión (p. ej. preventivo
                    // reprogramado); el consolidado L-6 lista cada equipo una sola vez.
                    .Distinct()
                    .ToListAsync();

            if (unitId.HasValue && equipmentData.Count == 0)
                throw new KeyNotFoundException("El equipo no pertenece a la gestión y laboratorio seleccionados.");

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
                    {
                        // Nombre completo en una línea («AULA DEMOSTRATIVA H-102»): en la plantilla la celda
                        // del valor (C7) es angosta y solo se veía la primera palabra. Se combina con la celda
                        // libre de al lado (D7), como ya hace la plantilla con el responsable (C8:F8), y solo
                        // si un nombre muy largo aún no entra se reduce la letra.
                        var valueCell = ws.Cells[r, c + 2];
                        valueCell.Value = labName?.ToUpper();
                        var neighbor = ws.Cells[r, c + 3];
                        if (!valueCell.Merge && !neighbor.Merge && string.IsNullOrEmpty(neighbor.Text))
                        {
                            ws.Cells[r, c + 2, r, c + 3].Merge = true;
                        }
                        valueCell.Style.WrapText = false;
                        valueCell.Style.ShrinkToFit = true;
                        valueCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    }
                        
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
                        // Datos históricos: PorConfirmar se imprime literal para no caer
                        // en el fallback del nombre técnico del enum ("PorConfirmar").
                        PhysicalCondition.PorConfirmar => "Por confirmar",
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
                    ws.Cells[row, colEstado].Style.Font.Color.SetColor(255, 255, 0, 0);

                ws.Cells[row, colMarca].Value = eq.Brand?.ToUpper() ?? "S/M";
                ws.Cells[row, colMarca].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                ws.Cells[row, colObs].Value = observationsText.ToUpper();

                // Sin System.Drawing.Color (regla de reportes): los colores se fijan con
                // SetColor(alfa, r, g, b) y el relleno con PatternType + BackgroundColor.
                var celdaFondo = ws.Cells[row, 1, row, 6].Style;
                celdaFondo.Fill.PatternType = ExcelFillStyle.Solid;
                celdaFondo.Fill.BackgroundColor.SetColor(255, alternate ? 242 : 255, alternate ? 246 : 255, alternate ? 252 : 255);
                for (int c = 1; c <= 6; c++)
                {
                    var estilo = ws.Cells[row, c].Style;
                    estilo.Border.BorderAround(ExcelBorderStyle.Thin);
                    estilo.Border.Top.Color.SetColor(255, 180, 180, 180);
                    estilo.Border.Bottom.Color.SetColor(255, 180, 180, 180);
                    estilo.Border.Left.Color.SetColor(255, 180, 180, 180);
                    estilo.Border.Right.Color.SetColor(255, 180, 180, 180);
                    estilo.VerticalAlignment = ExcelVerticalAlignment.Center;
                    estilo.WrapText = true;
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
