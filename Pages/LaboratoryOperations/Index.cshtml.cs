using System.Globalization;
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
/// Panel general de planificación de una gestión: cabecera, métricas y avisos accionables que
/// redirigen a la Grilla por laboratorio, donde está el detalle. Por defecto muestra la gestión
/// actual; con ?managementId= se consulta otra (de solo lectura si está completada).
/// Supervisor/Administrator/SuperAdmin pueden "Reponer día" desde aquí (modal con vista previa).
/// </summary>
public class IndexModel : PageModel
{
    private static readonly string[] ManagerRoles = AuthorizationHelper.ManagementRoles
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IArticleAvailabilityService _availability;
    private readonly IDayRecoveryService _recoveries;

    public IndexModel(ApplicationDbContext context, IAcademicManagementService managements,
        IArticleAvailabilityService availability, IDayRecoveryService recoveries)
    {
        _context = context;
        _managements = managements;
        _availability = availability;
        _recoveries = recoveries;
    }

    /// <summary>Periodo del ranking de uso: "week" (semana actual) o "month" (mes actual).</summary>
    [BindProperty(SupportsGet = true)]
    public string? UsagePeriod { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    /// <summary>Gestión mostrada (la actual o la pedida por ?managementId=).</summary>
    public AcademicManagementSummary? Management { get; private set; }
    public List<AcademicManagementSummary> Managements { get; private set; } = [];
    /// <summary>True si la gestión mostrada está completada (solo consulta).</summary>
    public bool IsReadOnly => Management is { IsEditable: false };
    /// <summary>True para Supervisor/Administrator/SuperAdmin con la gestión editable: pueden reponer días.</summary>
    public bool CanRecover => UserCanRecover && Management is { IsEditable: true };
    /// <summary>Solo el rol (sin exigir gestión editable): para handlers sin gestión resuelta.</summary>
    private bool UserCanRecover => ManagerRoles.Any(User.IsInRole);

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

    // Pestaña "Avisos y pendientes": avisos accionables que redirigen a la Grilla / Reposiciones.
    /// <summary>Reposiciones de días suspendidos en curso, con avance y prácticas pendientes.</summary>
    public IReadOnlyList<RecoveryNotice> Recoveries { get; private set; } = [];
    /// <summary>Reposiciones completadas (o cerradas) de los últimos 30 días.</summary>
    public IReadOnlyList<CompletedRecovery> CompletedRecoveries { get; private set; } = [];
    /// <summary>Planificaciones pendientes de reubicar (estado PendienteReposicion).</summary>
    public IReadOnlyList<PendingRescheduleNotice> PendingReschedules { get; private set; } = [];
    /// <summary>Planificaciones vigentes (no finalizadas) sin periodo asignado (StartPeriodId null).</summary>
    public IReadOnlyList<WithoutPeriodNotice> WithoutPeriod { get; private set; } = [];
    public int WithoutPeriodCount { get; private set; }
    /// <summary>Sesiones de los próximos 7 días agrupadas por ambiente.</summary>
    public IReadOnlyList<LabUpcomingNotice> UpcomingByLaboratory { get; private set; } = [];
    /// <summary>Total de avisos accionables (badge de la pestaña).</summary>
    public int AlertCount => Recoveries.Count + PendingReschedules.Count + WithoutPeriodCount;

    public sealed record RecoveryNotice(int Id, DateTime Date, string Reason, int Recovered, int Pending,
        IReadOnlyList<PendingPlanningItem> PendingItems, IReadOnlyList<RecoveryLabChip> Labs)
    {
        public int Total => Recovered + Pending;
    }

    public sealed record PendingPlanningItem(int Id, string Code, string Laboratory, string Subject, string Group, string Practice);

    /// <summary>Ambientes con planificaciones pendientes dentro de una reposición en curso.</summary>
    public sealed record RecoveryLabChip(string Label, int Count);

    /// <summary>Reposición ya completada (o cancelada) de los últimos 30 días.</summary>
    public sealed record CompletedRecovery(int Id, DateTime Date, string Reason, int Recovered, int Pending)
    {
        public int Total => Recovered + Pending;
    }

    public sealed record PendingRescheduleNotice(int Id, string Code, string Laboratory, string Subject, string Group,
        string Practice, DateTime Date, string TimeText, int? RecoveryId);

    public sealed record WithoutPeriodNotice(int Id, string Code, int? LaboratoryId, string Laboratory, string Subject,
        string Group, DateTime Date, string TimeText);

    public sealed record LabUpcomingNotice(int LaboratoryId, string Laboratory, int SessionCount, DateTime FirstSessionDate,
        string FirstSessionText, string FirstPractice);

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

        await LoadActionablesAsync(Management);

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

    /// <summary>
    /// Avisos accionables de la gestión: reposiciones de días suspendidos en curso, planificaciones
    /// pendientes de reubicar, planificaciones sin periodo asignado y próximos 7 días por ambiente.
    /// Todas las consultas son AsNoTracking, filtradas por gestión y con proyecciones ligeras.
    /// </summary>
    private async Task LoadActionablesAsync(AcademicManagementSummary management)
    {
        var today = DateTime.Today;

        // Reposiciones de días suspendidos: en curso (tarjeta principal) y cerradas de los últimos 30 días (colapsable).
        var recoveryCutoff = today.AddDays(-30);
        var allRecoveries = await _context.DayRecoveries.AsNoTracking()
            .Where(r => r.ManagementId == management.Id
                && (r.Status == DayRecoveryStatus.InProgress || r.Date >= recoveryCutoff))
            .OrderBy(r => r.Date)
            .Select(r => new { r.Id, r.Date, r.Reason, r.Status })
            .ToListAsync();
        var recoveries = allRecoveries.Where(r => r.Status == DayRecoveryStatus.InProgress).ToList();
        var completedRecoveries = allRecoveries.Where(r => r.Status != DayRecoveryStatus.InProgress).ToList();

        // Planificaciones esperando reposición (PendienteReposicion): alimentan (a) y (b) con una sola consulta.
        var pendingRows = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == management.Id && p.Status == PlanningStatus.PendingRecovery)
            .OrderBy(p => p.ScheduledDate).ThenBy(p => p.Laboratory!.Code)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.ScheduledDate,
                p.StartTime,
                p.EndTime,
                p.GroupName,
                p.PracticeName,
                Laboratory = p.Laboratory != null ? p.Laboratory.Code + " · " + p.Laboratory.Name : "Sin laboratorio",
                Subject = p.Subject != null ? p.Subject.Name : "Sin materia"
            })
            .ToListAsync();

        if (allRecoveries.Count > 0)
        {
            var recoveryIds = allRecoveries.Select(r => r.Id).ToList();
            var recoveredByRecovery = await _context.Reschedulings.AsNoTracking()
                .Where(x => x.DayRecoveryId != null && recoveryIds.Contains(x.DayRecoveryId.Value))
                .GroupBy(x => x.DayRecoveryId.Value)
                .Select(g => new { RecoveryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RecoveryId, x => x.Count);

            Recoveries = recoveries.Select(r => new RecoveryNotice(
                r.Id, r.Date.Date, r.Reason,
                recoveredByRecovery.GetValueOrDefault(r.Id),
                pendingRows.Count(p => p.ScheduledDate.Date == r.Date.Date),
                pendingRows.Where(p => p.ScheduledDate.Date == r.Date.Date)
                    .Select(p => new PendingPlanningItem(p.Id, L3Codes.Format(p.Id, p.Code), p.Laboratory, p.Subject,
                        p.GroupName ?? "—", p.PracticeName))
                    .ToList(),
                pendingRows.Where(p => p.ScheduledDate.Date == r.Date.Date)
                    .GroupBy(p => p.Laboratory)
                    .Select(g => new RecoveryLabChip(g.Key, g.Count()))
                    .OrderBy(c => c.Label)
                    .ToList()))
                .ToList();

            CompletedRecoveries = completedRecoveries
                .Select(r => new CompletedRecovery(r.Id, r.Date.Date, r.Reason,
                    recoveredByRecovery.GetValueOrDefault(r.Id),
                    pendingRows.Count(p => p.ScheduledDate.Date == r.Date.Date)))
                .OrderByDescending(r => r.Date)
                .ToList();
        }

        // Las pendientes de reposición se reubican desde la reposición de su día (la Grilla rechaza ?reprogramar).
        var recoveryByDate = recoveries.GroupBy(r => r.Date.Date).ToDictionary(g => g.Key, g => g.First().Id);
        PendingReschedules = pendingRows
            .Select(p => new PendingRescheduleNotice(p.Id, L3Codes.Format(p.Id, p.Code), p.Laboratory, p.Subject,
                p.GroupName ?? "—", p.PracticeName, p.ScheduledDate.Date, FormatTime(p.StartTime, p.EndTime),
                recoveryByDate.TryGetValue(p.ScheduledDate.Date, out var recoveryId) ? recoveryId : null))
            .ToList();

        // Planificaciones vigentes (no finalizadas) sin periodo asignado: no reservan horario en la grilla.
        var withoutPeriodRows = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == management.Id
                && p.StartPeriodId == null
                && p.Status != PlanningStatus.Finalized
                && !PlanningStatus.NonOccupying.Contains(p.Status))
            .OrderBy(p => p.ScheduledDate)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.ScheduledDate,
                p.StartTime,
                p.EndTime,
                p.LaboratoryId,
                p.GroupName,
                p.PracticeName,
                Laboratory = p.Laboratory != null ? p.Laboratory.Code + " · " + p.Laboratory.Name : "Sin laboratorio",
                Subject = p.Subject != null ? p.Subject.Name : "Sin materia"
            })
            .ToListAsync();
        WithoutPeriodCount = withoutPeriodRows.Count;
        WithoutPeriod = withoutPeriodRows.Take(6)
            .Select(p => new WithoutPeriodNotice(p.Id, L3Codes.Format(p.Id, p.Code), p.LaboratoryId, p.Laboratory, p.Subject,
                p.GroupName ?? "—", p.ScheduledDate.Date, FormatTime(p.StartTime, p.EndTime)))
            .ToList();

        // Próximos 7 días agrupados por ambiente (solo sesiones que ocupan ambiente).
        var upcomingRows = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == management.Id
                && p.ScheduledDate >= today
                && p.ScheduledDate < today.AddDays(7)
                && p.LaboratoryId != null
                && !PlanningStatus.NonOccupying.Contains(p.Status))
            .OrderBy(p => p.ScheduledDate).ThenBy(p => p.StartTime)
            .Select(p => new
            {
                p.LaboratoryId,
                Laboratory = p.Laboratory != null ? p.Laboratory.Code + " · " + p.Laboratory.Name : "—",
                p.ScheduledDate,
                p.StartTime,
                p.EndTime,
                p.PracticeName
            })
            .ToListAsync();
        UpcomingByLaboratory = upcomingRows
            .GroupBy(p => p.LaboratoryId!.Value)
            .Select(g =>
            {
                var first = g.First();
                return new LabUpcomingNotice(g.Key, first.Laboratory, g.Count(), first.ScheduledDate.Date,
                    $"{first.ScheduledDate:dd/MM} · {FormatTime(first.StartTime, first.EndTime)}", first.PracticeName);
            })
            .OrderBy(x => x.FirstSessionDate)
            .ToList();
    }

    /// <summary>Vista previa (GET, solo lectura) del modal "Reponer día": fecha yyyy-MM-dd.</summary>
    public async Task<IActionResult> OnGetRecoveryPreviewAsync(int managementId, string date)
    {
        if (!UserCanRecover)
            return new JsonResult(new DayRecoveryPreview(false, "No tiene permiso para reponer días.", default, null, 0, [], []));
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return new JsonResult(new DayRecoveryPreview(false, "Elija una fecha válida.", default, null, 0, [], []));
        return new JsonResult(await _recoveries.PreviewAsync(managementId, parsed));
    }

    /// <summary>Suspende el día elegido desde el panel, crea la reposición y abre la grilla con los afectados.</summary>
    public async Task<IActionResult> OnPostRecoverDayAsync(int managementId, string date, string? reason)
    {
        if (!UserCanRecover)
        {
            TempData.Error("No tiene permiso para reponer días suspendidos.");
            return RedirectToPage(new { managementId });
        }
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            TempData.Error("Elija una fecha válida para reponer.");
            return RedirectToPage(new { managementId });
        }
        var result = await _recoveries.SuspendByDateAsync(managementId, parsed, reason);
        if (result.Ok)
        {
            TempData.Success(result.Message);
            return RedirectToPage("./Grilla", new { managementId, recoveryId = result.RecoveryId });
        }
        TempData.Error(result.Message);
        return RedirectToPage(new { managementId });
    }

    private static string FormatTime(TimeSpan? start, TimeSpan? end) => start.HasValue
        ? $"{start:hh\\:mm}–{(end ?? start):hh\\:mm}"
        : "Sin horario";
}
