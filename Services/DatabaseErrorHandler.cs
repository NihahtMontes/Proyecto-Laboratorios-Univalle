using Npgsql;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public class DatabaseErrorHandler
    {
        private readonly ILogger<DatabaseErrorHandler> _logger;

        public DatabaseErrorHandler(ILogger<DatabaseErrorHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Registra un error de conexión en un archivo de texto local para diagnóstico
        /// </summary>
        public void LogConnectionError(Exception ex, string additionalContext = "")
        {
            try
            {
                var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "logs", "connection_errors.txt");
                var logDirectory = Path.GetDirectoryName(logFilePath);

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory!);
                }

                var errorMessage = new StringBuilder();
                errorMessage.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR DE CONEXIÓN A BASE DE DATOS");
                errorMessage.AppendLine($"Contexto: {additionalContext}");
                errorMessage.AppendLine($"Mensaje: {ex.Message}");
                errorMessage.AppendLine($"Tipo: {ex.GetType().Name}");

                if (ex.InnerException != null)
                {
                    errorMessage.AppendLine($"Excepción Interna: {ex.InnerException.Message}");
                }

                errorMessage.AppendLine($"Stack Trace: {ex.StackTrace}");
                errorMessage.AppendLine(new string('-', 80));

                File.AppendAllText(logFilePath, errorMessage.ToString());

                // También registrar en el logger de ASP.NET
                _logger.LogError(ex, "Error de conexión a base de datos: {Context}", additionalContext);
            }
            catch (Exception logEx)
            {
                // Si falla el registro, al menos intentar registrar en el logger
                _logger.LogError(logEx, "Error al intentar registrar error de base de datos");
            }
        }

        /// <summary>
        /// Verifica la conectividad a la base de datos y devuelve un mensaje descriptivo
        /// </summary>
        public async Task<(bool Success, string Message)> TestDatabaseConnection(DbContext context)
        {
            try
            {
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();
                return (true, "Conexión exitosa a la base de datos.");
            }
            catch (PostgresException pgEx)
            {
                LogConnectionError(pgEx, "Prueba de conexión a base de datos");

                var errorMessage = pgEx.SqlState switch
                {
                    "28P01" => "Credenciales incorrectas. Verifica el usuario y contraseña de la base de datos.",
                    "3D000" => "No se puede abrir la base de datos solicitada. Verifica el nombre de la base de datos.",
                    "08001" => "No se pudo conectar al servidor. Verifica que el servidor esté accesible y que no haya problemas de red.",
                    "08006" => "Error de conexión fallida.",
                    _ => $"Error PostgreSQL #{pgEx.SqlState}: {pgEx.MessageText}"
                };

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                LogConnectionError(ex, "Prueba de conexión a base de datos");
                return (false, $"Error inesperado: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene un mensaje de error amigable basado en la excepción
        /// </summary>
        public string GetFriendlyErrorMessage(Exception ex)
        {
            if (ex is PostgresException pgEx)
            {
                return pgEx.SqlState switch
                {
                    "08001" or "08006" => "No se pudo conectar a la base de datos. Por favor, intenta de nuevo más tarde.",
                    "28P01" => "Error de autenticación con la base de datos.",
                    "3D000" => "La base de datos no está disponible en este momento.",
                    "23505" => "Ya existe un registro con estos datos. Por favor, verifica los datos duplicados.", // unique violation
                    "23503" => "No se puede eliminar este registro porque está siendo utilizado por otros registros.", // foreign key violation
                    _ => "Ocurrió un error en la base de datos. Por favor, contacta al administrador."
                };
            }            else if (ex is DbUpdateException)
            {
                return "No se pudieron guardar los cambios. Verifica que los datos sean correctos.";
            }
            else if (ex is TimeoutException)
            {
                return "La operación tardó demasiado tiempo. Por favor, intenta de nuevo.";
            }

            return "Ocurrió un error inesperado. Por favor, contacta al administrador.";
        }
    }
}
