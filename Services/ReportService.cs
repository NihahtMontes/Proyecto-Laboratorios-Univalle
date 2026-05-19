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
        Task<byte[]> GenerateSolicitudMantenimientoExcel(Request request);
        Task<byte[]> GenerateSolicitudAdquisicionExcel(Request request);
        Task<byte[]> GenerateReport(int requestId);
        Task<byte[]> GenerateL8KardexExcel(int unitId);
        Task<byte[]> GenerateL48GanttExcel(int labId);
        Task<byte[]> GenerateL6VerificacionExcel(int labId, string responsable);
        Task<byte[]> GenerateL3SalidaExcel(int departureId);
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

        private static byte[] SavePackage(ExcelPackage package)
        {
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GenerateReport(int requestId)
        {
            var request = await _context.Requests
                .AsNoTracking()
                .Include(r => r.Laboratory).ThenInclude(l => l!.Faculty)
                .Include(r => r.Equipment).ThenInclude(e => e!.City)
                .Include(r => r.Equipment).ThenInclude(e => e!.Country)
                .Include(r => r.RequestedBy)
                .Include(r => r.EquipmentUnit).ThenInclude(u => u!.Laboratory).ThenInclude(l => l!.Faculty)
                .Include(r => r.CostDetails)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) throw new Exception("Solicitud no encontrada.");

            return request.Type == RequestType.Purchasing
                ? await GenerateSolicitudAdquisicionExcel(request)
                : await GenerateSolicitudMantenimientoExcel(request);
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

        public async Task<byte[]> GenerateSolicitudMantenimientoExcel(Request request)
        {
            try
            {
                // 1. EL CONTROLADOR YA OBTUVO LOS DATOS COMPLETOS DE LA SOLICITUD
                if (request == null)
                {
                    _logger.LogWarning("No se proporcionó una solicitud válida.");
                    throw new Exception("La solicitud no existe.");
                }

                // 2. CARGAR PLANTILLA
                var templatePath = Path.Combine(_env.WebRootPath, "templates", "L7.xlsx");

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException("Plantilla de Excel no encontrada.");
                }

                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];

                // ========================================
                // LIMPIEZA DE SEGURIDAD
                // ========================================
                // LIMPIEZA SEGURA: No usar .Merge=false en rangos amplios (crash 0xffffffff)
                for (int r = 40; r <= 60; r++)
                    for (int c = 1; c <= 26; c++)
                        try { worksheet.Cells[r, c].Value = null; } catch { }

                // ========================================
                // 4. RELLENAR DATOS SEGÚN TU PLANTILLA
                // ========================================

                // TÍTULO TOTALMENTE SIMÉTRICO (Arial 17, Fusionando A-E)
                worksheet.Row(2).Height = 35;
                worksheet.Row(3).Height = 35;

                // Configuración Fila 2 (Arial 16, Fusionando A-E)
                var range1 = worksheet.Cells["A2:E2"];
                try { range1.Merge = true; } catch { }
                range1.Value = "SOLICITUD MANTENIMIENTO Y CALIBRACION DE";
                range1.Style.Font.Bold = true;
                range1.Style.Font.Name = "Arial";
                range1.Style.Font.Size = 16;
                range1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Configuración Fila 3
                var range2 = worksheet.Cells["A3:E3"];
                try { range2.Merge = true; } catch { }
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
                                  : $"{ciudad} - {pais}".Trim(new char[] { ' ', '-' });

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

                // ===============================================
                // NUEVO: FIRMA DEL ENCARGADO DE LABORATORIO
                // ===============================================
                // Se coloca en la fila 50, centrado entre la columna B y D
                var firmaRange = worksheet.Cells["B50:D50"];
                try { firmaRange.Merge = true; } catch { }
                firmaRange.Value = "____________________________________";
                firmaRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                var labelFirmaRange = worksheet.Cells["B51:D51"];
                try { labelFirmaRange.Merge = true; } catch { }
                labelFirmaRange.Value = "Firma Encargado de Laboratorio";
                labelFirmaRange.Style.Font.Bold = true;
                labelFirmaRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // 5. ASEGURAR FORMATO (bordes, fuentes, etc.)
                AplicarEstilosACeldas(worksheet);

                // 7. RETORNAR ARCHIVO COMO BYTE ARRAY
                return SavePackage(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte Excel para solicitud #{RequestId}", request?.Id ?? 0);
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

        public async Task<byte[]> GenerateL8KardexExcel(int unitId)
        {
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

            var plansWithMaintenance = await _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.Maintenance)
                    .ThenInclude(m => m!.Technician)
                .Where(p => p.EquipmentUnitId == unitId && p.Maintenance != null && p.Maintenance.Status == MaintenanceStatus.Completed)
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

        public async Task<byte[]> GenerateL48GanttExcel(int labId)
        {
            var labName = await _context.Laboratories
                .Where(l => l.Id == labId)
                .Select(l => l.Name)
                .FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(labName)) throw new Exception("Laboratory not found");

            var plans = await _context.ManagementPlans
                .AsNoTracking()
                .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                .Include(p => p.Maintenance).ThenInclude(m => m!.Technician)
                .Where(p => p.EquipmentUnit!.LaboratoryId == labId)
                .OrderBy(p => p.EquipmentUnit!.InventoryNumber)
                .ToListAsync();

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "L48.xlsx");
            if (!File.Exists(templatePath)) throw new FileNotFoundException("Plantilla L-48 no encontrada.");

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0];

            worksheet.Cells["A5"].Value = $"PLAN DE MANTENIMIENTO PREVENTIVO Y CORRECTIVO EQUIPOS DE LABORATORIO GESTIÓN I/{DateTime.UtcNow.Year}";
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

        public async Task<byte[]> GenerateL6VerificacionExcel(int labId, string responsable = "Sistema")
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

            // BATCH QUERIES (3 totales, sin N+1)
            var equipmentData = await _context.EquipmentUnits
                .AsNoTracking()
                .Include(eu => eu.Equipment)
                .Where(eu => eu.LaboratoryId == labId && eu.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(eu => eu.InventoryNumber)
                .Select(eu => new { eu.Id, EquipmentName = eu.Equipment != null ? eu.Equipment.Name : "", Brand = eu.Equipment != null ? eu.Equipment.Brand : "", eu.InventoryNumber })
                .ToListAsync();

            var unitIds = equipmentData.Select(e => e.Id).ToList();

            var allVerifications = await _context.Verifications
                .AsNoTracking()
                .Where(v => unitIds.Contains(v.EquipmentUnitId) && v.Status != VerificationStatus.Annulled)
                .OrderByDescending(v => v.Date)
                .ToListAsync();

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
                        ws.Cells[r, c + 2].Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                        
                    if (text.StartsWith("GESTION") || text.StartsWith("GESTIÓN"))
                        ws.Cells[r, c].Value = $"GESTION I/{DateTime.Now.Year}";
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


        public async Task<byte[]> GenerateL3SalidaExcel(int departureId)
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

            var items = departure.Items.ToList();

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
                if (worksheet.Cells[r, 1].Text?.ToUpper().Contains("PRODUCTO") == true)
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

        private void AjustarAlturaFilaTextoLargo(ExcelWorksheet worksheet, int rowNumber, string texto, int charsPorLinea = 80)
        {
            if (string.IsNullOrEmpty(texto))
            {
                worksheet.Row(rowNumber).Height = 30;
                return;
            }

            var lineasExplicitas = texto.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var lineasPorLongitud = Math.Ceiling((double)texto.Length / charsPorLinea);
            var totalLineas = Math.Max(lineasExplicitas, lineasPorLongitud);
            worksheet.Row(rowNumber).Height = Math.Max(60, totalLineas * 14.0);
        }
    }
}
