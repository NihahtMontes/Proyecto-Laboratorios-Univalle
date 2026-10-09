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
/// Panel de planificación de una gestión: cabecera, métricas y calendario resumen (mes → semanas → días lunes a sábado).
/// Por defecto muestra la gestión actual; con ?managementId= se consulta otra (de solo lectura si está completada).
/// </summary>
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IArticleAvailabilityService _availability;

    public IndexModel(ApplicationDbContext context, IAcademicManagementService managements, IArticleAvailabilityService availability)
    {
        _context = context;
        _managements = managements;
        _availability = availability;
    }

    /// <summary>Periodo del ranking de uso: "week" (semana actual) o "month" (mes actual).</summary>
    [BindProperty(SupportsGet = true)]
    public string? UsagePeriod { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Month { get; set; }

    /// <summary>Gestión mostrada (la actual o la pedida por ?managementId=).</summary>
    public AcademicManagementSummary? Management { get; private set; }
    public List<AcademicManagementSummary> Managements { get; private set; } = [];
    /// <summary>True si la gestión mostrada está completada (solo consulta).</summary>
    public bool IsReadOnly => Management is { IsEditable: false };
    public PlanningCalendar? Calendar { get; private set; }

    public int WeekCount { get; private set; }
    public int EnabledDayCount { get; private set; }
    public int SuspendedDayCount { get; private set; }
    public int PlanningCount { get; private set; }
    public int PlannedCount { get; private set; }
    public int ApprovedCount { get; private set; }
    public int FinalizedCount { get; private set; }
    /// <summary>Orígenes reprogramados o repuestos y pendientes de reposición (no ocupan ambiente).</summary>
    public int MovedCount { get; private set; }
    public int IncidentCount { get; private set; }
    public int RecoveriesInProgress { get; private set; }

    // Notificaciones al entrar al panel.
    public DateTime Today { get; private set; } = DateTime.Today;
    /// <summary>Planificaciones de hoy (todas las gestiones: el material es compartido).</summary>
    public List<WeekPractice> TodayPractices { get; private set; } = [];
    /// <summary>Semana calendario actual (lunes a domingo) con conflictos y rotaciones.</summary>
    public WeekAvailability? CurrentWeek { get; private set; }
    public List<PendingReturn> PendingReturns { get; private set; } = [];

    public sealed record PendingReturn(int DepartureId, string? Code, int? PlanningId, string? PracticeName,
        DateTime DepartureDate, DateTime? EstimatedReturnDate, bool IsOverdue);

    // Pestaña "Uso de artículos".
    public bool UsageIsMonth => !string.Equals(UsagePeriod, "week", StringComparison.OrdinalIgnoreCase);
    public DateTime UsageFrom { get; private set; }
    public DateTime UsageTo { get; private set; }
    public List<ArticleUsage> MostUsed { get; private set; } = [];
    public List<ArticleUsage> MostIssues { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Managements = await _managements.ListAsync();
        Management = await _managements.ResolveAsync(ManagementId);
        if (ManagementId.HasValue && Management == null)
        {
            TempData.Error("La gestión solicitada no existe.");
            return RedirectToPage();
        }

        await LoadNotificationsAsync();

        if (Management == null) return Page();

        Calendar = await PlanningCalendar.BuildAsync(_context, Management, Month, allowAdd: Management.IsEditable);

        WeekCount = await _context.AcademicWeeks.AsNoTracking().CountAsync(w => w.ManagementId == Management.Id);
        var days = await _context.AcademicDays.AsNoTracking()
            .Where(d => d.AcademicWeek.ManagementId == Management.Id)
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
        EnabledDayCount = days.GetValueOrDefault(AcademicDayStatus.Enabled);
        SuspendedDayCount = days.GetValueOrDefault(AcademicDayStatus.Suspended);

        var byStatus = await _context.Plannings.AsNoTracking()
            .Where(r => r.ManagementId == Management.Id && r.Status != PlanningStatus.Cancelled)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
        PlannedCount = byStatus.GetValueOrDefault(PlanningStatus.Planned);
        ApprovedCount = byStatus.GetValueOrDefault(PlanningStatus.Approved);
        FinalizedCount = byStatus.GetValueOrDefault(PlanningStatus.Finalized);
        MovedCount = byStatus.GetValueOrDefault(PlanningStatus.Rescheduled)
            + byStatus.GetValueOrDefault(PlanningStatus.Recovered)
            + byStatus.GetValueOrDefault(PlanningStatus.PendingRecovery);
        PlanningCount = PlannedCount + ApprovedCount + FinalizedCount;
        IncidentCount = await _context.L5Incidents.AsNoTracking()
            .CountAsync(i => i.Planning != null && i.Planning.ManagementId == Management.Id);
        RecoveriesInProgress = await _context.DayRecoveries.AsNoTracking()
            .CountAsync(r => r.ManagementId == Management.Id && r.Status == DayRecoveryStatus.InProgress);

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
            .Where(d => d.PlanningId != null && openStatuses.Contains(d.Status))
            .OrderBy(d => d.EstimatedReturnDate ?? d.DepartureDate)
            .Take(20)
            .Select(d => new
            {
                d.Id, d.Code, d.PlanningId, d.DepartureDate, d.EstimatedReturnDate, d.Status,
                Practice = _context.Plannings.Where(r => r.Id == d.PlanningId).Select(r => r.PracticeName).FirstOrDefault()
            })
            .ToListAsync())
            .Select(d => new PendingReturn(d.Id, d.Code, d.PlanningId, d.Practice, d.DepartureDate, d.EstimatedReturnDate,
                d.Status == LoanStatus.Overdue || (d.EstimatedReturnDate.HasValue && d.EstimatedReturnDate.Value.Date < Today)))
            .ToList();
    }
}
