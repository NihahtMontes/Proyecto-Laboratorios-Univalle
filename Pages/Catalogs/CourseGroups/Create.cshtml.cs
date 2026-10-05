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

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.CourseGroups
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

        public SelectList ManagementOptions { get; set; } = new(Enumerable.Empty<Management>(), "Id", "Code");
        public SelectList TeacherOptions { get; set; } = new(Enumerable.Empty<Intern>(), "Id", "Name");
        public SelectList CareerOptions { get; set; } = new(Enumerable.Empty<CareerEntity>(), "Id", "Name");
        public SelectList LaboratoryOptions { get; set; } = new(Enumerable.Empty<Laboratory>(), "Id", "Name");

        /// <summary>Lista de materias activas con su CareerId para soportar el filtro opcional por carrera.</summary>
        public List<SubjectOptionItem> SubjectOptions { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Debe seleccionar la gestión")]
            [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una gestión válida")]
            [Display(Name = "Gestión")]
            public int? ManagementId { get; set; }

            [Required(ErrorMessage = "Debe seleccionar la materia")]
            [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una materia válida")]
            [Display(Name = "Materia")]
            public int? SubjectId { get; set; }

            [Required(ErrorMessage = "El nombre del grupo es obligatorio")]
            [StringLength(20, ErrorMessage = "El nombre no puede superar los 20 caracteres")]
            [Display(Name = "Nombre")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Debe seleccionar el docente")]
            [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un docente válido")]
            [Display(Name = "Docente")]
            public int? TeacherPersonId { get; set; }

            [Display(Name = "Laboratorio")]
            public int? LaboratoryId { get; set; }

            [Range(1, 6, ErrorMessage = "El día debe estar entre 1 (Lunes) y 6 (Sábado)")]
            [Display(Name = "Día")]
            public int? Weekday { get; set; }

            [Display(Name = "Hora de inicio")]
            public string? StartTime { get; set; }

            [Display(Name = "Hora de fin")]
            public string? EndTime { get; set; }

            [Range(0, 1000, ErrorMessage = "El número de estudiantes debe estar entre 0 y 1000")]
            [Display(Name = "Estudiantes")]
            public int? StudentCount { get; set; }

            [Display(Name = "Demostrativa")]
            public bool IsDemonstrative { get; set; }

            [Range(0, 1000, ErrorMessage = "El factor debe estar entre 0 y 1000")]
            [Display(Name = "Factor")]
            public int? ScalingFactor { get; set; }
        }

        public class SubjectOptionItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? CareerId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Parsear horas a TimeSpan (la fuente HTML5 time envía HH:mm).
            if (!TryParseTime(Input.StartTime, out var startTime))
                ModelState.AddModelError("Input.StartTime", "Formato de hora de inicio inválido (use HH:mm).");
            if (!TryParseTime(Input.EndTime, out var endTime))
                ModelState.AddModelError("Input.EndTime", "Formato de hora de fin inválido (use HH:mm).");

            if (!ModelState.IsValid)
            {
                await LoadListsAsync();
                return Page();
            }

            // Si ambas vienen vacías, son nulas. Si una viene y la otra no, exigimos paridad.
            if ((Input.StartTime is { Length: > 0 }) != (Input.EndTime is { Length: > 0 }))
            {
                ModelState.AddModelError("Input.EndTime", "Si define un horario, debe registrar hora de inicio y de fin.");
                await LoadListsAsync();
                return Page();
            }

            // Validación adicional de horario (check constraint ya valida, pero mostramos error claro)
            if (startTime.HasValue && endTime.HasValue && endTime <= startTime)
            {
                ModelState.AddModelError("Input.EndTime", "La hora de fin debe ser posterior a la hora de inicio.");
                await LoadListsAsync();
                return Page();
            }

            // Normalización
            var nameNormalized = Regex.Replace(Input.Name.Trim().ToUpper(), @"\s+", "");

            // Validación de FK: gestión válida y no Deleted
            var management = await _context.Managements.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == Input.ManagementId && m.Status != ManagementStatus.Deleted && m.Semester > 0);
            if (management == null)
            {
                ModelState.AddModelError("Input.ManagementId", "La gestión seleccionada no existe o no está disponible.");
                await LoadListsAsync();
                return Page();
            }

            // Validación de FK: materia activa
            var subject = await _context.Subjects.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == Input.SubjectId && s.Status == GeneralStatus.Activo);
            if (subject == null)
            {
                ModelState.AddModelError("Input.SubjectId", "La materia seleccionada no existe o no está activa.");
                await LoadListsAsync();
                return Page();
            }

            // Validación de FK: docente activo (Person en categoría Docente, status activos)
            var teacher = await _context.Interns.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == Input.TeacherPersonId
                                          && i.Category == PersonCategory.Docente
                                          && i.Status == GeneralStatus.Activo
                                          && i.InternStatus == GeneralStatus.Activo);
            if (teacher == null)
            {
                ModelState.AddModelError("Input.TeacherPersonId", "El docente seleccionado no existe o no está activo.");
                await LoadListsAsync();
                return Page();
            }

            // Validación de FK: laboratorio activo (si fue enviado)
            if (Input.LaboratoryId.HasValue)
            {
                var laboratory = await _context.Laboratories.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == Input.LaboratoryId && l.Status == GeneralStatus.Activo);
                if (laboratory == null)
                {
                    ModelState.AddModelError("Input.LaboratoryId", "El laboratorio seleccionado no existe o no está activo.");
                    await LoadListsAsync();
                    return Page();
                }
            }

            // Unicidad: (ManagementId, SubjectId, Name) entre no eliminados
            var nameExists = await _context.CourseGroups
                .IgnoreQueryFilters()
                .AnyAsync(g => g.ManagementId == Input.ManagementId
                               && g.SubjectId == Input.SubjectId
                               && g.Name.Trim().ToUpper() == nameNormalized
                               && g.Status != GeneralStatus.Eliminado);
            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe un grupo con este nombre para la gestión y materia seleccionadas.");
                await LoadListsAsync();
                return Page();
            }

            var courseGroup = new CourseGroup
            {
                Code = string.Empty,
                ManagementId = Input.ManagementId!.Value,
                SubjectId = Input.SubjectId!.Value,
                Name = Input.Name.Clean(),
                TeacherPersonId = Input.TeacherPersonId!.Value,
                LaboratoryId = Input.LaboratoryId,
                Weekday = Input.Weekday,
                StartTime = startTime,
                EndTime = endTime,
                StudentCount = Input.StudentCount,
                IsDemonstrative = Input.IsDemonstrative,
                ScalingFactor = Input.ScalingFactor,
                Status = GeneralStatus.Activo
            };

            _context.CourseGroups.Add(courseGroup);

            try
            {
                await _context.SaveWithCodeAsync(
                    _context.CourseGroups.IgnoreQueryFilters().Select(x => (string?)x.Code),
                    CatalogCodes.CourseGroup,
                    code => courseGroup.Code = code);
                TempData.Success($"Grupo '{courseGroup.Code}' ({courseGroup.Name}) registrado exitosamente.");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al crear grupo {Name}", courseGroup.Name);
                ModelState.AddModelError(string.Empty, "Ya existe un grupo con esa combinación de gestión/materia/nombre.");
                await LoadListsAsync();
                return Page();
            }
        }

        private async Task LoadListsAsync()
        {
            var managements = await _context.Managements.AsNoTracking()
                .Where(m => m.Status != ManagementStatus.Deleted && m.Semester > 0)
                .OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester)
                .Select(m => new { m.Id, m.Code })
                .ToListAsync();

            var subjects = await _context.Subjects.AsNoTracking()
                .Where(s => s.Status == GeneralStatus.Activo)
                .OrderBy(s => s.Name)
                .Select(s => new SubjectOptionItem { Id = s.Id, Name = s.Name, CareerId = s.CareerId })
                .ToListAsync();

            var teachers = await _context.Interns.AsNoTracking()
                .Where(i => i.Category == PersonCategory.Docente
                            && i.Status == GeneralStatus.Activo
                            && i.InternStatus == GeneralStatus.Activo)
                .OrderBy(i => i.Name)
                .Select(i => new { i.Id, i.Name })
                .ToListAsync();

            var careers = await _context.Careers.AsNoTracking()
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            var laboratories = await _context.Laboratories.AsNoTracking()
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Name)
                .Select(l => new { l.Id, Name = (l.Code + " · " + l.Name) })
                .ToListAsync();

            ManagementOptions = new SelectList(managements, "Id", "Code", Input.ManagementId);
            SubjectOptions = subjects;
            TeacherOptions = new SelectList(teachers, "Id", "Name", Input.TeacherPersonId);
            CareerOptions = new SelectList(careers, "Id", "Name");
            LaboratoryOptions = new SelectList(laboratories, "Id", "Name", Input.LaboratoryId);
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

        private static bool TryParseTime(string? value, out TimeSpan? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (TimeSpan.TryParseExact(value, [@"hh\:mm", @"h\:mm", @"hh\:mm\:ss"],
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                result = parsed;
                return true;
            }
            if (TimeSpan.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out parsed))
            {
                result = parsed;
                return true;
            }
            return false;
        }
    }
}