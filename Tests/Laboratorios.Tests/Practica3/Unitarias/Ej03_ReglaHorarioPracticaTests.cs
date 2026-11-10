using Laboratorios.Tests.Practica3.Soporte;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests.Practica3.Unitarias;

/// <summary>
/// Ejercicio 3 — Reglas de validación y restricciones.
/// Regla de negocio seleccionada (horario de una práctica, acordada con la responsable de laboratorios):
///   R1. Solo se programan prácticas de lunes a sábado.
///   R2. Una práctica ocupa uno o más periodos consecutivos del catálogo fijo de 12 periodos (07:35–18:35)
///       y debe caber completa en la jornada.
///   R3. El periodo inicial debe existir en el catálogo.
///   R4. En un mismo ambiente, entre dos prácticas debe quedar al menos un periodo libre (limpieza y montaje).
/// Funciones probadas: PlanningSlot.Apply, PlanningSlot.Fits y PlanningSlot.OverlapsWithGap.
/// </summary>
public class Ej03_ReglaHorarioPracticaTests : IDisposable
{
    private const string Ejercicio = "Ej03_regla_horario";
    private readonly TestDb _db = new();
    private IReadOnlyCollection<ClassPeriod> Periodos => _db.Periods();

    // Fechas de referencia de la gestión de prueba.
    private static readonly DateTime Lunes = new(2026, 11, 9);
    private static readonly DateTime Sabado = new(2026, 11, 14);
    private static readonly DateTime Domingo = new(2026, 11, 15);

    [Fact]
    public void V1_lunes_periodos_3_y_4_se_acepta_y_calcula_el_horario()
    {
        var p3 = Periodos.Single(p => p.Order == 3);
        var p4 = Periodos.Single(p => p.Order == 4);
        var planning = new Planning();

        PlanningSlot.Apply(planning, Lunes, p3.Id, 2, Periodos);

        var esperado = $"{Lunes:dd/MM} {p3.StartTime:hh\\:mm}–{p4.EndTime:hh\\:mm}";
        var obtenido = $"{planning.ScheduledDate:dd/MM} {planning.StartTime:hh\\:mm}–{planning.EndTime:hh\\:mm}";
        Evidencia.Registrar(Ejercicio, "C3-01", "Válido: lunes, periodos 3–4 (R1, R2)", "lunes 09/11; inicio P3; 2 periodos", "Aceptado; " + esperado, "Aceptado; " + obtenido, esperado == obtenido);
        Assert.Equal(Lunes + p3.StartTime, planning.ScheduledDate);
        Assert.Equal(Lunes + p4.EndTime, planning.ScheduledEndDate);
        Assert.Equal(2, planning.PeriodCount);
    }

    [Fact]
    public void V2_sabado_bloque_que_termina_en_el_periodo_12_se_acepta()
    {
        var p11 = Periodos.Single(p => p.Order == 11);
        var p12 = Periodos.Single(p => p.Order == 12);
        var planning = new Planning();

        PlanningSlot.Apply(planning, Sabado, p11.Id, 2, Periodos);

        var obtenido = $"{planning.StartTime:hh\\:mm}–{planning.EndTime:hh\\:mm}";
        var esperado = $"{p11.StartTime:hh\\:mm}–{p12.EndTime:hh\\:mm}";
        Evidencia.Registrar(Ejercicio, "C3-02", "Válido en el límite: sábado, periodos 11–12 (R1, R2)", "sábado 14/11; inicio P11; 2 periodos", "Aceptado; " + esperado, "Aceptado; " + obtenido, obtenido == esperado);
        Assert.Equal(p12.EndTime, planning.EndTime);
    }

    public static IEnumerable<object[]> CasosInvalidos() =>
    [
        ["C3-03", "Inválido: domingo (R1)", "domingo 15/11; inicio P3; 2 periodos", Domingo, 3, 2, "Los domingos no hay actividades académicas"],
        ["C3-04", "Inválido: cantidad de periodos 0 (R2)", "lunes; inicio P3; 0 periodos", Lunes, 3, 0, "La cantidad de periodos debe ser al menos 1"],
        ["C3-05", "Inválido: el bloque excede la jornada (R2)", "lunes; inicio P12; 2 periodos", Lunes, 12, 2, "El bloque no cabe en la jornada"],
        ["C3-06", "Inválido: periodo inicial inexistente (R3)", "lunes; inicio P99; 1 periodo", Lunes, 99, 1, "no existe en el catálogo de periodos"],
    ];

    [Theory]
    [MemberData(nameof(CasosInvalidos))]
    public void Casos_invalidos_se_rechazan_con_mensaje(string caso, string descripcion, string entrada, DateTime fecha, int periodoInicial, int cantidad, string mensajeEsperado)
    {
        var planning = new Planning();
        var startId = Periodos.FirstOrDefault(p => p.Order == periodoInicial)?.Id ?? periodoInicial;

        var ex = Record.Exception(() => PlanningSlot.Apply(planning, fecha, startId, cantidad, Periodos));

        var cumple = ex is ArgumentException && ex.Message.Contains(mensajeEsperado, StringComparison.Ordinal) && planning.StartPeriodId == null;
        Evidencia.Registrar(Ejercicio, caso, descripcion, entrada, $"Rechazado: «{mensajeEsperado}…»; sin cambios",
            ex == null ? "Aceptado (no se rechazó)" : $"Rechazado: «{ex.Message.Split(" (Parameter")[0]}»" + (planning.StartPeriodId == null ? "; sin cambios" : "; MODIFICÓ la planificación"), cumple);

        Assert.IsAssignableFrom<ArgumentException>(ex);
        Assert.Contains(mensajeEsperado, ex!.Message);
        Assert.Null(planning.StartPeriodId);
    }

    [Theory]
    [InlineData("C3-07", "Válido: queda un periodo libre entre prácticas (R4)", 1, 2, 4, 2, false)]
    [InlineData("C3-08", "Inválido: prácticas contiguas sin periodo libre (R4)", 1, 2, 3, 2, true)]
    [InlineData("C3-09", "Inválido: prácticas superpuestas (R4)", 1, 3, 3, 2, true)]
    [InlineData("C3-10", "Válido: bloques alejados en la jornada (R4)", 1, 2, 9, 3, false)]
    public void Separacion_minima_entre_practicas_del_mismo_ambiente(string caso, string descripcion, int aInicio, int aCant, int bInicio, int bCant, bool conflictoEsperado)
    {
        var conflicto = PlanningSlot.OverlapsWithGap(aInicio, aCant, bInicio, bCant);

        Evidencia.Registrar(Ejercicio, caso, descripcion,
            $"A = {PlanningSlot.PeriodsText(aInicio, aCant)}; B = {PlanningSlot.PeriodsText(bInicio, bCant)}",
            conflictoEsperado ? "Rechazado (conflicto)" : "Aceptado (sin conflicto)",
            conflicto ? "Rechazado (conflicto)" : "Aceptado (sin conflicto)", conflicto == conflictoEsperado);
        Assert.Equal(conflictoEsperado, conflicto);
    }

    /// <summary>
    /// Caso límite de robustez: un bloque con una cantidad de periodos enorme jamás cabe en una jornada de 12.
    /// Antes de la corrección, Fits calculaba el último periodo con aritmética int y, por desbordamiento, devolvía true.
    /// </summary>
    [Theory]
    [InlineData("C3-11", "Límite: cantidad de periodos 13 (mayor que la jornada)", 1, 13)]
    [InlineData("C3-12", "Límite extremo: cantidad = int.MaxValue (desbordamiento)", 2, int.MaxValue)]
    [InlineData("C3-13", "Límite extremo: periodo inicial = int.MaxValue (desbordamiento)", int.MaxValue, 2)]
    public void Un_bloque_mas_largo_que_la_jornada_nunca_cabe(string caso, string descripcion, int inicio, int cantidad)
    {
        var cabe = PlanningSlot.Fits(inicio, cantidad);

        Evidencia.Registrar(Ejercicio, caso, descripcion, $"Fits(inicio={inicio}; cantidad={cantidad})", "false (no cabe)", cabe ? "true (cabe)" : "false (no cabe)", !cabe);
        Assert.False(cabe);
    }

    public void Dispose() => _db.Dispose();
}
