using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

namespace Laboratorios.Tests;

/// <summary>
/// O1 (QA): las planificaciones importadas del Excel institucional traen su propio código persistido
/// (p. ej. "SOL3-0565"); la interfaz debe mostrar ese código y solo derivar "PRA-{Id:00000}" cuando no exista.
/// </summary>
public class L3CodesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_SinCodigoPersistido_UsaElDerivadoDelId(string? code)
    {
        Assert.Equal("PRA-00007", L3Codes.Format(7, code));
    }

    [Fact]
    public void Format_ConCodigoPersistido_LoDevuelveRecortado()
    {
        Assert.Equal("SOL3-0565", L3Codes.Format(7, "  SOL3-0565 "));
    }

    [Fact]
    public void Format_SoloId_SigueDerivandoPRA()
    {
        Assert.Equal("PRA-00007", L3Codes.Format(7));
    }
}
