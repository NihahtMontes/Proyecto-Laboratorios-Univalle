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
                // We manually substitute the service for this operation
                var migrationService = new DataMigrationService(scopedContext);
                
                string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "tmp", "migration_data.json");
                Message = await migrationService.MigrateFromJsonAsync(jsonPath);
            }
            
            return Page();
        }
    }
}
