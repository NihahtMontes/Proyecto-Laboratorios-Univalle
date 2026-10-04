namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Genera las semanas académicas (lunes a viernes) de un rango de fechas.
/// La semana 1 es la que contiene la fecha de inicio; la última, la que contiene la fecha de fin.
/// </summary>
public static class AcademicWeekGenerator
{
    public const int MaxWeeks = 60;

    public sealed record WeekSlot(int Number, DateTime Monday, DateTime Friday);

    public static DateTime MondayOf(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7; // lunes = 0 ... domingo = 6
        return date.Date.AddDays(-offset);
    }

    public static IReadOnlyList<WeekSlot> Generate(DateTime startDate, DateTime endDate)
    {
        if (endDate.Date < startDate.Date)
            throw new ArgumentException("La fecha de fin no puede ser anterior a la de inicio.");

        var weeks = new List<WeekSlot>();
        var monday = MondayOf(startDate);
        var lastMonday = MondayOf(endDate);
        for (var number = 1; monday <= lastMonday; number++, monday = monday.AddDays(7))
        {
            if (number > MaxWeeks)
                throw new ArgumentException($"El rango supera {MaxWeeks} semanas académicas.");
            weeks.Add(new WeekSlot(number, monday, monday.AddDays(4)));
        }
        return weeks;
    }

    /// <summary>Fecha y hora de una práctica a partir del lunes de su semana, el día (1 = lunes ... 5 = viernes) y la hora.</summary>
    public static DateTime PracticeDate(DateTime weekMonday, int weekday, TimeSpan time)
    {
        if (weekday is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(weekday), "El día debe estar entre lunes (1) y viernes (5).");
        return weekMonday.Date.AddDays(weekday - 1) + time;
    }

    /// <summary>Día hábil (1 = lunes ... 5 = viernes) de una fecha; 0 si es fin de semana.</summary>
    public static int WeekdayOf(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => 1,
        DayOfWeek.Tuesday => 2,
        DayOfWeek.Wednesday => 3,
        DayOfWeek.Thursday => 4,
        DayOfWeek.Friday => 5,
        _ => 0
    };

    public static readonly string[] WeekdayNames = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes"];
}
