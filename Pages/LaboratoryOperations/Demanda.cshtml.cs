using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class DemandaModel : PageModel
{
    private readonly LaboratoryPlanningDemoService _demoService;

    public DemandaModel(LaboratoryPlanningDemoService demoService)
    {
        _demoService = demoService;
    }

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public string Order { get; set; } = "mas";

    public IReadOnlyList<DemandRow> Rows { get; private set; } = [];
    public IReadOnlyList<string> Categories { get; private set; } = [];
    public int TotalRequested { get; private set; }
    public int DeficitCount { get; private set; }
    public int SurplusCount { get; private set; }

    public void OnGet()
    {
        var source = _demoService.GetPlans()
            .Where(plan => plan.Status != LaboratoryPlanningDemoStatus.Cancelled)
            .SelectMany(plan => plan.Requirements)
            .GroupBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var catalogItem = LaboratoryPlanningDemoCatalog.Find(group.Key);
                var requested = group.Sum(item => item.RequestedQuantity);
                var available = catalogItem?.AvailableStock ?? group.Max(item => item.AvailableQuantity);
                return new DemandRow
                {
                    Code = catalogItem?.Code ?? group.Key,
                    Name = catalogItem?.Name ?? group.First().Name,
                    Unit = catalogItem?.Unit ?? group.First().Unit,
                    Category = catalogItem?.Category ?? group.First().Category,
                    Requested = requested,
                    Available = available,
                    PlanCount = group.Count()
                };
            })
            .ToList();

        Categories = source
            .Select(item => item.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToList();

        var category = (Category ?? "todos").Trim();
        var filtered = string.Equals(category, "todos", StringComparison.OrdinalIgnoreCase)
            ? source
            : source.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

        Rows = string.Equals(Order, "menos", StringComparison.OrdinalIgnoreCase)
            ? filtered.OrderBy(item => item.Requested).ThenBy(item => item.Name).ToList()
            : filtered.OrderByDescending(item => item.Requested).ThenBy(item => item.Name).ToList();

        TotalRequested = Rows.Sum(item => item.Requested);
        DeficitCount = Rows.Count(item => item.Balance < 0);
        SurplusCount = Rows.Count(item => item.Balance >= 0);
    }

    public sealed class DemandRow
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public int Requested { get; init; }
        public int Available { get; init; }
        public int Balance => Available - Requested;
        public int PlanCount { get; init; }
    }
}
