using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>Calendario de una planificación: mes → semanas académicas → días lunes a viernes. Solo lectura.</summary>
public sealed record PlanningCalendar(
    int PlanningId,
    bool AllowAdd,
    string Month,
    string MonthLabel,
    string? PreviousMonth,
    string? NextMonth,
    IReadOnlyList<PlanningCalendar.MonthOption> Months,
    IReadOnlyList<PlanningCalendar.WeekView> Weeks)
{
    public int PracticesInMonth => Weeks.Sum(w => w.Days.Sum(d => d.Practices.Count));

    public sealed record MonthOption(string Value, string Label, bool Selected);
    public sealed record WeekView(int Id, int Number, DateTime StartDate, DateTime EndDate, bool IsLective, string? Note, List<DayView> Days);
    public sealed record DayView(int Weekday, string Name, DateTime Date, bool InMonth, bool CanAdd, List<PracticeCard> Practices);
    public sealed record PracticeCard(int Id, string Code, string PracticeName, string SubjectName, string LaboratoryName,
        string GroupName, string TeacherName, DateTime Start, DateTime End, string Status, string StatusCss, string NextUrl);

    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-BO");

    /// <param name="allowAdd">Solo la planificación activa permite "+ Práctica".</param>
    public static async Task<PlanningCalendar> BuildAsync(ApplicationDbContext context, PlanningSummary planning, string? month, bool allowAdd)
    {
        var today = DateTime.Today;
        var firstMonth = new DateTime(planning.StartDate.Year, planning.StartDate.Month, 1);
        var lastMonth = new DateTime(planning.EndDate.Year, planning.EndDate.Month, 1);
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var monthStart = DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            && parsed >= firstMonth && parsed <= lastMonth
                ? parsed
                : currentMonth >= firstMonth && currentMonth <= lastMonth ? currentMonth : firstMonth;

        var months = new List<MonthOption>();
        for (var m = firstMonth; m <= lastMonth; m = m.AddMonths(1))
            months.Add(new MonthOption(Key(m), Label(m), m == monthStart));

        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var weeks = await context.AcademicWeeks.AsNoTracking()
            .Where(w => w.AcademicPlanningId == planning.Id && w.StartDate <= monthEnd && w.EndDate >= monthStart)
            .OrderBy(w => w.Number)
            .ToListAsync();
        var weekIds = weeks.Select(w => w.Id).ToList();

        var practices = await context.L3Requests.AsNoTracking()
            .Where(r => r.AcademicPlanningId == planning.Id && r.AcademicWeekId != null && weekIds.Contains(r.AcademicWeekId.Value)
                && r.Status != L3RequestStatus.Cancelled)
            .OrderBy(r => r.ScheduledDate)
            .Select(r => new
            {
                r.Id, r.AcademicWeekId, r.PracticeName, SubjectName = r.Subject.Name, LaboratoryName = r.Laboratory.Name,
                GroupName = r.Group.Name, r.TeacherPersonId, r.ScheduledDate, r.ScheduledEndDate, r.Status,
                HasDetails = r.Details.Any()
            })
            .ToListAsync();

        var teacherIds = practices.Where(p => p.TeacherPersonId.HasValue).Select(p => p.TeacherPersonId!.Value).Distinct().ToList();
        var teachers = await context.Interns.AsNoTracking()
            .Where(i => teacherIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Name);

        var cards = practices
            .Select(p => new
            {
                WeekId = p.AcademicWeekId!.Value,
                Weekday = AcademicWeekGenerator.WeekdayOf(p.ScheduledDate),
                Card = new PracticeCard(p.Id, L3Codes.Format(p.Id), p.PracticeName, p.SubjectName, p.LaboratoryName, p.GroupName,
                    p.TeacherPersonId.HasValue && teachers.TryGetValue(p.TeacherPersonId.Value, out var t) ? t : "—",
                    p.ScheduledDate, p.ScheduledEndDate ?? p.ScheduledDate.AddHours(1), p.Status,
                    PracticeStatus.Css(p.Status), PracticeStatus.NextUrl(p.Id, p.Status, p.HasDetails))
            })
            .ToLookup(x => (x.WeekId, x.Weekday), x => x.Card);

        var weekViews = weeks.Select(w => new WeekView(w.Id, w.Number, w.StartDate, w.EndDate, w.IsLective, w.Note,
                Enumerable.Range(1, 5).Select(d =>
                {
                    var date = w.StartDate.AddDays(d - 1);
                    var inMonth = date.Month == monthStart.Month && date.Year == monthStart.Year;
                    var inRange = date >= planning.StartDate && date <= planning.EndDate;
                    return new DayView(d, AcademicWeekGenerator.WeekdayNames[d], date, inMonth,
                        allowAdd && w.IsLective && inRange, cards[(w.Id, d)].ToList());
                }).ToList()))
            .ToList();

        return new PlanningCalendar(planning.Id, allowAdd, Key(monthStart), Label(monthStart),
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

/// <summary>Presentación común del estado de una práctica (colores y siguiente paso).</summary>
public static class PracticeStatus
{
    public static string Css(string status) => status switch
    {
        L3RequestStatus.Approved => "info",
        L3RequestStatus.Finalized => "success",
        L3RequestStatus.Cancelled => "secondary",
        _ => "warning"
    };

    public static string NextUrl(int id, string status, bool hasDetails) => status switch
    {
        L3RequestStatus.Planned when !hasDetails => $"/LaboratoryOperations/Requerimientos?planId={id}",
        L3RequestStatus.Planned => $"/LaboratoryOperations/Disponibilidad?planId={id}",
        _ => $"/LaboratoryOperations/L3?id={id}"
    };
}
