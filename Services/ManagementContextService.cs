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
        Task<Management?> GetCurrentManagementAsync(ManagementType? type = null);
        void InvalidateCache();
    }

    public class ManagementContextService : IManagementContextService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "ActiveManagement";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public ManagementContextService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<Management?> GetCurrentManagementAsync(ManagementType? type = null)
        {
            var cacheKey = type.HasValue ? $"{CacheKeyPrefix}_{type.Value}" : CacheKeyPrefix;

            if (_cache.TryGetValue(cacheKey, out Management? cached) && cached != null)
                return cached;

            try
            {
                var query = _context.Managements
                    .AsNoTracking()
                    .Where(m => m.Status == ManagementStatus.Active);

                if (type.HasValue)
                    query = query.Where(m => m.Type == type.Value);

                var management = await query.FirstOrDefaultAsync();

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
                    _cache.Set(cacheKey, management, CacheDuration);
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
            _cache.Remove(CacheKeyPrefix);
            _cache.Remove($"{CacheKeyPrefix}_{ManagementType.Preventive}");
            _cache.Remove($"{CacheKeyPrefix}_{ManagementType.Corrective}");
        }
    }
}