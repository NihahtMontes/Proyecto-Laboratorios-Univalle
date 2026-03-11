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

        public IActionResult OnGet()
        {
            LoadLists();
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El laboratorio es obligatorio")]
            [Display(Name = "Laboratorio")]
            public int LaboratoryId { get; set; }

            [Required(ErrorMessage = "La unidad física es obligatoria")]
            [Display(Name = "Unidad Física")]
            public int EquipmentUnitId { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Fecha de Inspección")]
            public DateTime Date { get; set; } = DateTime.Today;

            // Checklist Items
            [Display(Name = "Cableado")]
            public VerificationResult CablingCheck { get; set; }
            [Display(Name = "Manguera Gas")]
            public VerificationResult GasHoseCheck { get; set; }
            [Display(Name = "Manguera Agua")]
            public VerificationResult WaterHoseCheck { get; set; }
            [Display(Name = "Quemador")]
            public VerificationResult BurnerCheck { get; set; }
            [Display(Name = "Intercambiador Calor")]
            public VerificationResult HeatExchangerCheck { get; set; }
            [Display(Name = "Sensor de Llama")]
            public VerificationResult FlameSensorCheck { get; set; }
            [Display(Name = "Ignitor")]
            public VerificationResult ElectrodeIgniterCheck { get; set; }
            [Display(Name = "Ventilador")]
            public VerificationResult FanCheck { get; set; }
            [Display(Name = "Llama Piloto")]
            public VerificationResult CombustionFlameCheck { get; set; }
            [Display(Name = "Lubricación")]
            public VerificationResult LubricationCheck { get; set; }
            [Display(Name = "Encendido Horno")]
            public VerificationResult OvenIgnitionCheck { get; set; }
            [Display(Name = "Control Temperatura")]
            public VerificationResult TemperatureControlCheck { get; set; }
            [Display(Name = "Limpieza Interna")]
            public VerificationResult InternalCleaningCheck { get; set; }
            [Display(Name = "Limpieza Externa")]
            public VerificationResult ExternalCleaningCheck { get; set; }
            [Display(Name = "Luces")]
            public VerificationResult LightsCheck { get; set; }
            [Display(Name = "Vapor Alta Temp")]
            public VerificationResult HighTempSteamCheck { get; set; }
            [Display(Name = "Display LED")]
            public VerificationResult LedDisplayCheck { get; set; }
            [Display(Name = "Válvula Solenoide")]
            public VerificationResult SolenoidValveCheck { get; set; }
            [Display(Name = "Alarma Sonora")]
            public VerificationResult SoundAlarmCheck { get; set; }
            [Display(Name = "Termocupla")]
            public VerificationResult ThermocoupleCheck { get; set; }
            [Display(Name = "Salida Vapor")]
            public VerificationResult SteamOutletCheck { get; set; }

            [Display(Name = "Observaciones")]
            public string? Observations { get; set; }
            [Display(Name = "Hallazgos Críticos")]
            public string? CriticalFindings { get; set; }
            [Display(Name = "Recomendaciones")]
            public string? Recommendations { get; set; }
            [Display(Name = "Estado Final")]
            public VerificationStatus Status { get; set; } = VerificationStatus.Draft;
        }

        // AJAX Handlers
        public async Task<JsonResult> OnGetLaboratoriesByFacultyAsync(int facultyId)
        {
            var labs = await _context.Laboratories
                .Where(l => l.FacultyId == facultyId && l.Status == GeneralStatus.Activo)
                .Select(l => new { id = l.Id, name = l.Name })
                .OrderBy(x => x.name)
                .ToListAsync();
            return new JsonResult(labs);
        }

        public async Task<JsonResult> OnGetUnitsByLabAsync(int laboratoryId)
        {
            var units = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Where(u => u.LaboratoryId == laboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                .OrderBy(u => u.Equipment!.Name)
                .ThenBy(u => u.InventoryNumber)
                .Select(u => new { 
                    id = u.Id, 
                    eqName = u.Equipment != null ? u.Equipment.Name : "Equipo",
                    inv = u.InventoryNumber
                })
                .ToListAsync();

            var result = units.Select(x => new {
                id = x.id,
                name = $"{x.eqName} (Inv: {x.inv})"
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAsync()
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

            var unit = await _context.EquipmentUnits.Include(u => u.Equipment).FirstOrDefaultAsync(u => u.Id == verification.EquipmentUnitId);
            TempData.Success(NotificationHelper.Verifications.Created(unit?.Equipment?.Name ?? "equipo"));

            return RedirectToPage("./Index");
        }

        private void LoadLists()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name), "Id", "Name");
            
            ViewData["LaboratoryId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewData["EquipmentUnitId"] = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }
}
