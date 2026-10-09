using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Listado de planificaciones (sesiones de práctica, código PRA) de una gestión: la actual por defecto
/// o la indicada por ?managementId=. Filtros por semana, estado y laboratorio. Solo lectura.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ManagementRoles)]
public class PlanificacionesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IPlanningLock _planningLock;

    public PlanificacionesModel(ApplicationDbContext context, IAcademicManagementService managements, IPlanningLock planningLock)
    {
        _context = context;
        _managements = managements;
        _planningLock = planningLock;
    }

    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? WeekNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? LaboratoryId { get; set; }

    public AcademicManagementSummary? Management { get; private set; }
    public IReadOnlyList<SelectListItem> ManagementOptions { get; private set; } = [];
    public IReadOnlyList<SelectListItem> WeekOptions { get; private set; } = [];
    public IReadOnlyList<SelectListItem> StatusOptions { get; private set; } = [];
    public SelectList LaboratoryOptions { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public IReadOnlyList<PlanningRow> Rows { get; private set; } = [];
    public bool IsSuperAdmin => User.IsInRole(AuthorizationHelper.RoleSuperAdmin);

    /// <summary>Solo SuperAdmin puede volver una Aprobada a Planificada para editar sus requerimientos.</summary>
    public async Task<IActionResult> OnPostReopenAsync(int id, int? managementId)
    {
        if (!IsSuperAdmin) return Forbid();
        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (!await _planningLock.TryAcquireAsync([PlanningLockResources.Planning(id)]))
        {
            TempData.Warning(PlanningLockResources.BusyMessage);
            return RedirectToPage(new { managementId });
        }
        var planning = await _context.Plannings.AsTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (planning == null || (managementId.HasValue && managementId != planning.ManagementId))
        {
            TempData.Error("La planificación no existe en esa gestión.");
            return RedirectToPage(new { managementId });
        }
        if (planning.Status != PlanningStatus.Approved
            || !await _managements.IsPlanningWritableAsync(id))
        {
            TempData.Error("Solo se puede reabrir una planificación aprobada en una gestión editable.");
            return RedirectToPage(new { managementId = planning.ManagementId });
        }
        if (await _context.Departures.AsNoTracking().AnyAsync(d => d.PlanningId == id && d.Status != LoanStatus.Cancelled)
            || await _context.L5Incidents.AsNoTracking().AnyAsync(i => i.PlanningId == id))
        {
            TempData.Error("No se puede reabrir: la planificación tiene una salida L-3 o un incidente L-5.");
            return RedirectToPage(new { managementId = planning.ManagementId });
        }

        planning.Status = PlanningStatus.Planned;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData.Success($"{L3Codes.Format(planning.Id)} se reabrió para editar.");
        return RedirectToPage("./Requerimientos", new { planId = planning.Id });
    }

    public async Task OnGetAsync()
    {
        var managements = await _managements.ListAsync();
        Management = await _managements.ResolveAsync(ManagementId);
        ManagementId = Management?.Id;
        ManagementOptions = managements
            .Select(m => new SelectListItem($"{m.Code} · {m.StatusText}{(m.IsCurrent ? " (actual)" : "")}", m.Id.ToString(), m.Id == ManagementId))
            .ToList();

        // Opciones de filtro (independientes de los resultados para no obligar a seleccionar).
        StatusOptions = PlanningStatus.All
            .Select(s => new SelectListItem(PlanningStatus.Label(s), s))
            .ToList();

        if (Management == null) return;

        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Where(w => w.ManagementId == Management.Id)
            .OrderBy(w => w.Number)
            .Select(w => new { w.Number, w.StartDate, w.EndDate })
            .ToListAsync();
        WeekOptions = weeks
            .Select(w => new SelectListItem($"Semana {w.Number} ({w.StartDate:dd/MM} – {w.EndDate:dd/MM})", w.Number.ToString()))
            .ToList();

        // Laboratorios presentes en planificaciones de la gestión (más estable que todo el catálogo).
        var labIds = await _context.Plannings.AsNoTracking()
            .Where(r => r.ManagementId == Management.Id && r.LaboratoryId != null)
            .Select(r => r.LaboratoryId!.Value)
            .Distinct()
            .ToListAsync();
        LaboratoryOptions = new SelectList(
            await _context.Laboratories.AsNoTracking()
                .Where(l => labIds.Contains(l.Id))
                .OrderBy(l => l.Code)
                .Select(l => new { l.Id, Name = l.Code + " · " + l.Name })
                .ToListAsync(),
            "Id", "Name", LaboratoryId);

        var query = _context.Plannings.AsNoTracking().Where(r => r.ManagementId == Management.Id);
        if (WeekNumber.HasValue)
            query = query.Where(r => r.AcademicWeek == WeekNumber.Value);
        if (!string.IsNullOrWhiteSpace(Status))
            query = query.Where(r => r.Status == Status);
        if (LaboratoryId.HasValue)
            query = query.Where(r => r.LaboratoryId == LaboratoryId.Value);

        var projections = await query
            .OrderBy(r => r.ScheduledDate)
            .Select(r => new
            {
                r.Id,
                r.PracticeName,
                r.AcademicWeek,
                r.ScheduledDate,
                r.ScheduledEndDate,
                r.Status,
                r.TeacherPersonId,
                StartOrder = r.StartPeriod != null ? (int?)r.StartPeriod.Order : null,
                r.PeriodCount,
                SubjectName = r.Subject != null ? r.Subject.Name : "Sin materia",
                GroupLabel = !string.IsNullOrEmpty(r.GroupName) ? r.GroupName : (r.Group != null ? r.Group.Name : "—"),
                LaboratoryName = r.AllLaboratories ? "Todos los laboratorios"
                    : (r.Laboratory != null ? r.Laboratory.Code + " · " + r.Laboratory.Name : "—"),
                HasRequirements = r.Requirements.Any(),
                HasIncident = r.Incidents.Any()
            })
            .ToListAsync();

        // Docentes: una sola consulta complementaria para evitar N+1.
        var teacherIds = projections.Select(p => p.TeacherPersonId).Distinct().ToList();
        var teachers = teacherIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.Interns.AsNoTracking()
                .Where(i => teacherIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Name })
                .ToDictionaryAsync(i => i.Id, i => i.Name);

        Rows = projections.Select(p => new PlanningRow(
            p.Id,
            L3Codes.Format(p.Id),
            p.PracticeName,
            p.SubjectName,
            p.GroupLabel,
            p.LaboratoryName,
            teachers.TryGetValue(p.TeacherPersonId, out var t) ? t : "Sin docente",
            p.AcademicWeek,
            AcademicWeekGenerator.WeekdayOf(p.ScheduledDate) is var wd and >= 1 and <= 6 ? AcademicWeekGenerator.WeekdayNames[wd] : "—",
            p.ScheduledDate,
            PlanningStatusView.TimeText(p.StartOrder, p.PeriodCount, p.ScheduledDate, p.ScheduledEndDate),
            p.Status,
            PlanningStatus.Label(p.Status),
            PlanningStatusView.Css(p.Status),
            PlanningStatusView.NextUrl(p.Id, p.Status, p.HasRequirements),
            p.HasIncident)).ToList();
    }

    public sealed record PlanningRow(
        int Id,
        string Code,
        string PracticeName,
        string SubjectName,
        string GroupName,
        string LaboratoryName,
        string TeacherName,
        int AcademicWeek,
        string WeekdayName,
        DateTime ScheduledDate,
        string TimeText,
        string Status,
        string StatusLabel,
        string StatusCss,
        string NextUrl,
        bool HasIncident);
}
