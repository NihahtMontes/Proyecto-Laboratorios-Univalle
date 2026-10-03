using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class L3Model : PageModel
{
    private readonly ApplicationDbContext _context;

    public L3Model(ApplicationDbContext context)
    {
        _context = context;
    }

    public L3Request? L3RequestData { get; set; }
    public List<L3RequestDetail> RequestDetails { get; set; } = new();

    /// <summary>Lista de incidentes L5 vinculados a esta solicitud L3.</summary>
    public List<L5Incident> IncidentesL5 { get; set; } = new();

    /// <summary>True si existe al menos un faltante sin incidente L5 registrado.</summary>
    public bool BloquearFinalizacion { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var request = await _context.L3Requests
            .Include(r => r.Details)
                .ThenInclude(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
            .Include(r => r.Teacher)
            .Include(r => r.Laboratory)
            .Include(r => r.Incidents)
            .FirstOrDefaultAsync(r => id == null || r.Id == id);

        if (request != null)
        {
            L3RequestData = request;
            RequestDetails = request.Details.ToList();
            IncidentesL5 = request.Incidents.ToList();
            EvaluateBlocking();
        }

        return Page();
    }

    /// <summary>Actualiza la cantidad devuelta de un detalle.</summary>
    public async Task<IActionResult> OnPostUpdateReturnAsync(int detailId, int returnedQuantity, int planId)
    {
        var detail = await _context.L3RequestDetails.FindAsync(detailId);
        if (detail != null)
        {
            detail.ReturnedQuantity = returnedQuantity;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage(new { id = planId });
    }

    /// <summary>Finaliza la solicitud L3 (solo si no hay faltantes sin incidente).</summary>
    public async Task<IActionResult> OnPostFinalizeAsync(int planId)
    {
        var request = await _context.L3Requests
            .Include(r => r.Details)
            .Include(r => r.Incidents)
            .FirstOrDefaultAsync(r => r.Id == planId);

        if (request == null) return RedirectToPage("Planificacion");

        // Re-validar bloqueo del lado servidor
        foreach (var detail in request.Details)
        {
            int faltante = detail.DeliveredQuantity - detail.ReturnedQuantity;
            if (faltante > 0)
            {
                bool tieneIncidente = request.Incidents.Any(i => i.EquipmentUnitId == detail.EquipmentUnitId);
                if (!tieneIncidente)
                {
                    // Bloqueo duro: no se puede finalizar
                    return RedirectToPage(new { id = planId });
                }
            }
        }

        request.Status = "Finalizado";
        request.LastModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToPage("/LaboratoryOperations/Index");
    }

    private void EvaluateBlocking()
    {
        BloquearFinalizacion = false;
        foreach (var detail in RequestDetails)
        {
            int faltante = detail.DeliveredQuantity - detail.ReturnedQuantity;
            if (faltante > 0)
            {
                bool tieneIncidente = IncidentesL5.Any(i => i.EquipmentUnitId == detail.EquipmentUnitId);
                if (!tieneIncidente)
                {
                    BloquearFinalizacion = true;
                    return;
                }
            }
        }
    }
}
