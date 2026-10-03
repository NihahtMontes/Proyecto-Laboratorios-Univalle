using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class DisponibilidadModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DisponibilidadModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public L3Request? PlanActivo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    public List<DisponibilidadItem> Items { get; set; } = new();
    public bool HasConflict { get; set; }

    public class DisponibilidadItem
    {
        public int DetailId { get; set; }
        public string Nombre { get; set; } = "";
        public string Inventario { get; set; } = "";
        public int Solicitado { get; set; }
        public int Disponible { get; set; }
        public int Diferencia => Disponible - Solicitado;
        public bool Ok => Diferencia >= 0;
    }

    public async Task<IActionResult> OnGetAsync(int planId)
    {
        PlanId = planId;

        PlanActivo = await _context.L3Requests
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.Teacher)
            .Include(r => r.Details)
                .ThenInclude(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
            .FirstOrDefaultAsync(r => r.Id == planId);

        if (PlanActivo == null || !PlanActivo.Details.Any())
        {
            return RedirectToPage("Requerimientos", new { planId });
        }

        // Validar disponibilidad de cada detalle
        foreach (var detail in PlanActivo.Details)
        {
            // Contar unidades operativas de ese tipo de equipo en ese laboratorio
            var equipmentId = detail.EquipmentUnit?.Equipment?.Id ?? 0;
            var stockDisponible = await _context.EquipmentUnits
                .Where(u => u.EquipmentId == equipmentId
                    && u.CurrentStatus == Models.Enums.EquipmentStatus.Operational)
                .CountAsync();

            var item = new DisponibilidadItem
            {
                DetailId = detail.Id,
                Nombre = detail.EquipmentUnit?.Equipment?.Name ?? "—",
                Inventario = detail.EquipmentUnit?.InventoryNumber ?? "—",
                Solicitado = detail.ExpectedQuantity,
                Disponible = stockDisponible
            };

            Items.Add(item);
            if (!item.Ok) HasConflict = true;
        }

        return Page();
    }

    /// <summary>
    /// Aprueba la reserva si no hay conflictos.
    /// Establece DeliveredQuantity = ExpectedQuantity y avanza al Paso 4.
    /// </summary>
    public async Task<IActionResult> OnPostApproveAsync(int planId)
    {
        PlanId = planId;

        var plan = await _context.L3Requests
            .Include(p => p.Details)
                .ThenInclude(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan == null) return RedirectToPage("Planificacion");

        // Re-validar stock antes de aprobar
        bool hasConflict = false;
        foreach (var detail in plan.Details)
        {
            var equipmentId = detail.EquipmentUnit?.Equipment?.Id ?? 0;
            var stockDisponible = await _context.EquipmentUnits
                .Where(u => u.EquipmentId == equipmentId
                    && u.CurrentStatus == Models.Enums.EquipmentStatus.Operational)
                .CountAsync();

            if (detail.ExpectedQuantity > stockDisponible)
            {
                hasConflict = true;
                break;
            }
        }

        if (hasConflict)
        {
            // Bloquea el avance y devuelve al paso 2
            return RedirectToPage("Requerimientos", new { planId, conflict = true });
        }

        // Aprobar: marcar DeliveredQuantity = ExpectedQuantity
        foreach (var detail in plan.Details)
        {
            detail.DeliveredQuantity = detail.ExpectedQuantity;
        }

        plan.Status = "Aprobado";
        await _context.SaveChangesAsync();

        return RedirectToPage("L3", new { id = planId });
    }
}
