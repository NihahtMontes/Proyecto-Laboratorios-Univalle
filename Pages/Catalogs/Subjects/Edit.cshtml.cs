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
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public SelectList CareerOptions { get; set; } = new(Enumerable.Empty<CareerEntity>(), "Id", "Name");

        /// <summary>Nombre de la carrera actualmente asociada a la materia (para panel lateral).</summary>
        public string? CurrentCareerName { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre de la materia es obligatorio")]
            [StringLength(255, ErrorMessage = "El nombre no puede superar los 255 caracteres")]
            [Display(Name = "Nombre de la Materia")]
            public string Name { get; set; } = string.Empty;

            /// <summary>Código asignado por el sistema. Solo se conserva para mostrarlo en la UI.</summary>
            [Display(Name = "Código")]
            public string? Code { get; set; }

            [Required(ErrorMessage = "Debe seleccionar la carrera a la que pertenece la materia")]
            [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una carrera válida")]
            [Display(Name = "Carrera")]
            public int? CareerId { get; set; }

            [Range(1, 12, ErrorMessage = "El semestre debe estar entre 1 y 12")]
            [Display(Name = "Semestre")]
            public int? Semester { get; set; }

            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // Subject no tiene query filter global: cargar incluso si está Eliminado para mostrar estado
            var subject = await _context.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            // Carrera actual para el panel lateral (excluir si fue Eliminada).
            if (subject.CareerId.HasValue)
            {
                CurrentCareerName = await _context.Careers.AsNoTracking()
                    .Where(c => c.Id == subject.CareerId.Value && c.Status != GeneralStatus.Eliminado)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();
            }

            Input = new InputModel
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code,
                CareerId = subject.CareerId,
                Semester = subject.Semester,
                Status = subject.Status
            };

            await LoadCareersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Subject no tiene query filter global: cargar el original con AsTracking para modificar
            var subjectToUpdate = await _context.Subjects
                .AsTracking()
                .FirstOrDefaultAsync(s => s.Id == Input.Id);
            if (subjectToUpdate == null) return NotFound();

            if (!ModelState.IsValid)
            {
                // El código no se envía desde el formulario: repoblar desde BD para mantenerlo visible.
                Input.Code = subjectToUpdate.Code;
                await LoadCareersAsync();
                return Page();
            }

            // Normalización
            var nameNormalized = Regex.Replace(Input.Name.Trim().ToLower(), @"\s+", " ");

            // Validación de FK: la carrera debe existir y estar Activa
            var career = await _context.Careers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == Input.CareerId && c.Status == GeneralStatus.Activo);
            if (career == null)
            {
                ModelState.AddModelError("Input.CareerId", "La carrera seleccionada no existe o no está activa.");
                await LoadCareersAsync();
                return Page();
            }

            // Unicidad: (CareerId, Name) entre no eliminados, excluyendo el actual
            var nameExists = await _context.Subjects
                .AsNoTracking()
                .AnyAsync(s => s.Id != Input.Id
                               && s.CareerId == Input.CareerId
                               && s.Name.Trim().ToLower() == nameNormalized
                               && s.Status != GeneralStatus.Eliminado);
            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe otra materia con este nombre en la carrera seleccionada.");
                await LoadCareersAsync();
                return Page();
            }

            // Entity Mapping
            subjectToUpdate.Name = Input.Name.Clean();
            // Code no se modifica: lo asigna el sistema.
            subjectToUpdate.CareerId = Input.CareerId;
            subjectToUpdate.Semester = Input.Semester;
            subjectToUpdate.Status = Input.Status;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Materia '{subjectToUpdate.Code}' ({subjectToUpdate.Name}) actualizada correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubjectExists(Input.Id)) return NotFound();
                throw;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al actualizar materia {Id}", Input.Id);
                ModelState.AddModelError(string.Empty, "Ya existe otra materia con ese nombre en la carrera seleccionada.");
                await LoadCareersAsync();
                return Page();
            }

            return RedirectToPage("./Index");
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

        private bool SubjectExists(int id)
        {
            return _context.Subjects.Any(e => e.Id == id);
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