using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class RequerimientosModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public RequerimientosModel(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>La planificación activa cargada desde BD con sus detalles.</summary>
    public L3Request? PlanActivo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    [BindProperty]
    public int EquipmentUnitId { get; set; }

    [BindProperty]
    public int ExpectedQuantity { get; set; } = 1;

    public SelectList EquipmentUnitsList { get; set; } = default!;
    public bool HasConflict { get; set; }

    public async Task<IActionResult> OnGetAsync(int planId, bool conflict = false)
    {
        PlanId = planId;
        HasConflict = conflict;

        PlanActivo = await _context.L3Requests
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.Teacher)
            .Include(r => r.Group)
            .Include(r => r.Details)
                .ThenInclude(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
            .FirstOrDefaultAsync(r => r.Id == planId);

        if (PlanActivo == null)
        {
            return RedirectToPage("Planificacion");
        }

        await LoadEquipmentDropdown();
        return Page();
    }

    /// <summary>Agrega un utensilio a la lista de requerimientos de esta planificación.</summary>
    public async Task<IActionResult> OnPostAddDetailAsync()
    {
        if (PlanId <= 0 || EquipmentUnitId <= 0 || ExpectedQuantity <= 0)
        {
            return RedirectToPage(new { planId = PlanId });
        }

        // Verificar si ya existe este utensilio en la lista
        var existing = await _context.L3RequestDetails
            .FirstOrDefaultAsync(d => d.L3RequestId == PlanId && d.EquipmentUnitId == EquipmentUnitId);

        if (existing != null)
        {
            existing.ExpectedQuantity += ExpectedQuantity;
        }
        else
        {
            _context.L3RequestDetails.Add(new L3RequestDetail
            {
                L3RequestId = PlanId,
                EquipmentUnitId = EquipmentUnitId,
                ExpectedQuantity = ExpectedQuantity
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToPage(new { planId = PlanId });
    }

    /// <summary>Elimina un detalle de la lista de requerimientos.</summary>
    public async Task<IActionResult> OnPostRemoveDetailAsync(int detailId)
    {
        var detail = await _context.L3RequestDetails.FindAsync(detailId);
        if (detail != null && detail.L3RequestId == PlanId)
        {
            _context.L3RequestDetails.Remove(detail);
            await _context.SaveChangesAsync();
        }
        return RedirectToPage(new { planId = PlanId });
    }

    /// <summary>Actualiza la cantidad solicitada de un detalle.</summary>
    public async Task<IActionResult> OnPostUpdateQuantityAsync(int detailId, int newQuantity)
    {
        var detail = await _context.L3RequestDetails.FindAsync(detailId);
        if (detail != null && detail.L3RequestId == PlanId && newQuantity > 0)
        {
            detail.ExpectedQuantity = newQuantity;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage(new { planId = PlanId });
    }

    private async Task LoadEquipmentDropdown()
    {
        var units = await _context.EquipmentUnits
            .Include(u => u.Equipment)
            .Where(u => u.Equipment != null)
            .OrderBy(u => u.Equipment!.Name)
            .Select(u => new { u.Id, DisplayName = u.Equipment!.Name + " (INV: " + u.InventoryNumber + ")" })
            .ToListAsync();
        EquipmentUnitsList = new SelectList(units, "Id", "DisplayName");
    }
}
