using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementContext;
        private readonly IManagementActivationService _managementActivation;

        public EditModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IManagementContextService managementContext,
            IManagementActivationService managementActivation)
        {
            _context = context;
            _userManager = userManager;
            _managementContext = managementContext;
            _managementActivation = managementActivation;
        }

        [BindProperty]
        public EditManagementInputModel Input { get; set; } = new();

        public ManagementType ManagementType { get; set; } = ManagementType.Preventive;

        public SelectList FacultyList { get; set; } = default!;

        public class EditManagementInputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El año es obligatorio")]
            [Range(2000, 2100, ErrorMessage = "Año fuera de rango")]
            [Display(Name = "Año")]
            public int Year { get; set; }

            [Required(ErrorMessage = "El semestre es obligatorio")]
            [Range(0, 2, ErrorMessage = "Semestre inválido (0, 1 o 2)")]
            [Display(Name = "Semestre")]
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
            [Display(Name = "Estado")]
            public ManagementStatus Status { get; set; }

            [Display(Name = "Facultad Asociada")]
            public int? FacultyId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var management = await _context.Managements
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (management == null || management.Status == ManagementStatus.Deleted)
            {
                TempData.Warning("La gestión solicitada fue eliminada o no existe.");
                return RedirectToPage("./Index");
            }

            ManagementType = management.Type;
            Input = new EditManagementInputModel
            {
                Id = management.Id,
                Year = management.Year,
                Semester = management.Semester,
                Description = management.Description,
                StartDate = management.StartDate,
                PlannedEndDate = management.PlannedEndDate,
                Status = management.Status,
                FacultyId = management.FacultyId
            };

            LoadFaculties();
            return Page();
        }

        private void LoadFaculties()
        {
            FacultyList = new SelectList(
                _context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name),
                "Id", "Name", Input.FacultyId);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadManagementTypeAsync(Input.Id);
                LoadFaculties();
                return Page();
            }

            var management = await _context.Managements
                .AsTracking()
                .FirstOrDefaultAsync(m => m.Id == Input.Id);

            if (management == null)
            {
                TempData.Warning("La gestión solicitada fue eliminada o no existe.");
                return RedirectToPage("./Index");
            }

            ManagementType = management.Type;

            var shouldActivate = Input.Status == ManagementStatus.Active;

            var currentUser = await _userManager.GetUserAsync(User);

            management.Year = Input.Year;
            management.Semester = Input.Semester;
            management.Code = BuildManagementCode(management.Type, Input.Year, Input.Semester);
            management.Description = Input.Description;
            management.StartDate = Input.StartDate;
            management.PlannedEndDate = Input.PlannedEndDate;
            management.Status = Input.Status;
            management.FacultyId = Input.FacultyId;
            management.ModifiedById = currentUser?.Id;
            management.LastModifiedDate = DateTime.UtcNow;

            var closedCount = 0;
            if (shouldActivate)
            {
                var activationResult = await _managementActivation.ActivateAsync(management);
                closedCount = activationResult.ClosedCount;
            }

            try
            {
                await _context.SaveChangesAsync();
                _managementContext.InvalidateCache();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ManagementExists(management.Id))
                {
                    return NotFound();
                }

                throw;
            }

            var handoffMessage = closedCount > 0
                ? $" Se cerró {closedCount} gestión activa anterior del mismo tipo."
                : string.Empty;
            TempData.Success($"Gestión actualizada correctamente.{handoffMessage}");
            return RedirectToPage("./Details", new { id = management.Id, ActiveTab = "dashboard" });
        }

        private bool ManagementExists(int id)
        {
            return _context.Managements.Any(e => e.Id == id);
        }

        private async Task LoadManagementTypeAsync(int id)
        {
            ManagementType = await _context.Managements
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => m.Type)
                .FirstOrDefaultAsync();
        }

        private static string BuildManagementCode(ManagementType type, int year, int semester)
        {
            return type == ManagementType.Corrective
                ? $"CORR-{year}-{semester}"
                : $"{year}-{semester}";
        }
    }
}
