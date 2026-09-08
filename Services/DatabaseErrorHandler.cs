using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
        /// Registra un error de conexion en el proveedor de logging configurado.
        /// </summary>
        public void LogConnectionError(Exception ex, string additionalContext = "")
        {
            _logger.LogError(ex, "Error de conexion a base de datos: {Context}", additionalContext);
        }

        /// <summary>
        /// Verifica la conectividad a la base de datos y devuelve un mensaje descriptivo.
        /// </summary>
        public async Task<(bool Success, string Message)> TestDatabaseConnection(DbContext context)
        {
            try
            {
                await context.Database.OpenConnectionAsync();
                await context.Database.CloseConnectionAsync();
                return (true, "Conexion exitosa a la base de datos.");
            }
            catch (SqlException sqlEx)
            {
                LogConnectionError(sqlEx, "Prueba de conexion a base de datos");
                return (false, GetSqlServerConnectionMessage(sqlEx));
            }
            catch (Exception ex)
            {
                LogConnectionError(ex, "Prueba de conexion a base de datos");
                return (false, "No se pudo comprobar la conexion. Revise los logs del servidor.");
            }
        }

        /// <summary>
        /// Obtiene un mensaje de error amigable basado en la excepcion.
        /// </summary>
        public string GetFriendlyErrorMessage(Exception ex)
        {
            if (ex is SqlException sqlEx)
            {
                return sqlEx.Number switch
                {
                    53 or 233 or 10054 or 10060 => "No se pudo conectar a la base de datos. Por favor, intenta de nuevo mas tarde.",
                    18456 => "Error de autenticacion con la base de datos.",
                    4060 => "La base de datos no esta disponible en este momento.",
                    2601 or 2627 => "Ya existe un registro con estos datos. Por favor, verifica los datos duplicados.",
                    547 => "No se puede eliminar este registro porque esta siendo utilizado por otros registros.",
                    _ => "Ocurrio un error en la base de datos. Por favor, contacta al administrador."
                };
            }

            if (ex is DbUpdateException dbUpdateException && dbUpdateException.InnerException is SqlException innerSqlEx)
            {
                return GetFriendlyErrorMessage(innerSqlEx);
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

        private static string GetSqlServerConnectionMessage(SqlException sqlEx)
        {
            return sqlEx.Number switch
            {
                18456 => "Credenciales incorrectas. Verifica el usuario y contrasena de SQL Server.",
                4060 => "No se puede abrir la base de datos solicitada. Verifica el nombre de la base de datos.",
                53 or 233 or 10054 or 10060 => "No se pudo conectar al servidor SQL Server. Verifica que el servidor este accesible.",
                -2 => "La conexion a SQL Server tardo demasiado tiempo.",
                _ => "SQL Server rechazo o interrumpio la conexion. Revise los logs del servidor."
            };
        }
    }
}
