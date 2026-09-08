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
using System.Data;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementContext;
        private readonly IManagementActivationService _managementActivation;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IManagementContextService managementContext,
            IManagementActivationService managementActivation,
            ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _managementContext = managementContext;
            _managementActivation = managementActivation;
            _logger = logger;
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
            [StringLength(1000)]
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
            if (!Enum.IsDefined(typeof(ManagementStatus), Input.Status) ||
                Input.Status == ManagementStatus.Deleted)
            {
                ModelState.AddModelError("Input.Status",
                    "Use la acción de baja lógica para eliminar una gestión.");
            }

            if (Input.StartDate.HasValue && Input.PlannedEndDate.HasValue &&
                Input.PlannedEndDate.Value.Date < Input.StartDate.Value.Date)
            {
                ModelState.AddModelError("Input.PlannedEndDate",
                    "La fecha de cierre planificada no puede ser anterior a la fecha de inicio.");
            }

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

            if (management.Type == ManagementType.Corrective)
            {
                Input.Semester = 0;
                ModelState.Remove("Input.Semester");
            }
            else if (Input.Semester is < 1 or > 2)
            {
                ModelState.AddModelError("Input.Semester",
                    "La gestión preventiva debe usar el semestre 1 o 2.");
            }

            if (Input.FacultyId.HasValue)
            {
                var facultyExists = await _context.Faculties.AnyAsync(faculty =>
                    faculty.Id == Input.FacultyId.Value && faculty.Status == GeneralStatus.Activo);
                if (!facultyExists)
                    ModelState.AddModelError("Input.FacultyId", "La facultad seleccionada no está disponible.");
            }

            var duplicateExists = await _context.Managements.AnyAsync(candidate =>
                candidate.Id != management.Id &&
                candidate.Type == management.Type &&
                candidate.Year == Input.Year &&
                candidate.Semester == Input.Semester &&
                candidate.Status != ManagementStatus.Deleted);

            if (duplicateExists)
                ModelState.AddModelError(string.Empty, "Ya existe otra gestión para el mismo tipo y periodo.");

            if (!ModelState.IsValid)
            {
                LoadFaculties();
                return Page();
            }

            var shouldActivate = Input.Status == ManagementStatus.Active;

            var currentUser = await _userManager.GetUserAsync(User);

            management.Year = Input.Year;
            management.Semester = Input.Semester;
            management.Code = BuildManagementCode(management.Type, Input.Year, Input.Semester);
            management.Description = Input.Description?.Clean();
            management.StartDate = Input.StartDate;
            management.PlannedEndDate = Input.PlannedEndDate;
            management.Status = Input.Status;
            management.FacultyId = Input.FacultyId;
            management.ModifiedById = currentUser?.Id;
            management.LastModifiedDate = DateTime.UtcNow;

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var closedCount = 0;
                if (shouldActivate)
                {
                    var activationResult = await _managementActivation.ActivateAsync(management);
                    closedCount = activationResult.ClosedCount;
                }
                else if (Input.Status == ManagementStatus.Completed)
                {
                    management.ActualClosedDate ??= DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _managementContext.InvalidateCache();

                var handoffMessage = closedCount > 0
                    ? $" Se cerró {closedCount} gestión activa anterior del mismo tipo."
                    : string.Empty;
                TempData.Success($"Gestión actualizada correctamente.{handoffMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo actualizar la gestión {ManagementId}.", management.Id);
                ModelState.AddModelError(string.Empty,
                    "No se pudo actualizar la gestión. Intente nuevamente o contacte al administrador.");
                LoadFaculties();
                return Page();
            }
            return RedirectToPage("./Details", new { id = management.Id, ActiveTab = "dashboard" });
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
