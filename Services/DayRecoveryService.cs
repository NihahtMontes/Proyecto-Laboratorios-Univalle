using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

public sealed record DayRecoveryResult(bool Ok, string Message, int? RecoveryId = null, int PendingCount = 0);

public interface IDayRecoveryService
{
    /// <summary>Suspende el día entero y conserva el estado previo de todas sus planificaciones vigentes.</summary>
    Task<DayRecoveryResult> SuspendAsync(int dayId, string? reason, CancellationToken ct = default);
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
            if (candidates.Any(p => p.Status is not (PlanningStatus.Planned or PlanningStatus.Approved or PlanningStatus.Finalized)))
                return Fail("Hay una planificación en estado no compatible con la reposición. Revise el día antes de suspenderlo.");

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

    private static DayRecoveryResult Fail(string message) => new(false, message);
}
