using System.Globalization;
using Laboratorios.Tests.Practica3.Soporte;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;
using Proyecto_Laboratorios_Univalle.Services;

namespace Laboratorios.Tests.Practica3.Unitarias;

/// <summary>
/// Ejercicio 2 — Cálculo de valores del sistema.
/// 2.1 Tope de cantidad afectada de un incidente L-5 (L5Rules.MaxAdditional):
///     tope = máx(0; ⌈máx(0; entregado − devuelto)⌉ − ya registrado)
///     Reglas: lo devuelto no puede generar incidentes; lo ya registrado en otros L-5 se descuenta; un incidente
///     cuenta unidades enteras, por eso el pendiente fraccionario se redondea hacia arriba; nunca es negativo.
/// 2.2 Pico de demanda simultánea de un artículo en un día (DemandPeak.Compute): suma máxima de cantidades de
///     prácticas que se superponen en el tiempo. Una práctica sin horario ocupa todo el día; una que termina a la
///     hora en que otra empieza no se superpone con ella.
/// Los valores esperados se calcularon a mano (ver informe) antes de ejecutar las pruebas.
/// </summary>
public class Ej02_CalculosDelSistemaTests
{
    private const string Ejercicio = "Ej02_calculos";

    [Theory]
    [InlineData("C2-01", "Valor normal: nada devuelto ni registrado", "6", "0", 0, 6)]
    [InlineData("C2-02", "Valor normal: devolución parcial y un incidente previo", "24", "20", 1, 3)]
    [InlineData("C2-03", "Cero: no se entregó nada", "0", "0", 0, 0)]
    [InlineData("C2-04", "Cero: todo fue devuelto", "3", "3", 0, 0)]
    [InlineData("C2-05", "Límite: el pendiente ya está cubierto por incidentes", "3", "2", 1, 0)]
    [InlineData("C2-06", "Límite: queda exactamente una unidad", "3", "2", 0, 1)]
    [InlineData("C2-07", "Pendiente fraccionario (2,5 kg) se redondea hacia arriba", "2.5", "0", 0, 3)]
    [InlineData("C2-08", "Dato inconsistente: devuelto mayor que entregado", "2", "5", 0, 0)]
    [InlineData("C2-09", "Registrado mayor que el pendiente: nunca negativo", "5", "1", 6, 0)]
    public void Tope_de_incidente_L5(string caso, string descripcion, string entregado, string devuelto, int registrado, int esperado)
    {
        var d = decimal.Parse(entregado, CultureInfo.InvariantCulture);
        var r = decimal.Parse(devuelto, CultureInfo.InvariantCulture);

        var obtenido = L5Rules.MaxAdditional(d, r, registrado);

        Evidencia.Registrar(Ejercicio, caso, descripcion, $"entregado={entregado}; devuelto={devuelto}; registrado={registrado}",
            esperado.ToString(), obtenido.ToString(), obtenido == esperado);
        Assert.Equal(esperado, obtenido);
    }

    private static TimedDemand D(decimal qty, string? start = null, string? end = null) =>
        new(qty, start == null ? null : TimeSpan.Parse(start, CultureInfo.InvariantCulture), end == null ? null : TimeSpan.Parse(end, CultureInfo.InvariantCulture));

    public static IEnumerable<object[]> CasosPico() =>
    [
        ["C2-10", "Normal: dos prácticas superpuestas", new[] { D(5, "08:00", "10:00"), D(3, "09:00", "11:00") }, 8m, "09:00", "10:00"],
        ["C2-11", "Normal: tres prácticas, pico en el cruce de dos", new[] { D(2, "07:35", "09:05"), D(4, "08:20", "10:35"), D(1, "11:20", "12:50") }, 6m, "08:20", "09:05"],
        ["C2-12", "Límite: una termina cuando la otra empieza (no se cruzan)", new[] { D(5, "08:00", "10:00"), D(3, "10:00", "12:00") }, 5m, "08:00", "10:00"],
        ["C2-13", "Cero: sin demandas en el día", Array.Empty<TimedDemand>(), 0m, null!, null!],
        ["C2-14", "Cero: demandas con cantidad 0", new[] { D(0, "08:00", "10:00"), D(0, "09:00", "11:00") }, 0m, null!, null!],
        ["C2-15", "Práctica sin horario ocupa todo el día", new[] { D(4), D(3, "14:00", "16:00") }, 7m, "14:00", "16:00"],
    ];

    [Theory]
    [MemberData(nameof(CasosPico))]
    public void Pico_de_demanda_simultanea(string caso, string descripcion, TimedDemand[] demandas, decimal picoEsperado, string? desde, string? hasta)
    {
        var (pico, inicio, fin) = DemandPeak.Compute(demandas);

        var entrada = demandas.Length == 0 ? "(sin demandas)" : string.Join(" + ", demandas.Select(x =>
            $"{x.Quantity.ToString(CultureInfo.InvariantCulture)} [{(x.StartTime.HasValue ? $"{x.StartTime:hh\\:mm}–{x.EndTime:hh\\:mm}" : "todo el día")}]"));
        var esperadoTxt = $"{picoEsperado.ToString(CultureInfo.InvariantCulture)}" + (desde != null ? $" ({desde}–{hasta})" : "");
        var obtenidoTxt = $"{pico.ToString(CultureInfo.InvariantCulture)}" + (inicio.HasValue && pico > 0 ? $" ({inicio:hh\\:mm}–{fin:hh\\:mm})" : "");
        var cumple = pico == picoEsperado && (desde == null || (inicio == TimeSpan.Parse(desde) && fin == TimeSpan.Parse(hasta!)));
        Evidencia.Registrar(Ejercicio, caso, descripcion, entrada, esperadoTxt, obtenidoTxt, cumple);

        Assert.Equal(picoEsperado, pico);
        if (desde != null)
        {
            Assert.Equal(TimeSpan.Parse(desde), inicio);
            Assert.Equal(TimeSpan.Parse(hasta!), fin);
        }
    }
}
