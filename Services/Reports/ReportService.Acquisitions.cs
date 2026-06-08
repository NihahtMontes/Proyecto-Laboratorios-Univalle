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
        public async Task<byte[]> GenerateSolicitudAdquisicionExcel(Request request)
        {
            try
            {
                if (request == null) throw new Exception($"Solicitud no válida.");

                var templatePath = Path.Combine(_env.WebRootPath, "templates", "Adquisicion.xlsx");
                if (!File.Exists(templatePath)) throw new FileNotFoundException("Plantilla no encontrada.");

                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];

                // ===============================================
                // LIMPIEZA SELECTIVA (sin borrar formatos)
                // ===============================================
                // Limpiamos celdas específicas de datos
                worksheet.Cells["D6"].Value = null;  // Unidad Solicitante
                worksheet.Cells["D7"].Value = null;  // Centro de Costo
                worksheet.Cells["D8"].Value = null;  // Responsable
                worksheet.Cells["D9"].Value = null;  // Código Inversión
                worksheet.Cells["Q8"].Value = null;  // Nro. Solicitud (CORREGIDO)
                worksheet.Cells["D11"].Value = null; // Fecha - Día
                worksheet.Cells["F11"].Value = null; // Fecha - Mes
                worksheet.Cells["H11"].Value = null; // Fecha - Año
                worksheet.Cells["C14"].Value = null; // Justificación/Requerimiento

                // Limpiar el cuerpo de la tabla (Filas 19 a 37) - LIMPIEZA SEGURA
                for (int r = 19; r <= 37; r++)
                    for (int c = 1; c <= 26; c++)
                        try { worksheet.Cells[r, c].Value = null; } catch { }
                for (int r = 19; r <= 37; r++)
                    try { worksheet.Row(r).Style.WrapText = true; } catch { }

                // Limpiar fila de totales y área de firmas (Fila 38 a 55) - LIMPIEZA SEGURA
                for (int r = 38; r <= 55; r++)
                    for (int c = 1; c <= 26; c++)
                        try { worksheet.Cells[r, c].Value = null; } catch { }

                // ===============================================
                // ENCABEZADO ADMINISTRATIVO
                // ===============================================
                // Unidad Solicitante (Facultad) -> D6
                var facultad = request.Laboratory?.Faculty?.Name ?? "General";
                worksheet.Cells["D6"].Value = facultad.ToUpper();

                // Centro de Costo (Lab + Sigla) -> D7
                var lab = request.Laboratory?.Name ?? "SIN LAB";
                var sigla = request.Laboratory?.Code ?? "";
                worksheet.Cells["D7"].Value = $"{sigla} - {lab}".ToUpper();

                // Responsable -> D8
                worksheet.Cells["D8"].Value = request.RequestedBy?.FullName?.ToUpper() ?? "SIN RESPONSABLE";

                // Código Inversión -> D9
                worksheet.Cells["D9"].Value = string.IsNullOrEmpty(request.InvestmentCode) ? "" : request.InvestmentCode;

                // Nro. Solicitud (Superior Derecha - Caja grande) -> Q8
                worksheet.Cells["Q8"].Value = request.Id.ToString();
                worksheet.Cells["Q8"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["Q8"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells["Q8"].Style.Font.Bold = true;
                worksheet.Cells["Q8"].Style.Font.Size = 14;

                // FECHA DESGLOSADA (Bloques 10-11 - CORREGIDO a Fecha Actual)
                var fechaActual = DateTime.UtcNow;

                // Día (D10:E11)
                var rangeDia = worksheet.Cells["D10:E11"];
                try { rangeDia.Merge = true; } catch { }
                rangeDia.Value = fechaActual.Day;
                rangeDia.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeDia.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Mes (F10:G11)
                var rangeMes = worksheet.Cells["F10:G11"];
                try { rangeMes.Merge = true; } catch { }
                rangeMes.Value = fechaActual.Month;
                rangeMes.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeMes.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Año (H10:I11)
                var rangeAño = worksheet.Cells["H10:I11"];
                try { rangeAño.Merge = true; } catch { }
                rangeAño.Value = fechaActual.Year;
                rangeAño.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeAño.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // JUSTIFICACIÓN / TÍTULO (Fila 14)
                worksheet.Cells["C14"].Value = request.Description ?? "SIN DESCRIPCIÓN";
                worksheet.Cells["C14"].Style.WrapText = true;

                // ===============================================
                // TABLA DE ÍTEMS
                // ===============================================
                int startRow = 19;
                var items = request.CostDetails.ToList();
                decimal granTotal = 0;

                // Limpiar rango de items de forma segura (sin Merge=false)
                for (int r2 = 19; r2 <= 37; r2++)
                    for (int c2 = 1; c2 <= 26; c2++)
                        try { worksheet.Cells[r2, c2].Value = null; } catch { }
                for (int r2 = 19; r2 <= 37; r2++)
                    try { worksheet.Row(r2).Style.WrapText = true; } catch { }

                // Etiqueta OBSERVACIONES (A35:D35) - Según imagen
                var rangeObs = worksheet.Cells["A35:D35"];
                try { rangeObs.Merge = true; } catch { }
                rangeObs.Value = "OBSERVACIONES";
                rangeObs.Style.Font.Bold = true;
                rangeObs.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeObs.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                // Borde inferior para que se vea como en la foto
                rangeObs.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    int currentRow = startRow + i;

                    // Protección contra desbordamiento hacia el footer
                    if (currentRow >= 38) break;

                    var subtotal = item.Quantity * item.UnitPrice;
                    granTotal += subtotal;

                    // ✅ AJUSTE DE COLUMNAS SEGÚN PLANTILLA
                    // Cantidad -> Fusionar A y B (Col 1-2)
                    try { worksheet.Cells[currentRow, 1, currentRow, 2].Merge = true; } catch { }
                    worksheet.Cells[currentRow, 1].Value = item.Quantity;
                    worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Unidad -> Fusionar C y D (Col 3-4)
                    try { worksheet.Cells[currentRow, 3, currentRow, 4].Merge = true; } catch { }
                    worksheet.Cells[currentRow, 3].Value = item.UnitOfMeasure ?? "Unidad";
                    worksheet.Cells[currentRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Descripción -> Fusionar E hasta M (Col 5-13)
                    try { worksheet.Cells[currentRow, 5, currentRow, 13].Merge = true; } catch { }
                    worksheet.Cells[currentRow, 5].Value = item.Concept ?? "Sin descripción";
                    worksheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alineado a la izquierda
                    worksheet.Cells[currentRow, 5].Style.Font.Italic = true; // Estilo cursiva como se ve en la imagen
                    worksheet.Cells[currentRow, 5].Style.Font.Bold = true;   // Negrita como se ve en la imagen

                    // Precio Unitario -> Fusionar N, O, P (Col 14-16)
                    var rangePU = worksheet.Cells[currentRow, 14, currentRow, 16];
                    try { rangePU.Merge = true; } catch { }
                    rangePU.Value = item.UnitPrice;
                    rangePU.Style.Numberformat.Format = "#,##0.00";
                    rangePU.Style.Font.Italic = true; // Cursiva como en la imagen
                    rangePU.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    // Valor Total -> Fusionar Q, R, S, T, U (Col 17-21)
                    var rangeSubtotal = worksheet.Cells[currentRow, 17, currentRow, 21];
                    try { rangeSubtotal.Merge = true; } catch { }
                    rangeSubtotal.Value = subtotal;
                    rangeSubtotal.Style.Numberformat.Format = "#,##0.00";
                    rangeSubtotal.Style.Font.Italic = true; // Cursiva como en la imagen
                    rangeSubtotal.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }

                // ===============================================
                // OBSERVACIONES (si hay espacio antes de totales)
                // ===============================================
                int rowObs = startRow + items.Count + 1;
                if (rowObs < 37 && !string.IsNullOrEmpty(request.Observations))
                {
                    worksheet.Cells[rowObs, 1].Value = $"OBSERVACIONES: {request.Observations}";
                    try { worksheet.Cells[rowObs, 1, rowObs, 13].Merge = true; } catch { } // Fusionar A-M
                    worksheet.Cells[rowObs, 1].Style.WrapText = true;
                }

                // ===============================================
                // TOTALES (Fila 38 y 39)
                // ===============================================
                int rowTotal = 38;

                // Etiqueta "Son:" (En A38)
                worksheet.Cells[rowTotal, 1].Value = "Son:";
                worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;

                // Monto en Letras -> Fusionar C38:O39 (DOS FILAS)
                var montoLetras = ConvertirNumeroALetras(granTotal);

                // Asegurar que no esté fusionado antes de fusionar
                var rangeLetras = worksheet.Cells[38, 3, 39, 15]; // C38:O39
                try { rangeLetras.Merge = true; } catch { }

                rangeLetras.Value = montoLetras;
                rangeLetras.Style.Font.Bold = true;
                rangeLetras.Style.Font.Italic = true; // Cursiva como en la imagen
                rangeLetras.Style.Font.Size = 14;   // Tamaño más grande
                rangeLetras.Style.WrapText = true;
                rangeLetras.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                rangeLetras.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alineado a la izquierda

                // Monto Total Numérico -> Q38 a U38 (Col 17-21)
                var rangeGranTotal = worksheet.Cells[38, 17, 38, 21];
                try { rangeGranTotal.Merge = true; } catch { }
                rangeGranTotal.Value = granTotal;
                rangeGranTotal.Style.Font.Bold = true;
                rangeGranTotal.Style.Font.Size = 14;
                rangeGranTotal.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                rangeGranTotal.Style.Numberformat.Format = "#,##0.00";

                // Etiqueta "Monto TOTAL" -> Q39 a U39 (Col 17-21)
                var rangeEtiqTotal = worksheet.Cells[39, 17, 39, 21];
                try { rangeEtiqTotal.Merge = true; } catch { }
                rangeEtiqTotal.Value = "Monto TOTAL";
                rangeEtiqTotal.Style.Font.Italic = true;
                rangeEtiqTotal.Style.Font.Bold = false;
                rangeEtiqTotal.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // ===============================================
                // RECONSTRUCCIÓN DE FIRMAS Y PIE DE PÁGINA (Filas 40-48)
                // ===============================================

                // 1. ÁREAS DE FIRMAS (Fila 41 a 44) - AJUSTADO A 5 BLOQUES SEGÚN IMÁGENES
                // Bloque 1 (A-D)
                var box1 = worksheet.Cells["A41:D44"];
                box1.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                try { worksheet.Cells["A43:D43"].Merge = true; } catch { }
                worksheet.Cells["A43"].Value = "Sello y Firma";
                try { worksheet.Cells["A44:D44"].Merge = true; } catch { }
                worksheet.Cells["A44"].Value = "Responsable Unidad Solicitante";

                // Bloque 2 (E-H)
                var box2 = worksheet.Cells["E41:H44"];
                box2.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                try { worksheet.Cells["E43:H43"].Merge = true; } catch { }
                worksheet.Cells["E43"].Value = "Director /Inmediato Superior";
                try { worksheet.Cells["E44:H44"].Merge = true; } catch { }
                worksheet.Cells["E44"].Value = "(Solo si corresponde)";

                // Bloque 3 (I-L)
                var box3 = worksheet.Cells["I41:L44"];
                box3.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                try { worksheet.Cells["I43:L43"].Merge = true; } catch { }
                worksheet.Cells["I43"].Value = "Almacenes";
                try { worksheet.Cells["I44:L44"].Merge = true; } catch { }
                worksheet.Cells["I44"].Value = "(NO existencias)";

                // Bloque 4 (M-O)
                var box4 = worksheet.Cells["M41:O44"];
                box4.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                try { worksheet.Cells["M43:O43"].Merge = true; } catch { }
                worksheet.Cells["M43"].Value = "Presupuestos";

                // Bloque 5 (P-U)
                var box5 = worksheet.Cells["P41:U44"];
                box5.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                try { worksheet.Cells["P43:U43"].Merge = true; } catch { }
                worksheet.Cells["P43"].Value = "Rector/Vicerrector";
                try { worksheet.Cells["P44:U44"].Merge = true; } catch { }
                worksheet.Cells["P44"].Value = "DAF/ADM";

                // Estilo General para Firmas
                var rangeFirmas = worksheet.Cells["A41:U44"];
                rangeFirmas.Style.Font.Size = 8;
                rangeFirmas.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeFirmas.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;

                // 2. PAGINACIÓN (Fila 45) - Centrado bajo bloques 4 y 5
                try { worksheet.Cells["M45:O45"].Merge = true; } catch { }
                worksheet.Cells["M45"].Value = "P A G I N A";
                worksheet.Cells["M45"].Style.Font.Bold = true;
                worksheet.Cells["M45"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                try { worksheet.Cells["S45:T45"].Merge = true; } catch { }
                worksheet.Cells["S45"].Value = "DE";
                worksheet.Cells["S45"].Style.Font.Bold = true;
                worksheet.Cells["S45"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // 3. NOTAS DEL PIE (Filas 46-47)
                var note1 = worksheet.Cells["A46:U46"];
                try { note1.Merge = true; } catch { }
                note1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                note1.Value = "1) El original debidamente firmado destinado para Adquisiciones  2) Una copia numerada por Adquisiciones al recibir para Unidad Solicitante";
                note1.Style.Font.Size = 8;

                var note2 = worksheet.Cells["A47:U47"];
                try { note2.Merge = true; } catch { }
                note2.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                note2.Value = "Requisitos Obligados: a) Sello de Almacenes verificando NO Existencias  b) Sello Presupuestos NO Sobregiros  c) Registro Adquisiciones";
                note2.Style.Font.Size = 8;
                note2.Style.Font.Bold = true;

                return SavePackage(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando Excel Adquisición #{RequestId}", request?.Id ?? 0);
                throw new Exception($"Error al generar reporte: {ex.Message}", ex);
            }
        }

        private string ConvertirNumeroALetras(decimal numero)
        {
            if (numero == 0) return "CERO ( 00/100 bolivianos )";

            long entero = (long)numero;
            int centavos = (int)Math.Round((numero - entero) * 100);

            string texto = ConvertirEnteroATexto(entero);
            return $"{texto.Trim()} ( {centavos:00}/100 bolivianos )";
        }

        private string ConvertirEnteroATexto(long numero)
        {
            if (numero == 0) return "";

            string[] unidades = { "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE" };
            string[] decenas = { "", "", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
            string[] especiales = { "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" };
            string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

            if (numero >= 1000000)
            {
                long millones = numero / 1000000;
                string textoMillones = millones == 1 ? "UN MILLON " : ConvertirEnteroATexto(millones) + "MILLONES ";
                return textoMillones + ConvertirEnteroATexto(numero % 1000000);
            }

            if (numero >= 1000)
            {
                long miles = numero / 1000;
                string textoMiles = miles == 1 ? "MIL " : ConvertirEnteroATexto(miles) + "MIL ";
                return textoMiles + ConvertirEnteroATexto(numero % 1000);
            }

            if (numero >= 100)
            {
                if (numero == 100) return "CIEN ";
                return centenas[numero / 100] + " " + ConvertirEnteroATexto(numero % 100);
            }

            if (numero >= 20)
            {
                long dec = numero / 10;
                long uni = numero % 10;
                return decenas[dec] + (uni > 0 ? " Y " + unidades[uni] : "") + " ";
            }

            if (numero >= 10)
            {
                return especiales[numero - 10] + " ";
            }

            if (numero > 0)
            {
                return unidades[numero] + " ";
            }

            return "";
        }

    }
}
