using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly Proyecto_Laboratorios_Univalle.Services.IEquipmentStateHistoryService _stateHistory;

        public DeleteModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            Proyecto_Laboratorios_Univalle.Services.IEquipmentStateHistoryService stateHistory)
        {
            _context = context;
            _stateHistory = stateHistory;
        }

        [BindProperty]
        public Maintenance Maintenance { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.Maintenances
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenance == null)
            {
                return NotFound();
            }
            else
            {
                Maintenance = maintenance;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.Maintenances
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (maintenance == null)
            {
                TempData.Warning("El mantenimiento no existe o ya fue anulado.");
                return RedirectToPage("./Index");
            }

            // QA 2026-10-18: anular un L-8 dejaba el plan apuntando a un L-8 anulado y la unidad «En mantenimiento».
            var linkedPlan = await _context.ManagementPlans.AsTracking()
                .FirstOrDefaultAsync(p => p.MaintenanceId == maintenance.Id);
            if (linkedPlan != null && linkedPlan.CurrentPhase > Models.Enums.WizardPhase.Maintenance)
            {
                TempData.Warning($"No se puede anular este mantenimiento: el activo ya avanzó a {WizardPhaseGuard.PhaseLabel(linkedPlan.CurrentPhase)}. Para corregirlo, abra el formulario del paso actual del equipo desde el asistente y use «Rehacer paso anterior».");
                return RedirectToPage("./Index");
            }

            var equipmentName = maintenance.EquipmentUnit?.Equipment?.Name;

            // Soft delete: cancelar el mantenimiento en lugar de eliminar físicamente
            maintenance.Status = Models.Enums.MaintenanceStatus.Cancelled;
            if (linkedPlan != null)
            {
                // El plan sigue esperando su L-8, ya sin el anulado.
                linkedPlan.MaintenanceId = null;
                linkedPlan.CurrentState = Models.Enums.WizardEquipmentState.AwaitingMaintenance;
            }

            // La unidad sale de «En mantenimiento»: se cierra su historial abierto y vuelve a Operativa.
            if (maintenance.EquipmentUnit is { CurrentStatus: Models.Enums.EquipmentStatus.UnderMaintenance } unit)
            {
                await _stateHistory.ChangeStatusAsync(unit, Models.Enums.EquipmentStatus.Operational,
                    $"Mantenimiento #{maintenance.Id} anulado.");
            }

            await _context.SaveChangesAsync();
            TempData.Success(NotificationHelper.Maintenances.Deleted(equipmentName));
            return RedirectToPage("./Index");
        }
    }
}
