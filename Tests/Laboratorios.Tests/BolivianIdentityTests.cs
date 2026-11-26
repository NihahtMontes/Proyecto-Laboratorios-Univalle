using Proyecto_Laboratorios_Univalle.Helpers;
using Xunit;

namespace Laboratorios.Tests;

public class BolivianIdentityTests
{
    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345678")]
    [InlineData("1234567-1A")]
    [InlineData("1234567-1a")]
    public void Cedula_valida(string value) => Assert.True(BolivianIdentity.IsValidCi(value));

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("123456789")]
    [InlineData("0123456")]
    [InlineData("QA0001")]
    [InlineData("QA-SUPER-1")]
    [InlineData("1234567-A1")]
    [InlineData("12 3456")]
    public void Cedula_invalida(string value) => Assert.False(BolivianIdentity.IsValidCi(value));

    [Theory]
    [InlineData("70000001")]
    [InlineData("60012345")]
    [InlineData("44251234")]
    [InlineData("22123456")]
    [InlineData("33123456")]
    [InlineData("+59170000001")]
    [InlineData("591 70000001")]
    [InlineData("+591-70000001")]
    public void Telefono_valido(string value) => Assert.True(BolivianIdentity.IsValidPhone(value));

    [Theory]
    [InlineData("")]
    [InlineData("7000000")]
    [InlineData("700000001")]
    [InlineData("50000001")]
    [InlineData("80000001")]
    [InlineData("abc")]
    [InlineData("+54 70000001")]
    public void Telefono_invalido(string value) => Assert.False(BolivianIdentity.IsValidPhone(value));

    [Theory]
    [InlineData("+591 70000001", "70000001")]
    [InlineData(" 70000001 ", "70000001")]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void Telefono_se_guarda_con_ocho_digitos(string? value, string? expected) =>
        Assert.Equal(expected, BolivianIdentity.NormalizePhone(value));
}
