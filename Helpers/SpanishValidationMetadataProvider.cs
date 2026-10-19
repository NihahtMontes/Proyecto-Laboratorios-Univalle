using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    /// <summary>
    /// Mensajes de validación en español para los atributos que no declaran <c>ErrorMessage</c>.
    /// Sin esto, ASP.NET Core muestra los textos por defecto en inglés
    /// («The field X must be a string with a maximum length of N.»), que el QA encontró en muchos formularios.
    /// Solo rellena el mensaje cuando el atributo no tiene uno propio; los mensajes escritos a mano se respetan.
    /// </summary>
    public sealed class SpanishValidationMetadataProvider : IValidationMetadataProvider
    {
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
            {
                if (!string.IsNullOrEmpty(attribute.ErrorMessage) || attribute.ErrorMessageResourceType != null)
                    continue;

                var message = attribute switch
                {
                    RequiredAttribute => "El campo {0} es obligatorio.",
                    StringLengthAttribute { MinimumLength: > 0 } => "El campo {0} debe tener entre {2} y {1} caracteres.",
                    StringLengthAttribute => "El campo {0} no puede superar los {1} caracteres.",
                    MaxLengthAttribute => "El campo {0} no puede superar los {1} caracteres.",
                    MinLengthAttribute => "El campo {0} debe tener al menos {1} caracteres.",
                    RangeAttribute => "El campo {0} debe estar entre {1} y {2}.",
                    EmailAddressAttribute => "El campo {0} no es un correo electrónico válido.",
                    PhoneAttribute => "El campo {0} no es un número de teléfono válido.",
                    UrlAttribute => "El campo {0} no es una dirección web válida.",
                    RegularExpressionAttribute => "El formato del campo {0} no es válido.",
                    CompareAttribute => "Los campos {0} y {1} no coinciden.",
                    _ => null
                };

                if (message != null)
                    attribute.ErrorMessage = message;
            }
        }
    }
}
