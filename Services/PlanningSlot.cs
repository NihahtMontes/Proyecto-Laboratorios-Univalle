namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Horario de una planificación en periodos. Único punto que escribe los campos derivados de Planning.
/// </summary>
public static class PlanningSlot
{
    public const int MaxPeriods = 12;

    /// <summary>Último orden ocupado por un bloque.</summary>
    public static int LastOrder(int startOrder, int count) => startOrder + count - 1;

    /// <summary>True si el bloque cabe en el catálogo (1..12) y count >= 1.</summary>
    public static bool Fits(int startOrder, int count, int maxOrder = MaxPeriods)
        => count >= 1 && startOrder >= 1 && LastOrder(startOrder, count) <= maxOrder;

    /// <summary>True si dos bloques [aStart, aStart+aCount-1] y [bStart, bStart+bCount-1] comparten algún periodo.</summary>
    public static bool Overlaps(int aStart, int aCount, int bStart, int bCount)
        => aCount >= 1 && bCount >= 1
            && aStart <= LastOrder(bStart, bCount)
            && bStart <= LastOrder(aStart, aCount);

    /// <summary>Día 1=lunes..6=sábado; 0 si es domingo.</summary>
    public static int WeekdayOf(DateTime date) => (int)date.DayOfWeek;

    /// <summary>
    /// Aplica fecha + periodo inicial + cantidad: escribe StartPeriodId, PeriodCount, ScheduledDate
    /// (fecha + hora inicio del periodo inicial), ScheduledEndDate (fecha + hora fin del último periodo),
    /// StartTime y EndTime. periods = catálogo completo (cualquier orden).
    /// Lanza ArgumentException (mensaje en español) si la fecha es domingo, si count menor a 1,
    /// si el periodo inicial no está en el catálogo o si el bloque no cabe.
    /// </summary>
    public static void Apply(Models.Planning planning, DateTime date, int startPeriodId, int count, IReadOnlyCollection<Models.ClassPeriod> periods)
    {
        if (planning is null)
            throw new ArgumentException("Debe indicar la planificación a la que se aplica el horario.", nameof(planning));

        if (WeekdayOf(date) == 0)
            throw new ArgumentException("Los domingos no hay actividades académicas: elija un día de lunes a sábado.", nameof(date));

        if (count < 1)
            throw new ArgumentException("La cantidad de periodos debe ser al menos 1.", nameof(count));

        if (periods is null || periods.Count == 0)
            throw new ArgumentException("El catálogo de periodos está vacío.", nameof(periods));

        // Buscar el periodo inicial por Id (el catálogo puede venir en cualquier orden; Id no se asume igual a Order).
        var start = periods.FirstOrDefault(p => p.Id == startPeriodId)
            ?? throw new ArgumentException($"El periodo inicial {startPeriodId} no existe en el catálogo de periodos.", nameof(startPeriodId));

        if (!Fits(start.Order, count))
            throw new ArgumentException($"El bloque no cabe en la jornada: el periodo {start.Order} con {count} periodos supera el periodo {MaxPeriods}.", nameof(count));

        // Último periodo del bloque según el Order del catálogo.
        var lastOrder = LastOrder(start.Order, count);
        var last = periods.FirstOrDefault(p => p.Order == lastOrder)
            ?? throw new ArgumentException($"El catálogo de periodos no contiene el periodo con orden {lastOrder}.", nameof(periods));

        planning.StartPeriodId = start.Id;
        planning.PeriodCount = count;
        planning.ScheduledDate = date.Date + start.StartTime;
        planning.ScheduledEndDate = date.Date + last.EndTime;
        planning.StartTime = start.StartTime;
        planning.EndTime = last.EndTime;
    }
}
