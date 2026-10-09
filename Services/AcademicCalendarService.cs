using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

public sealed record CalendarResult(bool Ok, string Message);

public interface IAcademicCalendarService
{
    /// <summary>Genera/actualiza semanas (lun-sáb) y días de la gestión desde el inicio y fin de clases (o las fechas de la gestión).</summary>
    Task<CalendarResult> GenerateWeeksAsync(int managementId, DateTime? classesStartDate = null,
        DateTime? classesEndDate = null, bool updateClassDates = false);

    /// <summary>Suspende (motivo obligatorio) o habilita un día sin planificaciones vigentes.</summary>
    Task<CalendarResult> SetDayStatusAsync(int dayId, AcademicDayStatus status, string? reason);
}

/// <summary>
/// Mantiene el calendario académico (semanas y días) de una gestión. Las semanas y sus días son filas de
/// configuración derivadas de las fechas de la gestión: se pueden crear y eliminar físicamente mientras no
/// tengan historial (planificaciones, programaciones o reposiciones que las referencien).
/// </summary>
public sealed class AcademicCalendarService : IAcademicCalendarService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AcademicCalendarService> _logger;
    private readonly IPlanningLock _lock;

    public AcademicCalendarService(ApplicationDbContext context, ILogger<AcademicCalendarService> logger, IPlanningLock planningLock)
    {
        _context = context;
        _logger = logger;
        _lock = planningLock;
    }

    public async Task<CalendarResult> GenerateWeeksAsync(int managementId, DateTime? classesStartDate = null,
        DateTime? classesEndDate = null, bool updateClassDates = false)
    {
        var management = await _context.Managements.AsTracking()
            .FirstOrDefaultAsync(m => m.Id == managementId);
        if (management == null || management.Semester <= 0
            || management.Status is not (ManagementStatus.Active or ManagementStatus.Inactive))
            return Fail("La gestión está completada o no existe.");

        // Las fechas solo se persisten si el calendario se genera correctamente en esta misma transacción.
        if (updateClassDates)
        {
            management.ClassesStartDate = classesStartDate?.Date;
            management.ClassesEndDate = classesEndDate?.Date;
        }

        // Las semanas se numeran desde el inicio de clases; sin fechas de clases se usan las de la gestión.
        var startDate = management.ClassesStartDate ?? management.StartDate;
        var endDate = management.ClassesEndDate ?? management.PlannedEndDate;
        if (startDate is not DateTime start || endDate is not DateTime end)
            return Fail("Registre las fechas de inicio y fin de la gestión (o de clases) antes de generar el calendario.");
        if ((management.StartDate is DateTime managementStart && start.Date < managementStart.Date)
            || (management.PlannedEndDate is DateTime managementEnd && end.Date > managementEnd.Date))
            return Fail($"Las fechas de clases deben estar dentro de las fechas de la gestión ({management.StartDate:dd/MM/yyyy} – {management.PlannedEndDate:dd/MM/yyyy}).");

        List<AcademicWeekGenerator.WeekSlot> slots;
        try
        {
            slots = [.. AcademicWeekGenerator.Generate(start, end)];
        }
        catch (ArgumentException ex)
        {
            return Fail(ex.Message);
        }

        var from = start.Date;
        var to = end.Date;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Generar, suspender y reservar usan los mismos recursos de día. Incluir todo el semestre
            // (también los días que todavía no existen) impide crear una reserva entre la comprobación
            // del historial y el borrado/recreación del calendario.
            var existingDates = await _context.AcademicDays.AsNoTracking()
                .Where(d => d.AcademicWeek.ManagementId == managementId)
                .Select(d => d.Date).ToListAsync();
            var firstProtected = (management.StartDate ?? start).Date;
            var lastProtected = (management.PlannedEndDate ?? end).Date;
            if (lastProtected < firstProtected || (lastProtected - firstProtected).TotalDays > 370)
                return Fail("El rango de la gestión no es válido para generar el calendario semestral.");
            var resources = Enumerable.Range(0, (lastProtected - firstProtected).Days + 1)
                .Select(i => PlanningLockResources.ManagementDay(managementId, firstProtected.AddDays(i)))
                .Concat(existingDates.Select(d => PlanningLockResources.ManagementDay(managementId, d)))
                .Append(PlanningLockResources.ManagementCalendar(managementId));
            if (!await _lock.TryAcquireAsync(resources))
                return Fail(PlanningLockResources.BusyMessage);

            var weeks = await _context.AcademicWeeks.AsTracking()
                .Include(w => w.Days)
                .Where(w => w.ManagementId == managementId)
                .OrderBy(w => w.StartDate)
                .ToListAsync();

            var historyWeekIds = await HistoryWeekIdsAsync(weeks);
            var slotByMonday = slots.ToDictionary(s => s.Monday.Date);

            // Las semanas se emparejan por su lunes (no por número): si cambia el inicio de clases, las semanas
            // existentes conservan su Id y sus días (incluidos los suspendidos), y solo cambia su número.
            var outside = weeks.Where(w => !slotByMonday.ContainsKey(w.StartDate.Date)).ToList();
            var outsideWithHistory = outside.Where(w => historyWeekIds.Contains(w.Id)).ToList();
            if (outsideWithHistory.Count > 0)
                return Fail("No se puede ajustar el calendario: las semanas del "
                    + string.Join(", ", outsideWithHistory.Select(w => w.StartDate.ToString("dd/MM/yyyy")))
                    + " tienen planificaciones y quedarían fuera de las fechas de clases.");

            // Semanas de configuración sin historial que quedan fuera del rango: eliminación física de sus días y de la semana.
            foreach (var week in outside)
            {
                _context.AcademicDays.RemoveRange(week.Days);
                _context.AcademicWeeks.Remove(week);
            }
            var kept = weeks.Except(outside).ToList();

            // Días existentes que quedaron fuera de [inicio, fin]: se quitan salvo que alguien los use.
            foreach (var week in kept)
            {
                foreach (var day in week.Days.Where(d => d.Date < from || d.Date > to).ToList())
                {
                    if (day.DayRecoveryId != null || await HasOccupyingPlanningsAsync(managementId, day.Date))
                        return Fail($"El {day.Date:dd/MM/yyyy} quedó fuera de las fechas de clases y tiene planificaciones vigentes o una reposición asociada: ajuste las fechas o reubique esas planificaciones.");
                    // Día de configuración sin uso: eliminación física.
                    _context.AcademicDays.Remove(day);
                }
            }

            // Renumeración en dos pasos para no chocar con el índice único (gestión, número).
            var renumbered = kept.Where(w => w.Number != slotByMonday[w.StartDate.Date].Number).ToList();
            if (renumbered.Count > 0)
            {
                foreach (var week in renumbered)
                    week.Number = -week.Number;
                await _context.SaveChangesAsync();
                foreach (var week in renumbered)
                    week.Number = slotByMonday[week.StartDate.Date].Number;
            }
            await _context.SaveChangesAsync();

            // Crear las semanas y días que falten (solo fechas lun-sáb dentro del rango).
            var weeksByMonday = kept.ToDictionary(w => w.StartDate.Date);
            foreach (var slot in slots)
            {
                if (!weeksByMonday.TryGetValue(slot.Monday.Date, out var week))
                {
                    week = new AcademicWeek
                    {
                        ManagementId = managementId,
                        Number = slot.Number,
                        StartDate = slot.Monday,
                        EndDate = slot.Saturday
                    };
                    _context.AcademicWeeks.Add(week);
                    weeksByMonday[slot.Monday.Date] = week;
                }

                foreach (var date in SlotDates(slot, from, to))
                {
                    if (week.Days.All(d => d.Date != date))
                        week.Days.Add(new AcademicDay { Date = date, Status = AcademicDayStatus.Enabled });
                }
            }
            await _context.SaveChangesAsync();

            // El número de semana copiado en las planificaciones sigue al calendario (incluidas las borradas).
            if (renumbered.Count > 0)
            {
                var numberById = weeksByMonday.Values.ToDictionary(w => w.Id, w => w.Number);
                var weekIds = numberById.Keys.ToList();
                var plannings = await _context.Plannings.IgnoreQueryFilters().AsTracking()
                    .Where(p => p.AcademicWeekId != null && weekIds.Contains(p.AcademicWeekId.Value))
                    .ToListAsync();
                foreach (var planning in plannings)
                    planning.AcademicWeek = numberById[planning.AcademicWeekId!.Value];
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            var dayCount = slots.Sum(slot => SlotDates(slot, from, to).Count);
            return new CalendarResult(true, $"Calendario académico actualizado: {slots.Count} semanas y {dayCount} días"
                + (renumbered.Count > 0 ? $"; se renumeraron {renumbered.Count} semanas existentes." : "."));
        }
        catch (DbUpdateException ex)
        {
            await transaction.SafeRollbackAsync();
            _logger.LogError(ex, "No se pudo actualizar el calendario académico de la gestión {ManagementId}", managementId);
            return Fail("No se pudo actualizar el calendario académico. Intente nuevamente.");
        }
    }

    public async Task<CalendarResult> SetDayStatusAsync(int dayId, AcademicDayStatus status, string? reason)
    {
        if (status is not (AcademicDayStatus.Enabled or AcademicDayStatus.Suspended))
            return Fail("El estado del día académico no es válido.");
        var scope = await _context.AcademicDays.AsNoTracking()
            .Where(d => d.Id == dayId)
            .Select(d => new { d.Date, d.AcademicWeek.ManagementId })
            .FirstOrDefaultAsync();
        if (scope == null)
            return Fail("El día académico no existe.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (!await _lock.TryAcquireAsync([PlanningLockResources.ManagementDay(scope.ManagementId, scope.Date)]))
            return Fail(PlanningLockResources.BusyMessage);

        var day = await _context.AcademicDays.AsTracking()
            .FirstOrDefaultAsync(d => d.Id == dayId && d.Date == scope.Date && d.AcademicWeek.ManagementId == scope.ManagementId);
        if (day == null)
            return Fail("El día académico cambió; actualice el calendario.");
        var managementStatus = await _context.Managements.AsNoTracking()
            .Where(m => m.Id == scope.ManagementId)
            .Select(m => (ManagementStatus?)m.Status)
            .FirstOrDefaultAsync();
        if (managementStatus is not (ManagementStatus.Active or ManagementStatus.Inactive))
            return Fail(AcademicManagementService.ReadOnlyMessage);

        if (status == AcademicDayStatus.Suspended)
        {
            var motivo = reason?.Trim();
            if (string.IsNullOrWhiteSpace(motivo))
                return Fail("Indique el motivo de la suspensión del día.");
            if (motivo.Length > 200)
                return Fail("El motivo de la suspensión no puede superar 200 caracteres.");

            var count = await _context.Plannings
                .CountAsync(p => p.ManagementId == scope.ManagementId
                    && p.ScheduledDate >= day.Date && p.ScheduledDate < day.Date.AddDays(1)
                    && !PlanningStatus.NonOccupying.Contains(p.Status));
            if (count > 0)
                return Fail($"Hay {count} planificaciones ese día: use la reposición de día para reubicarlas.");

            day.Status = AcademicDayStatus.Suspended;
            day.Reason = motivo;
        }
        else
        {
            if (day.DayRecoveryId != null)
                return Fail("El día pertenece a una reposición de día; no se puede habilitar aquí.");

            day.Status = AcademicDayStatus.Enabled;
            day.Reason = null;
        }

        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            await transaction.SafeRollbackAsync();
            _context.ChangeTracker.Clear();
            _logger.LogError(ex, "No se pudo cambiar el estado del día académico {DayId}", dayId);
            return Fail("No se pudo actualizar el día académico. Intente nuevamente.");
        }
        return status == AcademicDayStatus.Suspended
            ? new CalendarResult(true, $"Día {day.Date:dd/MM/yyyy} suspendido.")
            : new CalendarResult(true, $"Día {day.Date:dd/MM/yyyy} habilitado.");
    }

    /// <summary>Semanas referenciadas por planificaciones (incluidas las borradas), programaciones o reposiciones.</summary>
    private async Task<HashSet<int>> HistoryWeekIdsAsync(List<AcademicWeek> weeks)
    {
        var weekIds = weeks.Select(w => w.Id).ToList();
        if (weekIds.Count == 0) return [];

        // IgnoreQueryFilters: una planificación borrada también ancla su semana al calendario.
        var planningWeekIds = await _context.Plannings.IgnoreQueryFilters()
            .Where(p => p.AcademicWeekId != null && weekIds.Contains(p.AcademicWeekId.Value))
            .Select(p => p.AcademicWeekId!.Value)
            .ToListAsync();

        var scheduleWeekIds = await _context.PracticeSchedules
            .Where(s => s.AcademicWeekId != null && weekIds.Contains(s.AcademicWeekId.Value))
            .Select(s => s.AcademicWeekId!.Value)
            .ToListAsync();

        var recoveryWeekIds = weeks
            .Where(w => w.Days.Any(d => d.DayRecoveryId != null))
            .Select(w => w.Id);

        return planningWeekIds.Concat(scheduleWeekIds).Concat(recoveryWeekIds).ToHashSet();
    }

    /// <summary>True si la fecha tiene planificaciones no borradas con estado que ocupa ambiente/material.</summary>
    private Task<bool> HasOccupyingPlanningsAsync(int managementId, DateTime date) =>
        _context.Plannings
            .AnyAsync(p => p.ManagementId == managementId
                && p.ScheduledDate >= date && p.ScheduledDate < date.AddDays(1)
                && !PlanningStatus.NonOccupying.Contains(p.Status));

    /// <summary>Fechas lunes-sábado del slot que caen dentro de [from, to].</summary>
    private static List<DateTime> SlotDates(AcademicWeekGenerator.WeekSlot slot, DateTime from, DateTime to)
    {
        var dates = new List<DateTime>();
        for (var i = 0; i < 6; i++)
        {
            var date = slot.Monday.AddDays(i);
            if (date >= from && date <= to)
                dates.Add(date);
        }
        return dates;
    }

    private static CalendarResult Fail(string message) => new(false, message);
}
