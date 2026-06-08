using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;

namespace Proyecto_Laboratorios_Univalle.Pages
{
    [AllowAnonymous]
    public class DbMigratorModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<DbMigratorModel> _logger;

        public DbMigratorModel(ApplicationDbContext context, IConfiguration config, ILogger<DbMigratorModel> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public string Message { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostMigrateAsync()
        {
            // 1. Obtener cadena de conexión Cloud (destino)
            string cloudConnection = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            
            // Cadena de conexión Local (origen)
            string localConnection = "Server=localhost;Database=DB_Laboratorios_Univalle_DEV;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False";

            if (string.IsNullOrEmpty(cloudConnection) || cloudConnection.Contains("TU_SERVIDOR"))
            {
                ErrorMessage = "La cadena de conexión de la nube no está configurada o contiene placeholders. Por favor configúrala con User Secrets.";
                return Page();
            }

            try
            {
                _logger.LogInformation("Iniciando migración de base de datos a Cloud...");

                // 2. Ejecutar Migraciones de EF Core en la nube para asegurar que el esquema esté creado y actualizado
                _logger.LogInformation("Aplicando migraciones EF Core en la base destino...");
                await _context.Database.MigrateAsync();

                // 3. Copiar datos tabla por tabla
                string[] tables = new string[] {
                    "Countries", "Cities", "Faculties", "Careers", "Laboratories", "People", "Interns", "Externs",
                    "Equipments", "EquipmentNotes", "Managements", "EquipmentUnits", "Verifications", "VerificationFaults",
                    "VerificationCheckItems", "VerificationCheckResults", "Requests", "Maintenances", "MaintenanceTasks",
                    "CostDetails", "Departures", "DepartureItems", "EquipmentStateHistories", "ManagementPlans",
                    "Users", "Roles", "UserRoles", "UserClaims", "UserLogins", "UserTokens", "RoleClaims", 
                    "Notifications", "MaintenancePlans"
                };

                using (SqlConnection sourceConn = new SqlConnection(localConnection))
                using (SqlConnection destConn = new SqlConnection(cloudConnection))
                {
                    await sourceConn.OpenAsync();
                    await destConn.OpenAsync();

                    // Desactivar restricciones temporalmente en el destino
                    using (SqlCommand cmd = new SqlCommand("EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'", destConn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Limpiar tablas destino y copiar datos de origen usando INSERTs parametrizados para evitar incompatibilidad de Collation (Locale ID)
                    foreach (var table in tables)
                    {
                        _logger.LogInformation($"Migrando tabla: {table}");

                        // Verificar si la tabla existe en el origen
                        bool tableExists = false;
                        using (SqlCommand checkCmd = new SqlCommand($"SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{table}'", sourceConn))
                        {
                            var result = await checkCmd.ExecuteScalarAsync();
                            tableExists = result != null;
                        }

                        if (!tableExists)
                        {
                            _logger.LogWarning($"La tabla {table} no existe en el origen local. Saltando...");
                            continue;
                        }

                        // Limpiar tabla destino
                        using (SqlCommand deleteCmd = new SqlCommand($"DELETE FROM [{table}]", destConn))
                        {
                            await deleteCmd.ExecuteNonQueryAsync();
                        }

                        // Verificar si la tabla de origen tiene una columna IDENTITY
                        bool hasIdentity = false;
                        using (SqlCommand identityCmd = new SqlCommand($"SELECT OBJECTPROPERTY(OBJECT_ID('{table}'), 'TableHasIdentity')", sourceConn))
                        {
                            var result = await identityCmd.ExecuteScalarAsync();
                            hasIdentity = result != null && Convert.ToInt32(result) == 1;
                        }

                        // Obtener columnas de la tabla
                        List<string> columns = new List<string>();
                        using (SqlCommand colsCmd = new SqlCommand($"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}' ORDER BY ORDINAL_POSITION", sourceConn))
                        using (SqlDataReader colsReader = await colsCmd.ExecuteReaderAsync())
                        {
                            while (await colsReader.ReadAsync())
                            {
                                columns.Add(colsReader.GetString(0));
                            }
                        }

                        if (columns.Count == 0) continue;

                        // Armar sentencia de inserción parametrizada
                        string colList = string.Join(", ", columns.Select(c => $"[{c}]"));
                        string paramList = string.Join(", ", columns.Select(c => $"@{c}"));
                        string insertSql = $"INSERT INTO [{table}] ({colList}) VALUES ({paramList})";

                        // Leer datos del origen y guardarlos en el destino
                        using (SqlCommand selectCmd = new SqlCommand($"SELECT * FROM [{table}]", sourceConn))
                        using (SqlDataReader reader = await selectCmd.ExecuteReaderAsync())
                        {
                            if (hasIdentity)
                            {
                                using (SqlCommand identityInsertOn = new SqlCommand($"SET IDENTITY_INSERT [{table}] ON", destConn))
                                {
                                    await identityInsertOn.ExecuteNonQueryAsync();
                                }
                            }

                            while (await reader.ReadAsync())
                            {
                                using (SqlCommand insertCmd = new SqlCommand(insertSql, destConn))
                                {
                                    for (int i = 0; i < columns.Count; i++)
                                    {
                                        string paramName = $"@{columns[i]}";
                                        var value = reader.GetValue(i);
                                        insertCmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                                    }
                                    await insertCmd.ExecuteNonQueryAsync();
                                }
                            }

                            if (hasIdentity)
                            {
                                using (SqlCommand identityInsertOff = new SqlCommand($"SET IDENTITY_INSERT [{table}] OFF", destConn))
                                {
                                    await identityInsertOff.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }

                    // Habilitar restricciones nuevamente en el destino
                    using (SqlCommand cmd = new SqlCommand("EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'", destConn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                Message = "¡Migración completada con éxito! Todos los datos locales fueron copiados a la nube utilizando inserciones parametrizadas.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la migración local-cloud.");
                ErrorMessage = $"Error en la migración: {ex.Message}. Revisa que tu base local y cloud estén accesibles.";
            }

            return Page();
        }
    }
}
