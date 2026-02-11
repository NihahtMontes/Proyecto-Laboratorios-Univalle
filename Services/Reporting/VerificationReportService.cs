using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services.Reporting
{
    /// <summary>
    /// Implementation of the verification report service using QuestPDF and ClosedXML.
    /// </summary>
    public class VerificationReportService : IVerificationReportService
    {
        /// <summary>
        /// Generates a formal PDF report for a verification record.
        /// </summary>
        public byte[] GenerateVerificationPdf(Verification verification)
        {
            // Initial Basic Design (To be improved with user feedback)
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                    page.Header()
                        .Text($"Reporte de Verificación #{verification.Id}")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            x.Item().Text($"Fecha: {verification.Date:dd/MM/yyyy}");
                            x.Item().Text($"Estado: {verification.Status}");
                            
                            if (verification.EquipmentUnit?.Equipment != null)
                            {
                                x.Item().Text($"Equipo: {verification.EquipmentUnit.Equipment.Name}");
                                x.Item().Text($"Serie/Inv: {verification.EquipmentUnit.SerialNumber} / {verification.EquipmentUnit.InventoryNumber}");
                            }

                            x.Item().Text("Detalles de Verificación:");
                            // Additional details would be iterated here if present in the model
                            x.Item().Text("... (Detalles completos en desarrollo) ...");
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Generates an Excel spreadsheet containing multiple verification records.
        /// </summary>
        public byte[] GenerateVerificationsExcel(IEnumerable<Verification> verifications)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Verificaciones");
                
                // Headers in Spanish for the user
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Fecha";
                worksheet.Cell(1, 3).Value = "Equipo";
                worksheet.Cell(1, 4).Value = "Estado";
                worksheet.Cell(1, 5).Value = "Observaciones";
                
                var headerRange = worksheet.Range(1, 1, 1, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.AirForceBlue;
                headerRange.Style.Font.FontColor = XLColor.White;

                // Data Rows
                int row = 2;
                foreach (var v in verifications)
                {
                    worksheet.Cell(row, 1).Value = v.Id;
                    worksheet.Cell(row, 2).Value = v.Date.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = v.EquipmentUnit?.Equipment?.Name ?? "N/A";
                    worksheet.Cell(row, 4).Value = v.Status.ToString();
                    worksheet.Cell(row, 5).Value = v.Observations;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
        

        public byte[] GenerateLaboratoryReport(string laboratoryName, IEnumerable<EquipmentUnit> units, string term, string responsible, DateTime date)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("REPORTE L-6");

                // --- 1. GLOBAL STYLE SETTINGS ---
                ws.Style.Font.FontName = "Arial";
                ws.Style.Font.FontSize = 10;

                // --- 2. HEADER SECTION ---
                
                // Row 1: Main Title
                var titleRange = ws.Range("A1:F1");
                titleRange.Merge().Value = "VERIFICACIÓN ESTADO DE EQUIPOS POR LABORATORIO";
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Font.FontSize = 14;
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(1).Height = 25;

                // Row 2: Metadata Info
                ws.Range("A2:D2").Merge().Value = "Código de registro: RE-10-LAB-006";
                ws.Range("A2:D2").Style.Font.Bold = true;
                ws.Cell("F2").Value = "Versión: 5.0";
                ws.Cell("F2").Style.Font.Bold = true;
                ws.Cell("F2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Row 3: Subtitle
                ws.Range("A3:D3").Merge().Value = "Dirección de Laboratorios y Bibliotecas";
                ws.Cell("F3").Value = "Formulario L-6";
                ws.Cell("F3").Style.Font.Bold = true;
                ws.Cell("F3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Row 4-5: University and Term
                var universityRange = ws.Range("A4:F4");
                universityRange.Merge().Value = "UNIVERSIDAD DEL VALLE";
                universityRange.Style.Font.Bold = true;
                universityRange.Style.Font.FontSize = 16;
                universityRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Row(4).Height = 22;

                var termRange = ws.Range("A5:F5");
                termRange.Merge().Value = $"GESTION {term.ToUpper()}";
                termRange.Style.Font.Bold = true;
                termRange.Style.Font.FontSize = 12;
                termRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Row(5).Height = 18;

                // Row 7-9: Lab Details (Labels and Values)
                void FormatLabelValue(int row, string label, string value, string startCol, string endCol) {
                    try 
                    {
                        ws.Range(row, 1, row, 2).Merge().Value = label;
                        ws.Range(row, 1, row, 2).Style.Font.Bold = true;
                        ws.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        
                        var range = ws.Range($"{startCol}{row}:{endCol}{row}");
                        range.Merge().Value = value;
                        range.Style.Font.Bold = true;
                        range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    }
                    catch (Exception ex)
                    {
                        // Log or handle internally if needed, but for now we ensure it doesn't crash the whole process 
                        // if a specific cell merge fails, though here it's critical.
                        throw new ArgumentException($"Error formateando celda en fila {row}: {ex.Message}", ex);
                    }
                }

                FormatLabelValue(7, "LABORATORIO:", laboratoryName.ToUpper(), "C", "F");
                FormatLabelValue(8, "RESPONSABLE:", responsible.ToUpper(), "C", "F");
                
                ws.Range("A9:B9").Merge().Value = "FECHA VERIFICACION:";
                ws.Range("A9:B9").Style.Font.Bold = true;
                ws.Range("A9:B9").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell("C9").Value = date;
                ws.Cell("C9").Style.Font.Bold = true;
                ws.Cell("C9").Style.DateFormat.Format = "MM-dd-yy";
                ws.Cell("C9").Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                // --- 3. TABLE HEADERS (Row 12) ---
                int headerRow = 12;
                var headers = new[] { "ITEM", "DESCRIPCIÓN EQUIPO Y/O ACCESORIO", "# DE INV.", "MARCA", "ESTADO DEL EQUIPO", "OBSERVACIONES" };
                ws.Row(headerRow).Height = 35;

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
                }

                // --- 4. DATA ROWS ---
                int currentRow = 13;
                int itemCounter = 1;

                foreach (var unit in units)
                {
                    ws.Cell(currentRow, 1).Value = itemCounter++;
                    ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 2).Value = (unit.Equipment?.Name ?? "N/A").ToUpper();
                    ws.Cell(currentRow, 2).Style.Alignment.WrapText = true;

                    // Improved Inventory Formatting (Wrap multiple numbers)
                    ws.Cell(currentRow, 3).Value = unit.InventoryNumber;
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 3).Style.Alignment.WrapText = true;

                    ws.Cell(currentRow, 4).Value = (unit.Equipment?.Brand ?? "-").ToUpper();
                    ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Physical Condition Display Name Helper
                    string statusText = unit.PhysicalCondition switch {
                        PhysicalCondition.Excelente => "EXCELENTE",
                        PhysicalCondition.Bueno => "BUENO",
                        PhysicalCondition.Regular => "REGULAR",
                        PhysicalCondition.Malo => "MALO",
                        PhysicalCondition.Baja => "BAJA",
                        _ => "SIN EVALUAR"
                    };
                    
                    ws.Cell(currentRow, 5).Value = statusText;
                    ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Observations from unit (Notes/Observations)
                    ws.Cell(currentRow, 6).Value = unit.Notes ?? "";
                    ws.Cell(currentRow, 6).Style.Alignment.WrapText = true;

                    // Borders for data row
                    var rowRange = ws.Range(currentRow, 1, currentRow, 6);
                    rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    
                    ws.Row(currentRow).Height = -1; // Auto height based on wrap text
                    currentRow++;
                }

                // --- 5. FOOTER / SIGNATURE SECTION ---
                currentRow += 2; // Extra space
                var sigLineRange = ws.Range(currentRow, 2, currentRow, 5);
                sigLineRange.Merge().Style.Border.TopBorder = XLBorderStyleValues.Thin;
                
                var signatureRange = ws.Range(currentRow + 1, 1, currentRow + 1, 6);
                signatureRange.Merge().Value = "FIRMA ENCARGADO DE LABORATORIO";
                signatureRange.Style.Font.Bold = true;
                signatureRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // --- 6. PAGE AND COLUMN SETTINGS ---
                ws.Column(1).Width = 7;   // Item
                ws.Column(2).Width = 45;  // Description
                ws.Column(3).Width = 18;  // Inv
                ws.Column(4).Width = 15;  // Brand
                ws.Column(5).Width = 22;  // Status
                ws.Column(6).Width = 40;  // Observations

                // Printing Setup
                ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                ws.PageSetup.Margins.Top = 0.5;
                ws.PageSetup.Margins.Bottom = 0.5;
                ws.PageSetup.Margins.Left = 0.3;
                ws.PageSetup.Margins.Right = 0.3;
                ws.PageSetup.FitToPages(1, 0); // Fit to 1 page wide, automatic height

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
