using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>Gestión semestral vista desde la planificación (reemplaza al antiguo contenedor AcademicPlanning).</summary>
public sealed record AcademicManagementSummary(
    int Id,
    string Code,
    int Year,
    int Semester,
    DateTime? StartDate,
    DateTime? EndDate,
    ManagementStatus Status,
    bool IsCurrent,
    DateTime? ClassesStartDate = null,
    DateTime? ClassesEndDate = null)
{
    /// <summary>Se planifica en gestiones activas o inactivas (preparación de la siguiente); las completadas solo se consultan.</summary>
    public bool IsEditable => Status is ManagementStatus.Active or ManagementStatus.Inactive;

    public bool HasDates => StartDate.HasValue && EndDate.HasValue;

    /// <summary>Rango que numera las semanas académicas: el de clases si está definido, si no el de la gestión.</summary>
    public DateTime? CalendarStart => ClassesStartDate ?? StartDate;
    public DateTime? CalendarEnd => ClassesEndDate ?? EndDate;

    public string StatusText => Status switch
    {
        ManagementStatus.Active => "Activa",
        ManagementStatus.Inactive => "Inactiva",
        ManagementStatus.Completed => "Completada",
        _ => "Eliminada"
    };
}

public interface IAcademicManagementService
{
    /// <summary>Gestión por defecto de la planificación: la preventiva semestral activa o, si no hay, la semestral activa de otro tipo (null si no hay ninguna).</summary>
    Task<AcademicManagementSummary?> GetCurrentAsync();

    Task<AcademicManagementSummary?> GetSummaryAsync(int managementId);

    /// <summary>La gestión pedida o, si no se pide, la actual.</summary>
    Task<AcademicManagementSummary?> ResolveAsync(int? managementId);

    /// <summary>Gestiones semestrales no eliminadas, de la más reciente a la más antigua (para selectores).</summary>
    Task<List<AcademicManagementSummary>> ListAsync();

    /// <summary>True si la planificación existe y su gestión admite cambios.</summary>
    Task<bool> IsPlanningWritableAsync(int planningId);
}

public sealed class AcademicManagementService : IAcademicManagementService
{
    public const string ReadOnlyMessage = "La gestión de esta planificación está completada: solo se puede consultar.";

    private readonly ApplicationDbContext _context;

    public AcademicManagementService(ApplicationDbContext context) => _context = context;

    public async Task<AcademicManagementSummary?> GetCurrentAsync()
    {
        var currentId = await CurrentIdAsync();
        return currentId.HasValue ? await GetSummaryAsync(currentId.Value) : null;
    }

    public async Task<AcademicManagementSummary?> GetSummaryAsync(int managementId)
    {
        var currentId = await CurrentIdAsync();
        return (await Project(Semestral().Where(m => m.Id == managementId), currentId).ToListAsync()).FirstOrDefault();
    }

    public Task<AcademicManagementSummary?> ResolveAsync(int? managementId) =>
        managementId.HasValue ? GetSummaryAsync(managementId.Value) : GetCurrentAsync();

    public async Task<List<AcademicManagementSummary>> ListAsync()
    {
        var currentId = await CurrentIdAsync();
        return await Project(Semestral().OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester), currentId)
            .ToListAsync();
    }

    public async Task<bool> IsPlanningWritableAsync(int planningId)
    {
        var status = await _context.Plannings.AsNoTracking()
            .Where(p => p.Id == planningId)
            .Select(p => (ManagementStatus?)p.Management!.Status)
            .FirstOrDefaultAsync();
        return status is ManagementStatus.Active or ManagementStatus.Inactive;
    }

    // Gestiones académicas: semestrales y no correctivas (la correctiva también es semestral desde 2026-10-18).
    private IQueryable<Models.Management> Semestral() =>
        _context.Managements.AsNoTracking().Where(m => m.Semester > 0 && m.Type != ManagementType.Corrective && m.Status != ManagementStatus.Deleted);

    // La preventiva activa tiene prioridad; si no hay, se usa la gestión semestral Activa de cualquier tipo.
    // Con los datos importados todas las gestiones son «Por confirmar» y, sin este respaldo, el Panel, la Grilla,
    // el Calendario, las Reposiciones y la Demanda abrían en «No hay una gestión académica activa».
    private Task<int?> CurrentIdAsync() =>
        Semestral()
            .Where(m => m.Status == ManagementStatus.Active)
            .OrderByDescending(m => m.Type == ManagementType.Preventive)
            .ThenByDescending(m => m.Year).ThenByDescending(m => m.Semester)
            .Select(m => (int?)m.Id)
            .FirstOrDefaultAsync();

    private static IQueryable<AcademicManagementSummary> Project(IQueryable<Models.Management> query, int? currentId) =>
        query.Select(m => new AcademicManagementSummary(
            m.Id, m.Code, m.Year, m.Semester, m.StartDate, m.PlannedEndDate, m.Status, m.Id == currentId,
            m.ClassesStartDate, m.ClassesEndDate));
}
