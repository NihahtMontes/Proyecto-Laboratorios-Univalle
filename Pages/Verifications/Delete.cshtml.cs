using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
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
        public Verification Verification { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var verification = await _context.Verifications
                .Include(v => v.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (verification == null)
            {
                return NotFound();
            }
            else
            {
                Verification = verification;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var verification = await _context.Verifications.FindAsync(id);

            if (verification == null)
            {
                TempData.Warning("La verificación no existe o ya fue eliminada.");
                return RedirectToPage("./Index");
            }

            // QA 2026-10-18: anular la L-6 de un plan que ya avanzó dejaba el plan incoherente con un falso «eliminada».
            var linkedPlan = await _context.ManagementPlans.AsTracking()
                .FirstOrDefaultAsync(p => p.VerificationId == verification.Id);
            if (linkedPlan != null && linkedPlan.CurrentPhase > Models.Enums.WizardPhase.Verification)
            {
                TempData.Warning($"No se puede anular la verificación #{verification.Id}: el activo ya avanzó a {WizardPhaseGuard.PhaseLabel(linkedPlan.CurrentPhase)}. Use «Rehacer fase» en el asistente.");
                return RedirectToPage("./Index");
            }
            if (linkedPlan != null)
            {
                // El plan sigue en L-6: vuelve a quedar pendiente de verificación, sin la L-6 anulada.
                linkedPlan.VerificationId = null;
                linkedPlan.CurrentPhase = Models.Enums.WizardPhase.Verification;
                linkedPlan.CurrentState = Models.Enums.WizardEquipmentState.PendingVerification;
            }

            {
                Verification = verification;
                // Soft delete: anular la verificación en lugar de eliminar físicamente
                verification.Status = Models.Enums.VerificationStatus.Annulled;
                // NoTracking global: FindAsync devuelve la entidad sin seguimiento; sin Attach la anulación no persistía.
                _context.Attach(verification).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                await _context.SaveChangesAsync();
                
                TempData.Success(NotificationHelper.Verifications.Deleted(Verification.Id));
            }

            return RedirectToPage("./Index");
        }
    }
}
