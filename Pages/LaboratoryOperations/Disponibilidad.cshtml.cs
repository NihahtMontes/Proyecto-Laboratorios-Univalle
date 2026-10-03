using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class DisponibilidadModel : PageModel
{
    private readonly LaboratoryPlanningDemoService _demoService;

    public DisponibilidadModel(LaboratoryPlanningDemoService demoService)
    {
        _demoService = demoService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Id { get; set; }

    public LaboratoryPlanningDemo? Plan { get; private set; }
    public IReadOnlyList<AvailabilityRow> Rows { get; private set; } = [];
    public bool HasConflict => Rows.Any(row => row.Difference < 0);

    public IActionResult OnGet()
    {
        return LoadPage();
    }

    public IActionResult OnPostApprove(string id)
    {
        var plan = _demoService.Find(id);
        if (plan == null)
            return MissingPlan();

        if (plan.Requirements.Count == 0)
        {
            TempData.Error("La planificación no tiene requerimientos para aprobar.");
            return RedirectToPage("./Requerimientos", new { id });
        }

        if (plan.Requirements.Any(item => item.RequestedQuantity > item.AvailableQuantity))
        {
            TempData.Error("No se puede aprobar mientras exista un déficit de disponibilidad.");
            return RedirectToPage("./Disponibilidad", new { id });
        }

        plan.Status = LaboratoryPlanningDemoStatus.Approved;
        _demoService.Save(plan);
        TempData.Success("Disponibilidad validada. La práctica está lista para operar.");
        return RedirectToPage("./L3", new { id });
    }

    private IActionResult LoadPage()
    {
        Plan = _demoService.Find(Id);
        if (Plan == null)
            return MissingPlan();

        Rows = Plan.Requirements.Select(item => new AvailabilityRow
        {
            Name = item.Name,
            Unit = item.Unit,
            Requested = item.RequestedQuantity,
            Available = item.AvailableQuantity
        }).ToList();
        return Page();
    }

    private IActionResult MissingPlan()
    {
        TempData.Error("La planificación no existe o ya no está disponible en esta sesión.");
        return RedirectToPage("./Index");
    }

    public sealed class AvailabilityRow
    {
        public string Name { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
        public int Requested { get; init; }
        public int Available { get; init; }
        public int Difference => Available - Requested;
    }
}
