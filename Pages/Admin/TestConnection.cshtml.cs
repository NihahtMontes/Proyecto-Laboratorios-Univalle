using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Admin
{
    public class TestConnectionModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly DatabaseErrorHandler _errorHandler;
        private readonly IConfiguration _configuration;

        public TestConnectionModel(
            ApplicationDbContext context,
            DatabaseErrorHandler errorHandler,
            IConfiguration configuration)
        {
            _context = context;
            _errorHandler = errorHandler;
            _configuration = configuration;
        }

        public (bool Success, string Message)? TestResult { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string ServerInfo { get; set; } = string.Empty;
        public string DatabaseInfo { get; set; } = string.Empty;

        public void OnGet()
        {
            var connString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            
            // Ocultar la contraseña para mostrar en pantalla
            ConnectionString = HidePassword(connString);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var connString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            ConnectionString = HidePassword(connString);

            // Realizar la prueba de conexión
            TestResult = await _errorHandler.TestDatabaseConnection(_context);

            if (TestResult.Value.Success)
            {
                // Extraer información del servidor
                ExtractConnectionInfo(connString);
            }

            return Page();
        }

        private string HidePassword(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return "";

            var parts = connectionString.Split(';');
            var hiddenParts = parts.Select(part =>
            {
                if (part.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase) ||
                    part.Trim().StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase))
                {
                    return "Password=********";
                }
                return part;
            });

            return string.Join(";\n", hiddenParts);
        }

        private void ExtractConnectionInfo(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                {
                    ServerInfo = trimmed.Substring("Server=".Length);
                }
                else if (trimmed.StartsWith("Database=", StringComparison.OrdinalIgnoreCase) ||
                         trimmed.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                {
                    DatabaseInfo = trimmed.Contains("=") ? trimmed.Split('=')[1] : "";
                }
            }
        }
    }
}
