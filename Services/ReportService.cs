using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateSolicitudMantenimientoExcel(int requestId);
        Task<byte[]> GenerateSolicitudAdquisicionExcel(int requestId);
        Task<byte[]> GenerateReport(int requestId);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<ReportService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        public async Task<byte[]> GenerateReport(int requestId)
        {
            var type = await _context.Requests
                .Where(r => r.Id == requestId)
                .Select(r => r.Type)
                .FirstOrDefaultAsync();

            return type == RequestType.Purchasing 
                ? await GenerateSolicitudAdquisicionExcel(requestId) 
                : await GenerateSolicitudMantenimientoExcel(requestId);
        }

        public async Task<byte[]> GenerateSolicitudAdquisicionExcel(int requestId)
        {
            try
            {
                var request = await _context.Requests
                    .Include(r => r.Laboratory)
                        .ThenInclude(l => l.Faculty)
                    .Include(r => r.RequestedBy)
                    .Include(r => r.CostDetails)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null) throw new Exception($"Solicitud #{requestId} no encontrada.");

                var templatePath = Path.Combine(_env.WebRootPath, "templates", "solicitud_mantenimiento_template2.xlsx");
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
                
                // Limpiar el cuerpo de la tabla (Filas 19 a 37, Columnas A a Z - CORREGIDO)
                // Des-fusionamos primero para evitar errores con celdas combinadas largas como E35:U36
                worksheet.Cells["A19:Z37"].Merge = false;
                worksheet.Cells["A19:Z37"].Value = null; 
                worksheet.Cells["A19:Z37"].Style.WrapText = true;
                
                // Limpiar fila de totales y área de firmas (Fila 38 a 55, Columnas A a Z - CORREGIDO)
                // Des-fusionamos un rango mucho más amplio horizontalmente para evitar errores con celdas combinadas largas
                // Esto soluciona errores como: "Can't delete/overwrite merged cells... P40:U40"
                worksheet.Cells["A38:Z55"].Merge = false; 
                worksheet.Cells["A38:Z55"].Value = null;

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
                var fechaActual = DateTime.Now;

                // Día (D10:E11)
                var rangeDia = worksheet.Cells["D10:E11"];
                rangeDia.Merge = true;
                rangeDia.Value = fechaActual.Day;
                rangeDia.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeDia.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Mes (F10:G11)
                var rangeMes = worksheet.Cells["F10:G11"];
                rangeMes.Merge = true;
                rangeMes.Value = fechaActual.Month;
                rangeMes.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeMes.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Año (H10:I11)
                var rangeAño = worksheet.Cells["H10:I11"];
                rangeAño.Merge = true;
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
                
                // Limpiar rango de items de forma segura
                worksheet.Cells["A19:Z37"].Merge = false;
                worksheet.Cells["A19:Z37"].Value = null; 
                worksheet.Cells["A19:Z37"].Style.WrapText = true;

                // Etiqueta OBSERVACIONES (A35:D35) - Según imagen
                var rangeObs = worksheet.Cells["A35:D35"];
                rangeObs.Merge = true;
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
                    worksheet.Cells[currentRow, 1, currentRow, 2].Merge = true;
                    worksheet.Cells[currentRow, 1].Value = item.Quantity;
                    worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Unidad -> Fusionar C y D (Col 3-4)
                    worksheet.Cells[currentRow, 3, currentRow, 4].Merge = true;
                    worksheet.Cells[currentRow, 3].Value = item.UnitOfMeasure ?? "Unidad";
                    worksheet.Cells[currentRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Descripción -> Fusionar E hasta M (Col 5-13)
                    worksheet.Cells[currentRow, 5, currentRow, 13].Merge = true;
                    worksheet.Cells[currentRow, 5].Value = item.Concept ?? "Sin descripción";
                    worksheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alineado a la izquierda
                    worksheet.Cells[currentRow, 5].Style.Font.Italic = true; // Estilo cursiva como se ve en la imagen
                    worksheet.Cells[currentRow, 5].Style.Font.Bold = true;   // Negrita como se ve en la imagen
                    
                    // Precio Unitario -> Fusionar N, O, P (Col 14-16)
                    var rangePU = worksheet.Cells[currentRow, 14, currentRow, 16];
                    rangePU.Merge = true;
                    rangePU.Value = item.UnitPrice;
                    rangePU.Style.Numberformat.Format = "#,##0.00";
                    rangePU.Style.Font.Italic = true; // Cursiva como en la imagen
                    rangePU.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    
                    // Valor Total -> Fusionar Q, R, S, T, U (Col 17-21)
                    var rangeSubtotal = worksheet.Cells[currentRow, 17, currentRow, 21];
                    rangeSubtotal.Merge = true;
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
                    worksheet.Cells[rowObs, 1, rowObs, 13].Merge = true; // Fusionar A-M
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
                rangeLetras.Merge = false; 
                rangeLetras.Merge = true;
                
                rangeLetras.Value = montoLetras;
                rangeLetras.Style.Font.Bold = true; 
                rangeLetras.Style.Font.Italic = true; // Cursiva como en la imagen
                rangeLetras.Style.Font.Size = 14;   // Tamaño más grande
                rangeLetras.Style.WrapText = true;
                rangeLetras.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                rangeLetras.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left; // Alineado a la izquierda

                // Monto Total Numérico -> Q38 a U38 (Col 17-21)
                var rangeGranTotal = worksheet.Cells[38, 17, 38, 21];
                rangeGranTotal.Merge = true;
                rangeGranTotal.Value = granTotal;
                rangeGranTotal.Style.Font.Bold = true;
                rangeGranTotal.Style.Font.Size = 14; 
                rangeGranTotal.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                rangeGranTotal.Style.Numberformat.Format = "#,##0.00";

                // Etiqueta "Monto TOTAL" -> Q39 a U39 (Col 17-21)
                var rangeEtiqTotal = worksheet.Cells[39, 17, 39, 21];
                rangeEtiqTotal.Merge = true;
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
                worksheet.Cells["A43:D43"].Merge = true;
                worksheet.Cells["A43"].Value = "Sello y Firma";
                worksheet.Cells["A44:D44"].Merge = true;
                worksheet.Cells["A44"].Value = "Responsable Unidad Solicitante";

                // Bloque 2 (E-H)
                var box2 = worksheet.Cells["E41:H44"];
                box2.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells["E43:H43"].Merge = true;
                worksheet.Cells["E43"].Value = "Director /Inmediato Superior";
                worksheet.Cells["E44:H44"].Merge = true;
                worksheet.Cells["E44"].Value = "(Solo si corresponde)";

                // Bloque 3 (I-L)
                var box3 = worksheet.Cells["I41:L44"];
                box3.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells["I43:L43"].Merge = true;
                worksheet.Cells["I43"].Value = "Almacenes";
                worksheet.Cells["I44:L44"].Merge = true;
                worksheet.Cells["I44"].Value = "(NO existencias)";

                // Bloque 4 (M-O)
                var box4 = worksheet.Cells["M41:O44"];
                box4.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells["M43:O43"].Merge = true;
                worksheet.Cells["M43"].Value = "Presupuestos";

                // Bloque 5 (P-U)
                var box5 = worksheet.Cells["P41:U44"];
                box5.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells["P43:U43"].Merge = true;
                worksheet.Cells["P43"].Value = "Rector/Vicerrector";
                worksheet.Cells["P44:U44"].Merge = true;
                worksheet.Cells["P44"].Value = "DAF/ADM";

                // Estilo General para Firmas
                var rangeFirmas = worksheet.Cells["A41:U44"];
                rangeFirmas.Style.Font.Size = 8;
                rangeFirmas.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rangeFirmas.Style.VerticalAlignment = ExcelVerticalAlignment.Bottom;

                // 2. PAGINACIÓN (Fila 45) - Centrado bajo bloques 4 y 5
                worksheet.Cells["M45:O45"].Merge = true;
                worksheet.Cells["M45"].Value = "P A G I N A";
                worksheet.Cells["M45"].Style.Font.Bold = true;
                worksheet.Cells["M45"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                
                worksheet.Cells["S45:T45"].Merge = true;
                worksheet.Cells["S45"].Value = "DE";
                worksheet.Cells["S45"].Style.Font.Bold = true;
                worksheet.Cells["S45"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // 3. NOTAS DEL PIE (Filas 46-47)
                var note1 = worksheet.Cells["A46:U46"];
                note1.Merge = true;
                note1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                note1.Value = "1) El original debidamente firmado destinado para Adquisiciones  2) Una copia numerada por Adquisiciones al recibir para Unidad Solicitante";
                note1.Style.Font.Size = 8;

                var note2 = worksheet.Cells["A47:U47"];
                note2.Merge = true;
                note2.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                note2.Value = "Requisitos Obligados: a) Sello de Almacenes verificando NO Existencias  b) Sello Presupuestos NO Sobregiros  c) Registro Adquisiciones";
                note2.Style.Font.Size = 8;
                note2.Style.Font.Bold = true;

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando Excel Adquisición #{RequestId}", requestId);
                throw;
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
            
            string[] unidades = {"", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE"};
            string[] decenas = {"", "", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"};
            string[] especiales = {"DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE"};
            string[] centenas = {"", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"};
            
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

        public async Task<byte[]> GenerateSolicitudMantenimientoExcel(int requestId)
        {
            try
            {
                // 1. OBTENER DATOS COMPLETOS DE LA SOLICITUD
                var request = await _context.Requests
                    .Include(r => r.Laboratory)
                    .Include(r => r.Equipment)
                        .ThenInclude(e => e.City)
                    .Include(r => r.Equipment)
                        .ThenInclude(e => e.Country)
                    .Include(r => r.RequestedBy)
                    .Include(r => r.EquipmentUnit) 
                        .ThenInclude(u => u.Laboratory)
                            .ThenInclude(l => l.Faculty)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                {
                    _logger.LogWarning("No se encontró la solicitud {RequestId}", requestId);
                    throw new Exception($"La solicitud #{requestId} no existe.");
                }

                // 2. CARGAR PLANTILLA
                var templatePath = Path.Combine(_env.WebRootPath, "templates", "solicitud_mantenimiento_template.xlsx");
                
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException("Plantilla de Excel no encontrada.");
                }

                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];

                // ========================================
                // LIMPIEZA DE SEGURIDAD
                // ========================================
                // Des-fusionamos el área de firmas/pie de página (rango amplio) para evitar errores
                worksheet.Cells["A40:Z60"].Merge = false;
                worksheet.Cells["A40:Z60"].Value = null;

                // ========================================
                // 4. RELLENAR DATOS SEGÚN TU PLANTILLA
                // ========================================

                // TÍTULO TOTALMENTE SIMÉTRICO (Arial 17, Fusionando A-E)
                worksheet.Row(2).Height = 35;
                worksheet.Row(3).Height = 35;

                // Configuración Fila 2 (Arial 16, Fusionando A-E)
                var range1 = worksheet.Cells["A2:E2"];
                range1.Merge = true;
                range1.Value = "SOLICITUD MANTENIMIENTO Y CALIBRACION DE";
                range1.Style.Font.Bold = true;
                range1.Style.Font.Name = "Arial";
                range1.Style.Font.Size = 16;
                range1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Configuración Fila 3
                var range2 = worksheet.Cells["A3:E3"];
                range2.Merge = true;
                range2.Value = "EQUIPOS DE LABORATORIO";
                range2.Style.Font.Bold = true;
                range2.Style.Font.Name = "Arial";
                range2.Style.Font.Size = 16;
                range2.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range2.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Aplicamos un único borde al bloque completo (A2 a E3) para eliminar la línea del medio
                worksheet.Cells["A2:E3"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                // ENCABEZADO
                // Código de registro (celda D4)
                worksheet.Cells["D4"].Value = $"RE-10-LAB-{request.Id:D3}";

                // SECCIÓN: EQUIPO
                // Equipo (celda B11 - merged B11:D11)
                worksheet.Cells["B11"].Value = request.Equipment?.Name?.ToUpper() ?? "";
                
                // Laboratorio (celda B12)
                var labName = (request.EquipmentUnit?.Laboratory?.Name ?? request.Laboratory?.Name ?? "").ToUpper();
                worksheet.Cells["B12"].Value = labName;
                worksheet.Cells["B12"].Style.WrapText = true;
                AjustarAlturaFila(worksheet, 12, labName.ToString());
                
                // Fecha (celda D12)
                worksheet.Cells["D12"].Value = request.CreatedDate.ToString("M/d/yyyy");
                worksheet.Cells["D12"].Style.Font.Bold = true;
                
                // Responsable (celda B13) - BORRADO por solicitud de usuario
                worksheet.Cells["B13"].Value = "";

                // SECCIÓN: MARCA, MODELO, SERIE
                // Marca (celda B17)
                worksheet.Cells["B17"].Value = request.Equipment?.Brand ?? "";
                
                // Serie (celda D17)
                worksheet.Cells["D17"].Value = request.EquipmentUnit?.SerialNumber ?? "";
                
                // Modelo (celda B18)
                worksheet.Cells["B18"].Value = request.Equipment?.Model ?? "";
                
                // Procedencia (celda D18) format: Ciudad - País
                var ciudad = request.Equipment?.City?.Name ?? "";
                var pais = request.Equipment?.Country?.Name ?? "";
                var procedencia = string.IsNullOrEmpty(ciudad) && string.IsNullOrEmpty(pais) 
                                  ? "" 
                                  : $"{ciudad} - {pais}".Trim(new char[]{' ', '-'});
                
                worksheet.Cells["D18"].Value = procedencia;
                
                // # de Inventario (celda B19)
                worksheet.Cells["B19"].Value = request.EquipmentUnit?.InventoryNumber ?? "";

                // FALLAS O PROBLEMAS DEL EQUIPO (Fila 23)
                // Sobreescribimos el texto de la plantilla en la fila 23
                var fallasData = string.IsNullOrWhiteSpace(request.Description) ? "Sin descripción" : request.Description;
                var problemasCell = worksheet.Cells["A23"];
                problemasCell.Value = fallasData;
                problemasCell.Style.WrapText = true;
                AjustarAlturaFila(worksheet, 23, fallasData);
                worksheet.Cells["A24"].Value = ""; // Limpiamos la fila 24 que antes tenía el dato

                // SUGERENCIAS U OBSERVACIONES (Fila 28)
                // Sobreescribimos el texto de la plantilla en la fila 28
                var observacionesData = string.IsNullOrWhiteSpace(request.Observations) ? "Sin observaciones" : request.Observations;
                var observacionesCell = worksheet.Cells["A28"];
                observacionesCell.Value = observacionesData;
                observacionesCell.Style.WrapText = true;
                AjustarAlturaFila(worksheet, 28, observacionesData);
                worksheet.Cells["A29"].Value = ""; // Limpiamos la fila 29 que antes tenía el dato

                // PERIODO EN QUE FUE UTILIZADO (Fila 32 y 33)
                // Ponemos en negrita el título de la sección
                worksheet.Cells["A32"].Style.Font.Bold = true;
                
                var yearsUsed = request.EquipmentUnit?.YearsInOperation ?? 0;
                worksheet.Cells["A33"].Value = $"AÑOS: {yearsUsed}";
                worksheet.Cells["A33"].Style.Font.Bold = true;

                // TIEMPO ESTIMADO DE REPARACIÓN (Fila 35)
                var repairTime = request.EstimatedRepairTime ?? "";
                worksheet.Cells["A35"].Value = $"TIEMPO ESTIMADO DE REPARACIÓN: {repairTime}";
                worksheet.Cells["A35"].Style.Font.Bold = true;

                // 5. ASEGURAR FORMATO (bordes, fuentes, etc.)
                AplicarEstilosACeldas(worksheet);

                // 6. RETORNAR ARCHIVO COMO BYTE ARRAY
                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte Excel para solicitud #{RequestId}", requestId);
                throw new Exception($"Error al generar reporte: {ex.Message}", ex);
            }
        }


        private void AjustarAlturaFila(ExcelWorksheet worksheet, int rowNumber, string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                worksheet.Row(rowNumber).Height = 25; // Altura mínima estándar
                return;
            }

            // Calcular líneas aproximadas (asumiendo 60 caracteres por línea)
            var lineas = Math.Ceiling((double)texto.Length / 60);
            worksheet.Row(rowNumber).Height = Math.Max(25, (double)lineas * 15.0);
        }

        private void AplicarEstilosACeldas(ExcelWorksheet worksheet)
        {
            // Aplicar bordes a celdas de datos
            var celdasDatos = new[] { "B11", "B12", "D12", "B13", "B17", "D17", "B18", "D18", "B19" };
            
            foreach (var celda in celdasDatos)
            {
                var cell = worksheet.Cells[celda];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 10;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            // Asegurar que las celdas de texto largo tengan wrap
            worksheet.Cells["B12"].Style.WrapText = true;
            worksheet.Cells["A23"].Style.WrapText = true;
            worksheet.Cells["A28"].Style.WrapText = true;
        }
    }
}
