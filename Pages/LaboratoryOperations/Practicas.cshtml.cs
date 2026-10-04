using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Listado de prácticas (solo lectura) de la planificación activa por defecto,
/// o de la planificación indicada por ?planningId=. Permite filtrar por semana,
/// estado y laboratorio.
/// </summary>
public class PracticasModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planning;

    public PracticasModel(ApplicationDbContext context, IPlanningService planning)
    {
        _context = context;
        _planning = planning;
    }

    [BindProperty(SupportsGet = true)]
    public int? PlanningId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? WeekNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? LaboratoryId { get; set; }

    /// <summary>Planificación mostrada (la activa o la pedida por ?planningId=).</summary>
    public PlanningSummary? Planning { get; private set; }

    public IReadOnlyList<SelectListItem> WeekOptions { get; private set; } = [];
    public IReadOnlyList<SelectListItem> StatusOptions { get; private set; } = [];
    public SelectList LaboratoryOptions { get; private set; } = new(Enumerable.Empty<SelectListItem>());

    public IReadOnlyList<PracticeRow> Rows { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var active = await _planning.GetActiveAsync();
        Planning = PlanningId.HasValue && PlanningId != active?.Id
            ? await _planning.GetSummaryAsync(PlanningId.Value)
            : active;
        PlanningId = Planning?.Id;

        // Opciones de filtro (independientes de los resultados para no obligar a seleccionar).
        StatusOptions = new[]
        {
            new SelectListItem("Planificado", L3RequestStatus.Planned),
            new SelectListItem("Aprobado", L3RequestStatus.Approved),
            new SelectListItem("Finalizado", L3RequestStatus.Finalized),
            new SelectListItem("Cancelado", L3RequestStatus.Cancelled)
        };

        if (Planning == null)
        {
            WeekOptions = [];
            LaboratoryOptions = new SelectList(Enumerable.Empty<SelectListItem>());
            Rows = [];
            return;
        }

        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Where(w => w.AcademicPlanningId == Planning.Id)
            .OrderBy(w => w.Number)
            .Select(w => new { w.Number, w.StartDate, w.EndDate })
            .ToListAsync();
        WeekOptions = weeks
            .Select(w => new SelectListItem(
                $"Semana {w.Number} ({w.StartDate:dd/MM} – {w.EndDate:dd/MM})",
                w.Number.ToString()))
            .ToList();

        // Laboratorios presentes en prácticas de la planificación (más estable que todo el catálogo).
        var labIds = await _context.L3Requests.AsNoTracking()
            .Where(r => r.AcademicPlanningId == Planning.Id)
            .Select(r => r.LaboratoryId)
            .Distinct()
            .ToListAsync();
        LaboratoryOptions = labIds.Count == 0
            ? new SelectList(Enumerable.Empty<SelectListItem>())
            : new SelectList(
                await _context.Laboratories.AsNoTracking()
                    .Where(l => labIds.Contains(l.Id))
                    .OrderBy(l => l.Name)
                    .Select(l => new { l.Id, Name = l.Code + " · " + l.Name })
                    .ToListAsync(),
                "Id", "Name", LaboratoryId);

        // Prácticas filtradas: una sola proyección, sin Includes.
        var query = _context.L3Requests.AsNoTracking()
            .Where(r => r.AcademicPlanningId == Planning.Id);

        if (WeekNumber.HasValue)
            query = query.Where(r => r.AcademicWeek == WeekNumber.Value);
        if (!string.IsNullOrWhiteSpace(Status))
            query = query.Where(r => r.Status == Status);
        if (LaboratoryId.HasValue)
            query = query.Where(r => r.LaboratoryId == LaboratoryId.Value);

        var projections = await query
            .OrderBy(r => r.AcademicWeek)
            .ThenBy(r => r.ScheduledDate)
            .Select(r => new PracticeProjection
            {
                Id = r.Id,
                PracticeName = r.PracticeName,
                AcademicWeek = r.AcademicWeek,
                ScheduledDate = r.ScheduledDate,
                ScheduledEndDate = r.ScheduledEndDate,
                Status = r.Status,
                TeacherPersonId = r.TeacherPersonId,
                SubjectName = r.Subject.Name,
                GroupName = r.Group.Name,
                LaboratoryName = r.Laboratory.Name,
                HasDetails = r.Details.Any()
            })
            .ToListAsync();

        // Docentes: una sola consulta complementaria para evitar N+1.
        var teacherIds = projections
            .Where(p => p.TeacherPersonId.HasValue)
            .Select(p => p.TeacherPersonId!.Value)
            .Distinct()
            .ToList();
        var teachers = teacherIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.Interns.AsNoTracking()
                .Where(i => teacherIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Name })
                .ToDictionaryAsync(i => i.Id, i => i.Name);

        // Incidentes: un solo lookup por id.
        var ids = projections.Select(p => p.Id).ToList();
        var incidentIds = ids.Count == 0
            ? new HashSet<int>()
            : (await _context.L5Incidents.AsNoTracking()
                .Where(i => i.L3RequestId.HasValue && ids.Contains(i.L3RequestId.Value))
                .Select(i => i.L3RequestId!.Value)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        Rows = projections.Select(p => new PracticeRow
        {
            Id = p.Id,
            Code = L3Codes.Format(p.Id),
            PracticeName = p.PracticeName,
            SubjectName = p.SubjectName,
            GroupName = p.GroupName,
            LaboratoryName = p.LaboratoryName,
            TeacherName = p.TeacherPersonId.HasValue && teachers.TryGetValue(p.TeacherPersonId.Value, out var t) ? t : "Sin docente",
            AcademicWeek = p.AcademicWeek,
            WeekdayName = AcademicWeekGenerator.WeekdayOf(p.ScheduledDate) is var wd && wd >= 1 && wd <= 5
                ? AcademicWeekGenerator.WeekdayNames[wd]
                : "—",
            ScheduledDate = p.ScheduledDate,
            ScheduledEndDate = p.ScheduledEndDate,
            Status = p.Status,
            StatusCss = PracticeStatus.Css(p.Status),
            NextUrl = PracticeStatus.NextUrl(p.Id, p.Status, p.HasDetails),
            HasIncident = incidentIds.Contains(p.Id)
        }).ToList();
    }

    private sealed class PracticeProjection
    {
        public int Id { get; init; }
        public string PracticeName { get; init; } = string.Empty;
        public int AcademicWeek { get; init; }
        public DateTime ScheduledDate { get; init; }
        public DateTime? ScheduledEndDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public int? TeacherPersonId { get; init; }
        public string SubjectName { get; init; } = string.Empty;
        public string GroupName { get; init; } = string.Empty;
        public string LaboratoryName { get; init; } = string.Empty;
        public bool HasDetails { get; init; }
    }

    public sealed class PracticeRow
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string PracticeName { get; init; } = string.Empty;
        public string SubjectName { get; init; } = string.Empty;
        public string GroupName { get; init; } = string.Empty;
        public string LaboratoryName { get; init; } = string.Empty;
        public string TeacherName { get; init; } = string.Empty;
        public int AcademicWeek { get; init; }
        public string WeekdayName { get; init; } = string.Empty;
        public DateTime ScheduledDate { get; init; }
        public DateTime? ScheduledEndDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public string StatusCss { get; init; } = "secondary";
        public string NextUrl { get; init; } = string.Empty;
        public bool HasIncident { get; init; }
    }
}