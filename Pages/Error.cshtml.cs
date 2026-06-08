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
        public string Title { get; set; } = "No se pudo abrir el detalle";
        public string Module { get; set; } = "Sistema";
        public string Message { get; set; } = "Ocurrio un problema al procesar la solicitud.";
        public string? EntityId { get; set; }
        public string ReturnUrl { get; set; } = "/";
        public string ListUrl { get; set; } = "/";
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
                ? "La informacion solicitada no esta disponible o el registro historico esta incompleto."
                : message;
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
            ListUrl = Url.IsLocalUrl(listUrl) ? listUrl! : "/";
            ListText = ResolveListText(Module);

            var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;
            if (exception != null)
            {
                _logger.LogError(exception, "Error global capturado en {Module} para entidad {EntityId}. TraceId: {TraceId}", Module, EntityId, RequestId);
                Message = "Se produjo una excepcion inesperada al abrir esta pantalla.";
            }
        }

        private static string ResolveListText(string module)
        {
            if (module.Contains("L-7", StringComparison.OrdinalIgnoreCase)) return "Volver a solicitudes";
            if (module.Contains("L-8", StringComparison.OrdinalIgnoreCase)) return "Volver a mantenimientos";
            return "Volver al inicio";
        }
    }
}
