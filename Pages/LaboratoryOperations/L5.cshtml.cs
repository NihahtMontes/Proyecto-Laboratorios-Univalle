using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class L5Model : PageModel
{
    private readonly ApplicationDbContext _context;

    public L5Model(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public L5Incident Incident { get; set; } = new L5Incident();

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    public string? RelatedL3Name { get; set; }
    public string? RelatedLabName { get; set; }

    /// <summary>
    /// SelectList poblada SOLO con los utensilios prestados en este L3 específico.
    /// </summary>
    public List<SelectListItem> EquiposPrestadosList { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? planId, int? unitid)
    {
        if (!planId.HasValue)
        {
            return RedirectToPage("Planificacion");
        }

        PlanId = planId.Value;

        var l3 = await _context.L3Requests
            .Include(r => r.Laboratory)
            .Include(r => r.Details)
                .ThenInclude(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
            .FirstOrDefaultAsync(r => r.Id == PlanId);

        if (l3 == null)
        {
            return RedirectToPage("Planificacion");
        }

        RelatedL3Name = l3.PracticeName;
        RelatedLabName = l3.Laboratory?.Name;
        Incident.L3RequestId = PlanId;

        // Poblar combobox con SOLO los utensilios de esta práctica
        EquiposPrestadosList = l3.Details.Select(d => new SelectListItem
        {
            Value = d.EquipmentUnitId.ToString(),
            Text = (d.EquipmentUnit?.Equipment?.Name ?? "—") + " (INV: " + (d.EquipmentUnit?.InventoryNumber ?? "—") + ")",
            Selected = unitid.HasValue && d.EquipmentUnitId == unitid.Value
        }).ToList();

        // Pre-seleccionar si viene desde L3
        if (unitid.HasValue)
        {
            Incident.EquipmentUnitId = unitid.Value;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Limpiar navegaciones del model binder
        ModelState.Remove("Incident.EquipmentUnit");
        ModelState.Remove("Incident.L3Request");

        if (!ModelState.IsValid)
        {
            // Recargar datos para la vista
            await ReloadPageData();
            return Page();
        }

        Incident.RecordDate = DateTime.UtcNow;
        Incident.RepositionStatus = "Pendiente de Reposición";

        _context.L5Incidents.Add(Incident);
        await _context.SaveChangesAsync();

        // Redirigir de vuelta al L3 para que el usuario vea que el botón se desbloqueó
        return RedirectToPage("L3", new { id = PlanId });
    }

    private async Task ReloadPageData()
    {
        var l3 = await _context.L3Requests
            .Include(r => r.Laboratory)
            .Include(r => r.Details)
                .ThenInclude(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
            .FirstOrDefaultAsync(r => r.Id == PlanId);

        if (l3 != null)
        {
            RelatedL3Name = l3.PracticeName;
            RelatedLabName = l3.Laboratory?.Name;

            EquiposPrestadosList = l3.Details.Select(d => new SelectListItem
            {
                Value = d.EquipmentUnitId.ToString(),
                Text = (d.EquipmentUnit?.Equipment?.Name ?? "—") + " (INV: " + (d.EquipmentUnit?.InventoryNumber ?? "—") + ")"
            }).ToList();
        }
    }
}
