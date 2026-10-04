using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
// Evitar colisión con el namespace Pages.Career
using CareerEntity = Proyecto_Laboratorios_Univalle.Models.Career;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Subjects
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public SelectList CareerOptions { get; set; } = new(Enumerable.Empty<CareerEntity>(), "Id", "Name");

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre de la materia es obligatorio")]
            [StringLength(255, ErrorMessage = "El nombre no puede superar los 255 caracteres")]
            [Display(Name = "Nombre de la Materia")]
            public string Name { get; set; } = string.Empty;

            [StringLength(50, ErrorMessage = "El código no puede superar los 50 caracteres")]
            [Display(Name = "Código")]
            public string? Code { get; set; }

            [Required(ErrorMessage = "Debe seleccionar la carrera a la que pertenece la materia")]
            [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una carrera válida")]
            [Display(Name = "Carrera")]
            public int? CareerId { get; set; }

            [Range(1, 12, ErrorMessage = "El semestre debe estar entre 1 y 12")]
            [Display(Name = "Semestre")]
            public int? Semester { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCareersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCareersAsync();
                return Page();
            }

            // Normalización
            var nameNormalized = Regex.Replace(Input.Name.Trim().ToLower(), @"\s+", " ");
            var codeNormalized = string.IsNullOrWhiteSpace(Input.Code)
                ? string.Empty
                : Regex.Replace(Input.Code.Trim().ToUpper(), @"\s+", " ");

            // Validación de FK: la carrera debe existir y estar Activa
            var career = await _context.Careers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == Input.CareerId && c.Status == GeneralStatus.Activo);
            if (career == null)
            {
                ModelState.AddModelError("Input.CareerId", "La carrera seleccionada no existe o no está activa.");
                await LoadCareersAsync();
                return Page();
            }

            // Unicidad: Code (si no está vacío) entre no eliminados
            if (!string.IsNullOrWhiteSpace(codeNormalized))
            {
                var codeExists = await _context.Subjects
                    .AsNoTracking()
                    .AnyAsync(s => s.Code != null
                                   && s.Code.Trim().ToUpper() == codeNormalized
                                   && s.Status != GeneralStatus.Eliminado);
                if (codeExists)
                {
                    ModelState.AddModelError("Input.Code", "Ya existe una materia con este código en el sistema.");
                    await LoadCareersAsync();
                    return Page();
                }
            }

            // Unicidad: (CareerId, Name) entre no eliminados (índice único cuando CareerId no nulo)
            var nameExists = await _context.Subjects
                .AsNoTracking()
                .AnyAsync(s => s.CareerId == Input.CareerId
                               && s.Name.Trim().ToLower() == nameNormalized
                               && s.Status != GeneralStatus.Eliminado);
            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe una materia con este nombre en la carrera seleccionada.");
                await LoadCareersAsync();
                return Page();
            }

            var subject = new Subject
            {
                Name = Input.Name.Clean(),
                Code = string.IsNullOrWhiteSpace(Input.Code) ? string.Empty : Input.Code.Clean().ToUpper(),
                CareerId = Input.CareerId,
                Semester = Input.Semester,
                Status = GeneralStatus.Activo
            };

            _context.Subjects.Add(subject);

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Materia '{subject.Name}' registrada exitosamente.");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al crear materia {Name}", subject.Name);
                ModelState.AddModelError(string.Empty, "Ya existe un registro con esos datos (código o nombre duplicado).");
                await LoadCareersAsync();
                return Page();
            }
        }

        private async Task LoadCareersAsync()
        {
            var careers = await _context.Careers.AsNoTracking()
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            CareerOptions = new SelectList(careers, "Id", "Name", Input.CareerId);
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            while (inner != null)
            {
                if (inner is Microsoft.Data.SqlClient.SqlException sqlEx
                    && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    return true;
                }
                inner = inner.InnerException;
            }
            return false;
        }
    }
}