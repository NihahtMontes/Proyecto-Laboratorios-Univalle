using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.Docente;

/// <summary>Inicio del docente: próxima práctica, contadores, agenda de la semana y novedades de sus solicitudes.</summary>
public class IndexModel : TeacherPageBase
{
    public IndexModel(ApplicationDbContext db, ITeacherRequestService requests) : base(db, requests) { }

    public TeacherPlanningRow? Next { get; private set; }
    public DateTime? NextStart { get; private set; }
    public int InReview { get; private set; }
    public int Observed { get; private set; }
    public int Upcoming { get; private set; }
    public int Done { get; private set; }
    public int GroupCount { get; private set; }

    public DateTime WeekStart { get; private set; }
    public List<TeacherPlanningRow> Week { get; private set; } = [];
    public List<NewsItem> News { get; private set; } = [];

    public sealed record NewsItem(int PlanningId, string Code, string Practice, PlanningReviewAction Action, string? Comment, DateTime Date);

    public async Task OnGetAsync(DateTime? week)
    {
        var today = DateTime.Today;
        var reference = (week ?? today).Date;
        WeekStart = reference.AddDays(-(((int)reference.DayOfWeek + 6) % 7));
        var weekEnd = WeekStart.AddDays(6);

        var now = DateTime.Now;
        Next = await Project(Mine()
                .Where(p => p.Status == PlanningStatus.Approved && p.ScheduledEndDate != null && p.ScheduledEndDate >= now)
                .OrderBy(p => p.ScheduledDate))
            .FirstOrDefaultAsync();
        NextStart = Next == null ? null : Next.Date.Date + (Next.Start ?? TimeSpan.Zero);

        var counts = await Mine()
            .GroupBy(p => p.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        InReview = counts.GetValueOrDefault(PlanningStatus.Requested);
        Observed = counts.GetValueOrDefault(PlanningStatus.Observed);
        Done = counts.GetValueOrDefault(PlanningStatus.Finalized);
        Upcoming = await Mine().CountAsync(p => p.Status == PlanningStatus.Approved && p.ScheduledDate >= today);
        GroupCount = Window.ManagementId is int managementId
            ? await Db.CourseGroups.AsNoTracking().CountAsync(g => g.TeacherPersonId == Teacher.PersonId && g.ManagementId == managementId
                && g.Status == Models.Enums.GeneralStatus.Activo)
            : 0;

        Week = await Project(Mine()
                .Where(p => p.ScheduledDate >= WeekStart && p.ScheduledDate < weekEnd.AddDays(1)
                    && p.Status != PlanningStatus.Rejected && p.Status != PlanningStatus.Cancelled)
                .OrderBy(p => p.ScheduledDate).ThenBy(p => p.StartTime))
            .ToListAsync();

        News = await Db.PlanningReviews.AsNoTracking()
            .Where(r => r.Planning!.TeacherPersonId == Teacher.PersonId && r.UserId != Teacher.UserId)
            .OrderByDescending(r => r.CreatedDate)
            .Take(5)
            .Select(r => new NewsItem(r.PlanningId, r.Planning!.Code ?? "", r.Planning.PracticeName, r.Action, r.Comment, r.CreatedDate))
            .ToListAsync();
    }
}
