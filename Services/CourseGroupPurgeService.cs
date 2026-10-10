using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Purga (borrado físico) de grupos de materia al completar o cerrar una gestión, o al eliminarlos
/// manualmente. EXCEPCIÓN APROBADA POR EL USUARIO a la regla de soft-delete: los grupos y su horario
/// son datos de trabajo de una gestión y se desechan al cerrarla.
/// Antes de borrar, se suelta el vínculo CourseGroupId (FK Restrict) en Plannings y PracticeSchedules,
/// conservando en GroupName el nombre del grupo cuando estaba vacío.
/// </summary>
public interface ICourseGroupPurgeService
{
    /// <summary>
    /// Borra físicamente todos los grupos (cualquier estado) de la gestión indicada junto con sus
    /// celdas de horario, liberando antes las referencias CourseGroupId de Plannings y PracticeSchedules.
    /// Devuelve la cantidad de grupos borrados.
    /// </summary>
    Task<int> PurgeManagementAsync(int managementId, CancellationToken ct = default);

    /// <summary>
    /// Borra físicamente un grupo y sus celdas de horario, liberando antes las referencias
    /// CourseGroupId de Plannings y PracticeSchedules. Devuelve la cantidad de grupos borrados (0 si no existe).
    /// </summary>
    Task<int> DeleteGroupAsync(int courseGroupId, CancellationToken ct = default);

    /// <summary>
    /// Borra físicamente los grupos con Status == Eliminado de cualquier gestión (limpieza del soft-delete antiguo).
    /// Devuelve la cantidad de grupos borrados.
    /// </summary>
    Task<int> PurgeDeletedAsync(CancellationToken ct = default);
}

public sealed class CourseGroupPurgeService : ICourseGroupPurgeService
{
    /// <summary>Tamaño de lote de escritura: coherente con la política de lotes máximos de 100 filas.</summary>
    private const int BatchSize = 100;

    private readonly ApplicationDbContext _context;

    public CourseGroupPurgeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> PurgeManagementAsync(int managementId, CancellationToken ct = default)
    {
        var groupIds = await _context.CourseGroups
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(g => g.ManagementId == managementId)
            .Select(g => g.Id)
            .ToListAsync(ct);

        var deleted = await PurgeGroupsAsync(groupIds, ct);

        // Grupos de estudiantes de la gestión: sin materias ya no tienen sentido (mismo borrado físico aprobado).
        await RemoveStudentGroupsAsync(_context.StudentGroups.Where(g => g.ManagementId == managementId), ct);
        return deleted;
    }

    public async Task<int> DeleteGroupAsync(int courseGroupId, CancellationToken ct = default)
        => await PurgeGroupsAsync([courseGroupId], ct);

    public async Task<int> PurgeDeletedAsync(CancellationToken ct = default)
    {
        var groupIds = await _context.CourseGroups
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(g => g.Status == GeneralStatus.Eliminado)
            .Select(g => g.Id)
            .ToListAsync(ct);

        var deleted = await PurgeGroupsAsync(groupIds, ct);

        // Grupos de estudiantes eliminados que ya no tienen materias.
        await RemoveStudentGroupsAsync(_context.StudentGroups.Where(g => g.Status == GeneralStatus.Eliminado
            && !_context.CourseGroups.IgnoreQueryFilters().Any(c => c.StudentGroupId == g.Id)), ct);
        return deleted;
    }

    /// <summary>Borra grupos de estudiantes, soltando antes cualquier materia que aún los referencie.</summary>
    private async Task RemoveStudentGroupsAsync(IQueryable<Models.StudentGroup> query, CancellationToken ct)
    {
        var studentGroups = await query.AsTracking().ToListAsync(ct);
        if (studentGroups.Count == 0) return;

        var ids = studentGroups.Select(g => g.Id).ToList();
        var linked = await _context.CourseGroups.IgnoreQueryFilters().AsTracking()
            .Where(c => c.StudentGroupId != null && ids.Contains(c.StudentGroupId.Value))
            .ToListAsync(ct);
        foreach (var courseGroup in linked)
            courseGroup.StudentGroupId = null;

        _context.StudentGroups.RemoveRange(studentGroups);
        await _context.SaveChangesAsync(ct);
        _context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Núcleo de la purga: por lote de grupos libera referencias, borra slots y grupos, guarda y limpia
    /// el ChangeTracker. Abre una transacción propia solo si el llamador no ya tiene una abierta.
    /// </summary>
    private async Task<int> PurgeGroupsAsync(IReadOnlyList<int> groupIds, CancellationToken ct)
    {
        if (groupIds.Count == 0) return 0;

        await using var transaction = _context.Database.CurrentTransaction == null
            ? await _context.Database.BeginTransactionAsync(ct)
            : null;

        var deleted = 0;
        foreach (var batch in groupIds.Chunk(BatchSize))
        {
            deleted += await PurgeBatchAsync(batch, ct);
        }

        if (transaction != null)
            await transaction.CommitAsync(ct);

        return deleted;
    }

    private async Task<int> PurgeBatchAsync(int[] batchIds, CancellationToken ct)
    {
        // Nombres de los grupos del lote: para rellenar GroupName de las referencias que quedan sueltas.
        var namesById = await _context.CourseGroups
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(g => batchIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name, ct);

        // Planificaciones vinculadas (incluye soft-deleted: la FK Restrict también las bloquea).
        var plannings = await _context.Plannings
            .IgnoreQueryFilters()
            .AsTracking()
            .Where(p => p.CourseGroupId != null && batchIds.Contains(p.CourseGroupId.Value))
            .ToListAsync(ct);
        foreach (var planning in plannings)
        {
            if (string.IsNullOrWhiteSpace(planning.GroupName)
                && namesById.TryGetValue(planning.CourseGroupId!.Value, out var groupName))
            {
                planning.GroupName = groupName;
            }
            planning.CourseGroupId = null;
        }

        // Programaciones de prácticas vinculadas (incluye soft-deleted, misma razón).
        var schedules = await _context.PracticeSchedules
            .IgnoreQueryFilters()
            .AsTracking()
            .Where(s => s.CourseGroupId != null && batchIds.Contains(s.CourseGroupId.Value))
            .ToListAsync(ct);
        foreach (var schedule in schedules)
        {
            if (string.IsNullOrWhiteSpace(schedule.GroupName)
                && namesById.TryGetValue(schedule.CourseGroupId!.Value, out var groupName))
            {
                schedule.GroupName = groupName;
            }
            schedule.CourseGroupId = null;
        }

        // Celdas del horario semanal: configuración del grupo desechado (borrado físico aprobado;
        // IgnoreQueryFilters porque el query filter de slots depende del estado del grupo).
        var slots = await _context.CourseGroupScheduleSlots
            .IgnoreQueryFilters()
            .AsTracking()
            .Where(s => batchIds.Contains(s.CourseGroupId))
            .ToListAsync(ct);
        if (slots.Count > 0)
            _context.CourseGroupScheduleSlots.RemoveRange(slots);

        // Grupos de la gestión: datos de trabajo desechables al completar la gestión (borrado físico aprobado).
        var groups = await _context.CourseGroups
            .IgnoreQueryFilters()
            .AsTracking()
            .Where(g => batchIds.Contains(g.Id))
            .ToListAsync(ct);
        if (groups.Count > 0)
            _context.CourseGroups.RemoveRange(groups);

        await _context.SaveChangesAsync(ct);

        // El limpiado desacopla entidades previamente rastreadas del llamador: los cambios de estado
        // deben persistirse ANTES de invocar este servicio.
        _context.ChangeTracker.Clear();

        return groups.Count;
    }
}
