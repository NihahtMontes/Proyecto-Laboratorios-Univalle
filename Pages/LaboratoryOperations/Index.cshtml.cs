using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Panel "Planificación activa": cabecera de contexto + calendario (mes → semanas → días).
/// Sin planificación activa muestra el estado vacío, igual que la gestión.
/// Con ?planningId= se consulta otra planificación en solo lectura.
/// </summary>
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planning;
    private readonly IArticleAvailabilityService _availability;

    public IndexModel(ApplicationDbContext context, IPlanningService planning, IArticleAvailabilityService availability)
    {
        _context = context;
        _planning = planning;
        _availability = availability;
    }

    /// <summary>Periodo del ranking de uso: "week" (semana actual) o "month" (mes actual).</summary>
    [BindProperty(SupportsGet = true)]
    public string? UsagePeriod { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PlanningId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Month { get; set; }

    /// <summary>Planificación mostrada (la activa o la pedida por ?planningId=).</summary>
    public PlanningSummary? Planning { get; private set; }
    public bool HasActivePlanning { get; private set; }
    /// <summary>True si se consulta una planificación que no es la activa.</summary>
    public bool IsReadOnly => Planning is { IsActive: false };
    public PlanningCalendar? Calendar { get; private set; }

    public int WeekCount { get; private set; }
    public int LectiveWeekCount { get; private set; }
    public int PracticeCount { get; private set; }
    public int PlannedCount { get; private set; }
    public int ApprovedCount { get; private set; }
    public int FinalizedCount { get; private set; }
    public int IncidentCount { get; private set; }

    // Notificaciones al entrar al panel.
    public DateTime Today { get; private set; } = DateTime.Today;
    /// <summary>Prácticas de hoy (todas las planificaciones: el material es compartido).</summary>
    public List<WeekPractice> TodayPractices { get; private set; } = [];
    /// <summary>Semana calendario actual (lunes a domingo) con conflictos y rotaciones.</summary>
    public WeekAvailability? CurrentWeek { get; private set; }
    public List<PendingReturn> PendingReturns { get; private set; } = [];

    public sealed record PendingReturn(int DepartureId, string? Code, int? PracticeId, string? PracticeName,
        DateTime DepartureDate, DateTime? EstimatedReturnDate, bool IsOverdue);

    // Pestaña "Uso de artículos".
    public bool UsageIsMonth => !string.Equals(UsagePeriod, "week", StringComparison.OrdinalIgnoreCase);
    public DateTime UsageFrom { get; private set; }
    public DateTime UsageTo { get; private set; }
    public List<ArticleUsage> MostUsed { get; private set; } = [];
    public List<ArticleUsage> MostIssues { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var active = await _planning.GetActiveAsync();
        HasActivePlanning = active != null;

        if (PlanningId.HasValue && PlanningId != active?.Id)
        {
            Planning = await _planning.GetSummaryAsync(PlanningId.Value);
            if (Planning == null)
            {
                TempData.Error("La planificación solicitada no existe.");
                return RedirectToPage();
            }
        }
        else
        {
            Planning = active;
        }

        await LoadNotificationsAsync();

        if (Planning == null) return Page();

        Calendar = await PlanningCalendar.BuildAsync(_context, Planning, Month, allowAdd: Planning.IsActive);

        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Where(w => w.AcademicPlanningId == Planning.Id)
            .GroupBy(w => 1)
            .Select(g => new { Total = g.Count(), Lective = g.Count(w => w.IsLective) })
            .FirstOrDefaultAsync();
        WeekCount = weeks?.Total ?? 0;
        LectiveWeekCount = weeks?.Lective ?? 0;

        var byStatus = await _context.L3Requests.AsNoTracking()
            .Where(r => r.AcademicPlanningId == Planning.Id && r.Status != L3RequestStatus.Cancelled)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
        PlannedCount = byStatus.GetValueOrDefault(L3RequestStatus.Planned);
        ApprovedCount = byStatus.GetValueOrDefault(L3RequestStatus.Approved);
        FinalizedCount = byStatus.GetValueOrDefault(L3RequestStatus.Finalized);
        PracticeCount = PlannedCount + ApprovedCount + FinalizedCount;
        IncidentCount = await _context.L5Incidents.AsNoTracking()
            .CountAsync(i => i.L3Request != null && i.L3Request.AcademicPlanningId == Planning.Id);

        var usage = await _availability.GetUsageAsync(UsageFrom, UsageTo);
        MostUsed = usage.OrderByDescending(u => u.PracticeCount).ThenByDescending(u => u.TotalRequested).Take(10).ToList();
        MostIssues = usage.Where(u => u.IssueDays > 0)
            .OrderByDescending(u => u.ConflictDays).ThenByDescending(u => u.RotationDays).Take(10).ToList();

        return Page();
    }

    private async Task LoadNotificationsAsync()
    {
        var monday = Today.AddDays(-(((int)Today.DayOfWeek + 6) % 7));
        CurrentWeek = await _availability.GetRangeAsync(monday, monday.AddDays(6));
        TodayPractices = CurrentWeek.Practices.Where(p => p.Date == Today).ToList();

        UsageFrom = UsageIsMonth ? new DateTime(Today.Year, Today.Month, 1) : monday;
        UsageTo = UsageIsMonth ? UsageFrom.AddMonths(1).AddDays(-1) : monday.AddDays(6);

        var openStatuses = new[] { LoanStatus.Active, LoanStatus.Overdue };
        PendingReturns = (await _context.Departures.AsNoTracking()
            .Where(d => d.L3RequestId != null && openStatuses.Contains(d.Status))
            .OrderBy(d => d.EstimatedReturnDate ?? d.DepartureDate)
            .Take(20)
            .Select(d => new
            {
                d.Id, d.Code, d.L3RequestId, d.DepartureDate, d.EstimatedReturnDate, d.Status,
                Practice = _context.L3Requests.Where(r => r.Id == d.L3RequestId).Select(r => r.PracticeName).FirstOrDefault()
            })
            .ToListAsync())
            .Select(d => new PendingReturn(d.Id, d.Code, d.L3RequestId, d.Practice, d.DepartureDate, d.EstimatedReturnDate,
                d.Status == LoanStatus.Overdue || (d.EstimatedReturnDate.HasValue && d.EstimatedReturnDate.Value.Date < Today)))
            .ToList();
    }
}
