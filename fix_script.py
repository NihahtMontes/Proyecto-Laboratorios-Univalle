import re

with open('Services/ReportService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

start_sig = '        public async Task<byte[]> GenerateL3SalidaExcel(int departureId)'
end_sig = '        private void AjustarAlturaFilaTextoLargo(ExcelWorksheet worksheet, int rowNumber, string texto, int charsPorLinea = 80)'

start_idx = content.find(start_sig)
end_idx = content.find(end_sig)

new_method = '''        public async Task<byte[]> GenerateL3SalidaExcel(int departureId)
        {
            try
            {
                var departure = await _context.Departures
                    .AsNoTracking()
                    .Include(d => d.Items)
                    .Include(d => d.EquipmentUnit)
                        .ThenInclude(eu => eu!.Equipment)
                    .Include(d => d.EquipmentUnit)
                        .ThenInclude(eu => eu!.Laboratory)
                    .Include(d => d.Borrower)
                    .Include(d => d.CreatedBy)
                    .FirstOrDefaultAsync(d => d.Id == departureId);

                if (departure == null) throw new Exception("Salida L-3 no encontrada.");

                var templatePath = Path.Combine(_env.WebRootPath, "templates", "L3.xlsx");
                if (!File.Exists(templatePath))
                {
                    _logger.LogWarning("Plantilla L3.xlsx no encontrada.");
                    throw new FileNotFoundException("Plantilla L-3 no encontrada en wwwroot/templates/L3.xlsx");
                }

                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];

                // DYNAMIC METADATA REPLACEMENT
                for (int r = 1; r <= 20; r++)
                {
                    for (int c = 1; c <= 8; c++)
                    {
                        var text = worksheet.Cells[r, c].Text?.ToUpper()?.Trim() ?? "";
                        
                        if (text == "LABORATORIO" || text == "LABORATORIO DE:" || text == "LABORATORIO:") 
                            worksheet.Cells[r, c + 1].Value = departure.EquipmentUnit?.Laboratory?.Name?.ToUpper();
                            
                        if (text.StartsWith("ENTREGADO POR")) 
                            worksheet.Cells[r, c].Value = "ENTREGADO POR: " + (departure.CreatedBy?.FullName?.ToUpper() ?? "SISTEMA");
                            
                        if (text.StartsWith("RECIBIDO POR")) 
                            worksheet.Cells[r, c].Value = "RECIBIDO POR: " + (departure.Borrower?.FullName?.ToUpper() ?? "");
                            
                        if (text.StartsWith("GESTIÓN:") || text.StartsWith("GESTION:")) 
                            worksheet.Cells[r, c].Value = "GESTIÓN: I/" + departure.DepartureDate.Year;
                            
                        if (text.StartsWith("NRO. DE SOLICITUD") || text.StartsWith("NRO DE SOLICITUD")) 
                            worksheet.Cells[r, c].Value = "Nro. DE SOLICITUD: L3-" + departure.Id.ToString("D5");
                            
                        if (text == "DOCENTE/ESTUDIANTE:" || text == "DOCENTE/ESTUDIANTE" || text.StartsWith("DOCENTE")) 
                            worksheet.Cells[r, c + 1].Value = departure.Borrower?.FullName?.ToUpper() ?? "";
                            
                        if (text == "FECHA CRONOGRAMA" || text.StartsWith("FECHA CRONOGRAMA")) 
                            worksheet.Cells[r, c + 1].Value = departure.DepartureDate.ToString("dd/MM/yyyy");
                    }
                }

                // DYNAMIC TABLE MAPPING
                int headerRow = 18;
                for (int r = 10; r <= 25; r++)
                {
                    if (worksheet.Cells[r, 1].Text?.ToUpper().Contains("PRODUCTO") == True)
                    {
                        headerRow = r;
                        break;
                    }
                }

                int colProducto = 1, colCantidad = 2, colUnidad = 3, colDevolucion = 4, colSaldo = 5;
                for (int c = 1; c <= 10; c++)
                {
                    var text = worksheet.Cells[headerRow, c].Text?.ToUpper() ?? "";
                    if (text.Contains("PRODUCTO")) colProducto = c;
                    else if (text.Contains("CANTIDAD")) colCantidad = c;
                    else if (text.Contains("UNIDAD")) colUnidad = c;
                    else if (text.Contains("DEVOLUCION") || text.Contains("DEVUELTO")) colDevolucion = c;
                    else if (text.Contains("SALDO")) colSaldo = c;
                }

                int startRow = headerRow + 1;
                var items = departure.Items.ToList();

                // Clear previous dummy data in template safely
                for (int r = startRow; r <= startRow + 25; r++)
                {
                    for (int c = 1; c <= 8; c++)
                    {
                        worksheet.Cells[r, c].Value = null;
                    }
                }

                if (items.Count == 0)
                {
                    items.Add(new DepartureItem
                    {
                        ProductName = departure.EquipmentUnit?.Equipment?.Name ?? "Equipo Principal",
                        Quantity = 1,
                        UnitOfMeasure = "UNIDAD",
                        ReturnedQuantity = 0
                    });
                }

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    int currentRow = startRow + i;
                    
                    if(currentRow > headerRow + 30) break; // Limit to fit template

                    worksheet.Cells[currentRow, colProducto].Value = item.ProductName?.ToUpper();
                    worksheet.Cells[currentRow, colCantidad].Value = item.Quantity;
                    worksheet.Cells[currentRow, colUnidad].Value = item.UnitOfMeasure ?? "UNIDAD";
                    worksheet.Cells[currentRow, colDevolucion].Value = item.ReturnedQuantity ?? 0;
                    worksheet.Cells[currentRow, colSaldo].Value = item.Balance;
                }

                return SavePackage(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando Excel L-3 para salida #{DepartureId}", departureId);
                throw new Exception($"Error al generar reporte L-3: {ex.Message}", ex);
            }
        }

'''

with open('Services/ReportService.cs', 'w', encoding='utf-8') as f:
    f.write(content[:start_idx] + new_method + content[end_idx:])
