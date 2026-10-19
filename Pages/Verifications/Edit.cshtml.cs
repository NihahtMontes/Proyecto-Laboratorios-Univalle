using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public EditInputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public bool IsWizard { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FocusPlanId { get; set; }

        public class EditInputModel
        {
            public int Id { get; set; }

            [Required]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Fecha Inspección")]
            public DateTime Date { get; set; }

            [Display(Name = "Observaciones y Hallazgos")]
            public string? Observations { get; set; }

            [Required]
            [Display(Name = "Condición Física")]
            public PhysicalCondition PhysicalCondition { get; set; }

            [Required]
            [Display(Name = "Estado de Registro")]
            public VerificationStatus Status { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var verification = await _context.Verifications
                .Include(v => v.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (verification == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isHealthyCondition = Input.PhysicalCondition is PhysicalCondition.Excellent or PhysicalCondition.Good;

            // Mapear a InputModel simple
            Input = new EditInputModel
            {
                Id = verification.Id,
                EquipmentUnitId = verification.EquipmentUnitId,
                Date = verification.Date,
                Observations = verification.Observations,
                Status = verification.Status,
                PhysicalCondition = verification.PhysicalCondition
            };

            ViewData["EquipmentName"] = verification.EquipmentUnit?.Equipment?.Name ?? "N/A";
            ViewData["InventoryNumber"] = verification.EquipmentUnit?.InventoryNumber ?? "N/A";

            LoadLists();

            var currentMgmt = ManagementId.HasValue
                ? await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value)
                : await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == verification.ManagementId); // la gestión del propio registro (pueden estar activas una preventiva y una correctiva)

            ViewData["IsCorrective"] = currentMgmt?.Type == ManagementType.Corrective;
            ViewData["IsWizard"] = IsWizard;
            ViewData["ManagementId"] = currentMgmt?.Id;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Mismo criterio que el L-6 masivo (MassCreate): solo «Malo» (o «Dado de baja») es falla y exige describirla.
            var isHealthyCondition = Input.PhysicalCondition is not (PhysicalCondition.Bad or PhysicalCondition.Decommissioned);
            if (!isHealthyCondition && string.IsNullOrWhiteSpace(Input.Observations))
                ModelState.AddModelError("Input.Observations", "Describa las fallas del equipo marcado en estado Malo antes de guardar L-6.");
            if (Input.EquipmentUnitId <= 0 || !await _context.EquipmentUnits.AnyAsync(u => u.Id == Input.EquipmentUnitId))
                ModelState.AddModelError("Input.EquipmentUnitId", "La unidad física seleccionada no existe.");

            if (!ModelState.IsValid)
            {
                LoadLists();
                ViewData["IsWizard"] = IsWizard;
                ViewData["ManagementId"] = ManagementId;
                return Page();
            }

            var verification = await _context.Verifications
                .AsTracking()
                .Include(v => v.Faults)
                .FirstOrDefaultAsync(v => v.Id == Input.Id);

            if (verification == null) return NotFound();

            // Actualizar datos atómicos
            var user = await _userManager.GetUserAsync(User);

            verification.Date = Input.Date;
            verification.Observations = Input.Observations;
            verification.Status = isHealthyCondition ? VerificationStatus.Completed : VerificationStatus.WithObservations;
            verification.PhysicalCondition = Input.PhysicalCondition;
            verification.EquipmentUnitId = Input.EquipmentUnitId;

            // Auditoría
            if (user != null) verification.ModifiedById = user.Id;
            verification.LastModifiedDate = DateTime.UtcNow;

            ManagementPlan? linkedPlan = null;
            var planPhaseLocked = false;

            try
            {
                var linkedPlanQuery = _context.ManagementPlans
                    .AsTracking()
                    .Include(p => p.TechnicalRequest)
                    .Where(p => p.VerificationId == verification.Id);

                if (ManagementId.HasValue)
                {
                    linkedPlanQuery = linkedPlanQuery.Where(p => p.ManagementId == ManagementId.Value);
                }

                linkedPlan = await linkedPlanQuery.FirstOrDefaultAsync();

                // R-02: fuera de la ventana temprana el plan (y su solicitud L-7) no se toca;
                // solo se guardan los datos de la verificación.
                planPhaseLocked = linkedPlan != null && !CanUpdateLinkedPlanPhase(linkedPlan);

                if (linkedPlan != null && !planPhaseLocked)
                {
                    linkedPlan.LastModifiedDate = DateTime.UtcNow;
                    linkedPlan.ModifiedById = user?.Id;

                    if (isHealthyCondition)
                    {
                        if (verification.Faults != null)
                        {
                            foreach (var fault in verification.Faults.Where(f => !f.IsDeleted))
                            {
                                fault.IsDeleted = true;
                                fault.LastModifiedDate = DateTime.UtcNow;
                                fault.ModifiedById = user?.Id;
                            }
                        }

                        if (linkedPlan.TechnicalRequest?.Status == RequestStatus.Pending)
                        {
                            linkedPlan.TechnicalRequest.Status = RequestStatus.Cancelled;
                            linkedPlan.TechnicalRequest.LastModifiedDate = DateTime.UtcNow;
                            linkedPlan.TechnicalRequest.ModifiedById = user?.Id;
                            linkedPlan.RequestId = null;
                        }
                        else if (linkedPlan.RequestId.HasValue && linkedPlan.TechnicalRequest == null)
                        {
                            linkedPlan.RequestId = null;
                        }

                        linkedPlan.CurrentPhase = WizardPhase.Verification;
                        linkedPlan.CurrentState = WizardEquipmentState.VerifiedGood;
                    }
                    else
                    {
                        linkedPlan.CurrentPhase = WizardPhase.TechnicalRequest;
                        linkedPlan.CurrentState = WizardEquipmentState.AwaitingRequest;
                    }
                }

                await _context.SaveChangesAsync();

                if (planPhaseLocked)
                {
                    TempData.Warning($"La verificación se actualizó, pero el activo ya avanzó a {WizardPhaseGuard.PhaseLabel(linkedPlan!.CurrentPhase)}: el plan no se modificó. Use «Rehacer fase» si necesita corregirlo.");
                }
                else
                {
                    TempData.Success(isHealthyCondition
                        ? $"Verificacion #{verification.Id} actualizada. El activo fue movido a activos sanos."
                        : NotificationHelper.Verifications.Updated(verification.Id));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VerificationExists(verification.Id)) return NotFound();
                else throw;
            }

            if (IsWizard)
            {
                if (planPhaseLocked && linkedPlan != null)
                {
                    // El plan quedó en una fase posterior: devolver al paso real del wizard.
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = (int)linkedPlan.CurrentPhase, ManagementId, FocusPlanId });
                }

                if (isHealthyCondition)
                {
                    return RedirectToPage("/Index", new { ShowWizard = true, Step = FocusPlanId.HasValue ? (int)WizardPhase.Verification : 2, ManagementId, FocusPlanId });
                }

                return RedirectToPage("./Details", new { id = verification.Id, isWizard = IsWizard, managementId = ManagementId, focusPlanId = FocusPlanId });
            }

            return RedirectToPage("./Index");
        }

        /// <summary>
        /// R-02: determina si el plan vinculado todavía está en la ventana temprana que permite
        /// re-clasificar el activo desde L-6 (fase Verification, o TechnicalRequest/AwaitingRequest,
        /// es decir, con la solicitud L-7 aún sin registrar). Fuera de esa ventana el plan ya avanzó
        /// (L-7 registrado, L-8, L-3, Kardex, L-12 o completado) y editarlo movería el wizard hacia atrás.
        /// </summary>
        public static bool CanUpdateLinkedPlanPhase(ManagementPlan? plan)
        {
            if (plan == null)
                return false;

            if (plan.CurrentState == WizardEquipmentState.Completed || plan.PlanStatus == ManagementPlanStatus.Completed)
                return false;

            if (plan.CurrentPhase == WizardPhase.Verification)
                return true;

            // Un L-6 con falla deja una solicitud L-7 Pendiente creada automáticamente (RequestId ≠ null)
            // y el plan en AwaitingRequest: esa es justo la situación en que se corrige la verificación.
            // Al registrar el L-7 de verdad el plan pasa a Maintenance/AwaitingMaintenance y la ventana se cierra.
            return plan.CurrentPhase == WizardPhase.TechnicalRequest
                && plan.CurrentState == WizardEquipmentState.AwaitingRequest;
        }

        private void LoadLists()
        {
            var units = _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Select(u => new { 
                    Id = u.Id, 
                    DisplayName = $"{u.Equipment!.Name} (INV: {u.InventoryNumber})" 
                })
                .ToList();

            ViewData["EquipmentUnitId"] = new SelectList(units, "Id", "DisplayName", Input.EquipmentUnitId);
        }

        private bool VerificationExists(int id)
        {
            return _context.Verifications.Any(e => e.Id == id);
        }
    }
}
