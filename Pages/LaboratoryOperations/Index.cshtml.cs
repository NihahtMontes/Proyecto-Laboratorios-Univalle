using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
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

    public IndexModel(ApplicationDbContext context, IPlanningService planning)
    {
        _context = context;
        _planning = planning;
    }

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

        return Page();
    }
}
