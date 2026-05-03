using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public interface IManagementContextService
    {
        Task<Management?> GetCurrentManagementAsync();
        void InvalidateCache();
    }

    public class ManagementContextService : IManagementContextService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "ActiveManagement";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public ManagementContextService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<Management?> GetCurrentManagementAsync()
        {
            if (_cache.TryGetValue(CacheKey, out Management? cached) && cached != null)
                return cached;

            try
            {
                var management = await _context.Managements
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Status == ManagementStatus.Active);

                if (management == null)
                {
                    management = await _context.Managements
                        .AsNoTracking()
                        .OrderByDescending(m => m.Year)
                        .ThenByDescending(m => m.Semester)
                        .FirstOrDefaultAsync();
                }

                if (management != null)
                {
                    _cache.Set(CacheKey, management, CacheDuration);
                }

                return management;
            }
            catch (Exception ex) when (ex is SqlException || ex is InvalidOperationException)
            {
                return null;
            }
        }

        public void InvalidateCache()
        {
            _cache.Remove(CacheKey);
        }
    }
}