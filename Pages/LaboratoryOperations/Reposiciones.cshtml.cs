using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>Bandeja y avance de reposiciones de días suspendidos; Supervisor/Admin/SuperAdmin pueden trasladar bloques.</summary>
[Authorize(Roles = AuthorizationHelper.ManagementRoles)]
public class ReposicionesModel : PageModel
{
    private static readonly string[] ManagerRoles = AuthorizationHelper.ManagementRoles
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;

    public ReposicionesModel(ApplicationDbContext context, IAcademicManagementService managements)
    {
        _context = context;
        _managements = managements;
    }

    [BindProperty(SupportsGet = true)] public int? ManagementId { get; set; }
    [BindProperty(SupportsGet = true)] public int? RecoveryId { get; set; }
    public AcademicManagementSummary? Management { get; private set; }
    public IReadOnlyList<AcademicManagementSummary> Managements { get; private set; } = [];
    public bool CanRecover => ManagerRoles.Any(User.IsInRole) && Management?.IsEditable == true;
    public IReadOnlyList<RecoveryRow> Recoveries { get; private set; } = [];
    public RecoveryRow? Selected { get; private set; }
    public IReadOnlyList<PendingGroup> Groups { get; private set; } = [];
    /// <summary>Bloques ya repuestos de la reposición seleccionada (desde Reschedulings).</summary>
    public IReadOnlyList<RescheduledRow> RescheduledRows { get; private set; } = [];

    public sealed record RecoveryRow(int Id, DateTime Date, string Reason, DayRecoveryStatus Status, int Pending, int Recovered)
    {
        public int Total => Pending + Recovered;
    }
    public sealed record PendingPlanning(int Id, string Code, string Name, string Subject, string Group,
        string Teacher, int PeriodCount, string Periods, string PreviousStatus);
    public sealed record PendingGroup(string Laboratory, IReadOnlyList<PendingPlanning> Plannings);
    public sealed record RescheduledRow(int PlanningId, string Code, string Subject, string Group,
        DateTime Date, string Periods, string Laboratory);

    public async Task<IActionResult> OnGetAsync()
    {
        Managements = await _managements.ListAsync();
        Management = await _managements.ResolveAsync(ManagementId);
        ManagementId = Management?.Id;
        if (Management == null) return Page();

        var recoveries = await _context.DayRecoveries.AsNoTracking()
            .Where(r => r.ManagementId == Management.Id)
            .OrderByDescending(r => r.Date)
            .Select(r => new { r.Id, r.Date, r.Reason, r.Status })
            .ToListAsync();
        var pendingDates = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == Management.Id && p.Status == PlanningStatus.PendingRecovery)
            .Select(p => p.ScheduledDate).ToListAsync();
        var pending = pendingDates.GroupBy(d => d.Date).ToDictionary(g => g.Key, g => g.Count());
        var moved = await _context.Reschedulings.AsNoTracking()
            .Where(r => r.DayRecoveryId != null && r.DayRecovery!.ManagementId == Management.Id)
            .GroupBy(r => r.DayRecoveryId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Id, g => g.Count);

        Recoveries = recoveries.Select(r => new RecoveryRow(r.Id, r.Date, r.Reason, r.Status,
            pending.GetValueOrDefault(r.Date.Date), moved.GetValueOrDefault(r.Id))).ToList();
        Selected = Recoveries.FirstOrDefault(r => r.Id == RecoveryId)
            ?? Recoveries.FirstOrDefault(r => r.Status == DayRecoveryStatus.InProgress)
            ?? Recoveries.FirstOrDefault();
        if (Selected == null) return Page();

        var from = Selected.Date.Date;
        var to = from.AddDays(1);

        // Lo ya repuesto de esta reposición: cada Rescheduling apunta al bloque destino aplicado.
        var rescheduled = await _context.Reschedulings.AsNoTracking()
            .Where(x => x.DayRecoveryId == Selected.Id)
            .OrderBy(x => x.DestinationLaboratory!.Code).ThenBy(x => x.DestinationDate).ThenBy(x => x.DestinationStartPeriod!.Order)
            .Select(x => new
            {
                x.OriginPlanningId,
                OriginCode = x.OriginPlanning != null ? x.OriginPlanning.Code : null,
                x.DestinationDate,
                StartOrder = x.DestinationStartPeriod != null ? (int?)x.DestinationStartPeriod.Order : null,
                x.DestinationPeriodCount,
                Laboratory = x.DestinationLaboratory != null
                    ? x.DestinationLaboratory.Code + " · " + x.DestinationLaboratory.Name : "Sin laboratorio",
                Subject = x.DestinationPlanning.Subject != null
                    ? x.DestinationPlanning.Subject.Name : x.DestinationPlanning.PracticeName,
                Group = x.DestinationPlanning.GroupName
            })
            .ToListAsync();
        RescheduledRows = rescheduled.Select(x => new RescheduledRow(x.OriginPlanningId, L3Codes.Format(x.OriginPlanningId, x.OriginCode),
            x.Subject ?? "Sin materia", x.Group ?? "—", x.DestinationDate.Date,
            x.StartOrder is int order && x.DestinationPeriodCount > 0
                ? PlanningSlot.PeriodsText(order, x.DestinationPeriodCount) : "Sin periodos",
            x.Laboratory)).ToList();

        if (Selected.Pending == 0) return Page();

        var rows = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == Management.Id && p.Status == PlanningStatus.PendingRecovery
                && p.ScheduledDate >= from && p.ScheduledDate < to)
            .OrderBy(p => p.Laboratory!.Code).ThenBy(p => p.ScheduledDate)
            .Select(p => new
            {
                p.Id, p.Code, p.PracticeName, p.GroupName, p.TeacherPersonId, p.PeriodCount, p.StatusBeforeRecovery,
                StartOrder = p.StartPeriod != null ? (int?)p.StartPeriod.Order : null,
                Subject = p.Subject != null ? p.Subject.Name : "Sin materia",
                Laboratory = p.Laboratory != null ? p.Laboratory.Code + " · " + p.Laboratory.Name : "Sin laboratorio"
            }).ToListAsync();
        var teacherIds = rows.Select(p => p.TeacherPersonId).Distinct().ToList();
        var teachers = await _context.Interns.AsNoTracking().Where(t => teacherIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name);
        Groups = rows.GroupBy(p => p.Laboratory).Select(g => new PendingGroup(g.Key, g.Select(p =>
            new PendingPlanning(p.Id, L3Codes.Format(p.Id, p.Code), p.PracticeName, p.Subject,
                p.GroupName ?? "—", teachers.GetValueOrDefault(p.TeacherPersonId, "—"), p.PeriodCount ?? 0,
                p.StartOrder is int order && p.PeriodCount is int count
                    ? PlanningSlot.PeriodsText(order, count) : "Sin periodos",
                PlanningStatus.Label(p.StatusBeforeRecovery ?? PlanningStatus.PendingRecovery))).ToList())).ToList();
        return Page();
    }
}
