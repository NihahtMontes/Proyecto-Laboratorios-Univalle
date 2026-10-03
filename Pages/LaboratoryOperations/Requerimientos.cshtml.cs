using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class RequerimientosModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly LaboratoryPlanningDemoService _demoService;

    public RequerimientosModel(ApplicationDbContext context, LaboratoryPlanningDemoService demoService)
    {
        _context = context;
        _demoService = demoService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public LaboratoryPlanningDemo? Plan { get; private set; }
    public IReadOnlyList<LaboratoryPlanningDemoCatalogItem> Catalog { get; private set; } = [];
    public string LaboratoryName { get; private set; } = "Laboratorio";

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostAddAsync(string id)
    {
        Id = id;
        Plan = _demoService.Find(id);
        if (Plan == null)
            return MissingPlan();

        var catalogItem = LaboratoryPlanningDemoCatalog.Find(Input.Code);
        if (catalogItem == null)
            ModelState.AddModelError("Input.Code", "Selecciona un material válido.");

        if (!ModelState.IsValid)
            return await LoadPageAsync();

        var existing = Plan.Requirements.FirstOrDefault(item =>
            string.Equals(item.Code, catalogItem!.Code, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            Plan.Requirements.Add(new LaboratoryPlanningDemoRequirement
            {
                Code = catalogItem!.Code,
                Name = catalogItem.Name,
                Unit = catalogItem.Unit,
                Category = catalogItem.Category,
                RequestedQuantity = Input.Quantity,
                AvailableQuantity = catalogItem.AvailableStock
            });
        }
        else
        {
            existing.RequestedQuantity += Input.Quantity;
        }

        Plan.Status = LaboratoryPlanningDemoStatus.Draft;
        _demoService.Save(Plan);
        TempData.Success("Requerimiento agregado a la planificación.");
        return RedirectToPage("./Requerimientos", new { id });
    }

    public IActionResult OnPostRemove(string id, string code)
    {
        var plan = _demoService.Find(id);
        if (plan == null)
            return MissingPlan();

        plan.Requirements.RemoveAll(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
        plan.Status = LaboratoryPlanningDemoStatus.Draft;
        _demoService.Save(plan);
        TempData.Success("Requerimiento retirado.");
        return RedirectToPage("./Requerimientos", new { id });
    }

    public IActionResult OnPostContinue(string id)
    {
        var plan = _demoService.Find(id);
        if (plan == null)
            return MissingPlan();

        if (plan.Requirements.Count == 0)
        {
            TempData.Error("Agrega al menos un requerimiento antes de validar disponibilidad.");
            return RedirectToPage("./Requerimientos", new { id });
        }

        plan.Status = LaboratoryPlanningDemoStatus.PendingAvailability;
        _demoService.Save(plan);
        return RedirectToPage("./Disponibilidad", new { id });
    }

    private async Task<IActionResult> LoadPageAsync()
    {
        Plan ??= _demoService.Find(Id);
        if (Plan == null)
            return MissingPlan();

        Catalog = LaboratoryPlanningDemoCatalog.Items;
        var laboratory = await _context.Laboratories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == Plan.LaboratoryId);
        LaboratoryName = laboratory == null ? "Laboratorio no disponible" : $"{laboratory.Code} · {laboratory.Name}";
        return Page();
    }

    private IActionResult MissingPlan()
    {
        TempData.Error("La planificación no existe o ya no está disponible en esta sesión.");
        return RedirectToPage("./Index");
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Selecciona un material")]
        public string? Code { get; set; }

        [Range(1, 999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Quantity { get; set; } = 1;
    }
}
