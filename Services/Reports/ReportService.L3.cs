using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.Drawing;
using System.Globalization;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public partial class ReportService
    {
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
                .Include(d => d.OriginLaboratory)
                .Include(d => d.Planning)
                    .ThenInclude(p => p!.Laboratory)
                .Include(d => d.Planning)
                    .ThenInclude(p => p!.Subject)
                    .ThenInclude(s => s!.Career)
                .FirstOrDefaultAsync(d => d.Id == departureId);

            if (departure == null) throw new KeyNotFoundException("Salida L-3 no encontrada.");

            var plan = departure.Planning;
            var esPractica = departure.PlanningId.HasValue && plan != null;

            // Gestión impresa: la de la salida (semestre en romano y año); sin gestión semestral, la del semestre de la fecha.
            var departureManagement = await _context.Managements.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == departure.ManagementId);
            var gestionLabel = departureManagement is { Semester: 1 or 2 }
                ? $"{(departureManagement.Semester == 1 ? "I" : "II")}/{departureManagement.Year}"
                : $"{(departure.DepartureDate.Month <= 6 ? "I" : "II")}/{departure.DepartureDate.Year}";

            // Laboratorio del vale: en salidas de práctica no hay unidad de equipo, así que se resuelve
            // por el ambiente de la planificación y, como último recurso, por el laboratorio de origen.
            var nombreLaboratorio = (departure.EquipmentUnit?.Laboratory?.Name
                ?? plan?.Laboratory?.Name
                ?? departure.OriginLaboratory?.Name)?.ToUpper();

            var codigoPlanificacion = esPractica
                ? (string.IsNullOrEmpty(plan!.Code) ? $"PRA-{plan.Id:00000}" : plan.Code)
                : null;
            var codigoSalida = departure.Code ?? $"SAL-{departure.Id:00000}";
            var solicitudLabel = esPractica
                ? $"Nro. DE SOLICITUD: {codigoPlanificacion} · {codigoSalida}"
                : $"Nro. DE SOLICITUD: L3-{departure.Id:D5}";

            var items = departure.Items.ToList();

            var templatePath = Path.Combine(_env.WebRootPath, "templates", "L3.xlsx");
            if (!File.Exists(templatePath))
            {
                _logger.LogWarning("Plantilla L3.xlsx no encontrada.");
                throw new FileNotFoundException("Plantilla L-3 no encontrada en wwwroot/templates/L3.xlsx");
            }

            using var package = new ExcelPackage(new FileInfo(templatePath));
            var worksheet = package.Workbook.Worksheets[0];

            // Cabecera académica del vale (plantilla L3.xlsx): las etiquetas de la columna A llevan su valor en B
            // (CARRERA B8 … CODIGO ASIGNATURA B14) y las de C (DIA, HORARIO, LABORATORIO) en D5..D7.
            // C8..C12 son textos fijos de la plantilla (ENTREGADO/RECIBIDO POR, GESTIÓN, Nro.): no se escriben aquí.
            // Se sobrescriben siempre para no dejar los valores de ejemplo de la plantilla.
            var culturaEsBo = new CultureInfo("es-BO");
            var fechaCronograma = esPractica ? plan!.ScheduledDate : departure.DepartureDate;
            var diaSemana = culturaEsBo.DateTimeFormat.GetDayName(fechaCronograma.DayOfWeek).ToUpper();
            string? horario = null;
            if (esPractica && plan!.StartTime.HasValue && plan.EndTime.HasValue)
            {
                horario = $"{plan.StartTime.Value:hh\\:mm}–{plan.EndTime.Value:hh\\:mm}";
            }

            worksheet.Cells[5, 4].Value = diaSemana;                // DIA
            worksheet.Cells[6, 4].Value = horario;                  // HORARIO (null limpia el ejemplo de la plantilla)
            worksheet.Cells[7, 4].Value = nombreLaboratorio;        // LABORATORIO
            worksheet.Cells[8, 2].Value = esPractica ? plan!.Subject?.Career?.Name?.ToUpper() : null;  // CARRERA
            worksheet.Cells[9, 2].Value = esPractica ? plan!.Subject?.Name?.ToUpper() : null;          // MATERIA
            worksheet.Cells[10, 2].Value = fechaCronograma.ToString("dd/MM/yyyy");                     // FECHA CRONOGRAMA
            worksheet.Cells[11, 2].Value = esPractica ? plan!.PracticeName?.ToUpper() : null;          // PRACTICA
            worksheet.Cells[12, 2].Value = null;                                                       // DESCRIPCION DE PRACTICA
            worksheet.Cells[13, 2].Value = esPractica ? plan!.GroupName?.ToUpper() : null;             // GRUPO
            worksheet.Cells[14, 2].Value = esPractica ? plan!.Subject?.Code?.ToUpper() : null;         // CODIGO ASIGNATURA

            // DYNAMIC METADATA REPLACEMENT
            for (int r = 1; r <= 20; r++)
            {
                for (int c = 1; c <= 8; c++)
                {
                    var text = worksheet.Cells[r, c].Text?.ToUpper()?.Trim() ?? "";

                    if (text == "LABORATORIO" || text == "LABORATORIO DE:" || text == "LABORATORIO:")
                        worksheet.Cells[r, c + 1].Value = nombreLaboratorio;

                    if (text.StartsWith("ENTREGADO POR"))
                        worksheet.Cells[r, c].Value = "ENTREGADO POR: " + (departure.CreatedBy?.FullName?.ToUpper() ?? "SISTEMA");

                    if (text.StartsWith("RECIBIDO POR"))
                        worksheet.Cells[r, c].Value = "RECIBIDO POR: " + (departure.Borrower?.FullName?.ToUpper() ?? "");

                    if (text.StartsWith("GESTIÓN:") || text.StartsWith("GESTION:"))
                        worksheet.Cells[r, c].Value = "GESTIÓN: " + gestionLabel;

                    if (text.StartsWith("NRO. DE SOLICITUD") || text.StartsWith("NRO DE SOLICITUD"))
                        worksheet.Cells[r, c].Value = solicitudLabel;

                    if (text == "DOCENTE/ESTUDIANTE:" || text == "DOCENTE/ESTUDIANTE" || text.StartsWith("DOCENTE"))
                        worksheet.Cells[r, c + 1].Value = departure.Borrower?.FullName?.ToUpper() ?? "";

                    if (text == "FECHA CRONOGRAMA" || text.StartsWith("FECHA CRONOGRAMA"))
                        worksheet.Cells[r, c + 1].Value = fechaCronograma.ToString("dd/MM/yyyy");
                }
            }

            // DYNAMIC TABLE MAPPING
            // Cabecera PRODUCTO..SALDO en la fila 16 de la plantilla (etiqueta impresa en la
            // imagen de fondo); el escaneo se mantiene por si la plantilla trae las etiquetas como texto.
            int headerRow = 16;
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

            // Pie del área de ítems: primera fila posterior con texto en la columna A o que abre un
            // rango combinado A:E (la línea de firma de la plantilla, fila 30). Si no existe, se asume
            // la geometría de la plantilla: 13 filas de ítems + firma.
            int footerRow = 0;
            for (int r = startRow; r <= startRow + 30; r++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cells[r, 1].Text))
                {
                    footerRow = r;
                    break;
                }
                if (IniciaRangoCombinadoAE(worksheet, r))
                {
                    footerRow = r;
                    break;
                }
            }
            if (footerRow <= headerRow) footerRow = headerRow + 14;

            int capacity = footerRow - startRow;

            // Limpiar SOLO el área de ítems (filas startRow..footerRow-1, columnas 1..5):
            // la línea de firma y las notas combinadas nunca se pisan ni se borran.
            for (int r = startRow; r <= footerRow - 1; r++)
            {
                for (int c = 1; c <= 5; c++)
                {
                    worksheet.Cells[r, c].Value = null;
                }
            }

            if (items.Count == 0)
            {
                items.Add(new DepartureItem
                {
                    ProductName = departure.EquipmentUnit?.Equipment?.Name ?? "Equipo Principal",
                    Quantity = 1m,
                    UnitOfMeasure = "UNIDAD",
                    ReturnedQuantity = 0m
                });
            }

            // Con más ítems que filas disponibles se insertan filas antes del pie (copiando el
            // estilo de la última fila de ítems) para que firma y notas bajen y no se pierdan cantidades.
            if (items.Count > capacity)
            {
                worksheet.InsertRow(footerRow, items.Count - capacity, footerRow - 1);
            }

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int currentRow = startRow + i;

                worksheet.Cells[currentRow, colProducto].Value = item.ProductName?.ToUpper() ?? "PENDIENTE POR EVIDENCIA";
                worksheet.Cells[currentRow, colCantidad].Value = item.Quantity;
                worksheet.Cells[currentRow, colUnidad].Value = item.UnitOfMeasure ?? "UNIDAD";
                worksheet.Cells[currentRow, colDevolucion].Value = item.ReturnedQuantity ?? 0;
                worksheet.Cells[currentRow, colSaldo].Value = item.Balance;
            }

            return SavePackage(package);
        }

        /// <summary>Indica si la fila abre un rango combinado que arranca en la columna A y cubre hasta la E o más (firma/notas del vale L-3).</summary>
        private static bool IniciaRangoCombinadoAE(ExcelWorksheet worksheet, int row)
        {
            foreach (var direccion in worksheet.MergedCells)
            {
                var rango = new ExcelAddress(direccion);
                if (rango.Start.Row == row && rango.Start.Column == 1 && rango.End.Column >= 5)
                {
                    return true;
                }
            }
            return false;
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
