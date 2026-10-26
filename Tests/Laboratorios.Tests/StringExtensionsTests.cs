using Proyecto_Laboratorios_Univalle.Helpers;
using Xunit;

namespace Laboratorios.Tests;

/// <summary>
/// R-03 (2026-10-18): el checklist de 13 puntos del L-7 se guardaba con Clean(), que juntaba las líneas;
/// al reabrir el borrador se contaba 1 punto en vez de 13. CleanMultiline conserva los saltos de línea.
/// </summary>
public class StringExtensionsTests
{
    [Fact]
    public void CleanMultiline_ConservaLineasYLimpiaEspacios()
    {
        var texto = "  1.  Desconexión   eléctrica \r\n\r\n2. Limpieza\t interna\n3. Revisión  ";

        Assert.Equal("1. Desconexión eléctrica\n2. Limpieza interna\n3. Revisión", texto.CleanMultiline());
    }

    [Fact]
    public void CleanMultiline_VacioONulo_DevuelveCadenaVacia()
    {
        Assert.Equal(string.Empty, ((string?)null).CleanMultiline());
        Assert.Equal(string.Empty, "   \r\n ".CleanMultiline());
    }

    [Fact]
    public void Clean_SigueColapsandoTodoEnUnaLinea()
    {
        Assert.Equal("a b", "a\r\n  b".Clean());
    }
}
