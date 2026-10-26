using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DeleteModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Request MaintenanceRequest { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.RequestedBy)
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();
            
            MaintenanceRequest = request;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.Maintenance)
                .AsTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            // Referential Integrity Validation: Cannot delete if maintenance already exists linked to this request
            if (request.Maintenance != null)
            {
                TempData.Error("No se puede eliminar la solicitud porque ya tiene un registro de mantenimiento asociado.");
                return RedirectToPage("./Index");
            }

            // QA 2026-10-18: una solicitud que forma parte del proceso de un activo (L-7 o compra L-12 del plan) no se anula
            // desde aquí: dejaba el plan apuntando a una solicitud anulada (incluso planes Completados).
            var linkedPlan = await _context.ManagementPlans.AsTracking()
                .FirstOrDefaultAsync(p => p.RequestId == request.Id || p.AcquisitionRequestId == request.Id);
            if (linkedPlan != null)
            {
                var isPlanTechnicalRequest = linkedPlan.RequestId == request.Id && linkedPlan.CurrentPhase <= Models.Enums.WizardPhase.Maintenance;
                if (!isPlanTechnicalRequest)
                {
                    TempData.Error($"No se puede anular esta solicitud: forma parte del proceso del equipo (paso {WizardPhaseGuard.PhaseLabel(linkedPlan.CurrentPhase)}). Para corregirlo, abra el formulario del paso actual del equipo desde el asistente y use «Rehacer paso anterior».");
                    return RedirectToPage("./Index");
                }

                // L-7 de un plan que aún espera su L-8: se anula y el plan vuelve a esperar una L-7 nueva (no queda apuntando a la anulada).
                linkedPlan.RequestId = null;
                linkedPlan.CurrentPhase = Models.Enums.WizardPhase.TechnicalRequest;
                linkedPlan.CurrentState = Models.Enums.WizardEquipmentState.AwaitingRequest;
            }

            // Soft delete: cancelar la solicitud en lugar de eliminar físicamente
            request.Status = Models.Enums.RequestStatus.Cancelled;
            await _context.SaveChangesAsync();
            
            TempData.Success($"La solicitud #{request.Id} ha sido anulada correctamente.");
            return RedirectToPage("./Index");
        }
    }
}
