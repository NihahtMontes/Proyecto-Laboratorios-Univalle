using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services;

public sealed record CalendarResult(bool Ok, string Message);

public interface IAcademicCalendarService
{
    /// <summary>Genera/actualiza semanas (lun-sáb) y días de la gestión a partir de Management.StartDate y PlannedEndDate.</summary>
    Task<CalendarResult> GenerateWeeksAsync(int managementId);

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

    public AcademicCalendarService(ApplicationDbContext context, ILogger<AcademicCalendarService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CalendarResult> GenerateWeeksAsync(int managementId)
    {
        var management = await _context.Managements.AsTracking()
            .FirstOrDefaultAsync(m => m.Id == managementId);
        if (management == null || management.Semester <= 0
            || management.Status is not (ManagementStatus.Active or ManagementStatus.Inactive))
            return Fail("La gestión está completada o no existe.");
        if (management.StartDate is not DateTime start || management.PlannedEndDate is not DateTime end)
            return Fail("Registre las fechas de inicio y fin de la gestión antes de generar el calendario.");

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
            var weeks = await _context.AcademicWeeks.AsTracking()
                .Include(w => w.Days)
                .Where(w => w.ManagementId == managementId)
                .OrderBy(w => w.Number)
                .ToListAsync();

            var historyWeekIds = await HistoryWeekIdsAsync(weeks);

            // La fecha de inicio cambió de semana: el calendario entero se corrió de lugar.
            if (weeks.Count > 0 && weeks[0].StartDate.Date != slots[0].Monday.Date)
            {
                if (weeks.Any(w => historyWeekIds.Contains(w.Id)))
                    return Fail("La gestión ya tiene planificaciones: no se puede cambiar la fecha de inicio del calendario.");

                // Semanas y días sin historial son filas de configuración derivadas de las fechas de la
                // gestión (no datos de negocio): se eliminan físicamente y se regeneran desde cero.
                _context.AcademicDays.RemoveRange(weeks.SelectMany(w => w.Days));
                _context.AcademicWeeks.RemoveRange(weeks);
                await _context.SaveChangesAsync();
                weeks = [];
            }

            // Calendario más corto: las semanas que sobran solo se eliminan si nadie las referencia.
            var extra = weeks.Where(w => w.Number > slots.Count).OrderBy(w => w.Number).ToList();
            if (extra.Count > 0)
            {
                var withHistory = extra.Where(w => historyWeekIds.Contains(w.Id)).Select(w => w.Number).ToList();
                if (withHistory.Count > 0)
                    return Fail($"No se puede acortar el calendario: las semanas {string.Join(", ", withHistory)} tienen planificaciones.");

                // Semanas de configuración sin historial: eliminación física de sus días y de la semana.
                foreach (var week in extra)
                {
                    _context.AcademicDays.RemoveRange(week.Days);
                    _context.AcademicWeeks.Remove(week);
                }
                await _context.SaveChangesAsync();
                weeks = weeks.Where(w => w.Number <= slots.Count).ToList();
            }

            // Días existentes que quedaron fuera de [inicio, fin]: se quitan salvo que alguien los use.
            foreach (var week in weeks)
            {
                foreach (var day in week.Days.Where(d => d.Date < from || d.Date > to).ToList())
                {
                    if (day.DayRecoveryId != null || await HasOccupyingPlanningsAsync(day.Date))
                        return Fail($"El {day.Date:dd/MM/yyyy} quedó fuera de las fechas de la gestión y tiene planificaciones vigentes o una reposición asociada: ajuste las fechas o reubique esas planificaciones.");
                    // Día de configuración sin uso: eliminación física.
                    _context.AcademicDays.Remove(day);
                }
            }

            // Crear las semanas y días que falten (solo fechas lun-sáb dentro del rango).
            var weeksByNumber = weeks.ToDictionary(w => w.Number);
            foreach (var slot in slots)
            {
                if (!weeksByNumber.TryGetValue(slot.Number, out var week))
                {
                    week = new AcademicWeek
                    {
                        ManagementId = managementId,
                        Number = slot.Number,
                        StartDate = slot.Monday,
                        EndDate = slot.Saturday
                    };
                    _context.AcademicWeeks.Add(week);
                    weeksByNumber[slot.Number] = week;
                }

                foreach (var date in SlotDates(slot, from, to))
                {
                    if (week.Days.All(d => d.Date != date))
                        week.Days.Add(new AcademicDay { Date = date, Status = AcademicDayStatus.Enabled });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var dayCount = slots.Sum(slot => SlotDates(slot, from, to).Count);
            return new CalendarResult(true, $"Calendario académico actualizado: {slots.Count} semanas y {dayCount} días.");
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo actualizar el calendario académico de la gestión {ManagementId}", managementId);
            return Fail("No se pudo actualizar el calendario académico. Intente nuevamente.");
        }
    }

    public async Task<CalendarResult> SetDayStatusAsync(int dayId, AcademicDayStatus status, string? reason)
    {
        var day = await _context.AcademicDays.AsTracking()
            .Include(d => d.AcademicWeek)
            .FirstOrDefaultAsync(d => d.Id == dayId);
        if (day == null || day.AcademicWeek == null)
            return Fail("El día académico no existe.");

        var managementStatus = await _context.Managements.AsNoTracking()
            .Where(m => m.Id == day.AcademicWeek.ManagementId)
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
                .CountAsync(p => p.ScheduledDate >= day.Date && p.ScheduledDate < day.Date.AddDays(1)
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

        await _context.SaveChangesAsync();
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
    private Task<bool> HasOccupyingPlanningsAsync(DateTime date) =>
        _context.Plannings
            .AnyAsync(p => p.ScheduledDate >= date && p.ScheduledDate < date.AddDays(1)
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
