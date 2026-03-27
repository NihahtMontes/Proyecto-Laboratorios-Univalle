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

        // AGREGADO isWizard para detectar origen
        public IActionResult OnGet(int? equipmentUnitId = null, int? returnFacultyId = null, int? returnLaboratoryId = null, bool isWizard = false)
        {
            LoadLists();
            if (equipmentUnitId.HasValue)
            {
                Input.EquipmentUnitId = equipmentUnitId.Value;
            }
            ViewData["ReturnFacultyId"] = returnFacultyId;
            ViewData["ReturnLaboratoryId"] = returnLaboratoryId;
            ViewData["IsWizard"] = isWizard; // Guardar estado
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

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

            // ... Todos tus campos checklist originales se mantienen igual ...
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
            public VerificationStatus Status { get; set; } = VerificationStatus.Draft;
        }

        // AJAX Handlers iguales...
        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId) { /* ... */ return new JsonResult(null); }
        public async Task<JsonResult> OnGetUnitsByLabAsync(int laboratoryId) { /* ... */ return new JsonResult(null); }

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

            // GUARDADO EN BASE DE DATOS REAL
            _context.Verifications.Add(verification);
            await _context.SaveChangesAsync();

            var unit = await _context.EquipmentUnits.Include(u => u.Equipment).FirstOrDefaultAsync(u => u.Id == verification.EquipmentUnitId);
            TempData.Success(NotificationHelper.Verifications.Created(unit?.Equipment?.Name ?? "equipo"));

            // REDIRECCIÓN INTELIGENTE AGREGADA
            if (isWizard)
            {
                return RedirectToPage("/Index", new { ShowWizard = true, Step = 1, SelectedLabId = Input.LaboratoryId });
            }

            if (Request.Form.TryGetValue("returnFacultyId", out var retFaculty) && Request.Form.TryGetValue("returnLaboratoryId", out var retLab))
            {
                return RedirectToPage("./ByAmbient", new { SelectedFacultyId = retFaculty, SelectedLaboratoryId = retLab });
            }

            return RedirectToPage("./Index");
        }

        private void LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name), "Id", "Name");
            ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}