using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    /// <summary>
    /// Reglas para leer los cambios de material del docente (decididas con el usuario el 24/11/2026).
    /// Cambiar CANTIDADES de los mismos artículos es normal (casi siempre por el número de estudiantes);
    /// AGREGAR o QUITAR artículos muestra falta de planificación. Un cambio pedido con poca anticipación
    /// a la práctica es «último momento».
    /// </summary>
    public static class MaterialChangeRules
    {
        /// <summary>Un cambio pedido con menos de estos días de anticipación a la práctica es «último momento».</summary>
        public const int OnTimeDays = 7;

        /// <summary>Desde cuántos artículos agregados o quitados en una misma práctica se marca «llamada de atención».</summary>
        public const int WarningItemCount = 3;

        public const string QuantityKind = "Cantidad";
        public const string ItemKind = "Artículo";

        /// <summary>«Agregó» y «Quitó» cambian artículos; «Aumentó» y «Disminuyó» solo cantidades.</summary>
        public static bool IsItemChange(string change) => change is "Agregó" or "Quitó";
    }

    /// <summary>
    /// Una fila del detalle del reporte «Cambios de material del docente»: un artículo cuya cantidad
    /// difiere entre lo que el docente pidió en su solicitud y el kit escalado de la práctica.
    /// <paramref name="Cambio"/> es Aumentó / Disminuyó / Agregó / Quitó según la comparación;
    /// <paramref name="Tipo"/> es «Cantidad» o «Artículo». <paramref name="LlamadaAtencion"/> marca TODAS las
    /// filas de una solicitud con <see cref="MaterialChangeRules.WarningItemCount"/> o más artículos agregados
    /// o quitados, y <paramref name="UltimoMomento"/> las pedidas con menos de
    /// <see cref="MaterialChangeRules.OnTimeDays"/> días de anticipación.
    /// </summary>
    public sealed record MaterialChangeRow(
        DateTime FechaSolicitud,
        DateTime FechaPractica,
        int DiasAnticipacion,
        string Docente,
        string Materia,
        string Grupo,
        string Practica,
        string Articulo,
        string? Categoria,
        decimal Kit,
        decimal Pedido,
        decimal Diferencia,
        string Cambio,
        string Tipo,
        bool LlamadaAtencion,
        bool UltimoMomento,
        string Estado,
        string? Observacion);

    /// <summary>
    /// Totales de cambios de material de un docente dentro del rango consultado. Las solicitudes con cambios
    /// se dividen en «solo cantidades» y «cambió artículos»; las llamadas de atención y las de último momento
    /// cuentan solicitudes (no artículos).
    /// </summary>
    public sealed record MaterialChangeTeacherSummary(
        string Docente,
        int Solicitudes,
        int ConCambios,
        int SoloCantidades,
        int CambioArticulos,
        int LlamadasAtencion,
        int UltimoMomento,
        int ArticulosCambiados,
        int Aumentos,
        int Disminuciones,
        int Agregados,
        int Quitados,
        int Reenvios);

    /// <summary>Resultado completo del reporte: detalle por artículo y resumen por docente.</summary>
    public sealed record MaterialChangesReport(
        IReadOnlyList<MaterialChangeRow> Rows,
        IReadOnlyList<MaterialChangeTeacherSummary> Summaries);

    public partial class ReportService
    {
        /// <summary>Acumulador interno del resumen por docente (antes de volcarlo al record público).</summary>
        private sealed class CambiosDocente
        {
            public int Solicitudes;
            public int ConCambios;
            public int SoloCantidades;
            public int CambioArticulos;
            public int LlamadasAtencion;
            public int UltimoMomento;
            public int ArticulosCambiados;
            public int Aumentos;
            public int Disminuciones;
            public int Agregados;
            public int Quitados;
            public int Reenvios;
        }

        /// <summary>
        /// Compara cada solicitud de práctica que un docente envió por el sistema (planificación con al
        /// menos un review «Enviada», sin borradas) con el kit escalado de su práctica, artículo por artículo.
        /// La fecha de la solicitud es la del PRIMER review «Enviada» en hora local de Bolivia, y es la que
        /// filtran desde/hasta (ambos inclusive por día). Las solicitudes sin programación se omiten.
        /// Lo que cuenta del docente es lo que ÉL pidió (PreviousQuantity de la línea de review más antigua),
        /// no los ajustes que la Inge hizo al aprobar con cambios.
        /// </summary>
        public async Task<MaterialChangesReport> GetMaterialChangesAsync(int managementId,
            DateTime? from = null, DateTime? to = null, int? teacherPersonId = null, CancellationToken ct = default)
        {
            // 1) Solicitudes de docentes de la gestión: con al menos un review «Enviada»; excluye borradas.
            var consulta = _context.Plannings.AsNoTracking()
                .Where(p => !p.IsDeleted && p.ManagementId == managementId
                    && p.Reviews.Any(r => r.Action == PlanningReviewAction.Submitted));
            if (teacherPersonId.HasValue)
                consulta = consulta.Where(p => p.TeacherPersonId == teacherPersonId.Value);

            var solicitudes = await consulta
                .Select(p => new
                {
                    p.Id, p.TeacherPersonId, p.PracticeScheduleId, p.CourseGroupId,
                    Grupo = p.GroupName,
                    Materia = p.Subject != null ? p.Subject.Name : null,
                    p.PracticeName, p.Status, p.Observations, p.ScheduledDate, p.StudentCount
                })
                .ToListAsync(ct);
            if (solicitudes.Count == 0) return new MaterialChangesReport([], []);

            var planIds = solicitudes.Select(s => s.Id).ToList();

            // 2) Historial de revisión de esas solicitudes y sus líneas (ajustes «Aprobada con cambios»).
            var reviews = await _context.PlanningReviews.AsNoTracking()
                .Where(r => planIds.Contains(r.PlanningId))
                .Select(r => new { r.Id, r.PlanningId, r.Action, r.CreatedDate })
                .ToListAsync(ct);
            var reviewIds = reviews.Select(r => r.Id).ToList();
            var reviewLines = await _context.Set<PlanningReviewLine>().AsNoTracking()
                .Where(l => reviewIds.Contains(l.PlanningReviewId))
                .Select(l => new { l.Id, l.PlanningReviewId, l.ArticleId, l.PreviousQuantity, l.NewQuantity })
                .ToListAsync(ct);
            var reviewPorId = reviews.ToDictionary(r => r.Id);

            // 3) Grupos (estudiantes y factor de escalamiento) y programaciones (práctica del kit).
            var groupIds = solicitudes.Where(s => s.CourseGroupId.HasValue)
                .Select(s => s.CourseGroupId!.Value).Distinct().ToList();
            var grupos = new Dictionary<int, (int? StudentCount, int? ScalingFactor)>();
            if (groupIds.Count > 0)
            {
                grupos = (await _context.CourseGroups.AsNoTracking()
                        .Where(g => groupIds.Contains(g.Id))
                        .Select(g => new { g.Id, g.StudentCount, g.ScalingFactor })
                        .ToListAsync(ct))
                    .ToDictionary(g => g.Id, g => (g.StudentCount, g.ScalingFactor));
            }

            var scheduleIds = solicitudes.Where(s => s.PracticeScheduleId.HasValue)
                .Select(s => s.PracticeScheduleId!.Value).Distinct().ToList();
            var practicaPorSchedule = new Dictionary<int, int>();
            if (scheduleIds.Count > 0)
            {
                practicaPorSchedule = await _context.PracticeSchedules.AsNoTracking()
                    .Where(ps => scheduleIds.Contains(ps.Id))
                    .ToDictionaryAsync(ps => ps.Id, ps => ps.PracticeId, ct);
            }

            // 4) Kits de las prácticas involucradas: misma regla que OnGetKitAsync de NuevaSolicitud
            //    (líneas no retiradas con artículo activo). El escalado es por solicitud: estudiantes
            //    de la planificación (o del grupo) y factor del grupo.
            var practiceIds = practicaPorSchedule.Values.Distinct().ToList();
            var kitPorPractica = new Dictionary<int, List<RequirementPickerService.KitLine>>();
            if (practiceIds.Count > 0)
            {
                var kitRows = await _context.PracticeRequirements.AsNoTracking()
                    .Where(r => practiceIds.Contains(r.PracticeId) && !r.IsRemoved
                        && r.Article!.Status == GeneralStatus.Activo)
                    .Select(r => new { r.PracticeId, r.ArticleId, r.Quantity, r.IsScalable })
                    .ToListAsync(ct);
                kitPorPractica = kitRows
                    .GroupBy(k => k.PracticeId)
                    .ToDictionary(g => g.Key,
                        g => g.Select(k => new RequirementPickerService.KitLine(k.ArticleId, k.Quantity, k.IsScalable)).ToList());
            }

            // 5) Material pedido por el docente en cada solicitud (RequestedQuantity ?? ExpectedQuantity).
            var pedidos = await _context.PlanningRequirements.AsNoTracking()
                .Where(r => planIds.Contains(r.PlanningId))
                .Select(r => new { r.PlanningId, r.ArticleId, r.RequestedQuantity, r.ExpectedQuantity })
                .ToListAsync(ct);

            // 6) Nombres y categorías de todos los artículos involucrados (kit ∪ pedido ∪ review lines).
            var articleIds = kitPorPractica.Values.SelectMany(v => v).Select(k => k.ArticleId)
                .Concat(pedidos.Select(p => p.ArticleId))
                .Concat(reviewLines.Select(l => l.ArticleId))
                .Distinct().ToList();
            var articulos = new Dictionary<int, (string? Articulo, string? Categoria)>();
            if (articleIds.Count > 0)
            {
                articulos = (await _context.Articles.AsNoTracking()
                        .Where(a => articleIds.Contains(a.Id))
                        .Select(a => new { a.Id, a.Code, a.Name, Categoria = a.ArticleCategory != null ? a.ArticleCategory.Name : a.Category })
                        .ToListAsync(ct))
                    .ToDictionary(a => a.Id, a => (Articulo: (string?)$"{a.Code} · {a.Name}", a.Categoria));
            }

            // 7) Nombre visible del docente: Intern.Name (Person.FullName es NotMapped y no se puede consultar).
            var teacherIds = solicitudes.Select(s => s.TeacherPersonId).Distinct().ToList();
            var docentes = await _context.Interns.AsNoTracking()
                .Where(t => teacherIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

            // ===== Cálculo en memoria =====
            var reviewPorPlanning = reviews.GroupBy(r => r.PlanningId).ToDictionary(g => g.Key, g => g.ToList());
            var lineasPorReview = reviewLines.GroupBy(l => l.PlanningReviewId).ToDictionary(g => g.Key, g => g.ToList());
            var pedidoPorPlanning = pedidos.GroupBy(p => p.PlanningId).ToDictionary(g => g.Key, g => g.ToList());

            var filas = new List<MaterialChangeRow>();
            var resumenPorDocente = new Dictionary<int, CambiosDocente>();

            foreach (var s in solicitudes)
            {
                // Fecha de la solicitud: PRIMER review «Enviada», pasada a hora local de Bolivia.
                var primeraEnvio = reviewPorPlanning.GetValueOrDefault(s.Id)
                    ?.Where(r => r.Action == PlanningReviewAction.Submitted)
                    .OrderBy(r => r.CreatedDate).ThenBy(r => r.Id)
                    .FirstOrDefault();
                if (primeraEnvio == null) continue;
                var fechaSolicitud = BoliviaTime.ToLocal(primeraEnvio.CreatedDate).Date;
                if (from.HasValue && fechaSolicitud < from.Value.Date) continue;
                if (to.HasValue && fechaSolicitud > to.Value.Date) continue;

                // Sin programación la solicitud se omite: no tiene práctica ni kit contra qué comparar.
                if (s.PracticeScheduleId is not int scheduleId
                    || !practicaPorSchedule.TryGetValue(scheduleId, out var practiceId)) continue;

                var grupo = s.CourseGroupId.HasValue ? grupos.GetValueOrDefault(s.CourseGroupId.Value) : default;
                var kit = kitPorPractica.TryGetValue(practiceId, out var lineasKit)
                    ? RequirementPickerService.ScaleKit(lineasKit, s.StudentCount ?? grupo.StudentCount, grupo.ScalingFactor)
                    : new Dictionary<int, (decimal Quantity, string? Note)>();

                // Pedido del docente por artículo, según las líneas de su solicitud.
                var pedido = new Dictionary<int, decimal>();
                foreach (var req in pedidoPorPlanning.GetValueOrDefault(s.Id) ?? [])
                    pedido[req.ArticleId] = pedido.GetValueOrDefault(req.ArticleId)
                        + (req.RequestedQuantity ?? req.ExpectedQuantity);

                // Si la Inge aprobó con cambios, la cantidad del docente es la PreviousQuantity de la
                // línea MÁS ANTIGUA de ese artículo (null = el docente no lo pidió; nunca los ajustes de ella).
                var pedidoDocente = new Dictionary<int, decimal>();
                var lineasSolicitud = (reviewPorPlanning.GetValueOrDefault(s.Id) ?? [])
                    .SelectMany(r => lineasPorReview.GetValueOrDefault(r.Id) ?? []);
                foreach (var grupoLineas in lineasSolicitud.GroupBy(l => l.ArticleId))
                {
                    var masAntigua = grupoLineas
                        .OrderBy(l => reviewPorId[l.PlanningReviewId].CreatedDate).ThenBy(l => l.Id)
                        .First();
                    pedidoDocente[grupoLineas.Key] = masAntigua.PreviousQuantity ?? 0m;
                }

                // Clasificación por artículo sobre la unión kit ∪ pedido; lo ausente cuenta 0.
                var cambios = new List<(int ArticleId, decimal KitQty, decimal PedidoQty, decimal Diferencia, string Cambio)>();
                foreach (var articleId in kit.Keys.Union(pedidoDocente.Keys).Union(pedido.Keys))
                {
                    var kitQty = kit.GetValueOrDefault(articleId).Quantity;
                    var pedidoQty = pedidoDocente.TryGetValue(articleId, out var delDocente)
                        ? delDocente
                        : pedido.GetValueOrDefault(articleId);
                    var cambio =
                        kitQty == 0 && pedidoQty > 0 ? "Agregó"
                        : kitQty > 0 && pedidoQty == 0 ? "Quitó"
                        : pedidoQty > kitQty ? "Aumentó"
                        : pedidoQty > 0 && pedidoQty < kitQty ? "Disminuyó"
                        : null;   // iguales: sin cambio, no va al detalle
                    if (cambio == null) continue;
                    cambios.Add((articleId, kitQty, pedidoQty, pedidoQty - kitQty, cambio));
                }

                if (!resumenPorDocente.TryGetValue(s.TeacherPersonId, out var resumen))
                    resumenPorDocente[s.TeacherPersonId] = resumen = new CambiosDocente();
                resumen.Solicitudes++;
                resumen.Reenvios += (reviewPorPlanning.GetValueOrDefault(s.Id) ?? [])
                    .Count(rv => rv.Action == PlanningReviewAction.Resubmitted);

                // Lectura de la solicitud: cuántos artículos agregó o quitó (los de cantidad no cuentan para
                // la llamada de atención) y con cuántos días de anticipación a la práctica la pidió.
                var cambiosDeArticulo = cambios.Count(c => MaterialChangeRules.IsItemChange(c.Cambio));
                var llamadaAtencion = cambiosDeArticulo >= MaterialChangeRules.WarningItemCount;
                var diasAnticipacion = (s.ScheduledDate.Date - fechaSolicitud).Days;
                var ultimoMomento = cambios.Count > 0 && diasAnticipacion < MaterialChangeRules.OnTimeDays;

                var nombreDocente = docentes.GetValueOrDefault(s.TeacherPersonId, "—");
                foreach (var (articleId, kitQty, pedidoQty, diferencia, cambio) in cambios)
                {
                    var articulo = articulos.GetValueOrDefault(articleId);
                    filas.Add(new MaterialChangeRow(
                        fechaSolicitud,
                        s.ScheduledDate,
                        diasAnticipacion,
                        nombreDocente,
                        s.Materia ?? "Sin materia",
                        string.IsNullOrWhiteSpace(s.Grupo) ? "—" : s.Grupo!,
                        s.PracticeName,
                        articulo.Articulo ?? $"Artículo {articleId}",
                        articulo.Categoria,
                        kitQty, pedidoQty, diferencia, cambio,
                        MaterialChangeRules.IsItemChange(cambio) ? MaterialChangeRules.ItemKind : MaterialChangeRules.QuantityKind,
                        llamadaAtencion,
                        ultimoMomento,
                        s.Status,
                        s.Observations));
                }
                resumen.ConCambios += cambios.Count > 0 ? 1 : 0;
                resumen.SoloCantidades += cambios.Count > 0 && cambiosDeArticulo == 0 ? 1 : 0;
                resumen.CambioArticulos += cambiosDeArticulo > 0 ? 1 : 0;
                resumen.LlamadasAtencion += llamadaAtencion ? 1 : 0;
                resumen.UltimoMomento += ultimoMomento ? 1 : 0;
                resumen.ArticulosCambiados += cambios.Count;
                foreach (var c in cambios)
                {
                    switch (c.Cambio)
                    {
                        case "Aumentó": resumen.Aumentos++; break;
                        case "Disminuyó": resumen.Disminuciones++; break;
                        case "Agregó": resumen.Agregados++; break;
                        default: resumen.Quitados++; break;
                    }
                }
            }

            // Detalle: docente, fecha de solicitud, práctica y artículo. Resumen: primero quien tiene más
            // llamadas de atención, luego más solicitudes con artículos cambiados, de último momento y con
            // cambios; al final alfabético por docente (incluye los de 0 cambios).
            var rows = filas
                .OrderBy(f => f.Docente).ThenBy(f => f.FechaSolicitud).ThenBy(f => f.Practica).ThenBy(f => f.Articulo)
                .ToList();
            var summaries = resumenPorDocente
                .Select(kv => new MaterialChangeTeacherSummary(
                    docentes.GetValueOrDefault(kv.Key, "—"),
                    kv.Value.Solicitudes, kv.Value.ConCambios, kv.Value.SoloCantidades, kv.Value.CambioArticulos,
                    kv.Value.LlamadasAtencion, kv.Value.UltimoMomento, kv.Value.ArticulosCambiados,
                    kv.Value.Aumentos, kv.Value.Disminuciones, kv.Value.Agregados, kv.Value.Quitados, kv.Value.Reenvios))
                .OrderByDescending(x => x.LlamadasAtencion)
                .ThenByDescending(x => x.CambioArticulos)
                .ThenByDescending(x => x.UltimoMomento)
                .ThenByDescending(x => x.ConCambios)
                .ThenByDescending(x => x.ArticulosCambiados)
                .ThenBy(x => x.Docente)
                .ToList();

            return new MaterialChangesReport(rows, summaries);
        }

        /// <summary>
        /// Excel del reporte de cambios de material: hoja «Resumen por docente» (incluye docentes sin cambios)
        /// y hoja «Detalle» con una fila por artículo cambiado. Sin plantilla institucional: EPPlus desde cero,
        /// sin System.Drawing; fechas dd/MM/yyyy, cantidades con hasta 2 decimales, columnas autoajustadas
        /// (ancho máximo 60) y panel inmovilizado bajo los encabezados. Sin filas deja una nota por hoja.
        /// </summary>
        public async Task<byte[]> GenerateMaterialChangesExcel(int managementId,
            DateTime? from = null, DateTime? to = null, int? teacherPersonId = null)
        {
            var management = await _context.Managements.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == managementId)
                ?? throw new KeyNotFoundException("Gestión no encontrada.");
            var reporte = await GetMaterialChangesAsync(managementId, from, to, teacherPersonId);

            var rango = from.HasValue && to.HasValue
                ? $"{from.Value:dd/MM/yyyy} al {to.Value:dd/MM/yyyy}"
                : from.HasValue ? $"desde {from.Value:dd/MM/yyyy}"
                : to.HasValue ? $"hasta {to.Value:dd/MM/yyyy}"
                : "todo el periodo";
            var titulo = $"CAMBIOS DE MATERIAL DEL DOCENTE · {management.Code} · {rango}";

            using var package = new ExcelPackage();
            const int filaEncabezado = 3;

            // Fila 2 de ambas hojas: cómo leer las alertas (reglas acordadas en MaterialChangeRules).
            var leyenda = $"Llamada de atención: {MaterialChangeRules.WarningItemCount} o más artículos agregados o quitados en una misma práctica. "
                + $"Último momento: pedido con menos de {MaterialChangeRules.OnTimeDays} días de anticipación a la práctica. "
                + "Cambiar solo cantidades de los mismos artículos no cuenta como llamada de atención.";

            // ===== Hoja 1: Resumen por docente =====
            string[] encabezadosResumen =
            [
                "Docente", "Solicitudes", "Con cambios", "Solo cantidades", "Cambió artículos",
                $"Llamadas de atención ({MaterialChangeRules.WarningItemCount}+ artículos)",
                $"A último momento (menos de {MaterialChangeRules.OnTimeDays} días)",
                "Artículos cambiados", "Aumentos", "Disminuciones", "Agregados", "Quitados", "Reenvíos"
            ];
            var wsResumen = package.Workbook.Worksheets.Add("Resumen por docente");
            EscribirTituloCambios(wsResumen, titulo, encabezadosResumen.Length);
            EscribirLeyendaCambios(wsResumen, leyenda, encabezadosResumen.Length);
            EscribirEncabezadosCambios(wsResumen, filaEncabezado, encabezadosResumen);
            var ultimaFila = filaEncabezado;
            foreach (var s in reporte.Summaries)
            {
                ultimaFila++;
                wsResumen.Cells[ultimaFila, 1].Value = s.Docente;
                int[] valores =
                [
                    s.Solicitudes, s.ConCambios, s.SoloCantidades, s.CambioArticulos, s.LlamadasAtencion, s.UltimoMomento,
                    s.ArticulosCambiados, s.Aumentos, s.Disminuciones, s.Agregados, s.Quitados, s.Reenvios
                ];
                for (var i = 0; i < valores.Length; i++)
                    wsResumen.Cells[ultimaFila, i + 2].Value = valores[i];
                BordesCambios(wsResumen, ultimaFila, encabezadosResumen.Length);
                if (s.LlamadasAtencion > 0) ResaltarAlerta(wsResumen.Cells[ultimaFila, 6], esLlamada: true);
                if (s.UltimoMomento > 0) ResaltarAlerta(wsResumen.Cells[ultimaFila, 7], esLlamada: false);
            }
            if (reporte.Summaries.Count == 0)
                ultimaFila = EscribirNotaVacio(wsResumen, ultimaFila, encabezadosResumen.Length);
            AplicarFormatoFinal(wsResumen, filaEncabezado, ultimaFila, encabezadosResumen.Length);

            // ===== Hoja 2: Detalle =====
            string[] encabezadosDetalle =
            [
                "Fecha de solicitud", "Fecha de práctica", "Días de anticipación", "Docente", "Materia", "Grupo",
                "Práctica", "Artículo", "Categoría", "Kit", "Pedido", "Diferencia", "Cambio", "Tipo", "Alerta",
                "Estado", "Observación del docente"
            ];
            var wsDetalle = package.Workbook.Worksheets.Add("Detalle");
            EscribirTituloCambios(wsDetalle, titulo, encabezadosDetalle.Length);
            EscribirLeyendaCambios(wsDetalle, leyenda, encabezadosDetalle.Length);
            EscribirEncabezadosCambios(wsDetalle, filaEncabezado, encabezadosDetalle);
            ultimaFila = filaEncabezado;
            foreach (var f in reporte.Rows)
            {
                ultimaFila++;
                EscribirFecha(wsDetalle, ultimaFila, 1, f.FechaSolicitud);
                EscribirFecha(wsDetalle, ultimaFila, 2, f.FechaPractica);
                wsDetalle.Cells[ultimaFila, 3].Value = f.DiasAnticipacion;
                wsDetalle.Cells[ultimaFila, 4].Value = f.Docente;
                wsDetalle.Cells[ultimaFila, 5].Value = f.Materia;
                wsDetalle.Cells[ultimaFila, 6].Value = f.Grupo;
                wsDetalle.Cells[ultimaFila, 7].Value = f.Practica;
                wsDetalle.Cells[ultimaFila, 8].Value = f.Articulo;
                wsDetalle.Cells[ultimaFila, 9].Value = f.Categoria ?? "—";
                EscribirCantidad(wsDetalle, ultimaFila, 10, f.Kit);
                EscribirCantidad(wsDetalle, ultimaFila, 11, f.Pedido);
                EscribirCantidad(wsDetalle, ultimaFila, 12, f.Diferencia);
                wsDetalle.Cells[ultimaFila, 13].Value = f.Cambio;
                wsDetalle.Cells[ultimaFila, 14].Value = f.Tipo;
                wsDetalle.Cells[ultimaFila, 15].Value = string.Join(" · ", new[]
                {
                    f.LlamadaAtencion ? "Llamada de atención" : null,
                    f.UltimoMomento ? "Último momento" : null
                }.Where(t => t != null));
                wsDetalle.Cells[ultimaFila, 16].Value = f.Estado;
                wsDetalle.Cells[ultimaFila, 17].Value = f.Observacion;
                wsDetalle.Cells[ultimaFila, 17].Style.WrapText = true;
                BordesCambios(wsDetalle, ultimaFila, encabezadosDetalle.Length);
                if (f.LlamadaAtencion || f.UltimoMomento)
                    ResaltarAlerta(wsDetalle.Cells[ultimaFila, 15], esLlamada: f.LlamadaAtencion);
            }
            if (reporte.Rows.Count == 0)
                ultimaFila = EscribirNotaVacio(wsDetalle, ultimaFila, encabezadosDetalle.Length);
            AplicarFormatoFinal(wsDetalle, filaEncabezado, ultimaFila, encabezadosDetalle.Length);

            return SavePackage(package);
        }

        /// <summary>Título fusionado en la fila 1 del reporte.</summary>
        private static void EscribirTituloCambios(ExcelWorksheet ws, string titulo, int columnas)
        {
            ws.Cells[1, 1].Value = titulo;
            ws.Cells[1, 1, 1, columnas].Merge = true;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Row(1).Height = 24;
        }

        /// <summary>Fila 2: cómo leer las alertas, fusionada sobre todas las columnas y en cursiva.</summary>
        private static void EscribirLeyendaCambios(ExcelWorksheet ws, string leyenda, int columnas)
        {
            ws.Cells[2, 1].Value = leyenda;
            ws.Cells[2, 1, 2, columnas].Merge = true;
            ws.Cells[2, 1].Style.Font.Italic = true;
            ws.Cells[2, 1].Style.Font.Size = 9;
            ws.Cells[2, 1].Style.WrapText = true;
            ws.Cells[2, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Row(2).Height = 30;
        }

        /// <summary>Fondo de alerta: rojo suave para la llamada de atención y ámbar para el último momento (RGB, sin System.Drawing).</summary>
        private static void ResaltarAlerta(ExcelRange celda, bool esLlamada)
        {
            celda.Style.Fill.PatternType = ExcelFillStyle.Solid;
            if (esLlamada) celda.Style.Fill.BackgroundColor.SetColor(255, 248, 215, 218);
            else celda.Style.Fill.BackgroundColor.SetColor(255, 255, 236, 179);
            celda.Style.Font.Bold = true;
        }

        /// <summary>Fila de encabezados en negrita con fondo (RGB directo, sin System.Drawing).</summary>
        private static void EscribirEncabezadosCambios(ExcelWorksheet ws, int fila, string[] encabezados)
        {
            for (var c = 0; c < encabezados.Length; c++)
            {
                var celda = ws.Cells[fila, c + 1];
                celda.Value = encabezados[c];
                celda.Style.Font.Bold = true;
                celda.Style.Font.Color.SetColor(255, 255, 255, 255);
                celda.Style.Fill.PatternType = ExcelFillStyle.Solid;
                celda.Style.Fill.BackgroundColor.SetColor(255, 48, 84, 150);
                celda.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                celda.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                celda.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
            ws.Row(fila).Height = 28;
        }

        private static void BordesCambios(ExcelWorksheet ws, int fila, int columnas)
        {
            for (var c = 1; c <= columnas; c++)
            {
                ws.Cells[fila, c].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                ws.Cells[fila, c].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }
        }

        /// <summary>Fila de nota cuando la hoja no tiene datos (el archivo abre igual).</summary>
        private static int EscribirNotaVacio(ExcelWorksheet ws, int ultimaFila, int columnas)
        {
            ultimaFila++;
            ws.Cells[ultimaFila, 1, ultimaFila, columnas].Merge = true;
            ws.Cells[ultimaFila, 1].Value = "Sin solicitudes de docentes en el rango.";
            ws.Cells[ultimaFila, 1].Style.Font.Italic = true;
            ws.Cells[ultimaFila, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            return ultimaFila;
        }

        /// <summary>Autofiltros, panel inmovilizado bajo los encabezados y anchos autoajustados (máximo 60).</summary>
        private static void AplicarFormatoFinal(ExcelWorksheet ws, int filaEncabezado, int ultimaFila, int columnas)
        {
            ws.Cells[filaEncabezado, 1, ultimaFila, columnas].AutoFilter = true;
            ws.View.FreezePanes(filaEncabezado + 1, 1);
            ws.Cells[filaEncabezado, 1, ultimaFila, columnas].AutoFitColumns();
            for (var c = 1; c <= columnas; c++)
                ws.Column(c).Width = Math.Clamp(ws.Column(c).Width, 8, 60);
        }

        private static void EscribirFecha(ExcelWorksheet ws, int fila, int columna, DateTime fecha)
        {
            ws.Cells[fila, columna].Value = fecha;
            ws.Cells[fila, columna].Style.Numberformat.Format = "dd/MM/yyyy";
        }

        /// <summary>Cantidad con hasta 2 decimales visibles (5 se ve «5», 4.5 se ve «4,5» según configuración).</summary>
        private static void EscribirCantidad(ExcelWorksheet ws, int fila, int columna, decimal cantidad)
        {
            ws.Cells[fila, columna].Value = cantidad;
            ws.Cells[fila, columna].Style.Numberformat.Format = "0.##";
        }
    }
}
