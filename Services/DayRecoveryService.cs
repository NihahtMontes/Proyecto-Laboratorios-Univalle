using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

namespace Proyecto_Laboratorios_Univalle.Services;

public sealed record DayRecoveryResult(bool Ok, string Message, int? RecoveryId = null, int PendingCount = 0);

/// <summary>Laboratorio afectado por la reposición de un día (Label "COD · Nombre").</summary>
public sealed record DayRecoveryPreviewLab(int? LaboratoryId, string Label, int Count);

/// <summary>Planificación en estado no compatible con la reposición del día.</summary>
public sealed record DayRecoveryPreviewBlocker(int PlanningId, string Code, string Practice, string StatusLabel);

/// <summary>Vista previa de solo lectura de lo que pasaría al suspender y reponer un día.</summary>
public sealed record DayRecoveryPreview(bool Ok, string Message, DateTime Date, int? DayId, int Total,
    IReadOnlyList<DayRecoveryPreviewLab> Labs, IReadOnlyList<DayRecoveryPreviewBlocker> Blockers);

public interface IDayRecoveryService
{
    /// <summary>Suspende el día entero y conserva el estado previo de todas sus planificaciones vigentes.</summary>
    Task<DayRecoveryResult> SuspendAsync(int dayId, string? reason, CancellationToken ct = default);

    /// <summary>Vista previa de solo lectura: día del calendario, planificaciones afectadas por laboratorio y bloqueos.</summary>
    Task<DayRecoveryPreview> PreviewAsync(int managementId, DateTime date, CancellationToken ct = default);

    /// <summary>Resuelve el día de la gestión por fecha y lo suspende (misma operación que SuspendAsync).</summary>
    Task<DayRecoveryResult> SuspendByDateAsync(int managementId, DateTime date, string? reason, CancellationToken ct = default);
}

public sealed class DayRecoveryService : IDayRecoveryService
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningLock _lock;
    private readonly ILogger<DayRecoveryService> _logger;

    public DayRecoveryService(ApplicationDbContext context, IPlanningLock planningLock, ILogger<DayRecoveryService> logger)
    {
        _context = context;
        _lock = planningLock;
        _logger = logger;
    }

    public async Task<DayRecoveryResult> SuspendAsync(int dayId, string? reason, CancellationToken ct = default)
    {
        var cleanReason = reason?.Trim();
        if (string.IsNullOrEmpty(cleanReason) || cleanReason.Length > 200)
            return Fail("Indique un motivo de suspensión de hasta 200 caracteres.");

        var scope = await _context.AcademicDays.AsNoTracking().Where(d => d.Id == dayId)
            .Select(d => new { d.Date, d.AcademicWeek.ManagementId }).FirstOrDefaultAsync(ct);
        if (scope == null) return Fail("El día académico no existe.");

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            if (!await _lock.TryAcquireAsync([PlanningLockResources.ManagementDay(scope.ManagementId, scope.Date)], ct))
                return Fail(PlanningLockResources.BusyMessage);

            var managementStatus = await _context.Managements.AsNoTracking()
                .Where(m => m.Id == scope.ManagementId)
                .Select(m => (ManagementStatus?)m.Status).FirstOrDefaultAsync(ct);
            if (managementStatus is not (ManagementStatus.Active or ManagementStatus.Inactive))
                return Fail(AcademicManagementService.ReadOnlyMessage);

            var day = await _context.AcademicDays.AsTracking()
                .FirstOrDefaultAsync(d => d.Id == dayId && d.Date == scope.Date
                    && d.AcademicWeek.ManagementId == scope.ManagementId, ct);
            if (day == null || day.Status != AcademicDayStatus.Enabled || day.DayRecoveryId != null
                || await _context.DayRecoveries.AsNoTracking().AnyAsync(r => r.ManagementId == scope.ManagementId && r.Date == scope.Date, ct))
                return Fail("El día ya está suspendido o tiene una reposición; actualice el calendario.");

            var start = scope.Date.Date;
            var end = start.AddDays(1);
            var candidates = await _context.Plannings.AsNoTracking()
                .Where(p => p.ManagementId == scope.ManagementId && p.ScheduledDate >= start && p.ScheduledDate < end
                    && !PlanningStatus.NonOccupying.Contains(p.Status))
                .Select(p => new { p.Id, p.Status }).ToListAsync(ct);
            if (candidates.Count == 0)
                return Fail("El día no tiene planificaciones vigentes: suspéndalo desde Calendario académico.");
            var incompatible = candidates
                .Where(p => p.Status is not (PlanningStatus.Planned or PlanningStatus.Approved or PlanningStatus.Finalized))
                .ToList();
            if (incompatible.Count > 0)
                return Fail(IncompatibleMessage(incompatible.Count,
                    incompatible.Select(p => (Id: p.Id, Status: (string)p.Status))));

            var recovery = new DayRecovery
            {
                ManagementId = scope.ManagementId, Date = start,
                Reason = cleanReason, Status = DayRecoveryStatus.InProgress
            };
            _context.DayRecoveries.Add(recovery);
            await _context.SaveChangesAsync(ct);
            day.Status = AcademicDayStatus.Suspended;
            day.Reason = cleanReason;
            day.DayRecoveryId = recovery.Id;
            await _context.SaveChangesAsync(ct);
            _context.ChangeTracker.Clear();

            // La transacción conserva el bloqueo durante todos los lotes; no deja media suspensión visible.
            foreach (var ids in candidates.Select(p => p.Id).Chunk(100))
            {
                var batch = await _context.Plannings.AsTracking().Where(p => ids.Contains(p.Id)).ToListAsync(ct);
                foreach (var planning in batch)
                {
                    planning.StatusBeforeRecovery = planning.Status;
                    planning.Status = PlanningStatus.PendingRecovery;
                }
                await _context.SaveChangesAsync(ct);
                _context.ChangeTracker.Clear();
            }

            await transaction.CommitAsync(ct);
            return new DayRecoveryResult(true,
                $"Día suspendido. {candidates.Count} planificación(es) pendientes de reposición.", recovery.Id, candidates.Count);
        }
        catch (DbUpdateException ex)
        {
            await transaction.SafeRollbackAsync(ct);
            _context.ChangeTracker.Clear();
            _logger.LogError(ex, "No se pudo suspender el día académico {DayId} para reposición", dayId);
            return Fail("No se pudo crear la reposición; actualice el calendario e intente de nuevo.");
        }
    }

    /// <summary>
    /// Vista previa de solo lectura para el panel: confirma que la fecha está en el calendario de la gestión,
    /// que el día está habilitado sin reposición y cuenta las planificaciones a suspender por laboratorio.
    /// </summary>
    public async Task<DayRecoveryPreview> PreviewAsync(int managementId, DateTime date, CancellationToken ct = default)
    {
        var target = date.Date;
        var day = await _context.AcademicDays.AsNoTracking()
            .Where(d => d.Date == target && d.AcademicWeek.ManagementId == managementId)
            .Select(d => new { d.Id, d.Status })
            .FirstOrDefaultAsync(ct);
        if (day == null)
            return new(false, $"La fecha {target:dd/MM/yyyy} no pertenece al calendario académico de la gestión.", target, null, 0, [], []);

        if (day.Status != AcademicDayStatus.Enabled || await _context.DayRecoveries.AsNoTracking()
                .AnyAsync(r => r.ManagementId == managementId && r.Date == target, ct))
            return new(false, $"El {target:dd/MM/yyyy} ya está suspendido o tiene una reposición en curso.", target, day.Id, 0, [], []);

        var candidates = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == managementId && p.ScheduledDate >= target && p.ScheduledDate < target.AddDays(1)
                && !PlanningStatus.NonOccupying.Contains(p.Status))
            .Select(p => new { p.Id, p.PracticeName, p.Status, p.LaboratoryId,
                Laboratory = p.Laboratory != null ? p.Laboratory.Code + " · " + p.Laboratory.Name : "Sin ambiente" })
            .ToListAsync(ct);
        if (candidates.Count == 0)
            return new(false, $"El {target:dd/MM/yyyy} no tiene planificaciones vigentes.", target, day.Id, 0, [], []);

        var blockers = candidates
            .Where(p => p.Status is not (PlanningStatus.Planned or PlanningStatus.Approved or PlanningStatus.Finalized))
            .Select(p => new DayRecoveryPreviewBlocker(p.Id, L3Codes.Format(p.Id), p.PracticeName, PlanningStatus.Label(p.Status)))
            .ToList();
        if (blockers.Count > 0)
            return new(false, IncompatibleMessage(blockers.Count, blockers.Select(b => (Id: b.PlanningId, Status: b.StatusLabel))),
                target, day.Id, candidates.Count, [], blockers);

        var labs = candidates.GroupBy(p => p.LaboratoryId)
            .Select(g => new DayRecoveryPreviewLab(g.Key, g.First().Laboratory, g.Count()))
            .OrderBy(l => l.Label)
            .ToList();
        return new(true, $"El {target:dd/MM/yyyy} tiene {candidates.Count} planificación(es) para reponer.",
            target, day.Id, candidates.Count, labs, []);
    }

    /// <summary>Suspende por fecha (panel de planificación); falla con mensaje explícito si el día no existe.</summary>
    public async Task<DayRecoveryResult> SuspendByDateAsync(int managementId, DateTime date, string? reason, CancellationToken ct = default)
    {
        var target = date.Date;
        var dayId = await _context.AcademicDays.AsNoTracking()
            .Where(d => d.Date == target && d.AcademicWeek.ManagementId == managementId)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync(ct);
        if (dayId == null)
            return Fail($"La fecha {target:dd/MM/yyyy} no pertenece al calendario académico de la gestión.");
        return await SuspendAsync(dayId.Value, reason, ct);
    }

    /// <summary>"3 planificación(es) en estado no compatible: PRA-00001 (Aprobada), ... y 2 más."</summary>
    private static string IncompatibleMessage(int count, IEnumerable<(int Id, string Status)> incompatible)
    {
        var parts = incompatible.Take(5).Select(p => $"{L3Codes.Format(p.Id)} ({PlanningStatus.Label(p.Status)})").ToList();
        var more = count - parts.Count;
        return $"{count} planificación(es) en estado no compatible con la reposición: {string.Join(", ", parts)}" +
               (more > 0 ? $" y {more} más" : string.Empty) + ". Revise el día antes de suspenderlo.";
    }

    private static DayRecoveryResult Fail(string message) => new(false, message);
}
