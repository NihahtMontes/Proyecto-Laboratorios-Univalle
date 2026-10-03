using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class L3Model : PageModel
{
    private readonly LaboratoryPlanningDemoService _demoService;

    public L3Model(LaboratoryPlanningDemoService demoService)
    {
        _demoService = demoService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Id { get; set; }

    [BindProperty]
    public List<ReturnInput> Items { get; set; } = [];

    public LaboratoryPlanningDemo? Plan { get; private set; }

    public IActionResult OnGet()
    {
        return LoadPage();
    }

    public IActionResult OnPostConfirm(string id)
    {
        Id = id;
        var plan = _demoService.Find(id);
        if (plan == null)
            return MissingPlan();

        var postedItems = Items ?? [];
        var normalized = plan.Requirements.Select(requirement =>
        {
            var posted = postedItems.FirstOrDefault(item => string.Equals(item.Code, requirement.Code, StringComparison.OrdinalIgnoreCase));
            return new ReturnInput
            {
                Code = requirement.Code,
                Name = requirement.Name,
                Requested = requirement.RequestedQuantity,
                Returned = posted?.Returned ?? requirement.RequestedQuantity
            };
        }).ToList();

        foreach (var item in normalized)
        {
            if (item.Returned < 0 || item.Returned > item.Requested)
                ModelState.AddModelError(string.Empty, $"La devolución de {item.Name} debe estar entre 0 y {item.Requested}.");
        }

        if (!ModelState.IsValid)
        {
            Plan = plan;
            Items = normalized;
            return Page();
        }

        plan.Returns = normalized.Select(item => new LaboratoryPlanningDemoReturn
        {
            RequirementCode = item.Code,
            ReturnedQuantity = item.Returned
        }).ToList();

        var missing = normalized.Sum(item => item.Requested - item.Returned);
        if (missing > 0)
        {
            plan.Status = LaboratoryPlanningDemoStatus.InProgress;
            _demoService.Save(plan);
            TempData.Warning("La devolución quedó parcial. Registra la incidencia L-5 para cerrar el caso.");
            return RedirectToPage("./L5", new { id });
        }

        plan.Status = LaboratoryPlanningDemoStatus.Completed;
        _demoService.Save(plan);
        TempData.Success("Devolución confirmada y planificación cerrada.");
        return RedirectToPage("./Index", new { filter = "todos" });
    }

    private IActionResult LoadPage()
    {
        Plan = _demoService.Find(Id);
        if (Plan == null)
            return MissingPlan();

        var returns = Plan.Returns.ToDictionary(item => item.RequirementCode, StringComparer.OrdinalIgnoreCase);
        Items = Plan.Requirements.Select(requirement => new ReturnInput
        {
            Code = requirement.Code,
            Name = requirement.Name,
            Requested = requirement.RequestedQuantity,
            Returned = returns.TryGetValue(requirement.Code, out var item)
                ? item.ReturnedQuantity
                : requirement.RequestedQuantity
        }).ToList();
        return Page();
    }

    private IActionResult MissingPlan()
    {
        TempData.Error("La planificación no existe o ya no está disponible en esta sesión.");
        return RedirectToPage("./Index");
    }

    public sealed class ReturnInput
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Requested { get; set; }
        public int Returned { get; set; }
    }
}
