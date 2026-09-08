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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IManagementContextService _managementContext;
        private readonly IManagementActivationService _managementActivation;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IManagementContextService managementContext,
            IManagementActivationService managementActivation,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _managementContext = managementContext;
            _managementActivation = managementActivation;
            _logger = logger;
        }

        [BindProperty]
        public ManagementInputModel Input { get; set; } = new();

        public SelectList FacultyList { get; set; } = default!;

        public IActionResult OnGet(string? type = null)
        {
            Input.Year = DateTime.Now.Year;

            if (!string.IsNullOrEmpty(type) &&
                Enum.TryParse<ManagementType>(type, ignoreCase: true, out var typeEnum) &&
                Enum.IsDefined(typeof(ManagementType), typeEnum))
            {
                Input.Type = typeEnum;
            }

            Input.Semester = Input.Type == ManagementType.Corrective
                ? 0
                : DateTime.Now.Month <= 6 ? 1 : 2;

            LoadFaculties();
            return Page();
        }

        private void LoadFaculties()
        {
            FacultyList = new SelectList(
                _context.Faculties.Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name),
                "Id", "Name", Input.FacultyId);
        }

        public class ManagementInputModel
        {
            [Required(ErrorMessage = "El anio es obligatorio")]
            [Range(2000, 2100, ErrorMessage = "Anio fuera de rango permitido")]
            [Display(Name = "Anio")]
            public int Year { get; set; }

            [Required(ErrorMessage = "El semestre es obligatorio")]
            [Range(0, 2, ErrorMessage = "El semestre debe ser 0, 1 o 2")]
            [Display(Name = "Semestre")]
            public int Semester { get; set; }

            [Display(Name = "Descripcion general")]
            [StringLength(1000)]
            public string? Description { get; set; }

            [Display(Name = "Fecha de Inicio")]
            [DataType(DataType.Date)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "Fecha Limite Planificada")]
            [DataType(DataType.Date)]
            public DateTime? PlannedEndDate { get; set; }

            [Required]
            [Display(Name = "Estado Inicial")]
            public ManagementStatus Status { get; set; } = ManagementStatus.Active;

            [Required]
            [Display(Name = "Tipo de Gestion")]
            public ManagementType Type { get; set; } = ManagementType.Preventive;

            [Display(Name = "Facultad Asociada")]
            public int? FacultyId { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!Enum.IsDefined(typeof(ManagementType), Input.Type))
                ModelState.AddModelError("Input.Type", "Seleccione un tipo de gestión válido.");

            if (!Enum.IsDefined(typeof(ManagementStatus), Input.Status) ||
                Input.Status == ManagementStatus.Deleted)
            {
                ModelState.AddModelError("Input.Status", "Seleccione un estado inicial válido.");
            }

            if (Input.Type == ManagementType.Corrective)
            {
                Input.Semester = 0;
                ModelState.Remove("Input.Semester");
            }
            else if (Input.Semester is < 1 or > 2)
            {
                ModelState.AddModelError("Input.Semester", "La gestión preventiva debe usar el semestre 1 o 2.");
            }

            if (Input.StartDate.HasValue && Input.PlannedEndDate.HasValue &&
                Input.PlannedEndDate.Value.Date < Input.StartDate.Value.Date)
            {
                ModelState.AddModelError("Input.PlannedEndDate",
                    "La fecha de cierre planificada no puede ser anterior a la fecha de inicio.");
            }

            if (Input.FacultyId.HasValue)
            {
                var facultyExists = await _context.Faculties.AnyAsync(faculty =>
                    faculty.Id == Input.FacultyId.Value && faculty.Status == GeneralStatus.Activo);
                if (!facultyExists)
                    ModelState.AddModelError("Input.FacultyId", "La facultad seleccionada no está disponible.");
            }

            if (!ModelState.IsValid)
            {
                LoadFaculties();
                return Page();
            }

            if (Input.Year > DateTime.Now.Year + 1)
            {
                ModelState.AddModelError("Input.Year", "No se puede registrar una gestion para un anio tan lejano en el futuro.");
                LoadFaculties();
                return Page();
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var code = BuildManagementCode(Input.Type, Input.Year, Input.Semester);
                var exists = await _context.Managements.AnyAsync(m =>
                    m.Year == Input.Year &&
                    m.Semester == Input.Semester &&
                    m.Type == Input.Type &&
                    m.Status != ManagementStatus.Deleted);

                if (exists)
                {
                    ModelState.AddModelError(string.Empty, $"Ya existe una gestion registrada para {code}.");
                    LoadFaculties();
                    return Page();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var management = new Management
                {
                    Year = Input.Year,
                    Semester = Input.Semester,
                    Code = code,
                    Description = Input.Description?.Clean(),
                    StartDate = Input.StartDate,
                    PlannedEndDate = Input.PlannedEndDate,
                    Status = Input.Status,
                    Type = Input.Type,
                    FacultyId = Input.FacultyId,
                    Responsible = currentUser?.FullName ?? User.Identity?.Name ?? "Sistema",
                    CreatedById = currentUser?.Id,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Managements.Add(management);

                var closedCount = 0;
                if (management.Status == ManagementStatus.Active)
                {
                    var activationResult = await _managementActivation.ActivateAsync(management);
                    closedCount = activationResult.ClosedCount;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _managementContext.InvalidateCache();

                var handoffMessage = closedCount > 0
                    ? $" Se cerro {closedCount} gestion activa anterior del mismo tipo."
                    : string.Empty;

                if (Input.Type == ManagementType.Preventive)
                {
                    TempData.Success($"La Gestion {code} ha sido creada correctamente.{handoffMessage} Seleccione manualmente los activos que participaran en esta ronda preventiva.");
                    return RedirectToPage("./PlanAssets", new { id = management.Id });
                }

                TempData.Success($"La Gestion Correctiva {code} ha sido creada correctamente. Lista para recibir fallas criticas.{handoffMessage}");
                return RedirectToPage("./Index", new { type = Input.Type.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear gestion {ManagementType} {Year}-{Semester}", Input.Type, Input.Year, Input.Semester);
                ModelState.AddModelError(string.Empty,
                    "No se pudo crear la gestión. Intente nuevamente o contacte al administrador.");
                LoadFaculties();
                return Page();
            }
        }

        private static string BuildManagementCode(ManagementType type, int year, int semester)
        {
            return type == ManagementType.Corrective
                ? $"CORR-{year}-{semester}"
                : $"{year}-{semester}";
        }
    }
}
