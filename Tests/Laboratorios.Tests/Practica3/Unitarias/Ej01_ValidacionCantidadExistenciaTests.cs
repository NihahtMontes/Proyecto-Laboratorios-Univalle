using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Laboratorios.Tests.Practica3.Soporte;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Proyecto_Laboratorios_Univalle.Helpers;
using StockEdit = Proyecto_Laboratorios_Univalle.Pages.Catalogs.Stock.EditModel;

namespace Laboratorios.Tests.Practica3.Unitarias;

/// <summary>
/// Ejercicio 1 — Validación de datos de entrada.
/// Dato seleccionado: «Cantidad disponible» de un artículo en Existencias (Catálogos › Existencias › Editar).
/// Funciones de validación identificadas en el sistema (se prueban aisladas, sin servidor ni base de datos):
///   1) FlexibleDecimalModelBinder: convierte el texto del formulario a decimal (acepta punto o coma) o informa formato inválido.
///   2) Anotaciones de EditModel.InputModel.AvailableQuantity: [Required] y [Range(0; 999999)].
/// </summary>
public class Ej01_ValidacionCantidadExistenciaTests
{
    private const string Ejercicio = "Ej01_validacion_cantidad";

    /// <summary>Resultado de validar una entrada: valor convertido (si lo hubo) y mensajes de error.</summary>
    private sealed record Validacion(decimal? Valor, IReadOnlyList<string> Errores)
    {
        public bool EsValida => Errores.Count == 0;
    }

    /// <summary>Ejecuta la misma cadena que ASP.NET Core aplica al campo: enlace de modelo y luego anotaciones.</summary>
    private static async Task<Validacion> ValidarAsync(string? textoIngresado)
    {
        // 1) Enlace del valor con el binder del sistema.
        var values = new Dictionary<string, StringValues>();
        if (textoIngresado != null) values["Input.AvailableQuantity"] = textoIngresado;
        var valueProvider = new QueryStringValueProvider(BindingSource.Form, new QueryCollection(values), CultureInfo.InvariantCulture);
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(decimal?));
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(actionContext, valueProvider, metadata, bindingInfo: null, "Input.AvailableQuantity");

        await new FlexibleDecimalModelBinder().BindModelAsync(bindingContext);

        var errores = bindingContext.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
        decimal? valor = bindingContext.Result.IsModelSet ? (decimal?)bindingContext.Result.Model : null;

        // 2) Anotaciones de datos del InputModel (solo si el enlace no falló, como en el pipeline real).
        if (errores.Count == 0)
        {
            var input = new StockEdit.InputModel { LaboratoryId = 1, ArticleId = 1, AvailableQuantity = valor };
            var resultados = new List<ValidationResult>();
            Validator.TryValidateObject(input, new ValidationContext(input), resultados, validateAllProperties: true);
            errores.AddRange(resultados.Select(r => r.ErrorMessage ?? string.Empty));
        }
        return new Validacion(valor, errores);
    }

    [Theory]
    [InlineData("C1-01", "Dato válido (separador punto)", "25.5", "25.5")]
    [InlineData("C1-02", "Dato válido (separador coma)", "12,75", "12.75")]
    [InlineData("C1-03", "Límite inferior permitido", "0", "0")]
    [InlineData("C1-04", "Límite superior permitido", "999999", "999999")]
    [InlineData("C1-05", "Miles con punto y decimales con coma", "1.234,50", "1234.50")]
    public async Task Acepta_cantidades_validas(string caso, string descripcion, string entrada, string esperado)
    {
        var resultado = await ValidarAsync(entrada);
        var valorEsperado = decimal.Parse(esperado, CultureInfo.InvariantCulture);
        var cumple = resultado.EsValida && resultado.Valor == valorEsperado;
        Evidencia.Registrar(Ejercicio, caso, descripcion, $"\"{entrada}\"", $"Aceptado; valor {valorEsperado.ToString(CultureInfo.InvariantCulture)}",
            resultado.EsValida ? $"Aceptado; valor {resultado.Valor?.ToString(CultureInfo.InvariantCulture)}" : "Rechazado: " + string.Join(" | ", resultado.Errores), cumple);

        Assert.True(resultado.EsValida, string.Join(" | ", resultado.Errores));
        Assert.Equal(valorEsperado, resultado.Valor);
    }

    [Theory]
    [InlineData("C1-06", "Dato vacío", "", "La cantidad disponible es obligatoria.")]
    [InlineData("C1-07", "Dato solo con espacios", "   ", "La cantidad disponible es obligatoria.")]
    [InlineData("C1-08", "Dato negativo (no corresponde en existencias)", "-3", "La cantidad debe estar entre 0 y 999999.")]
    [InlineData("C1-09", "Fuera de rango (superior)", "1000000", "La cantidad debe estar entre 0 y 999999.")]
    [InlineData("C1-10", "Fuera de rango por un centésimo", "999999.01", "La cantidad debe estar entre 0 y 999999.")]
    [InlineData("C1-11", "Formato incorrecto (texto)", "abc", "El valor 'abc' no es un número válido.")]
    [InlineData("C1-12", "Formato incorrecto (unidad pegada)", "12.5kg", "El valor '12.5kg' no es un número válido.")]
    public async Task Rechaza_cantidades_invalidas_con_mensaje_claro(string caso, string descripcion, string entrada, string mensajeEsperado)
    {
        var resultado = await ValidarAsync(entrada);
        var cumple = !resultado.EsValida && resultado.Errores.Contains(mensajeEsperado);
        Evidencia.Registrar(Ejercicio, caso, descripcion, $"\"{entrada}\"", "Rechazado: " + mensajeEsperado,
            resultado.EsValida ? $"Aceptado; valor {resultado.Valor?.ToString(CultureInfo.InvariantCulture)}" : "Rechazado: " + string.Join(" | ", resultado.Errores), cumple);

        Assert.False(resultado.EsValida);
        Assert.Contains(mensajeEsperado, resultado.Errores);
    }

    [Fact]
    public async Task Campo_ausente_en_el_formulario_tambien_es_obligatorio()
    {
        var resultado = await ValidarAsync(null);
        const string esperado = "La cantidad disponible es obligatoria.";
        var cumple = !resultado.EsValida && resultado.Errores.Contains(esperado);
        Evidencia.Registrar(Ejercicio, "C1-13", "Campo ausente en la solicitud", "(sin campo)", "Rechazado: " + esperado,
            resultado.EsValida ? "Aceptado" : "Rechazado: " + string.Join(" | ", resultado.Errores), cumple);
        Assert.Contains(esperado, resultado.Errores);
    }
}
