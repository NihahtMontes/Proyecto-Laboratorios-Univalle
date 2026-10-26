using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;

namespace Proyecto_Laboratorios_Univalle.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string Title { get; set; } = "No se pudo abrir la página";
        public string Module { get; set; } = "Sistema";
        public string Message { get; set; } = "La página no existe o no está disponible.";
        public string? EntityId { get; set; }
        public string ReturnUrl { get; set; } = "/Index";
        public string ListUrl { get; set; } = "/Index";
        public string ListText { get; set; } = "Volver al inicio";

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(string? module, string? entityId, string? message, string? returnUrl, string? listUrl)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            Module = string.IsNullOrWhiteSpace(module) ? "Sistema" : module;
            EntityId = entityId;
            Message = string.IsNullOrWhiteSpace(message)
                ? "La página no existe o no está disponible."
                : message;
            // El botón Volver no debe regresar a una pantalla prohibida (bucle de acceso denegado):
            // si el returnUrl no es local o el usuario llegó por un 403, vuelve al inicio.
            var cameFromAccessDenied = HttpContext.Response.StatusCode == StatusCodes.Status403Forbidden;
            ReturnUrl = Url.IsLocalUrl(returnUrl) && !cameFromAccessDenied ? returnUrl! : "/Index";
            ListUrl = Url.IsLocalUrl(listUrl) ? listUrl! : "/Index";
            ListText = ResolveListText(Module);

            var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;
            if (exception != null)
            {
                _logger.LogError(exception, "Error global capturado en {Module} para entidad {EntityId}. TraceId: {TraceId}", Module, EntityId, RequestId);
                Message = "Ocurrió un error inesperado al abrir esta pantalla.";
            }
        }

        private static string ResolveListText(string module)
        {
            if (module.Contains("solicitud", StringComparison.OrdinalIgnoreCase)) return "Volver a solicitudes";
            if (module.Contains("mantenimiento", StringComparison.OrdinalIgnoreCase)) return "Volver a mantenimientos";
            return "Volver al inicio";
        }
    }
}
