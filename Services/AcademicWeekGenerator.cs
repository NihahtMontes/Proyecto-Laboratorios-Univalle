namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Genera las semanas académicas (lunes a sábado) de un rango de fechas.
/// La semana 1 es la que contiene la fecha de inicio; la última, la que contiene la fecha de fin.
/// Los domingos no existen en el calendario académico.
/// </summary>
public static class AcademicWeekGenerator
{
    public const int MaxWeeks = 60;

    public sealed record WeekSlot(int Number, DateTime Monday, DateTime Saturday);

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
            weeks.Add(new WeekSlot(number, monday, monday.AddDays(5)));
        }
        return weeks;
    }

    /// <summary>Día académico (1 = lunes ... 6 = sábado) de una fecha; 0 si es domingo.</summary>
    public static int WeekdayOf(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => 1,
        DayOfWeek.Tuesday => 2,
        DayOfWeek.Wednesday => 3,
        DayOfWeek.Thursday => 4,
        DayOfWeek.Friday => 5,
        DayOfWeek.Saturday => 6,
        _ => 0
    };

    /// <summary>Nombres largos de los días académicos; el índice 0 no se usa (domingo).</summary>
    public static readonly string[] WeekdayNames = ["", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    /// <summary>Nombres cortos de los días académicos; el índice 0 no se usa (domingo).</summary>
    public static readonly string[] ShortWeekdayNames = ["", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];
}
