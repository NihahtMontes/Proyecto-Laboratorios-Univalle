using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public interface IManagementContextService
    {
        Task<Management> GetCurrentManagementAsync();
        Management GetCurrentManagement();
    }

    /// <summary>
    /// Servicio que provee el contexto de la gestión vigente para toda la aplicación.
    /// Asegura que cada nueva actividad (L2 a L8) se asocie automáticamente 
    /// a la gestión activa en el sistema.
    /// </summary>
    public class ManagementContextService : IManagementContextService
    {
        private readonly ApplicationDbContext _context;
        private Management? _currentActive;

        public ManagementContextService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Management?> GetCurrentManagementAsync()
        {
            try 
            {
                if (_currentActive == null)
                {
                    _currentActive = await _context.Managements
                        .FirstOrDefaultAsync(m => m.Status == ManagementStatus.Active);

                    if (_currentActive == null)
                    {
                        // Fallback a la más reciente por año/semestre si nada está marcado como Active
                        _currentActive = await _context.Managements
                            .OrderByDescending(m => m.Year)
                            .ThenByDescending(m => m.Semester)
                            .FirstOrDefaultAsync();
                    }
                }
                return _currentActive;
            }
            catch (Exception ex) when (ex is SqlException || ex is InvalidOperationException)
            {
                // Si la tabla no existe o la columna ManagementId falta en la DB, 
                // retornamos null para manejarlo en el PageModel con un SweetAlert.
                return null;
            }
        }

        public Management? GetCurrentManagement()
        {
            try
            {
                if (_currentActive == null)
                {
                    _currentActive = _context.Managements
                        .FirstOrDefault(m => m.Status == ManagementStatus.Active);

                    if (_currentActive == null)
                    {
                        _currentActive = _context.Managements
                            .OrderByDescending(m => m.Year)
                            .ThenByDescending(m => m.Semester)
                            .FirstOrDefault();
                    }
                }
                return _currentActive;
            }
            catch (Exception ex) when (ex is SqlException || ex is InvalidOperationException)
            {
                return null;
            }
        }
    }
}
