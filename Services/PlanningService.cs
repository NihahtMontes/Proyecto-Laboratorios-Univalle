using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Resumen inmutable de una planificación (seguro para caché y vistas).</summary>
public sealed record PlanningSummary(
    int Id,
    string Code,
    string Name,
    string ManagementCode,
    int FacultyId,
    string FacultyName,
    string CareerName,
    DateTime StartDate,
    DateTime EndDate,
    AcademicPlanningStatus Status,
    int? CampusId,
    string? CampusName)
{
    public bool IsActive => Status == AcademicPlanningStatus.Active;

    public string StatusText => Status switch
    {
        AcademicPlanningStatus.Active => "Activa",
        AcademicPlanningStatus.Completed => "Cerrada",
        _ => "Inactiva"
    };
}

/// <summary>
/// Planificación académica activa (una en todo el sistema), mismo patrón que la gestión activa:
/// activar una cierra las demás; la activa se cachea y se invalida al cambiar.
/// </summary>
public interface IPlanningService
{
    Task<PlanningSummary?> GetActiveAsync();
    Task<PlanningSummary?> GetSummaryAsync(int planningId);

    /// <summary>Activa la planificación (rastreada) y cierra las demás activas. El llamador guarda y confirma.</summary>
    Task<int> ActivateAsync(AcademicPlanning planning);

    /// <summary>Cierra la planificación (rastreada). El llamador guarda y confirma.</summary>
    void Close(AcademicPlanning planning);

    void InvalidateCache();
}

public sealed class PlanningService : IPlanningService
{
    private const string CacheKey = "ActiveAcademicPlanning";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public PlanningService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PlanningSummary?> GetActiveAsync()
    {
        if (_cache.TryGetValue(CacheKey, out PlanningSummary? cached) && cached != null)
            return cached;

        var active = await Project(_context.AcademicPlannings.AsNoTracking()
                .Where(p => p.Status == AcademicPlanningStatus.Active)
                .OrderByDescending(p => p.StartDate))
            .FirstOrDefaultAsync();

        if (active != null)
            _cache.Set(CacheKey, active, CacheDuration);
        return active;
    }

    public Task<PlanningSummary?> GetSummaryAsync(int planningId) =>
        Project(_context.AcademicPlannings.AsNoTracking().Where(p => p.Id == planningId)).FirstOrDefaultAsync();

    public async Task<int> ActivateAsync(AcademicPlanning planning)
    {
        planning.Status = AcademicPlanningStatus.Active;

        var others = await _context.AcademicPlannings.AsTracking()
            .Where(p => p.Status == AcademicPlanningStatus.Active && p.Id != planning.Id)
            .ToListAsync();
        foreach (var other in others)
            other.Status = AcademicPlanningStatus.Completed;

        return others.Count;
    }

    public void Close(AcademicPlanning planning) => planning.Status = AcademicPlanningStatus.Completed;

    public void InvalidateCache() => _cache.Remove(CacheKey);

    private static IQueryable<PlanningSummary> Project(IQueryable<AcademicPlanning> query) =>
        query.Select(p => new PlanningSummary(
            p.Id,
            p.Management.Code + " · " + (p.Career.Code ?? ("PLAN-" + p.Id)),
            p.Name,
            p.Management.Code,
            p.FacultyId,
            p.Faculty.Name,
            p.Career.Name,
            p.StartDate,
            p.EndDate,
            p.Status,
            p.CampusId,
            p.Campus != null ? p.Campus.Name : null));
}
