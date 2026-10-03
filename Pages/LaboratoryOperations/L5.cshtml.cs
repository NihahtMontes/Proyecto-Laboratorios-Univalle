using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class L5Model : PageModel
{
    private readonly LaboratoryPlanningDemoService _demoService;

    public L5Model(LaboratoryPlanningDemoService demoService)
    {
        _demoService = demoService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public LaboratoryPlanningDemo? Plan { get; private set; }

    public IActionResult OnGet()
    {
        return LoadPage();
    }

    public IActionResult OnPostSave(string id)
    {
        Id = id;
        Plan = _demoService.Find(id);
        if (Plan == null)
            return MissingPlan();

        if (Input.Quantity > Plan.Requirements.Sum(item => item.RequestedQuantity))
            ModelState.AddModelError("Input.Quantity", "La cantidad afectada no puede superar la cantidad solicitada.");

        if (string.IsNullOrWhiteSpace(Input.AffectedItem)
            || !Plan.Requirements.Any(item => string.Equals(item.Name, Input.AffectedItem, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError("Input.AffectedItem", "Selecciona un ítem incluido en los requerimientos.");
        }

        if (!ModelState.IsValid)
            return Page();

        Plan.Incident = new LaboratoryPlanningDemoIncident
        {
            Code = $"L5-{DateTime.Today:yyyyMMdd}-{_demoService.GetPlans().Count:000}",
            Type = Input.Type,
            AffectedItem = Input.AffectedItem,
            Quantity = Input.Quantity,
            Observations = Input.Observations.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        Plan.Status = LaboratoryPlanningDemoStatus.Incident;
        _demoService.Save(Plan);
        TempData.Success("Incidencia L-5 registrada en la sesión de demostración.");
        return RedirectToPage("./Index", new { filter = "todos" });
    }

    private IActionResult LoadPage()
    {
        Plan = _demoService.Find(Id);
        if (Plan == null)
            return MissingPlan();

        if (Plan.Incident != null)
        {
            Input = new InputModel
            {
                Type = Plan.Incident.Type,
                AffectedItem = Plan.Incident.AffectedItem,
                Quantity = Plan.Incident.Quantity,
                Observations = Plan.Incident.Observations
            };
        }

        return Page();
    }

    private IActionResult MissingPlan()
    {
        TempData.Error("La planificación no existe o ya no está disponible en esta sesión.");
        return RedirectToPage("./Index");
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Selecciona el tipo de novedad")]
        public string Type { get; set; } = "Daño";

        [Required(ErrorMessage = "Selecciona el ítem afectado")]
        public string? AffectedItem { get; set; }

        [Range(1, 999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Describe la incidencia"), StringLength(1000)]
        public string Observations { get; set; } = string.Empty;
    }
}
