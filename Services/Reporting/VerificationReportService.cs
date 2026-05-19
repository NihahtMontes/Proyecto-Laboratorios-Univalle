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
    /// Adapted for dynamic database-driven checklist architecture.
    /// </summary>
    public class VerificationReportService : IVerificationReportService
    {
        /// <summary>
        /// Generates a formal PDF report for a verification record.
        /// Iterates over dynamic CheckResults.
        /// </summary>
        public byte[] GenerateVerificationPdf(Verification verification)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    // Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("UNIVERSIDAD DEL VALLE").FontSize(16).SemiBold().FontColor(Colors.Red.Medium);
                            col.Item().Text("DIRECCIÓN DE LABORATORIOS").FontSize(10).SemiBold();
                            col.Item().Text("RE-10-LAB-006 | Formulario L-6").FontSize(8).FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"ACTA DE VERIFICACIÓN #{verification.Id}").FontSize(14).Bold();
                            col.Item().Text($"Fecha: {verification.Date:dd/MM/yyyy}").FontSize(10);
                        });
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        // Equipment Info
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("EQUIPO:").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(verification.EquipmentUnit?.Equipment?.Name ?? "N/A").FontSize(11).Bold();
                                c.Item().PaddingTop(5).Text("LABORATORIO:").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(verification.EquipmentUnit?.Laboratory?.Name ?? "N/A").FontSize(10);
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("NRO. INVENTARIO:").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(verification.EquipmentUnit?.InventoryNumber ?? "N/A").FontSize(11).Bold();
                                c.Item().PaddingTop(5).Text("RESPONSABLE:").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(verification.CreatedBy?.FullName ?? "N/A").FontSize(10);
                            });
                        });

                        // Dynamic Checklist
                        col.Item().PaddingTop(20).Text("PUNTOS DE CONTROL Y VERIFICACIÓN").FontSize(12).Bold().FontColor(Colors.Blue.Medium);
                        
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Punto de Control");
                                header.Cell().Element(CellStyle).AlignCenter().Text("Resultado");

                                static IContainer CellStyle(IContainer container) => container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            });

                            int index = 1;
                            foreach (var result in verification.CheckResults.OrderBy(r => r.CheckItem?.Order))
                            {
                                table.Cell().Element(CellStyle).Text((index++).ToString()).FontSize(9);
                                table.Cell().Element(CellStyle).Text(result.CheckItem?.Name ?? "Desconocido").FontSize(9);
                                
                                var resultText = result.Result == VerificationResult.Completed ? "REALIZADO" : "PENDIENTE";
                                var textColor = result.Result == VerificationResult.Completed ? Colors.Green.Medium : Colors.Grey.Medium;

                                table.Cell().Element(CellStyle).AlignCenter().Text(resultText).FontSize(9).FontColor(textColor).Bold();

                                static IContainer CellStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                            }
                        });

                        // Observations
                        col.Item().PaddingTop(20).Column(c => { 
                            c.Item().Text("OBSERVACIONES TÉCNICAS").FontSize(10).SemiBold();
                            c.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Background(Colors.Grey.Lighten4)
                                .Text(string.IsNullOrEmpty(verification.Observations) ? "Sin observaciones particulares." : verification.Observations).FontSize(9).Italic();
                        });
                        
                        // Condition Snapshot
                        col.Item().PaddingTop(10).Row(r => {
                            r.RelativeItem().Text(t => {
                                t.Span("Condición detectada: ").FontSize(10);
                                t.Span(verification.PhysicalCondition.ToString().ToUpper()).FontSize(10).Bold();
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateVerificationsExcel(IEnumerable<Verification> verifications)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Verificaciones");
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Fecha";
                worksheet.Cell(1, 3).Value = "Equipo";
                worksheet.Cell(1, 4).Value = "Estado";
                worksheet.Cell(1, 5).Value = "Observaciones";
                
                var headerRange = worksheet.Range(1, 1, 1, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.AirForceBlue;
                headerRange.Style.Font.FontColor = XLColor.White;

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
                using (var stream = new MemoryStream()) { workbook.SaveAs(stream); return stream.ToArray(); }
            }
        }

        public byte[] GenerateLaboratoryReport(string laboratoryName, IEnumerable<EquipmentUnit> units, string term, string responsible, DateTime date)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("REPORTE L-6");
                ws.Style.Font.FontName = "Arial";
                ws.Style.Font.FontSize = 10;

                // Header
                ws.Range("A1:F1").Merge().Value = "VERIFICACIÓN ESTADO DE EQUIPOS POR LABORATORIO";
                ws.Range("A1:F1").Style.Font.Bold = true;
                ws.Range("A1:F1").Style.Font.FontSize = 14;
                ws.Range("A1:F1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell("D2").Value = "Gestión: " + term;
                ws.Cell("A2").Value = "Lab: " + laboratoryName;
                ws.Cell("A3").Value = "Responsable: " + responsible;
                ws.Cell("D3").Value = "Fecha: " + date.ToString("dd/MM/yyyy");

                // Table Headers
                int headerRow = 5;
                var headers = new[] { "ITEM", "DESCRIPCIÓN EQUIPO", "# DE INV.", "MARCA", "ESTADO DEL EQUIPO", "OBSERVACIONES" };
                for (int i = 0; i < headers.Length; i++) {
                    var cell = ws.Cell(headerRow, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Data
                int currentRow = 6;
                int itemCounter = 1;
                foreach (var unit in units)
                {
                    ws.Cell(currentRow, 1).Value = itemCounter++;
                    ws.Cell(currentRow, 2).Value = unit.Equipment?.Name ?? "N/A";
                    ws.Cell(currentRow, 3).Value = unit.InventoryNumber;
                    ws.Cell(currentRow, 4).Value = unit.Equipment?.Brand ?? "-";

                    // CORRECCIÓN: Usar los nombres actualizados del enum PhysicalCondition
                    ws.Cell(currentRow, 5).Value = unit.PhysicalCondition switch {
                        PhysicalCondition.Excellent => "EXCELENTE",
                        PhysicalCondition.Good => "BUENO",
                        PhysicalCondition.Regular => "REGULAR",
                        PhysicalCondition.Bad => "MALO",
                        PhysicalCondition.Decommissioned => "BAJA",
                        _ => "SIN EVALUAR"
                    };

                    ws.Cell(currentRow, 6).Value = unit.Notes ?? "";
                    currentRow++;
                }

                ws.Columns().AdjustToContents();
                using (var stream = new MemoryStream()) { workbook.SaveAs(stream); return stream.ToArray(); }
            }
        }
    }
}
