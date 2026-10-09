using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>Calendario de una gestión: mes → semanas académicas → días lunes a sábado. Solo lectura (vista resumen).</summary>
public sealed record PlanningCalendar(
    int ManagementId,
    bool AllowAdd,
    string Month,
    string MonthLabel,
    string? PreviousMonth,
    string? NextMonth,
    IReadOnlyList<PlanningCalendar.MonthOption> Months,
    IReadOnlyList<PlanningCalendar.WeekView> Weeks)
{
    /// <summary>Planificaciones vigentes (que ocupan ambiente) en los días del mes.</summary>
    public int PlanningsInMonth => Weeks.Sum(w => w.Days.Where(d => d.InMonth).Sum(d => d.Plannings.Count(p => p.Occupies)));

    public sealed record MonthOption(string Value, string Label, bool Selected);
    public sealed record WeekView(int Id, int Number, DateTime StartDate, DateTime EndDate, List<DayView> Days);

    /// <param name="Exists">False si el día cae fuera de las fechas de la gestión (no hay día académico).</param>
    public sealed record DayView(int Weekday, string Name, DateTime Date, bool InMonth, bool Exists, bool IsSuspended,
        string? Reason, bool CanAdd, List<PlanningCard> Plannings);

    public sealed record PlanningCard(int Id, string Code, string PracticeName, string SubjectName, string LaboratoryName,
        string GroupName, string TeacherName, string TimeText, string Status, string StatusLabel, string StatusCss,
        bool Occupies, string NextUrl);

    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-BO");

    /// <param name="allowAdd">Solo una gestión editable permite "+ Planificación".</param>
    public static async Task<PlanningCalendar> BuildAsync(ApplicationDbContext context, AcademicManagementSummary management, string? month, bool allowAdd)
    {
        var weeks = await context.AcademicWeeks.AsNoTracking()
            .Where(w => w.ManagementId == management.Id)
            .OrderBy(w => w.Number)
            .Select(w => new { w.Id, w.Number, w.StartDate, w.EndDate })
            .ToListAsync();

        // Rango de meses: las fechas de la gestión o, si faltan, las semanas generadas.
        var rangeStart = management.StartDate ?? weeks.Select(w => (DateTime?)w.StartDate).FirstOrDefault() ?? DateTime.Today;
        var rangeEnd = management.EndDate ?? weeks.Select(w => (DateTime?)w.EndDate).LastOrDefault() ?? rangeStart;
        if (rangeEnd < rangeStart) rangeEnd = rangeStart;

        var today = DateTime.Today;
        var firstMonth = new DateTime(rangeStart.Year, rangeStart.Month, 1);
        var lastMonth = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var monthStart = DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            && parsed >= firstMonth && parsed <= lastMonth
                ? parsed
                : currentMonth >= firstMonth && currentMonth <= lastMonth ? currentMonth : firstMonth;

        var months = new List<MonthOption>();
        for (var m = firstMonth; m <= lastMonth; m = m.AddMonths(1))
            months.Add(new MonthOption(Key(m), Label(m), m == monthStart));

        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var monthWeeks = weeks.Where(w => w.StartDate <= monthEnd && w.EndDate >= monthStart).ToList();
        var weekIds = monthWeeks.Select(w => w.Id).ToList();

        var days = await context.AcademicDays.AsNoTracking()
            .Where(d => weekIds.Contains(d.AcademicWeekId))
            .Select(d => new { d.Date, d.Status, d.Reason })
            .ToListAsync();
        var dayByDate = days.ToDictionary(d => d.Date.Date);

        var from = monthWeeks.Count > 0 ? monthWeeks.Min(w => w.StartDate) : monthStart;
        var to = (monthWeeks.Count > 0 ? monthWeeks.Max(w => w.EndDate) : monthEnd).AddDays(1);
        var plannings = await context.Plannings.AsNoTracking()
            .Where(r => r.ManagementId == management.Id && r.ScheduledDate >= from && r.ScheduledDate < to
                && r.Status != PlanningStatus.Cancelled)
            .OrderBy(r => r.ScheduledDate)
            .Select(r => new
            {
                r.Id, r.PracticeName, r.ScheduledDate, r.ScheduledEndDate, r.Status, r.TeacherPersonId,
                StartOrder = r.StartPeriod != null ? (int?)r.StartPeriod.Order : null,
                r.PeriodCount,
                SubjectName = r.Subject != null ? r.Subject.Name : "Sin materia",
                LaboratoryName = r.AllLaboratories ? "Todos los laboratorios"
                    : (r.Laboratory != null ? r.Laboratory.Code + " · " + r.Laboratory.Name : "—"),
                GroupLabel = !string.IsNullOrEmpty(r.GroupName) ? r.GroupName
                    : (r.Group != null ? r.Group.Name : "—"),
                HasRequirements = r.Requirements.Any()
            })
            .ToListAsync();

        var teacherIds = plannings.Select(p => p.TeacherPersonId).Distinct().ToList();
        var teachers = await context.Interns.AsNoTracking()
            .Where(i => teacherIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Name);

        var cards = plannings.ToLookup(p => p.ScheduledDate.Date, p => new PlanningCard(
            p.Id, L3Codes.Format(p.Id), p.PracticeName, p.SubjectName, p.LaboratoryName, p.GroupLabel,
            teachers.TryGetValue(p.TeacherPersonId, out var t) ? t : "—",
            PlanningStatusView.TimeText(p.StartOrder, p.PeriodCount, p.ScheduledDate, p.ScheduledEndDate),
            p.Status, PlanningStatus.Label(p.Status), PlanningStatusView.Css(p.Status), PlanningStatus.Occupies(p.Status),
            PlanningStatusView.NextUrl(p.Id, p.Status, p.HasRequirements)));

        var weekViews = monthWeeks.Select(w => new WeekView(w.Id, w.Number, w.StartDate, w.EndDate,
                Enumerable.Range(1, 6).Select(d =>
                {
                    var date = w.StartDate.AddDays(d - 1).Date;
                    var inMonth = date.Month == monthStart.Month && date.Year == monthStart.Year;
                    var exists = dayByDate.TryGetValue(date, out var day);
                    var suspended = exists && day!.Status == AcademicDayStatus.Suspended;
                    return new DayView(d, AcademicWeekGenerator.WeekdayNames[d], date, inMonth, exists, suspended,
                        suspended ? day!.Reason : null, allowAdd && exists && !suspended, cards[date].ToList());
                }).ToList()))
            .ToList();

        return new PlanningCalendar(management.Id, allowAdd, Key(monthStart), Label(monthStart),
            monthStart > firstMonth ? Key(monthStart.AddMonths(-1)) : null,
            monthStart < lastMonth ? Key(monthStart.AddMonths(1)) : null,
            months, weekViews);
    }

    private static string Key(DateTime month) => month.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    private static string Label(DateTime month)
    {
        var value = month.ToString("MMMM yyyy", Es);
        return char.ToUpper(value[0], Es) + value[1..];
    }
}

/// <summary>Presentación común del estado de una planificación (colores, horario y siguiente paso).</summary>
public static class PlanningStatusView
{
    public static string Css(string status) => status switch
    {
        PlanningStatus.Approved => "info",
        PlanningStatus.Finalized => "success",
        PlanningStatus.Cancelled => "secondary",
        PlanningStatus.Rescheduled or PlanningStatus.Recovered => "light",
        PlanningStatus.PendingRecovery => "danger",
        _ => "warning"
    };

    /// <summary>"P1–P4 · 07:35–11:15", o solo las horas en planificaciones sin periodo (legado).</summary>
    public static string TimeText(int? startOrder, int? periodCount, DateTime start, DateTime? end)
    {
        var hours = $"{start:HH\\:mm}–{(end ?? start.AddHours(1)):HH\\:mm}";
        if (startOrder is not int first || periodCount is not int count) return hours;
        var periods = count == 1 ? $"P{first}" : $"P{first}–P{PlanningSlot.LastOrder(first, count)}";
        return $"{periods} · {hours}";
    }

    public static string NextUrl(int id, string status, bool hasRequirements) => status switch
    {
        PlanningStatus.Planned when !hasRequirements => $"/LaboratoryOperations/Requerimientos?planId={id}",
        PlanningStatus.Planned => $"/LaboratoryOperations/Disponibilidad?planId={id}",
        _ => $"/LaboratoryOperations/L3?id={id}"
    };
}
