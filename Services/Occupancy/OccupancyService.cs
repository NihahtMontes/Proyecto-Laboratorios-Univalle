namespace Proyecto_Laboratorios_Univalle.Services.Occupancy;

/// <summary>
/// Servicio único de ocupación de ambientes y docentes. Concatena todas las <see cref="IOccupancySource"/> registradas
/// (hoy solo planificaciones; a futuro horario regular de grupos y bloqueos manuales).
/// </summary>
public interface IOccupancyService
{
    /// <summary>Bloques de todas las fuentes que cumplen la consulta.</summary>
    Task<IReadOnlyList<OccupancyBlock>> GetAsync(OccupancyQuery query, CancellationToken ct = default);

    /// <summary>Bloques que ocupan el ambiente en la fecha y se solapan con [startOrder, startOrder + periodCount − 1].</summary>
    Task<IReadOnlyList<OccupancyBlock>> FindConflictsAsync(int laboratoryId, DateTime date, int startOrder, int periodCount,
        IReadOnlyCollection<int>? excludePlanningIds, CancellationToken ct = default);

    /// <summary>Bloques del docente en la fecha (en cualquier ambiente) que se solapan con el rango de periodos.</summary>
    Task<IReadOnlyList<OccupancyBlock>> FindTeacherConflictsAsync(int teacherPersonId, DateTime date, int startOrder, int periodCount,
        IReadOnlyCollection<int>? excludePlanningIds, CancellationToken ct = default);
}

public sealed class OccupancyService : IOccupancyService
{
    private readonly IReadOnlyList<IOccupancySource> _sources;

    public OccupancyService(IEnumerable<IOccupancySource> sources) => _sources = sources.ToList();

    public async Task<IReadOnlyList<OccupancyBlock>> GetAsync(OccupancyQuery query, CancellationToken ct = default)
    {
        var blocks = new List<OccupancyBlock>();
        // Secuencial a propósito: las fuentes comparten el DbContext (no admite consultas concurrentes).
        foreach (var source in _sources)
            blocks.AddRange(await source.GetAsync(query, ct));
        return blocks
            .OrderBy(b => b.Date).ThenBy(b => b.StartOrder).ThenBy(b => b.LaboratoryId)
            .ToList();
    }

    public async Task<IReadOnlyList<OccupancyBlock>> FindConflictsAsync(int laboratoryId, DateTime date, int startOrder, int periodCount,
        IReadOnlyCollection<int>? excludePlanningIds, CancellationToken ct = default)
    {
        var blocks = await GetAsync(new OccupancyQuery(date.Date, date.Date, [laboratoryId], null, excludePlanningIds), ct);
        return blocks.Where(b => b.LaboratoryId == laboratoryId && Overlaps(b, date, startOrder, periodCount, excludePlanningIds)).ToList();
    }

    public async Task<IReadOnlyList<OccupancyBlock>> FindTeacherConflictsAsync(int teacherPersonId, DateTime date, int startOrder, int periodCount,
        IReadOnlyCollection<int>? excludePlanningIds, CancellationToken ct = default)
    {
        var blocks = await GetAsync(new OccupancyQuery(date.Date, date.Date, null, [teacherPersonId], excludePlanningIds), ct);
        return blocks.Where(b => b.TeacherPersonId == teacherPersonId && Overlaps(b, date, startOrder, periodCount, excludePlanningIds)).ToList();
    }

    // Se vuelve a filtrar en memoria por si una fuente ignora algún criterio de la consulta.
    private static bool Overlaps(OccupancyBlock block, DateTime date, int startOrder, int periodCount, IReadOnlyCollection<int>? excluded)
        => block.Date == date.Date
            && !(block.PlanningId is int id && excluded != null && excluded.Contains(id))
            && PlanningSlot.Overlaps(block.StartOrder, block.PeriodCount, startOrder, periodCount);
}
