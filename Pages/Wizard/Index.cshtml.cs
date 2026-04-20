using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Wizard
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly Data.ApplicationDbContext _context;

        public IndexModel(Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int Step { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        public SelectList LaboratoriesList { get; set; } = default!;
        public List<ManagementPlan> ActivePlans { get; set; } = new();
        public Management? ActiveManagement { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Step < 0 || Step > 6) Step = 0;

            ActiveManagement = await _context.Managements
                .FirstOrDefaultAsync(m => m.Status == ManagementStatus.Active);

            if (ActiveManagement == null)
            {
                return Page(); // Mostraremos un mensaje en la vista si no hay gestión activa
            }

            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .Select(l => new { l.Id, l.Name })
                .ToListAsync();

            LaboratoriesList = new SelectList(labs, "Id", "Name", SelectedLabId);

            // Include EquipmentUnit -> Laboratory for step filtering
            var query = _context.ManagementPlans
                .Include(mp => mp.EquipmentUnit)
                    .ThenInclude(u => u!.Equipment)
                .Include(mp => mp.EquipmentUnit)
                    .ThenInclude(u => u!.Laboratory)
                .Include(mp => mp.Verification)
                    .ThenInclude(v => v!.CheckResults)
                .Include(mp => mp.TechnicalRequest)
                .Include(mp => mp.Maintenance)
                .Include(mp => mp.Departure)
                .Where(mp => mp.ManagementId == ActiveManagement.Id);

            if (SelectedLabId.HasValue)
            {
                query = query.Where(mp => mp.EquipmentUnit.LaboratoryId == SelectedLabId.Value);
            }

            ActivePlans = await query.ToListAsync();

            return Page();
        }

        public int CountVerificationFails(Verification? v)
        {
            return v?.FailuresCount ?? 0;
        }

        public IActionResult OnPostNextStep()
        {
            return RedirectToPage(new { Step = Step + 1, SelectedLabId });
        }

        public IActionResult OnPostPreviousStep()
        {
            int prevStep = Step > 1 ? Step - 1 : 1;
            return RedirectToPage(new { Step = prevStep, SelectedLabId });
        }

        public async Task<IActionResult> OnPostConfirmKardexAsync(int planId)
        {
            try
            {
                var plan = await _context.ManagementPlans
                    .Include(p => p.EquipmentUnit)
                    .Include(p => p.Maintenance)
                    .FirstOrDefaultAsync(p => p.Id == planId);

                if (plan != null)
                {
                    var kardexEntry = new EquipmentStateHistory
                    {
                        EquipmentUnitId = plan.EquipmentUnitId ?? 0,
                        Status = EquipmentStatus.Operational,
                        StartDate = DateTime.UtcNow,
                        Reason = $"Kardex: Mantenimiento #{plan.MaintenanceId} finalizado en Gestión #{plan.ManagementId}."
                    };
                    _context.EquipmentStateHistories.Add(kardexEntry);

                    if (plan.EquipmentUnit != null)
                    {
                        plan.EquipmentUnit.CurrentStatus = EquipmentStatus.Operational;
                        _context.EquipmentUnits.Update(plan.EquipmentUnit);
                    }

                    plan.CurrentPhase = WizardPhase.Disbursement;
                    plan.CurrentState = WizardEquipmentState.Completed;
                    plan.PlanStatus = ManagementPlanStatus.Completed;

                    await _context.SaveChangesAsync();
                }
            }
            catch { /* Silent - continue */ }

            return RedirectToPage(new { Step = 5, SelectedLabId });
        }
        public async Task<IActionResult> OnPostFastFailAsync(int planId)
        {
            var plan = await _context.ManagementPlans.FindAsync(planId);
            if (plan != null && plan.CurrentPhase == WizardPhase.Verification)
            {
                plan.CurrentPhase = WizardPhase.TechnicalRequest;
                plan.CurrentState = WizardEquipmentState.AwaitingRequest;
                await _context.SaveChangesAsync();
                
                // Redirige directamente al formulario de creación L7 manteniendo el contexto del Wizard
                return RedirectToPage("/Requests/Create", new { equipmentUnitId = plan.EquipmentUnitId, isWizard = true, managementPlanId = plan.Id });
            }
            
            return RedirectToPage(new { Step = 2, SelectedLabId });
        }
    }
}