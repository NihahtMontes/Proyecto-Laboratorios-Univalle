using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Enlaza decimales aceptando punto o coma como separador decimal. Los input type="number" del navegador
    /// siempre envían punto ("2.5") y la cultura del servidor (es) lo leía como separador de miles (25).
    /// </summary>
    public sealed class FlexibleDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (result == ValueProviderResult.None) return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);
            var raw = result.FirstValue?.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
                    bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            // Un solo tipo de separador se interpreta como decimal; con ambos, el último es el decimal.
            var lastDot = raw.LastIndexOf('.');
            var lastComma = raw.LastIndexOf(',');
            var normalized = lastDot >= 0 && lastComma >= 0
                ? (lastComma > lastDot ? raw.Replace(".", "").Replace(',', '.') : raw.Replace(",", ""))
                : raw.Replace(',', '.');

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                bindingContext.Result = ModelBindingResult.Success(value);
            else
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                    $"El valor '{raw}' no es un número válido.");
            return Task.CompletedTask;
        }
    }

    public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
            context.Metadata.UnderlyingOrModelType == typeof(decimal) ? new FlexibleDecimalModelBinder() : null;
    }
}
