namespace Proyecto_Laboratorios_Univalle.Services.Occupancy;

/// <summary>
/// Bloque de periodos consecutivos que ocupa un ambiente (y opcionalmente un docente) en una fecha.
/// Es la unidad común que entregan todas las fuentes de ocupación.
/// </summary>
/// <param name="Source">Origen del bloque (p. ej. "Planificación").</param>
/// <param name="PlanningId">Planificación que lo genera (null si la fuente no es una planificación).</param>
/// <param name="Label">Texto visible, p. ej. "PRA-00024 · Materia (Grupo)".</param>
/// <param name="LaboratoryId">Ambiente ocupado.</param>
/// <param name="TeacherPersonId">Docente ocupado (null si la fuente no ocupa docente).</param>
/// <param name="Date">Fecha (solo la parte de fecha).</param>
/// <param name="StartOrder">Orden del periodo inicial (1..12).</param>
/// <param name="PeriodCount">Cantidad de periodos consecutivos.</param>
/// <param name="Code">Código corto visible (PRA-00024, código del cupo).</param>
/// <param name="PracticeName">Nombre de la práctica (decisión de privacidad 2026-11-23: todos ven lo mismo que el admin).</param>
/// <param name="SubjectName">Materia.</param>
/// <param name="GroupName">Grupo.</param>
/// <param name="TeacherName">Docente.</param>
/// <param name="FreePracticeSlotId">Cupo de práctica libre que lo genera (null si no es un cupo).</param>
/// <param name="Capacity">Capacidad del cupo de práctica libre.</param>
public sealed record OccupancyBlock(
    string Source,
    int? PlanningId,
    string Label,
    int LaboratoryId,
    int? TeacherPersonId,
    DateTime Date,
    int StartOrder,
    int PeriodCount,
    string? Code = null,
    string? PracticeName = null,
    string? SubjectName = null,
    string? GroupName = null,
    string? TeacherName = null,
    int? FreePracticeSlotId = null,
    int? Capacity = null)
{
    public int LastOrder => PlanningSlot.LastOrder(StartOrder, PeriodCount);

    /// <summary>Texto de los periodos ocupados: "Periodo 3" o "Periodos 3–5".</summary>
    public string PeriodsText => PlanningSlot.PeriodsText(StartOrder, PeriodCount);

    /// <summary>True si el bloque es un cupo de práctica libre de estudiantes.</summary>
    public bool IsFreePracticeSlot => FreePracticeSlotId.HasValue;
}

/// <summary>Textos visibles comunes de un bloque de ocupación (grilla admin y portal docente).</summary>
public static class OccupancyBlockText
{
    /// <summary>Nombre corto para una celda: el nombre de la práctica; los cupos son "Práctica libre".</summary>
    public static string ShortLabel(this OccupancyBlock block)
    {
        if (block.IsFreePracticeSlot) return "Práctica libre";
        return string.IsNullOrWhiteSpace(block.PracticeName) ? "Ocupado" : block.PracticeName.Trim();
    }

    /// <summary>Detalle completo "Práctica · Materia · Grupo g · Docente · Código · Periodos 3–5" (sin datos vacíos).</summary>
    public static string Detail(this OccupancyBlock block)
    {
        if (block.IsFreePracticeSlot)
        {
            var capacity = block.Capacity is int cap ? $" · cupo {cap}" : string.Empty;
            var code = string.IsNullOrWhiteSpace(block.Code) ? string.Empty : $" · {block.Code.Trim()}";
            return $"Práctica libre de estudiantes{capacity}{code} · {block.PeriodsText}";
        }

        var parts = new[]
        {
            block.PracticeName?.Trim(),
            block.SubjectName?.Trim(),
            string.IsNullOrWhiteSpace(block.GroupName) ? null : "Grupo " + block.GroupName.Trim(),
            block.TeacherName?.Trim(),
            block.Code?.Trim(),
            block.PeriodsText
        }.Where(p => !string.IsNullOrEmpty(p));
        return string.Join(" · ", parts);
    }
}

/// <summary>Consulta de ocupación entre dos fechas (inclusive). Las colecciones nulas o vacías no filtran.</summary>
public sealed record OccupancyQuery(
    DateTime From,
    DateTime To,
    IReadOnlyCollection<int>? LaboratoryIds = null,
    IReadOnlyCollection<int>? TeacherPersonIds = null,
    IReadOnlyCollection<int>? ExcludePlanningIds = null);

/// <summary>
/// Fuente de ocupación de ambientes/docentes. Implementaciones actuales: <see cref="PlanningOccupancySource"/>.
/// </summary>
/// <remarks>
/// TODO (fases futuras), sin implementar todavía:
/// <list type="bullet">
/// <item><c>CourseGroupScheduleSource</c>: horario regular de materias/grupos (CourseGroup.Weekday + horas).
/// El grupo no puede tener otra clase a la vez y el ambiente queda ocupado por sus clases regulares.</item>
/// <item><c>ManualBlockSource</c>: bloqueos manuales de un ambiente por eventos (mantenimiento, actos, etc.).</item>
/// </list>
/// Cada nueva fuente se registra en DI como otra <see cref="IOccupancySource"/>; <see cref="OccupancyService"/> las concatena.
/// </remarks>
public interface IOccupancySource
{
    Task<IReadOnlyList<OccupancyBlock>> GetAsync(OccupancyQuery query, CancellationToken ct = default);
}
