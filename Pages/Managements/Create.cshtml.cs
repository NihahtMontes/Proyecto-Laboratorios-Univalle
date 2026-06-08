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
            Input.Semester = DateTime.Now.Month <= 6 ? 1 : 2;

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ManagementType>(type, out var typeEnum))
            {
                Input.Type = typeEnum;
            }

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
            [Range(1, 2, ErrorMessage = "El semestre debe ser 1 o 2")]
            [Display(Name = "Semestre (1 o 2)")]
            public int Semester { get; set; }

            [Display(Name = "Descripcion general")]
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
            if (!ModelState.IsValid)
            {
                LoadFaculties();
                return Page();
            }

            if (Input.Year > DateTime.Now.Year + 1)
            {
                ModelState.AddModelError("Input.Year", "No se puede registrar una gestion para un anio tan lejano en el futuro.");
                return Page();
            }

            try
            {
                var code = BuildManagementCode(Input.Type, Input.Year, Input.Semester);
                var exists = await _context.Managements.AnyAsync(m =>
                    m.Year == Input.Year &&
                    m.Semester == Input.Semester &&
                    m.Type == Input.Type &&
                    m.Status != ManagementStatus.Deleted);

                if (exists)
                {
                    ModelState.AddModelError(string.Empty, $"Ya existe una gestion registrada para {code}.");
                    return Page();
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
                    Type = Input.Type,
                    FacultyId = Input.FacultyId,
                    Responsible = currentUser?.FullName ?? User.Identity?.Name ?? "Sistema"
                };

                _context.Managements.Add(management);

                var closedCount = 0;
                if (management.Status == ManagementStatus.Active)
                {
                    var activationResult = await _managementActivation.ActivateAsync(management);
                    closedCount = activationResult.ClosedCount;
                }

                await _context.SaveChangesAsync();
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
                return RedirectToPage("/Error", new
                {
                    module = "Gestion L-48",
                    entityId = $"{Input.Type} {Input.Year}-{Input.Semester}",
                    message = ex.Message,
                    returnUrl = Url.Page("./Create", new { type = Input.Type.ToString() }),
                    listUrl = Url.Page("./Index", new { Type = Input.Type.ToString() })
                });
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
