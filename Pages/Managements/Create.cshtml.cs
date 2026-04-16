using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            Input.Year = DateTime.Now.Year;
            Input.Semester = DateTime.Now.Month <= 6 ? 1 : 2;
            return Page();
        }

        [BindProperty]
        public ManagementInputModel Input { get; set; } = new();

        public class ManagementInputModel
        {
            [Required(ErrorMessage = "El año es obligatorio")]
            [Range(2000, 2100, ErrorMessage = "Año fuera de rango permitido")]
            [Display(Name = "Año")]
            public int Year { get; set; }

            [Required(ErrorMessage = "El semestre es obligatorio")]
            [Range(1, 2, ErrorMessage = "El semestre debe ser 1 o 2")]
            [Display(Name = "Semestre (1 o 2)")]
            public int Semester { get; set; }

            [Display(Name = "Descripción general")]
            public string? Description { get; set; }

            [Display(Name = "Fecha de Inicio")]
            [DataType(DataType.Date)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "Fecha Límite Planificada")]
            [DataType(DataType.Date)]
            public DateTime? PlannedEndDate { get; set; }

            [Required]
            [Display(Name = "Estado Inicial")]
            public ManagementStatus Status { get; set; } = ManagementStatus.Active;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validar que el año no sea superior al año actual + 1
            if (Input.Year > DateTime.Now.Year + 1)
            {
                ModelState.AddModelError("Input.Year", "No se puede registrar una gestión para un año tan lejano en el futuro.");
                return Page();
            }

            // Validar que no exista ya esta gestión (Año-Semestre)
            var code = $"{Input.Year}-{Input.Semester}";
            var exists = await _context.Managements.AnyAsync(m => m.Year == Input.Year && m.Semester == Input.Semester && m.Status != ManagementStatus.Deleted);
            if (exists)
            {
                ModelState.AddModelError(string.Empty, $"Ya existe una gestión registrada para {code}.");
                return Page();
            }

            // Si se intenta crear como ACTIVA, validar que no haya otra activa
            if (Input.Status == ManagementStatus.Active)
            {
                var anyActive = await _context.Managements.AnyAsync(m => m.Status == ManagementStatus.Active);
                if (anyActive)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe una gestión activa. Por favor, cierre o inactive la gestión actual antes de activar una nueva.");
                    return Page();
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            
            var management = new Management
            {
                Year = Input.Year,
                Semester = Input.Semester,
                Code = code,
                Description = Input.Description,
                StartDate = Input.StartDate,
                PlannedEndDate = Input.PlannedEndDate,
                Status = Input.Status,
                Responsible = currentUser?.FullName ?? User.Identity?.Name ?? "Sistema"
            };

            _context.Managements.Add(management);
            await _context.SaveChangesAsync();

            // Sincronización Automática: Cargar todos los equipos activos a la nueva ronda
            var activeUnits = await _context.EquipmentUnits
                .Where(u => u.CurrentStatus != EquipmentStatus.Deleted)
                .ToListAsync();

            foreach (var unit in activeUnits)
            {
                _context.ManagementPlans.Add(new ManagementPlan
                {
                    ManagementId = management.Id,
                    EquipmentUnitId = unit.Id,
                    CurrentPhase = WizardPhase.Verification,
                    CurrentState = WizardEquipmentState.PendingVerification,
                    PlanStatus = ManagementPlanStatus.Pending,
                    PlannedDate = null
                });
            }
            await _context.SaveChangesAsync();

            TempData.Success($"La Gestión {code} ha sido creada correctamente con todos los equipos activos.");
            return RedirectToPage("./Index");
        }
    }
}
