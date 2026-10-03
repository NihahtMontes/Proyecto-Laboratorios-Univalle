using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 2: requerimientos por tipo de equipo del laboratorio de la planificación.
/// Cada L3RequestDetail apunta a una unidad representativa de ese tipo.
/// </summary>
public class RequerimientosModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public RequerimientosModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Conflict { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public L3Request? Plan { get; private set; }
    public string Code => L3Codes.Format(PlanId);
    public bool IsEditable => Plan?.Status == L3RequestStatus.Planned;
    public List<RequirementRow> Items { get; private set; } = [];
    public List<CatalogOption> Catalog { get; private set; } = [];

    public sealed record RequirementRow(int DetailId, int EquipmentId, string EquipmentName, int Quantity, int OperationalInLab);
    public sealed record CatalogOption(int EquipmentId, string Name, int OperationalInLab, int TotalInLab);

    public async Task<IActionResult> OnGetAsync() => await LoadPageAsync();

    public async Task<IActionResult> OnPostAddAsync()
    {
        var plan = await FindEditablePlanAsync();
        if (plan == null) return RedirectToPage(new { planId = PlanId });

        if (!ModelState.IsValid) return await LoadPageAsync();

        var units = await _context.EquipmentUnits.AsNoTracking()
            .Where(u => u.LaboratoryId == plan.LaboratoryId && u.EquipmentId == Input.EquipmentId
                && u.CurrentStatus != EquipmentStatus.Deleted)
            .OrderBy(u => u.CurrentStatus == EquipmentStatus.Operational ? 0 : 1).ThenBy(u => u.InventoryNumber)
            .Select(u => u.Id)
            .ToListAsync();
        if (units.Count == 0)
        {
            ModelState.AddModelError("Input.EquipmentId", "El equipo no existe en el laboratorio de esta planificación.");
            return await LoadPageAsync();
        }

        var existing = await _context.L3RequestDetails.AsTracking()
            .FirstOrDefaultAsync(d => d.L3RequestId == plan.Id && d.EquipmentUnit.EquipmentId == Input.EquipmentId);
        if (existing != null)
        {
            existing.ExpectedQuantity += Input.Quantity;
        }
        else
        {
            _context.L3RequestDetails.Add(new L3RequestDetail
            {
                L3RequestId = plan.Id,
                EquipmentUnitId = units[0],
                ExpectedQuantity = Input.Quantity
            });
        }

        await _context.SaveChangesAsync();
        TempData.Success("Requerimiento agregado a la planificación.");
        return RedirectToPage(new { planId = PlanId });
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int detailId, int newQuantity)
    {
        var plan = await FindEditablePlanAsync();
        if (plan == null) return RedirectToPage(new { planId = PlanId });

        if (newQuantity < 1 || newQuantity > 999)
        {
            TempData.Error("La cantidad debe estar entre 1 y 999.");
            return RedirectToPage(new { planId = PlanId });
        }

        var detail = await _context.L3RequestDetails.AsTracking()
            .FirstOrDefaultAsync(d => d.Id == detailId && d.L3RequestId == plan.Id);
        if (detail == null)
        {
            TempData.Error("El requerimiento no pertenece a esta planificación.");
            return RedirectToPage(new { planId = PlanId });
        }

        detail.ExpectedQuantity = newQuantity;
        await _context.SaveChangesAsync();
        TempData.Success("Cantidad actualizada.");
        return RedirectToPage(new { planId = PlanId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(int detailId)
    {
        var plan = await FindEditablePlanAsync();
        if (plan == null) return RedirectToPage(new { planId = PlanId });

        var detail = await _context.L3RequestDetails.AsTracking()
            .FirstOrDefaultAsync(d => d.Id == detailId && d.L3RequestId == plan.Id);
        if (detail != null)
        {
            // Línea de borrador de una planificación aún no aprobada: no tiene historial que preservar.
            _context.L3RequestDetails.Remove(detail);
            await _context.SaveChangesAsync();
            TempData.Success("Requerimiento retirado.");
        }
        return RedirectToPage(new { planId = PlanId });
    }

    public async Task<IActionResult> OnPostContinueAsync()
    {
        var plan = await FindEditablePlanAsync();
        if (plan == null) return RedirectToPage(new { planId = PlanId });

        if (!await _context.L3RequestDetails.AsNoTracking().AnyAsync(d => d.L3RequestId == plan.Id))
        {
            TempData.Error("Agrega al menos un requerimiento antes de validar disponibilidad.");
            return RedirectToPage(new { planId = PlanId });
        }

        return RedirectToPage("./Disponibilidad", new { planId = PlanId });
    }

    private async Task<L3Request?> FindEditablePlanAsync()
    {
        var plan = await _context.L3Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == PlanId);
        if (plan == null)
        {
            TempData.Error("La planificación no existe.");
            return null;
        }
        if (plan.Status != L3RequestStatus.Planned)
        {
            TempData.Error("La planificación ya fue aprobada; sus requerimientos no se pueden modificar.");
            return null;
        }
        return plan;
    }

    private async Task<IActionResult> LoadPageAsync()
    {
        Plan = await _context.L3Requests.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.Group)
            .Include(r => r.TeacherPerson)
            .FirstOrDefaultAsync(r => r.Id == PlanId);
        if (Plan == null)
        {
            TempData.Error("La planificación no existe.");
            return RedirectToPage("./Index");
        }

        var stock = await _context.EquipmentUnits.AsNoTracking()
            .Where(u => u.LaboratoryId == Plan.LaboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
            .GroupBy(u => new { u.EquipmentId, u.Equipment!.Name })
            .Select(g => new CatalogOption(
                g.Key.EquipmentId,
                g.Key.Name,
                g.Count(u => u.CurrentStatus == EquipmentStatus.Operational),
                g.Count()))
            .ToListAsync();
        Catalog = stock.OrderBy(c => c.Name).ToList();
        var stockByEquipment = stock.ToDictionary(s => s.EquipmentId);

        var details = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => d.L3RequestId == Plan.Id)
            .Select(d => new { d.Id, d.EquipmentUnit.EquipmentId, Name = d.EquipmentUnit.Equipment!.Name, d.ExpectedQuantity })
            .ToListAsync();
        Items = details
            .Select(d => new RequirementRow(d.Id, d.EquipmentId, d.Name, d.ExpectedQuantity,
                stockByEquipment.TryGetValue(d.EquipmentId, out var s) ? s.OperationalInLab : 0))
            .OrderBy(d => d.EquipmentName)
            .ToList();

        return Page();
    }

    public sealed class InputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un equipo o utensilio")]
        public int EquipmentId { get; set; }

        [Range(1, 999, ErrorMessage = "La cantidad debe estar entre 1 y 999")]
        public int Quantity { get; set; } = 1;
    }
}
