namespace Proyecto_Laboratorios_Univalle.Pages.Shared;

/// <summary>Fila de la columna de periodos de la grilla semanal: número de orden y texto de hora.</summary>
public sealed record PeriodWeekGridPeriod(int Order, string TimeText);

/// <summary>
/// Modelo simple del partial <c>_PeriodWeekGrid</c>: pinta la esquina "Periodo", los encabezados
/// de lunes a sábado y la columna de periodos de una grilla CSS (<c>.gr-grid</c> de grilla.css).
/// La página contenedora define el contenedor con sus filas y pinta las celdas día × periodo
/// (elementos con <c>data-weekday</c> 1..6 y <c>data-order</c> 1..N).
/// </summary>
public sealed record PeriodWeekGridModel(
    IReadOnlyList<PeriodWeekGridPeriod> Periods,
    /// <summary>Nombres de días en orden: índice 0 = lunes (weekday 1) ... índice 5 = sábado (weekday 6).</summary>
    IReadOnlyList<string> DayNames)
{
    /// <summary>Días hábiles de clase (lunes a sábado) con su número de weekday.</summary>
    public static readonly string[] DefaultDayNames =
        ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
}
