using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public partial class ReportService
    {
        /// <summary>Combina el rango solo si hace falta: si ya está combinado (coincidencia exacta)
        /// o se solapa con una combinación existente de la plantilla, no se toca (EPPlus lanza
        /// excepción al sobrescribir rangos parcialmente combinados).</summary>
        private static void CombinarSiHaceFalta(ExcelWorksheet worksheet, int desdeFila, int desdeCol, int hastaFila, int hastaCol)
        {
            var rango = worksheet.Cells[desdeFila, desdeCol, hastaFila, hastaCol];
            if (rango.Merge) return; // coincidencia exacta con un merge existente

            foreach (var direccion in worksheet.MergedCells)
            {
                var existente = new ExcelAddress(direccion);
                // Solapamiento de rectángulos por coordenadas Inicio/Fin.
                var solapado = existente.Start.Row <= hastaFila && desdeFila <= existente.End.Row
                            && existente.Start.Column <= hastaCol && desdeCol <= existente.End.Column;
                if (solapado) return; // solapado con la plantilla: no combinar
            }

            rango.Merge = true;
        }

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
                // Requerimiento: A13:U16 está combinado en la plantilla (C14 queda dentro y no se ve); se escribe en A13.
                worksheet.Cells["A13"].Value = null;

                // Limpiar el cuerpo de la tabla (Filas 19 a 37)
                for (int r = 19; r <= 37; r++)
                {
                    for (int c = 1; c <= 26; c++)
                        worksheet.Cells[r, c].Value = null;
                    worksheet.Row(r).Style.WrapText = true;
                }

                // Limpiar fila de totales y área de firmas (Fila 38 a 55)
                for (int r = 38; r <= 55; r++)
                    for (int c = 1; c <= 26; c++)
                        worksheet.Cells[r, c].Value = null;

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

                // Responsable -> D8: usuario solicitante; si la solicitud es histórica (sin usuario),
                // la persona registrada como solicitante.
                var responsable = request.RequestedBy?.FullName ?? request.RequestedByPerson?.FullName;
                worksheet.Cells["D8"].Value = string.IsNullOrWhiteSpace(responsable) ? "SIN RESPONSABLE" : responsable.ToUpper();

                // Código Inversión -> D9
                worksheet.Cells["D9"].Value = string.IsNullOrEmpty(request.InvestmentCode) ? "" : request.InvestmentCode;

                // Nro. Solicitud (Superior Derecha - Caja grande) -> Q8
                worksheet.Cells["Q8"].Value = request.Id.ToString();
                worksheet.Cells["Q8"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells["Q8"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells["Q8"].Style.Font.Bold = true;
                worksheet.Cells["Q8"].Style.Font.Size = 14;

                // FECHA DESGLOSADA (Bloques 10-11): fecha de la solicitud (o de registro si no tiene)
                var fechaDocumento = request.RequestDate ?? request.CreatedDate;

                // Día (D10:E11)
                var rangeDia = worksheet.Cells["D10:E11"];
                CombinarSiHaceFalta(worksheet, 10, 4, 11, 5);
                rangeDia.Value = fechaDocumento.Day;
                rangeDia.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeDia.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Mes (F10:G11)
                var rangeMes = worksheet.Cells["F10:G11"];
                CombinarSiHaceFalta(worksheet, 10, 6, 11, 7);
                rangeMes.Value = fechaDocumento.Month;
                rangeMes.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeMes.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Año (H10:I11)
                var rangeAño = worksheet.Cells["H10:I11"];
                CombinarSiHaceFalta(worksheet, 10, 8, 11, 9);
                rangeAño.Value = fechaDocumento.Year;
                rangeAño.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeAño.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // JUSTIFICACIÓN / TÍTULO (Fila 14)
                worksheet.Cells["A13"].Value = $"Requerimiento: {request.Description ?? "SIN DESCRIPCIÓN"}";
                worksheet.Cells["A13"].Style.WrapText = true;

                // ===============================================
                // TABLA DE ÍTEMS
                // ===============================================
                // Geometría de la plantilla Adquisicion.xlsx (contrato intocable):
                // - Ítems: filas 19 a 34 y fila 37 (la fila 37 es la última antes del bloque de totales 38-39).
                // - Filas 35-36: bloque "OBSERVACIONES" de la plantilla (A35:D35 y E35:U36 combinados),
                //   no usable para ítems: la unidad y el total de un ítem ahí quedarían invisibles.
                // - Totales en 38-39 y firmas en 40-47 con coordenadas fijas.
                // Decisión para solicitudes con más ítems que filas: NO se insertan filas (empujaría
                // totales y firmas de un formulario institucional de una sola página y obligaría a
                // reescribir todas las coordenadas fijas). Se listan los primeros 16 ítems y en la
                // última fila (37) se deja la nota visible "… y N ítems más (ver sistema)"; el total
                // del documento SIEMPRE suma todos los ítems. Se registra warning para auditoría.
                const int PrimerItemRow = 19;
                const int UltimoBloqueObsRow = 36; // 35-36 reservadas para observaciones
                const int UltimaItemRow = 37;
                var items = request.CostDetails.ToList();

                var itemRows = new List<int>();
                for (int r = PrimerItemRow; r <= UltimaItemRow; r++)
                {
                    if (r >= 35 && r <= UltimoBloqueObsRow) continue;
                    itemRows.Add(r);
                }

                var filasParaItems = itemRows.Take(Math.Max(0, itemRows.Count - 1)).ToList(); // deja libre la última fila para la nota
                // Sin desborde se usan todas las filas de ítems (19-34 y 37). Con desborde, la
                // última fila (37) se reserva para la nota y los ítems ocupan 19-34.
                var capacidadTotal = itemRows.Count;
                var hayDesborde = items.Count > capacidadTotal;
                var filasVisibles = hayDesborde ? filasParaItems : itemRows.Take(items.Count).ToList();
                var notaFila = itemRows[^1];

                decimal granTotal = 0;
                foreach (var item in items)
                {
                    granTotal += item.Quantity * item.UnitPrice;
                }

                if (hayDesborde)
                {
                    var omitidos = items.Count - filasParaItems.Count;
                    _logger.LogWarning(
                        "L-12 (adquisición) #{RequestId}: {Total} ítems exceden las {Capacidad} filas de la plantilla; se listaron {Mostrados} y se omitieron {Omitidos} con nota en el documento.",
                        request.Id, items.Count, filasParaItems.Count, filasParaItems.Count, omitidos);
                }

                // Etiqueta OBSERVACIONES (A35:D35) - Según imagen
                var rangeObs = worksheet.Cells["A35:D35"];
                CombinarSiHaceFalta(worksheet, 35, 1, 35, 4);
                rangeObs.Value = "OBSERVACIONES";
                rangeObs.Style.Font.Bold = true;
                rangeObs.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeObs.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                // Borde inferior para que se vea como en la foto
                rangeObs.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                for (int i = 0; i < filasVisibles.Count; i++)
                {
                    var item = items[i];
                    int currentRow = filasVisibles[i];

                    var subtotal = item.Quantity * item.UnitPrice;

                    // ✅ AJUSTE DE COLUMNAS SEGÚN PLANTILLA
                    // Cantidad -> Fusionar A y B (Col 1-2)
                    CombinarSiHaceFalta(worksheet, currentRow, 1, currentRow, 2);
                    worksheet.Cells[currentRow, 1].Value = item.Quantity;
                    worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Unidad -> Fusionar C y D (Col 3-4)
                    CombinarSiHaceFalta(worksheet, currentRow, 3, currentRow, 4);
                    worksheet.Cells[currentRow, 3].Value = item.UnitOfMeasure ?? "Unidad";
                    worksheet.Cells[currentRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Descripción -> Fusionar E hasta M (Col 5-13)
                    CombinarSiHaceFalta(worksheet, currentRow, 5, currentRow, 13);
                    worksheet.Cells[currentRow, 5].Value = item.Concept ?? "Sin descripción";
                    worksheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alineado a la izquierda
                    worksheet.Cells[currentRow, 5].Style.Font.Italic = true; // Estilo cursiva como se ve en la imagen
                    worksheet.Cells[currentRow, 5].Style.Font.Bold = true;   // Negrita como se ve en la imagen

                    // Precio Unitario -> Fusionar N, O, P (Col 14-16)
                    var rangePU = worksheet.Cells[currentRow, 14, currentRow, 16];
                    CombinarSiHaceFalta(worksheet, currentRow, 14, currentRow, 16);
                    rangePU.Value = item.UnitPrice;
                    rangePU.Style.Numberformat.Format = "#,##0.00";
                    rangePU.Style.Font.Italic = true; // Cursiva como en la imagen
                    rangePU.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    // Valor Total -> Fusionar Q, R, S, T, U (Col 17-21)
                    var rangeSubtotal = worksheet.Cells[currentRow, 17, currentRow, 21];
                    CombinarSiHaceFalta(worksheet, currentRow, 17, currentRow, 21);
                    rangeSubtotal.Value = subtotal;
                    rangeSubtotal.Style.Numberformat.Format = "#,##0.00";
                    rangeSubtotal.Style.Font.Italic = true; // Cursiva como en la imagen
                    rangeSubtotal.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }

                // Nota de desborde en la última fila de ítems: evita perder ítems en silencio.
                if (hayDesborde)
                {
                    var omitidos = items.Count - filasParaItems.Count;
                    CombinarSiHaceFalta(worksheet, notaFila, 1, notaFila, 2);
                    worksheet.Cells[notaFila, 1].Value = "…";
                    worksheet.Cells[notaFila, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    CombinarSiHaceFalta(worksheet, notaFila, 5, notaFila, 13);
                    worksheet.Cells[notaFila, 5].Value = $"Y {omitidos} ÍTEMS MÁS (VER SISTEMA)";
                    worksheet.Cells[notaFila, 5].Style.Font.Bold = true;
                    worksheet.Cells[notaFila, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    CombinarSiHaceFalta(worksheet, notaFila, 17, notaFila, 21);
                    var rangeOmitidos = worksheet.Cells[notaFila, 17, notaFila, 21];
                    rangeOmitidos.Value = items.Skip(filasParaItems.Count).Sum(i => i.Quantity * i.UnitPrice);
                    rangeOmitidos.Style.Numberformat.Format = "#,##0.00";
                    rangeOmitidos.Style.Font.Italic = true;
                    rangeOmitidos.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }

                // ===============================================
                // OBSERVACIONES (si hay espacio antes de totales)
                // ===============================================
                int rowObs = PrimerItemRow + items.Count + 1;
                if (rowObs < 35 && !string.IsNullOrEmpty(request.Observations))
                {
                    worksheet.Cells[rowObs, 1].Value = $"OBSERVACIONES: {request.Observations}";
                    CombinarSiHaceFalta(worksheet, rowObs, 1, rowObs, 13); // Fusionar A-M
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
                CombinarSiHaceFalta(worksheet, 38, 3, 39, 15);

                rangeLetras.Value = montoLetras;
                rangeLetras.Style.Font.Bold = true;
                rangeLetras.Style.Font.Italic = true; // Cursiva como en la imagen
                rangeLetras.Style.Font.Size = 14;   // Tamaño más grande
                rangeLetras.Style.WrapText = true;
                rangeLetras.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                rangeLetras.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alineado a la izquierda

                // Monto Total Numérico -> Q38 a U38 (Col 17-21)
                var rangeGranTotal = worksheet.Cells[38, 17, 38, 21];
                CombinarSiHaceFalta(worksheet, 38, 17, 38, 21);
                rangeGranTotal.Value = granTotal;
                rangeGranTotal.Style.Font.Bold = true;
                rangeGranTotal.Style.Font.Size = 14;
                rangeGranTotal.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                rangeGranTotal.Style.Numberformat.Format = "#,##0.00";

                // Etiqueta "Monto TOTAL" -> Q39 a U39 (Col 17-21)
                var rangeEtiqTotal = worksheet.Cells[39, 17, 39, 21];
                CombinarSiHaceFalta(worksheet, 39, 17, 39, 21);
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
                CombinarSiHaceFalta(worksheet, 43, 1, 43, 4);
                worksheet.Cells["A43"].Value = "Sello y Firma";
                CombinarSiHaceFalta(worksheet, 44, 1, 44, 4);
                worksheet.Cells["A44"].Value = "Responsable Unidad Solicitante";

                // Bloque 2 (E-H)
                var box2 = worksheet.Cells["E41:H44"];
                box2.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CombinarSiHaceFalta(worksheet, 43, 5, 43, 8);
                worksheet.Cells["E43"].Value = "Director /Inmediato Superior";
                CombinarSiHaceFalta(worksheet, 44, 5, 44, 8);
                worksheet.Cells["E44"].Value = "(Solo si corresponde)";

                // Bloque 3 (I-L)
                var box3 = worksheet.Cells["I41:L44"];
                box3.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CombinarSiHaceFalta(worksheet, 43, 9, 43, 12);
                worksheet.Cells["I43"].Value = "Almacenes";
                CombinarSiHaceFalta(worksheet, 44, 9, 44, 12);
                worksheet.Cells["I44"].Value = "(NO existencias)";

                // Bloque 4 (M-O)
                var box4 = worksheet.Cells["M41:O44"];
                box4.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CombinarSiHaceFalta(worksheet, 43, 13, 43, 15);
                worksheet.Cells["M43"].Value = "Presupuestos";

                // Bloque 5 (P-U)
                var box5 = worksheet.Cells["P41:U44"];
                box5.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                CombinarSiHaceFalta(worksheet, 43, 16, 43, 21);
                worksheet.Cells["P43"].Value = "Rector/Vicerrector";
                CombinarSiHaceFalta(worksheet, 44, 16, 44, 21);
                worksheet.Cells["P44"].Value = "DAF/ADM";

                // Estilo General para Firmas
                var rangeFirmas = worksheet.Cells["A41:U44"];
                rangeFirmas.Style.Font.Size = 8;
                rangeFirmas.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeFirmas.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;

                // 2. PAGINACIÓN (Fila 45) - Centrado bajo bloques 4 y 5
                CombinarSiHaceFalta(worksheet, 45, 13, 45, 16);
                worksheet.Cells["M45"].Value = "P A G I N A";
                worksheet.Cells["M45"].Style.Font.Bold = true;
                worksheet.Cells["M45"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                CombinarSiHaceFalta(worksheet, 45, 19, 45, 20);
                worksheet.Cells["S45"].Value = "DE";
                worksheet.Cells["S45"].Style.Font.Bold = true;
                worksheet.Cells["S45"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // 3. NOTAS DEL PIE (Filas 46-47)
                var note1 = worksheet.Cells["A46:U46"];
                CombinarSiHaceFalta(worksheet, 46, 1, 46, 21);
                note1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                note1.Value = "1) El original debidamente firmado destinado para Adquisiciones  2) Una copia numerada por Adquisiciones al recibir para Unidad Solicitante";
                note1.Style.Font.Size = 8;

                var note2 = worksheet.Cells["A47:U47"];
                CombinarSiHaceFalta(worksheet, 47, 1, 47, 21);
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
