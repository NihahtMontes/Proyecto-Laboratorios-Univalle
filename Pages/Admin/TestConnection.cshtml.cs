using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Admin
{
    [Authorize(Roles = AuthorizationHelper.RoleSuperAdmin)]
    public class TestConnectionModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseErrorHandler _errorHandler;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public TestConnectionModel(
            ApplicationDbContext context,
            DatabaseErrorHandler errorHandler,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _context = context;
            _errorHandler = errorHandler;
            _configuration = configuration;
            _environment = environment;
        }

        public (bool Success, string Message)? TestResult { get; set; }
        public string ServerInfo { get; set; } = string.Empty;
        public string DatabaseInfo { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            ExtractConnectionInfo(_configuration.GetConnectionString("DefaultConnection") ?? string.Empty);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            var connString = _configuration.GetConnectionString("DefaultConnection") ?? "";

            // Realizar la prueba de conexión
            TestResult = await _errorHandler.TestDatabaseConnection(_context);

            if (TestResult.Value.Success)
            {
                // Extraer información del servidor
                ExtractConnectionInfo(connString);
            }

            return Page();
        }

        private void ExtractConnectionInfo(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                ServerInfo = "No configurado";
                DatabaseInfo = "No configurada";
                return;
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                ServerInfo = string.IsNullOrWhiteSpace(builder.DataSource) ? "No configurado" : builder.DataSource;
                DatabaseInfo = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "No configurada" : builder.InitialCatalog;
            }
            catch (ArgumentException)
            {
                ServerInfo = "Configuracion invalida";
                DatabaseInfo = "Configuracion invalida";
            }
        }
    }
}
