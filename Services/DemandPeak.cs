namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Demanda de un artículo por una práctica en un horario del día (sin horario = todo el día).</summary>
public readonly record struct TimedDemand(decimal Quantity, TimeSpan? StartTime, TimeSpan? EndTime);

/// <summary>Tramo del día con carga constante: las demandas de ActiveIds piden Load unidades durante [Start, End).</summary>
public sealed record DemandSegment(TimeSpan Start, TimeSpan End, decimal Load, IReadOnlyList<int> ActiveIds);

/// <summary>
/// Cálculo del pico de demanda simultánea de un artículo en un día. Lo comparten la disponibilidad
/// semanal (<see cref="ArticleAvailabilityService"/>) y la validación de destino (<see cref="SlotValidationService"/>).
/// </summary>
public static class DemandPeak
{
    /// <summary>
    /// Máxima cantidad pedida a la vez (barrido por horario). Una práctica sin horario ocupa todo el día;
    /// una que termina a la hora en que otra empieza no se cruza con ella.
    /// Devuelve Start/End del pico, o null si todas las demandas son de día completo.
    /// </summary>
    public static (decimal Peak, TimeSpan? Start, TimeSpan? End) Compute(IReadOnlyList<TimedDemand> demands)
    {
        var events = new List<(TimeSpan Time, decimal Delta)>();
        foreach (var demand in demands)
        {
            var (start, end) = Window(demand);
            events.Add((start, demand.Quantity));
            events.Add((end, -demand.Quantity));
        }

        decimal current = 0, peak = 0;
        TimeSpan? peakStart = null, peakEnd = null;
        var ordered = events.OrderBy(e => e.Time).ThenBy(e => e.Delta).ToList(); // salidas antes que entradas a la misma hora
        for (var i = 0; i < ordered.Count; i++)
        {
            current += ordered[i].Delta;
            if (current > peak)
            {
                peak = current;
                peakStart = ordered[i].Time;
                peakEnd = i + 1 < ordered.Count ? ordered[i + 1].Time : null;
            }
        }

        // Si todas son de día completo, no tiene sentido mostrar 00:00–24:00.
        if (demands.All(d => !d.StartTime.HasValue)) return (peak, null, null);
        return (peak, peakStart, peakEnd);
    }

    /// <summary>
    /// Tramos consecutivos del día entre puntos de cambio con carga positiva: cada tramo lleva la carga
    /// constante y los Ids activos (ascendente, sin repetir). Mismas reglas de horario que <see cref="Compute"/>.
    /// </summary>
    public static IReadOnlyList<DemandSegment> Segments(IReadOnlyList<(int Id, TimedDemand Demand)> demands)
    {
        var segments = new List<DemandSegment>();
        if (demands.Count == 0) return segments;

        // Ventanas efectivas de las demandas con carga y puntos de cambio del día (inicios y fines).
        var ventanas = new List<(int Id, TimeSpan Start, TimeSpan End, decimal Quantity)>(demands.Count);
        var puntos = new SortedSet<TimeSpan>();
        foreach (var (id, demand) in demands)
        {
            if (demand.Quantity <= 0) continue;   // sin carga no aporta tramos
            var (start, end) = Window(demand);
            ventanas.Add((id, start, end, demand.Quantity));
            puntos.Add(start);
            puntos.Add(end);
        }

        var ordenados = puntos.ToList();
        for (var i = 0; i < ordenados.Count - 1; i++)
        {
            var a = ordenados[i];
            var b = ordenados[i + 1];   // tramo [a, b): una demanda está activa si cubre todo el tramo
            var activas = ventanas.Where(w => w.Start <= a && w.End >= b).ToList();
            if (activas.Count == 0) continue;

            var ids = activas.Select(w => w.Id).Distinct().OrderBy(x => x).ToList();
            var load = activas.Sum(w => w.Quantity);
            if (load <= 0) continue;

            // Tramos contiguos con el mismo conjunto de Ids se fusionan en uno.
            var previo = segments.Count > 0 ? segments[^1] : null;
            if (previo is not null && previo.End == a && previo.ActiveIds.SequenceEqual(ids))
                segments[^1] = previo with { End = b };
            else
                segments.Add(new DemandSegment(a, b, load, ids));
        }

        return segments;
    }

    /// <summary>Ventana efectiva de una demanda: sin horario cubre todo el día; fin no posterior al inicio dura un minuto.</summary>
    private static (TimeSpan Start, TimeSpan End) Window(TimedDemand demand)
    {
        var start = demand.StartTime ?? TimeSpan.Zero;
        var end = demand.EndTime is TimeSpan e && e > start
            ? e
            : demand.StartTime.HasValue ? start.Add(TimeSpan.FromMinutes(1)) : TimeSpan.FromDays(1);
        return (start, end);
    }
}
