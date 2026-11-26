using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

namespace Proyecto_Laboratorios_Univalle.Services
{
    /// <summary>
    /// Fila del informe L-9 de reprogramaciones y reposiciones de una gestión, ya formateada para
    /// mostrar. Tipo "Reprogramación" (movimiento manual de una práctica) o "Reposición"
    /// (reubicación de un bloque de un día suspendido, con el motivo de la suspensión).
    /// </summary>
    public sealed record L9ReportRow(
        int Numero,
        string Tipo,
        string CodigoPractica,
        string Materia,
        string Practica,
        string Grupo,
        string Docente,
        DateTime FechaOrigen,
        string PeriodosOrigen,
        string LaboratorioOrigen,
        DateTime FechaDestino,
        string PeriodosDestino,
        string LaboratorioDestino,
        string Motivo,
        string RegistradoPor,
        DateTime FechaRegistro);

    public partial class ReportService
    {
        /// <summary>
        /// Filas del L-9 de una gestión ordenadas por fecha de registro (y luego por Id). El rango
        /// opcional filtra por la FECHA ORIGINAL de la práctica (la del calendario que se movió),
        /// que siempre pertenece al calendario académico de la gestión consultada.
        /// </summary>
        public async Task<IReadOnlyList<L9ReportRow>> GetL9ReportRowsAsync(int managementId,
            DateTime? originFrom = null, DateTime? originTo = null, CancellationToken ct = default)
        {
            var consulta = _context.Reschedulings.AsNoTracking()
                .Where(r => r.OriginPlanning.ManagementId == managementId);
            if (originFrom.HasValue)
                consulta = consulta.Where(r => r.OriginDate >= originFrom.Value.Date);
            if (originTo.HasValue)
                consulta = consulta.Where(r => r.OriginDate < originTo.Value.Date.AddDays(1));

            var datos = await consulta
                .OrderBy(r => r.CreatedDate).ThenBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    EsReposicion = r.DayRecoveryId != null,
                    MotivoReprogramacion = r.Reason,
                    MotivoCategoria = r.DayRecovery != null ? r.DayRecovery.ReasonCategory : null,
                    MotivoReposicion = r.DayRecovery != null ? r.DayRecovery.Reason : null,
                    FechaOrigen = r.OriginDate,
                    OrdenOrigen = r.OriginStartPeriod != null ? (int?)r.OriginStartPeriod.Order : null,
                    CantidadOrigen = r.OriginPeriodCount,
                    LabOrigen = r.OriginLaboratory != null
                        ? r.OriginLaboratory.Code + " · " + r.OriginLaboratory.Name : null,
                    FechaDestino = r.DestinationDate,
                    OrdenDestino = r.DestinationStartPeriod != null ? (int?)r.DestinationStartPeriod.Order : null,
                    CantidadDestino = (int?)r.DestinationPeriodCount,
                    LabDestino = r.DestinationLaboratory != null
                        ? r.DestinationLaboratory.Code + " · " + r.DestinationLaboratory.Name : null,
                    DestinoId = r.DestinationPlanningId,
                    Codigo = r.DestinationPlanning.Code,
                    Materia = r.DestinationPlanning.Subject != null ? r.DestinationPlanning.Subject.Name : null,
                    r.DestinationPlanning.PracticeName,
                    r.DestinationPlanning.PracticeNumber,
                    Grupo = r.DestinationPlanning.GroupName,
                    DocenteId = r.DestinationTeacherPersonId,
                    CreadoPorId = r.CreatedById,
                    FechaRegistro = r.CreatedDate
                })
                .ToListAsync(ct);

            // Nombres visibles: docentes (Intern.Name) y usuarios del registro (FirstName + LastName);
            // Person.FullName es [NotMapped] y no se puede usar en consultas.
            var docenteIds = datos.Select(d => d.DocenteId).Distinct().ToList();
            var docentes = await _context.Interns.AsNoTracking()
                .Where(t => docenteIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);
            var usuarioIds = datos.Where(d => d.CreadoPorId.HasValue).Select(d => d.CreadoPorId!.Value).Distinct().ToList();
            var usuarios = await _context.Users.AsNoTracking()
                .Where(u => usuarioIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync(ct);
            var usuarioPorId = usuarios.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

            // Catálogo de periodos para las horas reales de cada bloque (Id ≠ Order en SQL Server).
            var periodosPorOrden = (await _context.ClassPeriods.AsNoTracking().ToListAsync(ct))
                .ToDictionary(p => p.Order);

            var filas = new List<L9ReportRow>(datos.Count);
            foreach (var d in datos)
            {
                var motivo = d.EsReposicion
                    ? string.IsNullOrWhiteSpace(d.MotivoCategoria)
                        ? d.MotivoReposicion ?? "—"
                        : $"{d.MotivoCategoria}: {d.MotivoReposicion}"
                    : string.IsNullOrWhiteSpace(d.MotivoReprogramacion) ? "—" : d.MotivoReprogramacion;
                filas.Add(new L9ReportRow(
                    filas.Count + 1,
                    d.EsReposicion ? "Reposición" : "Reprogramación",
                    L3Codes.Format(d.DestinoId, d.Codigo),
                    d.Materia ?? "Sin materia",
                    string.IsNullOrWhiteSpace(d.PracticeNumber)
                        ? d.PracticeName
                        : $"{d.PracticeNumber} · {d.PracticeName}",
                    string.IsNullOrWhiteSpace(d.Grupo) ? "—" : d.Grupo!,
                    docentes.GetValueOrDefault(d.DocenteId, "—"),
                    d.FechaOrigen,
                    PeriodosConHoras(d.OrdenOrigen, d.CantidadOrigen, periodosPorOrden),
                    d.LabOrigen ?? "Sin laboratorio",
                    d.FechaDestino,
                    PeriodosConHoras(d.OrdenDestino, d.CantidadDestino, periodosPorOrden),
                    d.LabDestino ?? "Sin laboratorio",
                    motivo,
                    d.CreadoPorId is int uid && usuarioPorId.TryGetValue(uid, out var nombre) ? nombre : "Sistema",
                    BoliviaTime.ToLocal(d.FechaRegistro)));
            }

            return filas;
        }

        /// <summary>"Periodos 2–5 · 08:35–12:05" con las horas reales del catálogo; "Sin periodos" si el bloque falta.</summary>
        private static string PeriodosConHoras(int? startOrder, int? count, IReadOnlyDictionary<int, ClassPeriod> porOrden)
        {
            if (startOrder is not int orden || count is not int cantidad || cantidad < 1) return "Sin periodos";
            if (!porOrden.TryGetValue(orden, out var inicio)) return "Sin periodos";
            var texto = PlanningSlot.PeriodsText(orden, cantidad);
            if (!porOrden.TryGetValue(PlanningSlot.LastOrder(orden, cantidad), out var fin)) return texto;
            return $"{texto} · {inicio.StartTime:hh\\:mm}–{fin.EndTime:hh\\:mm}";
        }

        /// <summary>
        /// Excel del L-9 (sin plantilla institucional: se arma con EPPlus desde cero). Encabezado
        /// sobrio con título, gestión, rango consultado y fecha de emisión; tabla con autofiltros,
        /// anchos legibles y paneles congelados. Sin filas muestra una nota en lugar de quedar vacío.
        /// </summary>
        public async Task<byte[]> GenerateL9ReprogramacionesExcel(int managementId,
            DateTime? originFrom = null, DateTime? originTo = null)
        {
            var management = await _context.Managements.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == managementId)
                ?? throw new KeyNotFoundException("Gestión no encontrada.");
            var filas = await GetL9ReportRowsAsync(managementId, originFrom, originTo);

            var gestionLabel = management.Semester is 1 or 2
                ? $"{(management.Semester == 1 ? "I" : "II")}/{management.Year}"
                : management.Code;
            var rangoLabel = originFrom.HasValue || originTo.HasValue
                ? $"Periodo consultado: {(originFrom.HasValue ? originFrom.Value.ToString("dd/MM/yyyy") : "inicio")} al " +
                  $"{(originTo.HasValue ? originTo.Value.ToString("dd/MM/yyyy") : "hoy")} · por fecha original de la práctica"
                : "Periodo consultado: toda la gestión · por fecha original de la práctica";

            string[] encabezados =
            [
                "N.º", "Tipo", "Práctica", "Materia", "N.º / nombre de práctica", "Grupo", "Docente",
                "Fecha original", "Periodos y horas de origen", "Laboratorio de origen",
                "Fecha de destino", "Periodos y horas de destino", "Laboratorio de destino",
                "Motivo", "Registrado por", "Fecha de registro"
            ];
            int[] anchos = [5, 15, 12, 24, 26, 8, 24, 12, 24, 22, 12, 24, 22, 42, 20, 17];
            const int filaEncabezado = 6;
            const int primeraColumna = 1;
            var ultimaColumna = encabezados.Length;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("L-9");

            ws.Cells[1, 1].Value = "L-9 · INFORME DE REPROGRAMACIONES Y REPOSICIONES";
            ws.Cells[1, 1, 1, ultimaColumna].Merge = true;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Row(1).Height = 24;

            ws.Cells[2, 1].Value = $"Gestión: {gestionLabel}";
            ws.Cells[3, 1].Value = rangoLabel;
            ws.Cells[4, 1].Value = $"Fecha de emisión: {BoliviaTime.ToLocal(DateTime.UtcNow):dd/MM/yyyy HH:mm}";
            ws.Cells[2, 1, 4, 1].Style.Font.Size = 11;

            for (var c = 0; c < encabezados.Length; c++)
            {
                var celda = ws.Cells[filaEncabezado, primeraColumna + c];
                celda.Value = encabezados[c];
                celda.Style.Font.Bold = true;
                celda.Style.Font.Color.SetColor(255, 255, 255, 255);
                SetL9Fondo(celda, 48, 84, 150);
                celda.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                celda.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                celda.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                ws.Column(primeraColumna + c).Width = anchos[c];
            }
            ws.Row(filaEncabezado).Height = 28;

            var ultimaFila = filaEncabezado;
            foreach (var fila in filas)
            {
                ultimaFila++;
                ws.Cells[ultimaFila, 1].Value = fila.Numero;
                ws.Cells[ultimaFila, 2].Value = fila.Tipo;
                ws.Cells[ultimaFila, 3].Value = fila.CodigoPractica;
                ws.Cells[ultimaFila, 4].Value = fila.Materia;
                ws.Cells[ultimaFila, 5].Value = fila.Practica;
                ws.Cells[ultimaFila, 6].Value = fila.Grupo;
                ws.Cells[ultimaFila, 7].Value = fila.Docente;

                ws.Cells[ultimaFila, 8].Value = fila.FechaOrigen;
                ws.Cells[ultimaFila, 8].Style.Numberformat.Format = "dd/MM/yyyy";
                ws.Cells[ultimaFila, 9].Value = fila.PeriodosOrigen;
                ws.Cells[ultimaFila, 10].Value = fila.LaboratorioOrigen;

                ws.Cells[ultimaFila, 11].Value = fila.FechaDestino;
                ws.Cells[ultimaFila, 11].Style.Numberformat.Format = "dd/MM/yyyy";
                ws.Cells[ultimaFila, 12].Value = fila.PeriodosDestino;
                ws.Cells[ultimaFila, 13].Value = fila.LaboratorioDestino;

                ws.Cells[ultimaFila, 14].Value = fila.Motivo;
                ws.Cells[ultimaFila, 14].Style.WrapText = true;
                ws.Cells[ultimaFila, 15].Value = fila.RegistradoPor;
                ws.Cells[ultimaFila, 16].Value = fila.FechaRegistro;
                ws.Cells[ultimaFila, 16].Style.Numberformat.Format = "dd/MM/yyyy HH:mm";

                for (var c = 1; c <= ultimaColumna; c++)
                {
                    ws.Cells[ultimaFila, c].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    ws.Cells[ultimaFila, c].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                }
            }

            if (filas.Count == 0)
            {
                ultimaFila++;
                ws.Cells[ultimaFila, 1, ultimaFila, ultimaColumna].Merge = true;
                ws.Cells[ultimaFila, 1].Value = "Sin reprogramaciones ni reposiciones en el rango consultado.";
                ws.Cells[ultimaFila, 1].Style.Font.Italic = true;
                ws.Cells[ultimaFila, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Filtros por columna y encabezado fijo al hacer scroll.
            ws.Cells[filaEncabezado, primeraColumna, ultimaFila, ultimaColumna].AutoFilter = true;
            ws.View.FreezePanes(filaEncabezado + 1, primeraColumna);

            return SavePackage(package);
        }

        /// <summary>Relleno sólido con RGB directo (sin System.Drawing.Color, como en el resto de los parciales).</summary>
        private static void SetL9Fondo(ExcelRange celda, int r, int g, int b)
        {
            celda.Style.Fill.PatternType = ExcelFillStyle.Solid;
            celda.Style.Fill.BackgroundColor.SetColor(255, r, g, b);
        }
    }
}
