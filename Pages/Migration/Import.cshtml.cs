using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Migration
{
    public class ImportModel : PageModel
    {
        private readonly IServiceProvider _serviceProvider;

        public ImportModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string Message { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Message = "Servicio de migración no disponible durante el periodo de refactorización de modelos.";
            }
            
            return Page();
        }
    }
}
