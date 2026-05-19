using Microsoft.Data.SqlClient;
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
                errorMessage.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR DE CONEXION A BASE DE DATOS");
                errorMessage.AppendLine($"Contexto: {additionalContext}");
                errorMessage.AppendLine($"Mensaje: {ex.Message}");
                errorMessage.AppendLine($"Tipo: {ex.GetType().Name}");

                if (ex.InnerException != null)
                {
                    errorMessage.AppendLine($"Excepcion Interna: {ex.InnerException.Message}");
                }

                errorMessage.AppendLine($"Stack Trace: {ex.StackTrace}");
                errorMessage.AppendLine(new string('-', 80));

                File.AppendAllText(logFilePath, errorMessage.ToString());
                _logger.LogError(ex, "Error de conexion a base de datos: {Context}", additionalContext);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Error al intentar registrar error de base de datos");
            }
        }

        public async Task<(bool Success, string Message)> TestDatabaseConnection(DbContext context)
        {
            try
            {
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();
                return (true, "Conexion exitosa a SQL Server.");
            }
            catch (SqlException sqlEx)
            {
                LogConnectionError(sqlEx, "Prueba de conexion a base de datos");

                var errorMessage = sqlEx.Number switch
                {
                    18456 => "Credenciales incorrectas. Verifica el usuario y contrasena de SQL Server.",
                    4060 => "No se puede abrir la base de datos solicitada. Verifica el nombre de la base de datos.",
                    -2 => "La conexion a SQL Server excedio el tiempo de espera.",
                    53 or 11001 => "No se pudo conectar al servidor. Verifica que SQL Server este accesible.",
                    _ => $"Error SQL Server #{sqlEx.Number}: {sqlEx.Message}"
                };

                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                LogConnectionError(ex, "Prueba de conexion a base de datos");
                return (false, $"Error inesperado: {ex.Message}");
            }
        }

        public string GetFriendlyErrorMessage(Exception ex)
        {
            if (ex is SqlException sqlEx)
            {
                return sqlEx.Number switch
                {
                    53 or 11001 or -2 => "No se pudo conectar a la base de datos. Por favor, intenta de nuevo mas tarde.",
                    18456 => "Error de autenticacion con la base de datos.",
                    4060 => "La base de datos no esta disponible en este momento.",
                    2601 or 2627 => "Ya existe un registro con estos datos. Por favor, verifica los datos duplicados.",
                    547 => "No se puede eliminar este registro porque esta siendo utilizado por otros registros.",
                    _ => "Ocurrio un error en la base de datos. Por favor, contacta al administrador."
                };
            }

            if (ex is DbUpdateException)
            {
                return "No se pudieron guardar los cambios. Verifica que los datos sean correctos.";
            }

            if (ex is TimeoutException)
            {
                return "La operacion tardo demasiado tiempo. Por favor, intenta de nuevo.";
            }

            return "Ocurrio un error inesperado. Por favor, contacta al administrador.";
        }
    }
}
