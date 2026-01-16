using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public int EquipmentId { get; set; }
            [DataType(DataType.Date)]
            public DateTime Date { get; set; } = DateTime.Today;

            // Checklist
            public VerificationResult CablingCheck { get; set; }
            public VerificationResult GasHoseCheck { get; set; }
            public VerificationResult WaterHoseCheck { get; set; }
            public VerificationResult BurnerCheck { get; set; }
            public VerificationResult HeatExchangerCheck { get; set; }
            public VerificationResult FlameSensorCheck { get; set; }
            public VerificationResult ElectrodeIgniterCheck { get; set; }
            public VerificationResult FanCheck { get; set; }
            public VerificationResult CombustionFlameCheck { get; set; }
            public VerificationResult LubricationCheck { get; set; }
            public VerificationResult OvenIgnitionCheck { get; set; }
            public VerificationResult TemperatureControlCheck { get; set; }
            public VerificationResult InternalCleaningCheck { get; set; }
            public VerificationResult ExternalCleaningCheck { get; set; }
            public VerificationResult LightsCheck { get; set; }
            public VerificationResult HighTempSteamCheck { get; set; }
            public VerificationResult LedDisplayCheck { get; set; }
            public VerificationResult SolenoidValveCheck { get; set; }
            public VerificationResult SoundAlarmCheck { get; set; }
            public VerificationResult ThermocoupleCheck { get; set; }
            public VerificationResult SteamOutletCheck { get; set; }

            public string? Observations { get; set; }
            public string? CriticalFindings { get; set; }
            public string? Recommendations { get; set; }
            public VerificationStatus Status { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
                return Page();
            }

            var verification = new Verification
            {
                EquipmentId = Input.EquipmentId,
                Date = Input.Date,
                CablingCheck = Input.CablingCheck,
                GasHoseCheck = Input.GasHoseCheck,
                WaterHoseCheck = Input.WaterHoseCheck,
                BurnerCheck = Input.BurnerCheck,
                HeatExchangerCheck = Input.HeatExchangerCheck,
                FlameSensorCheck = Input.FlameSensorCheck,
                ElectrodeIgniterCheck = Input.ElectrodeIgniterCheck,
                FanCheck = Input.FanCheck,
                CombustionFlameCheck = Input.CombustionFlameCheck,
                LubricationCheck = Input.LubricationCheck,
                OvenIgnitionCheck = Input.OvenIgnitionCheck,
                TemperatureControlCheck = Input.TemperatureControlCheck,
                InternalCleaningCheck = Input.InternalCleaningCheck,
                ExternalCleaningCheck = Input.ExternalCleaningCheck,
                LightsCheck = Input.LightsCheck,
                HighTempSteamCheck = Input.HighTempSteamCheck,
                LedDisplayCheck = Input.LedDisplayCheck,
                SolenoidValveCheck = Input.SolenoidValveCheck,
                SoundAlarmCheck = Input.SoundAlarmCheck,
                ThermocoupleCheck = Input.ThermocoupleCheck,
                SteamOutletCheck = Input.SteamOutletCheck,
                Observations = Input.Observations,
                CriticalFindings = Input.CriticalFindings,
                Recommendations = Input.Recommendations,
                Status = Input.Status,
                CreatedDate = DateTime.Now
            };

            _context.Verifications.Add(verification);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
