namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Demanda de un artículo por una práctica en un horario del día (sin horario = todo el día).</summary>
public readonly record struct TimedDemand(decimal Quantity, TimeSpan? StartTime, TimeSpan? EndTime);

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
            var start = demand.StartTime ?? TimeSpan.Zero;
            var end = demand.EndTime is TimeSpan e && e > start ? e : (demand.StartTime.HasValue ? start.Add(TimeSpan.FromMinutes(1)) : TimeSpan.FromDays(1));
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
}
