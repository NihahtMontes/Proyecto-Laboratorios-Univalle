using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

namespace Proyecto_Laboratorios_Univalle.Services;

/// <summary>
/// Pedido de reprogramación de UNA planificación: el bloque se mueve entero (sus N periodos) al destino indicado.
/// AcceptedWarningCodes = códigos de advertencia que la persona vio y aceptó en la previsualización.
/// DayRecoveryId solo se usa al reponer un día suspendido (el origen debe estar PendienteReposicion en esa fecha).
/// </summary>
public sealed record RescheduleCommand(
    int PlanningId,
    DateTime Date,
    int StartPeriodId,
    int LaboratoryId,
    int TeacherPersonId,
    string Reason,
    IReadOnlyCollection<string> AcceptedWarningCodes,
    int? DayRecoveryId = null);

/// <param name="Validation">Validación del servidor al confirmar (null si falló antes de validar).</param>
public sealed record RescheduleResult(bool Ok, string Message, int? DestinationPlanningId, SlotValidationResult? Validation);

public interface IReschedulingService
{
    /// <summary>
    /// Reprograma en una sola transacción: revalida el destino bajo bloqueo, crea la planificación destino (con los
    /// requerimientos copiados), deja el origen Reprogramada (o Repuesta en una reposición) y registra la Rescheduling.
    /// Nunca cancela ni elimina: el L-3 y los L-5 quedan en el origen.
    /// </summary>
    Task<RescheduleResult> ExecuteAsync(RescheduleCommand command, CancellationToken ct = default);
}

public sealed class ReschedulingService : IReschedulingService
{
    public const int MaxReasonLength = 500;
    public const string SuccessMessage = "Se reprogramó con éxito";
    public const string ConditionsChangedMessage = "Las condiciones cambiaron desde la previsualización: revise las advertencias.";
    public const string AlreadyRescheduledMessage = "Esta planificación ya fue reprogramada por otro usuario.";

    private const string OriginUniqueIndex = "IX_Reschedulings_OriginPlanningId";

    private readonly ApplicationDbContext _context;
    private readonly ISlotValidationService _validation;
    private readonly IPlanningLock _lock;
    private readonly ILogger<ReschedulingService> _logger;

    public ReschedulingService(ApplicationDbContext context, ISlotValidationService validation, IPlanningLock planningLock,
        ILogger<ReschedulingService> logger)
    {
        _context = context;
        _validation = validation;
        _lock = planningLock;
        _logger = logger;
    }

    /// <summary>Docentes que se pueden asignar (mismo criterio que la planificación).</summary>
    public static IQueryable<Intern> ActiveTeachers(ApplicationDbContext context) => context.Interns.AsNoTracking()
        .Where(i => i.Category == PersonCategory.Docente && i.Status == GeneralStatus.Activo && i.InternStatus == GeneralStatus.Activo);

    /// <summary>
    /// Estado con el que nace el destino: Planificada y Aprobada se conservan; Finalizada vuelve a Aprobada (se ejecutará
    /// de nuevo); PendienteReposicion hereda el estado previo, obligatorio al confirmar la reposición.
    /// </summary>
    public static string InheritedStatus(string originStatus, string? statusBeforeRecovery) => originStatus switch
    {
        PlanningStatus.Planned => PlanningStatus.Planned,
        PlanningStatus.Approved or PlanningStatus.Finalized => PlanningStatus.Approved,
        PlanningStatus.PendingRecovery when statusBeforeRecovery is PlanningStatus.Approved or PlanningStatus.Finalized => PlanningStatus.Approved,
        _ => PlanningStatus.Planned
    };

    public async Task<RescheduleResult> ExecuteAsync(RescheduleCommand command, CancellationToken ct = default)
    {
        var reason = command.Reason?.Trim() ?? string.Empty;
        if (reason.Length == 0)
            return Fail("Indique el motivo de la reprogramación.");
        if (reason.Length > MaxReasonLength)
            return Fail($"El motivo no puede superar los {MaxReasonLength} caracteres.");

        var date = command.Date.Date;
        var accepted = new HashSet<string>(command.AcceptedWarningCodes ?? [], StringComparer.Ordinal);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var originScope = await _context.Plannings.AsNoTracking()
                .Where(p => p.Id == command.PlanningId)
                .Select(p => new { p.ManagementId, p.ScheduledDate })
                .FirstOrDefaultAsync(ct);
            if (originScope == null)
                return await RollbackAsync(transaction, Fail("La planificación no existe o fue eliminada."));

            var resources = new List<string>
            {
                PlanningLockResources.Planning(command.PlanningId),
                PlanningLockResources.LaboratoryDay(command.LaboratoryId, date),
                PlanningLockResources.ManagementDay(originScope.ManagementId, originScope.ScheduledDate.Date),
                PlanningLockResources.ManagementDay(originScope.ManagementId, date),
                PlanningLockResources.TeacherDay(command.TeacherPersonId, date)
            };
            if (command.DayRecoveryId is int recoveryIdToLock)
                resources.Add(PlanningLockResources.DayRecovery(recoveryIdToLock));
            // Tomar todos los recursos ordenados en una llamada evita interbloqueos entre flujos.
            if (!await _lock.TryAcquireAsync(resources, ct))
                return await RollbackAsync(transaction, Fail(PlanningLockResources.BusyMessage));

            var origin = await _context.Plannings.AsTracking().FirstOrDefaultAsync(p => p.Id == command.PlanningId, ct);
            if (origin == null || origin.ManagementId != originScope.ManagementId
                || origin.ScheduledDate.Date != originScope.ScheduledDate.Date)
                return await RollbackAsync(transaction, Fail("La planificación cambió durante el traslado; actualice la grilla."));

            var managementStatus = await _context.Managements.AsNoTracking()
                .Where(m => m.Id == origin.ManagementId)
                .Select(m => (ManagementStatus?)m.Status)
                .FirstOrDefaultAsync(ct);
            if (managementStatus is not (ManagementStatus.Active or ManagementStatus.Inactive))
                return await RollbackAsync(transaction, Fail(AcademicManagementService.ReadOnlyMessage));

            if (command.TeacherPersonId != origin.TeacherPersonId
                && !await ActiveTeachers(_context).AnyAsync(t => t.Id == command.TeacherPersonId, ct))
                return await RollbackAsync(transaction, Fail("Seleccione un docente activo."));

            // Revalidación en el servidor, ya con los bloqueos tomados.
            var validation = await _validation.ValidateAsync(
                new SlotRequest(origin.Id, date, command.StartPeriodId, command.LaboratoryId, command.TeacherPersonId), ct);
            if (validation.HasBlocking)
            {
                var blocking = validation.Issues.Where(i => i.Severity == SlotSeverity.Block).Select(i => i.Message);
                return await RollbackAsync(transaction, Fail($"No se puede reprogramar a ese destino: {string.Join(" ", blocking)}", validation));
            }

            if (validation.Issues.Any(i => i.Severity == SlotSeverity.Warning && !accepted.Contains(i.Code)))
                return await RollbackAsync(transaction, Fail(ConditionsChangedMessage, validation));

            // Reposición de día: el origen tiene que estar esperando en la bandeja de esa fecha (y viceversa).
            DayRecovery? recovery = null;
            if (command.DayRecoveryId is int recoveryId)
            {
                recovery = await _context.DayRecoveries.AsTracking().FirstOrDefaultAsync(r => r.Id == recoveryId, ct);
                if (recovery == null || recovery.ManagementId != origin.ManagementId
                    || recovery.Status != DayRecoveryStatus.InProgress
                    || origin.Status != PlanningStatus.PendingRecovery || origin.StatusBeforeRecovery is null
                    || origin.ScheduledDate.Date != recovery.Date.Date
                    || !await _context.AcademicDays.AsNoTracking().AnyAsync(d => d.Date == recovery.Date
                        && d.AcademicWeek.ManagementId == recovery.ManagementId
                        && d.Status == AcademicDayStatus.Suspended && d.DayRecoveryId == recoveryId, ct))
                    return await RollbackAsync(transaction,
                        Fail("La planificación no está pendiente de reposición en el día de esa reposición.", validation));
            }
            else if (origin.Status == PlanningStatus.PendingRecovery)
            {
                return await RollbackAsync(transaction,
                    Fail("La planificación está pendiente de reposición: reubíquela desde la reposición del día suspendido.", validation));
            }

            if (origin.PeriodCount is not int periodCount)
                return await RollbackAsync(transaction, Fail("La planificación de origen no tiene cantidad de periodos.", validation));

            var periods = await _context.ClassPeriods.AsNoTracking().ToListAsync(ct);
            var week = await _context.AcademicDays.AsNoTracking()
                .Where(d => d.Date == date && d.AcademicWeek.ManagementId == origin.ManagementId)
                .Select(d => new { d.AcademicWeekId, d.AcademicWeek.Number })
                .FirstOrDefaultAsync(ct);
            var campusId = await _context.Laboratories.AsNoTracking()
                .Where(l => l.Id == command.LaboratoryId)
                .Select(l => l.CampusId)
                .FirstOrDefaultAsync(ct);
            var requirements = await _context.PlanningRequirements.AsNoTracking()
                .Where(r => r.PlanningId == origin.Id)
                .OrderBy(r => r.Id)
                .Select(r => new { r.ArticleId, r.EquipmentUnitId, r.RequestedQuantity, r.Observations })
                .ToListAsync(ct);

            // Instantánea del origen ANTES de cambiarlo.
            var snapshot = new
            {
                Date = origin.ScheduledDate.Date,
                origin.StartPeriodId,
                origin.PeriodCount,
                origin.LaboratoryId,
                origin.TeacherPersonId,
                // En una reposición el estado real es el previo a la suspensión del día.
                Status = origin.Status == PlanningStatus.PendingRecovery && !string.IsNullOrEmpty(origin.StatusBeforeRecovery)
                    ? origin.StatusBeforeRecovery!
                    : origin.Status
            };

            var destination = new Planning
            {
                ManagementId = origin.ManagementId,
                PracticeScheduleId = origin.PracticeScheduleId,
                SubjectId = origin.SubjectId,
                GroupId = origin.GroupId,
                CourseGroupId = origin.CourseGroupId,
                GroupName = origin.GroupName,
                PracticeName = origin.PracticeName,
                PracticeNumber = origin.PracticeNumber,
                Observations = origin.Observations,
                TeacherPersonId = command.TeacherPersonId,
                LaboratoryId = command.LaboratoryId,
                CampusId = campusId,
                AllLaboratories = false,
                RequestDate = DateTime.UtcNow,
                AcademicWeekId = week?.AcademicWeekId,
                AcademicWeek = week?.Number ?? origin.AcademicWeek,
                Status = InheritedStatus(origin.Status, origin.StatusBeforeRecovery)
            };
            PlanningSlot.Apply(destination, date, command.StartPeriodId, periodCount, periods);

            // El material no se edita al reprogramar: se copian los requerimientos (sin código, que es único).
            foreach (var r in requirements)
            {
                destination.Requirements.Add(new PlanningRequirement
                {
                    ArticleId = r.ArticleId,
                    EquipmentUnitId = r.EquipmentUnitId,
                    RequestedQuantity = r.RequestedQuantity,
                    Observations = r.Observations
                });
            }

            _context.Plannings.Add(destination);
            await _context.SaveChangesAsync(ct);
            destination.Code = L3Codes.Format(destination.Id);

            origin.Status = recovery != null ? PlanningStatus.Recovered : PlanningStatus.Rescheduled;

            _context.Reschedulings.Add(new Rescheduling
            {
                OriginPlanningId = origin.Id,
                DestinationPlanningId = destination.Id,
                OriginDate = snapshot.Date,
                OriginStartPeriodId = snapshot.StartPeriodId,
                OriginPeriodCount = snapshot.PeriodCount,
                OriginLaboratoryId = snapshot.LaboratoryId,
                OriginTeacherPersonId = snapshot.TeacherPersonId,
                OriginStatus = snapshot.Status,
                DestinationDate = date,
                DestinationStartPeriodId = destination.StartPeriodId!.Value,
                DestinationPeriodCount = destination.PeriodCount!.Value,
                DestinationLaboratoryId = command.LaboratoryId,
                DestinationTeacherPersonId = command.TeacherPersonId,
                Reason = reason,
                DayRecoveryId = recovery?.Id,
                AcceptedWarnings = AcceptedWarningsJson(validation)
            });

            if (origin.PracticeScheduleId is int scheduleId)
            {
                var schedule = await _context.PracticeSchedules.AsTracking().FirstOrDefaultAsync(s => s.Id == scheduleId, ct);
                if (schedule != null)
                {
                    schedule.ExecutionDate = date;
                    schedule.StartTime = destination.StartTime;
                    schedule.EndTime = destination.EndTime;
                    schedule.LaboratoryId = command.LaboratoryId;
                    schedule.TeacherPersonId = command.TeacherPersonId;
                    schedule.AcademicWeekId = destination.AcademicWeekId;
                    schedule.Status = PracticeScheduleStatus.Rescheduled;
                }
            }

            if (recovery != null)
            {
                var from = recovery.Date.Date;
                var to = from.AddDays(1);
                var stillPending = await _context.Plannings.AsNoTracking()
                    .AnyAsync(p => p.Id != origin.Id && p.ManagementId == recovery.ManagementId
                        && p.Status == PlanningStatus.PendingRecovery
                        && p.ScheduledDate >= from && p.ScheduledDate < to, ct);
                if (!stillPending)
                    recovery.Status = DayRecoveryStatus.Completed;
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new RescheduleResult(true, recovery != null ? "Se repuso con éxito" : SuccessMessage, destination.Id, validation);
        }
        catch (DbUpdateException ex)
        {
            await SafeRollbackAsync(transaction);
            _context.ChangeTracker.Clear();
            if (IsOriginUniqueViolation(ex))
                return Fail(AlreadyRescheduledMessage);
            _logger.LogError(ex, "No se pudo reprogramar la planificación {PlanningId}", command.PlanningId);
            return Fail("No se pudo guardar la reprogramación. Intente nuevamente.");
        }
        catch (ArgumentException ex)
        {
            // PlanningSlot.Apply: el bloque no cabe o el periodo no existe (la validación debería haberlo detectado).
            await SafeRollbackAsync(transaction);
            _context.ChangeTracker.Clear();
            return Fail(ex.Message);
        }
    }

    /// <summary>JSON [{code, message}] de las advertencias presentes al confirmar (todas aceptadas a esa altura).</summary>
    public static string? AcceptedWarningsJson(SlotValidationResult validation)
    {
        var warnings = validation.Issues
            .Where(i => i.Severity == SlotSeverity.Warning)
            .Select(i => new { code = i.Code, message = i.Message })
            .ToList();
        return warnings.Count == 0 ? null : JsonSerializer.Serialize(warnings);
    }

    private static bool IsOriginUniqueViolation(DbUpdateException ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
        {
            if (e.Message.Contains(OriginUniqueIndex, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private async Task<RescheduleResult> RollbackAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, RescheduleResult result)
    {
        await SafeRollbackAsync(transaction);
        _context.ChangeTracker.Clear();
        return result;
    }

    private static async Task SafeRollbackAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (InvalidOperationException)
        {
            // La transacción ya no está activa (p. ej. la conexión la revirtió): no hay nada más que deshacer.
        }
    }

    private static RescheduleResult Fail(string message, SlotValidationResult? validation = null) => new(false, message, null, validation);
}
