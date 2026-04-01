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
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ManagementPlanId { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            public int FacultyId { get; set; }
            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            public int LaboratoryId { get; set; }
            [Required(ErrorMessage = "La unidad física es obligatoria")]
            public int EquipmentUnitId { get; set; }
            [DataType(DataType.Date)]
            public DateTime Date { get; set; } = DateTime.Today;

            public VerificationResult CablingCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult GasHoseCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult WaterHoseCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult BurnerCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult HeatExchangerCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult FlameSensorCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult ElectrodeIgniterCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult FanCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult CombustionFlameCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult LubricationCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult OvenIgnitionCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult TemperatureControlCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult InternalCleaningCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult ExternalCleaningCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult LightsCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult HighTempSteamCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult LedDisplayCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult SolenoidValveCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult SoundAlarmCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult ThermocoupleCheck { get; set; } = VerificationResult.NotChecked;
            public VerificationResult SteamOutletCheck { get; set; } = VerificationResult.NotChecked;
            public string? Observations { get; set; }
            public string? CriticalFindings { get; set; }
            public string? Recommendations { get; set; }
            public VerificationStatus Status { get; set; } = VerificationStatus.Draft;
        }

        public IActionResult OnGet(int? equipmentUnitId = null, int? returnFacultyId = null, int? returnLaboratoryId = null, bool isWizard = false)
        {
            if (equipmentUnitId.HasValue)
            {
                Input.EquipmentUnitId = equipmentUnitId.Value;
                var unit = _context.EquipmentUnits
                    .Include(u => u.Laboratory)
                    .FirstOrDefault(u => u.Id == equipmentUnitId.Value);

                if (unit != null)
                {
                    Input.LaboratoryId = unit.LaboratoryId ?? 0;
                    if (unit.Laboratory != null)
                    {
                        Input.FacultyId = unit.Laboratory.FacultyId;
                    }
                }
            }

            LoadLists();

            ViewData["ReturnFacultyId"] = returnFacultyId;
            ViewData["ReturnLaboratoryId"] = returnLaboratoryId;
            ViewData["IsWizard"] = isWizard;
            return Page();
        }

        // AJAX Handlers Corregidos para el Paso 4
        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLabAsync(int laboratoryId)
        {
            var units = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .Select(u => new { id = u.Id, name = u.Equipment.Name + " (" + u.InventoryNumber + ")" })
                .ToListAsync();
            return new JsonResult(units);
        }

        public async Task<IActionResult> OnPostAsync(bool isWizard = false)
        {
            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

            var verification = new Verification
            {
                EquipmentUnitId = Input.EquipmentUnitId,
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
                CreatedDate = DateTime.UtcNow
            };

            var user = await _userManager.GetUserAsync(User);
            if (user != null) verification.CreatedById = user.Id;

            _context.Verifications.Add(verification);
            await _context.SaveChangesAsync();

            bool hasFailures = typeof(InputModel).GetProperties()
                .Where(p => p.PropertyType == typeof(VerificationResult))
                .Any(p => (VerificationResult)p.GetValue(Input)! == VerificationResult.Bad);

            if (ManagementPlanId.HasValue)
            {
                var plan = await _context.ManagementPlans.FindAsync(ManagementPlanId.Value);
                if (plan != null)
                {
                    plan.VerificationId = verification.Id;
                    
                    if (hasFailures)
                    {
                        plan.CurrentPhase = WizardPhase.TechnicalRequest;
                        plan.CurrentState = WizardEquipmentState.AwaitingRequest;
                    }
                    else
                    {
                        // Si es perfecto (sin fallas), salta L-7 y va directo a Mantenimiento (L-8 preventivo)
                        plan.CurrentPhase = WizardPhase.Maintenance; 
                        plan.CurrentState = WizardEquipmentState.AwaitingMaintenance;
                    }
                    
                    await _context.SaveChangesAsync();
                }
            }

            if (isWizard)
            {
                if (hasFailures)
                    return RedirectToPage("/Wizard/Index", new { Step = 2, SelectedLabId = Input.LaboratoryId });

                // Al no tener fallos, lo mandamos al paso 3 (Mantenimiento preventivo)
                return RedirectToPage("/Wizard/Index", new { Step = 3, SelectedLabId = Input.LaboratoryId });
            }

            return RedirectToPage("./Index");
        }

        private void LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name), "Id", "Name", Input.FacultyId);

            if (Input.FacultyId > 0)
            {
                var labs = _context.Laboratories
                    .Where(l => l.FacultyId == Input.FacultyId && l.Status == GeneralStatus.Activo)
                    .OrderBy(l => l.Name)
                    .ToList();
                ViewData["LaboratoryId"] = new SelectList(labs, "Id", "Name", Input.LaboratoryId);
            }
            else
            {
                ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            if (Input.LaboratoryId > 0)
            {
                var units = _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Where(u => u.LaboratoryId == Input.LaboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                    .Select(u => new { Id = u.Id, Name = u.Equipment.Name + " (" + u.InventoryNumber + ")" })
                    .ToList();
                ViewData["EquipmentUnitId"] = new SelectList(units, "Id", "Name", Input.EquipmentUnitId);
            }
            else
            {
                ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }
    }
}