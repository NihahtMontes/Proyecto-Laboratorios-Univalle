using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Net;
using System.Text.Json;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public static class RazorJsHelper
    {
        /// <summary>
        /// Convierte un valor de TempData en un literal JavaScript seguro.
        /// - Decodifica entidades HTML (ej: &aacute; → á)
        /// - Serializa a JSON válido (escapa comillas, saltos de línea, etc.)
        /// - Previene errores de sintaxis y XSS en bloques <script>
        /// </summary>
        /// <param name="html">Helper de Razor</param>
        /// <param name="key">Clave del TempData</param>
        /// <returns>Cadena JSON lista para usar en JavaScript</returns>
        public static IHtmlContent JsTempData(this IHtmlHelper html, string key)
        {
            var tempData = html.ViewContext.TempData;

            // Validación robusta
            if (!tempData.TryGetValue(key, out var value) || value == null)
                return HtmlString.Empty;

            // Manejo según tipo
            string stringValue = value switch
            {
                string str => WebUtility.HtmlDecode(str),
                _ => value.ToString() ?? string.Empty
            };

            // Serialización segura a JSON
            var json = JsonSerializer.Serialize(stringValue, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            return new HtmlString(json);
        }

        /// <summary>
        /// Renderiza todos los mensajes de alerta estándar (Success, Error, Warning).
        /// Usar en _Layout.cshtml antes del cierre de </body>
        /// </summary>
        public static IHtmlContent RenderAlerts(this IHtmlHelper html)
        {
            var tempData = html.ViewContext.TempData;
            var scripts = new System.Text.StringBuilder();

            // Success Alert
            if (tempData.TryGetValue("SuccessMessage", out var success) && success != null)
            {
                var message = html.JsTempData("SuccessMessage");
                scripts.AppendLine($@"
                <script>
                    Swal.fire({{
                        icon: 'success',
                        title: '\u00a1Excelente!',
                        text: {message},
                        confirmButtonText: 'Aceptar',
                        confirmButtonColor: '#4CAF50',
                        timer: 4000
                    }});
                </script>");
            }

            // Error Alert
            if (tempData.TryGetValue("ErrorMessage", out var error) && error != null)
            {
                var message = html.JsTempData("ErrorMessage");
                scripts.AppendLine($@"
                <script>
                    Swal.fire({{
                        icon: 'error',
                        title: '\u00a1Error!',
                        text: {message},
                        confirmButtonText: 'Entendido',
                        confirmButtonColor: '#f44336'
                    }});
                </script>");
            }

            // Warning Alert
            if (tempData.TryGetValue("WarningMessage", out var warning) && warning != null)
            {
                var message = html.JsTempData("WarningMessage");
                scripts.AppendLine($@"
                <script>
                    Swal.fire({{
                        icon: 'warning',
                        title: 'Advertencia',
                        text: {message},
                        confirmButtonText: 'OK',
                        confirmButtonColor: '#ff9800'
                    }});
                </script>");
            }

            return new HtmlString(scripts.ToString());
        }
    }
}