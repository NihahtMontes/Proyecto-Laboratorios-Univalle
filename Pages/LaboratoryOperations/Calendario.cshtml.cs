using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Navegador de la planificación: mes → semanas académicas → días lunes a viernes con sus prácticas.
/// Solo lectura.
/// </summary>
public class CalendarioModel : PageModel
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-BO");
    private readonly ApplicationDbContext _context;

    public CalendarioModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public int? PlanningId { get; set; }

    /// <summary>Mes seleccionado en formato yyyy-MM.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Month { get; set; }

    public bool HasPlanning => Planning != null;
    public PlanningSummary? Planning { get; private set; }
    public SelectList PlanningList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public List<MonthOption> Months { get; private set; } = [];
    public string MonthLabel { get; private set; } = string.Empty;
    public string? PreviousMonth { get; private set; }
    public string? NextMonth { get; private set; }
    public List<WeekView> Weeks { get; private set; } = [];
    public int PracticesInMonth => Weeks.Sum(w => w.Days.Sum(d => d.Practices.Count));

    public sealed record PlanningSummary(int Id, string Code, string Name, string ManagementCode, string FacultyName,
        string CareerName, DateTime StartDate, DateTime EndDate, int WeekCount, int PracticeCount);
    public sealed record MonthOption(string Value, string Label, bool Selected);
    public sealed record WeekView(int Id, int Number, DateTime StartDate, DateTime EndDate, bool IsLective, string? Note, List<DayView> Days);
    public sealed record DayView(int Weekday, string Name, DateTime Date, bool InMonth, bool CanAdd, List<PracticeCard> Practices);
    public sealed record PracticeCard(int Id, string Code, string PracticeName, string SubjectName, string LaboratoryName,
        string GroupName, string TeacherName, DateTime Start, DateTime End, string Status, string StatusCss, string NextUrl);

    public async Task OnGetAsync()
    {
        var plannings = await _context.AcademicPlannings.AsNoTracking()
            .OrderByDescending(p => p.StartDate)
            .Select(p => new { p.Id, p.Name, ManagementCode = p.Management.Code, CareerCode = p.Career.Code, CareerName = p.Career.Name, p.StartDate, p.EndDate })
            .ToListAsync();
        if (plannings.Count == 0) return;

        var today = DateTime.Today;
        var selected = plannings.FirstOrDefault(p => p.Id == PlanningId)
            ?? plannings.FirstOrDefault(p => p.StartDate <= today && today <= p.EndDate)
            ?? plannings[0];
        PlanningId = selected.Id;
        PlanningList = new SelectList(
            plannings.Select(p => new { p.Id, Name = $"{L3Codes.Planning(p.ManagementCode, p.CareerCode, p.Id)} — {p.Name}" }),
            "Id", "Name", selected.Id);

        Planning = await _context.AcademicPlannings.AsNoTracking()
            .Where(p => p.Id == selected.Id)
            .Select(p => new PlanningSummary(p.Id, L3Codes.Planning(p.Management.Code, p.Career.Code, p.Id), p.Name,
                p.Management.Code, p.Faculty.Name, p.Career.Name, p.StartDate, p.EndDate,
                p.Weeks.Count(), p.Practices.Count(r => r.Status != L3RequestStatus.Cancelled)))
            .FirstAsync();

        // Meses que cubre la planificación.
        var firstMonth = new DateTime(Planning.StartDate.Year, Planning.StartDate.Month, 1);
        var lastMonth = new DateTime(Planning.EndDate.Year, Planning.EndDate.Month, 1);
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var monthStart = DateTime.TryParseExact(Month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            && parsed >= firstMonth && parsed <= lastMonth
                ? parsed
                : currentMonth >= firstMonth && currentMonth <= lastMonth ? currentMonth : firstMonth;
        Month = monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        MonthLabel = Capitalize(monthStart.ToString("MMMM yyyy", Es));
        PreviousMonth = monthStart > firstMonth ? monthStart.AddMonths(-1).ToString("yyyy-MM", CultureInfo.InvariantCulture) : null;
        NextMonth = monthStart < lastMonth ? monthStart.AddMonths(1).ToString("yyyy-MM", CultureInfo.InvariantCulture) : null;
        for (var m = firstMonth; m <= lastMonth; m = m.AddMonths(1))
            Months.Add(new MonthOption(m.ToString("yyyy-MM", CultureInfo.InvariantCulture), Capitalize(m.ToString("MMMM yyyy", Es)), m == monthStart));

        // Semanas cuyo lunes-viernes toca el mes seleccionado.
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Where(w => w.AcademicPlanningId == selected.Id && w.StartDate <= monthEnd && w.EndDate >= monthStart)
            .OrderBy(w => w.Number)
            .ToListAsync();
        var weekIds = weeks.Select(w => w.Id).ToList();

        var practices = await _context.L3Requests.AsNoTracking()
            .Where(r => r.AcademicPlanningId == selected.Id && r.AcademicWeekId != null && weekIds.Contains(r.AcademicWeekId.Value)
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
        var teachers = await _context.Interns.AsNoTracking()
            .Where(i => teacherIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Name);

        var cardsByWeekDay = practices
            .Select(p => new
            {
                WeekId = p.AcademicWeekId!.Value,
                Weekday = AcademicWeekGenerator.WeekdayOf(p.ScheduledDate),
                Card = new PracticeCard(p.Id, L3Codes.Format(p.Id), p.PracticeName, p.SubjectName, p.LaboratoryName, p.GroupName,
                    p.TeacherPersonId.HasValue && teachers.TryGetValue(p.TeacherPersonId.Value, out var t) ? t : "—",
                    p.ScheduledDate, p.ScheduledEndDate ?? p.ScheduledDate.AddHours(1), p.Status, StatusCss(p.Status),
                    NextUrl(p.Id, p.Status, p.HasDetails))
            })
            .ToLookup(x => (x.WeekId, x.Weekday), x => x.Card);

        Weeks = weeks.Select(w => new WeekView(w.Id, w.Number, w.StartDate, w.EndDate, w.IsLective, w.Note,
                Enumerable.Range(1, 5).Select(d =>
                {
                    var date = w.StartDate.AddDays(d - 1);
                    var inMonth = date.Month == monthStart.Month && date.Year == monthStart.Year;
                    var inRange = date >= Planning.StartDate && date <= Planning.EndDate;
                    return new DayView(d, AcademicWeekGenerator.WeekdayNames[d], date, inMonth,
                        w.IsLective && inRange, cardsByWeekDay[(w.Id, d)].ToList());
                }).ToList()))
            .ToList();
    }

    private static string NextUrl(int id, string status, bool hasDetails) => status switch
    {
        L3RequestStatus.Planned when !hasDetails => $"/LaboratoryOperations/Requerimientos?planId={id}",
        L3RequestStatus.Planned => $"/LaboratoryOperations/Disponibilidad?planId={id}",
        _ => $"/LaboratoryOperations/L3?id={id}"
    };

    private static string StatusCss(string status) => status switch
    {
        L3RequestStatus.Approved => "info",
        L3RequestStatus.Finalized => "success",
        L3RequestStatus.Cancelled => "secondary",
        _ => "warning"
    };

    private static string Capitalize(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0], Es) + value[1..];
}
