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

        // Helper para inyectar el globo rojo visualmente en la UI
        public int CountVerificationFails(Verification? v)
        {
            if (v == null) return 0;
            int fails = 0;
            
            // Asumiendo que el valor entero de Bad es algo específico, 
            // contaremos todos los que no sean Good o NotChecked
            // En el Enum, suele ser { NotChecked = 0, Good = 1, Bad = 2, NA = 3 }
            
            if (v.CablingCheck == VerificationResult.Bad) fails++;
            if (v.GasHoseCheck == VerificationResult.Bad) fails++;
            if (v.WaterHoseCheck == VerificationResult.Bad) fails++;
            if (v.BurnerCheck == VerificationResult.Bad) fails++;
            if (v.HeatExchangerCheck == VerificationResult.Bad) fails++;
            if (v.FlameSensorCheck == VerificationResult.Bad) fails++;
            if (v.ElectrodeIgniterCheck == VerificationResult.Bad) fails++;
            if (v.FanCheck == VerificationResult.Bad) fails++;
            if (v.CombustionFlameCheck == VerificationResult.Bad) fails++;
            if (v.LubricationCheck == VerificationResult.Bad) fails++;
            if (v.OvenIgnitionCheck == VerificationResult.Bad) fails++;
            if (v.TemperatureControlCheck == VerificationResult.Bad) fails++;
            if (v.InternalCleaningCheck == VerificationResult.Bad) fails++;
            if (v.ExternalCleaningCheck == VerificationResult.Bad) fails++;
            if (v.LightsCheck == VerificationResult.Bad) fails++;
            if (v.HighTempSteamCheck == VerificationResult.Bad) fails++;
            if (v.LedDisplayCheck == VerificationResult.Bad) fails++;
            if (v.SolenoidValveCheck == VerificationResult.Bad) fails++;
            if (v.SoundAlarmCheck == VerificationResult.Bad) fails++;
            if (v.ThermocoupleCheck == VerificationResult.Bad) fails++;
            if (v.SteamOutletCheck == VerificationResult.Bad) fails++;
            
            return fails;
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
    }
}