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
public sealed record OccupancyBlock(
    string Source,
    int? PlanningId,
    string Label,
    int LaboratoryId,
    int? TeacherPersonId,
    DateTime Date,
    int StartOrder,
    int PeriodCount)
{
    public int LastOrder => PlanningSlot.LastOrder(StartOrder, PeriodCount);

    /// <summary>Texto de los periodos ocupados: "Periodo 3" o "Periodos 3–5".</summary>
    public string PeriodsText => PlanningSlot.PeriodsText(StartOrder, PeriodCount);
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
