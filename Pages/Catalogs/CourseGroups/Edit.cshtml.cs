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

        public SelectList ManagementOptions { get; set; } = new(Enumerable.Empty<Management>(), "Id", "Code");
        public SelectList TeacherOptions { get; set; } = new(Enumerable.Empty<Intern>(), "Id", "Name");
        public SelectList CareerOptions { get; set; } = new(Enumerable.Empty<CareerEntity>(), "Id", "Name");
        public SelectList LaboratoryOptions { get; set; } = new(Enumerable.Empty<Laboratory>(), "Id", "Name");

        public List<SubjectOptionItem> SubjectOptions { get; set; } = new();

        // Datos para panel de auditoría (no se bindean).
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string? CreatedByName { get; set; }
        public string? ModifiedByName { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El código del grupo es obligatorio")]
            [StringLength(50, ErrorMessage = "El código no puede superar los 50 caracteres")]
            [Display(Name = "Código")]
            public string Code { get; set; } = string.Empty;

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

            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
        }

        public class SubjectOptionItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? CareerId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var group = await _context.CourseGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            Input = new InputModel
            {
                Id = group.Id,
                Code = group.Code,
                ManagementId = group.ManagementId,
                SubjectId = group.SubjectId,
                Name = group.Name,
                TeacherPersonId = group.TeacherPersonId,
                LaboratoryId = group.LaboratoryId,
                Weekday = group.Weekday,
                StartTime = group.StartTime?.ToString(@"hh\:mm", System.Globalization.CultureInfo.InvariantCulture),
                EndTime = group.EndTime?.ToString(@"hh\:mm", System.Globalization.CultureInfo.InvariantCulture),
                StudentCount = group.StudentCount,
                IsDemonstrative = group.IsDemonstrative,
                ScalingFactor = group.ScalingFactor,
                Status = group.Status
            };

            await LoadAuditAsync(group);
            await LoadListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Cargar el original con tracking para modificar.
            var groupToUpdate = await _context.CourseGroups
                .AsTracking()
                .FirstOrDefaultAsync(g => g.Id == Input.Id);
            if (groupToUpdate == null) return NotFound();

            // Parsear horas a TimeSpan (la fuente HTML5 time envía HH:mm).
            if (!TryParseTime(Input.StartTime, out var startTime))
                ModelState.AddModelError("Input.StartTime", "Formato de hora de inicio inválido (use HH:mm).");
            if (!TryParseTime(Input.EndTime, out var endTime))
                ModelState.AddModelError("Input.EndTime", "Formato de hora de fin inválido (use HH:mm).");

            if (!ModelState.IsValid)
            {
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Paridad de horario: si una viene vacía y la otra no, exigir paridad.
            if ((Input.StartTime is { Length: > 0 }) != (Input.EndTime is { Length: > 0 }))
            {
                ModelState.AddModelError("Input.EndTime", "Si define un horario, debe registrar hora de inicio y de fin.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Validación adicional de horario
            if (startTime.HasValue && endTime.HasValue && endTime <= startTime)
            {
                ModelState.AddModelError("Input.EndTime", "La hora de fin debe ser posterior a la hora de inicio.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Normalización
            var codeNormalized = Regex.Replace(Input.Code.Trim().ToUpper(), @"\s+", "");
            var nameNormalized = Regex.Replace(Input.Name.Trim().ToUpper(), @"\s+", "");

            // Validación de FK: gestión
            var management = await _context.Managements.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == Input.ManagementId && m.Status != ManagementStatus.Deleted && m.Semester > 0);
            if (management == null)
            {
                ModelState.AddModelError("Input.ManagementId", "La gestión seleccionada no existe o no está disponible.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Validación de FK: materia activa
            var subject = await _context.Subjects.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == Input.SubjectId && s.Status == GeneralStatus.Activo);
            if (subject == null)
            {
                ModelState.AddModelError("Input.SubjectId", "La materia seleccionada no existe o no está activa.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Validación de FK: docente activo
            var teacher = await _context.Interns.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == Input.TeacherPersonId
                                          && i.Category == PersonCategory.Docente
                                          && i.Status == GeneralStatus.Activo
                                          && i.InternStatus == GeneralStatus.Activo);
            if (teacher == null)
            {
                ModelState.AddModelError("Input.TeacherPersonId", "El docente seleccionado no existe o no está activo.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Validación de FK: laboratorio (si fue enviado)
            if (Input.LaboratoryId.HasValue)
            {
                var laboratory = await _context.Laboratories.AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == Input.LaboratoryId && l.Status == GeneralStatus.Activo);
                if (laboratory == null)
                {
                    ModelState.AddModelError("Input.LaboratoryId", "El laboratorio seleccionado no existe o no está activo.");
                    await LoadAuditAsync(groupToUpdate);
                    await LoadListsAsync();
                    return Page();
                }
            }

            // Unicidad: Code (excluyendo el actual)
            var codeExists = await _context.CourseGroups
                .IgnoreQueryFilters()
                .AnyAsync(g => g.Id != Input.Id
                               && g.Code == codeNormalized
                               && g.Status != GeneralStatus.Eliminado);
            if (codeExists)
            {
                ModelState.AddModelError("Input.Code", "Ya existe otro grupo con este código en el sistema institucional.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Unicidad: (ManagementId, SubjectId, Name) (excluyendo el actual)
            var nameExists = await _context.CourseGroups
                .IgnoreQueryFilters()
                .AnyAsync(g => g.Id != Input.Id
                               && g.ManagementId == Input.ManagementId
                               && g.SubjectId == Input.SubjectId
                               && g.Name.Trim().ToUpper() == nameNormalized
                               && g.Status != GeneralStatus.Eliminado);
            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe otro grupo con este nombre para la gestión y materia seleccionadas.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Entity Mapping (preserva audit CreatedDate/CreatedById)
            groupToUpdate.Code = Input.Code.Clean().ToUpper();
            groupToUpdate.ManagementId = Input.ManagementId!.Value;
            groupToUpdate.SubjectId = Input.SubjectId!.Value;
            groupToUpdate.Name = Input.Name.Clean();
            groupToUpdate.TeacherPersonId = Input.TeacherPersonId!.Value;
            groupToUpdate.LaboratoryId = Input.LaboratoryId;
            groupToUpdate.Weekday = Input.Weekday;
            groupToUpdate.StartTime = startTime;
            groupToUpdate.EndTime = endTime;
            groupToUpdate.StudentCount = Input.StudentCount;
            groupToUpdate.IsDemonstrative = Input.IsDemonstrative;
            groupToUpdate.ScalingFactor = Input.ScalingFactor;
            groupToUpdate.Status = Input.Status;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Grupo '{groupToUpdate.Name}' actualizado correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseGroupExists(Input.Id)) return NotFound();
                throw;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al actualizar grupo {Id}", Input.Id);
                ModelState.AddModelError(string.Empty, "Ya existe un grupo con ese código o combinación de gestión/materia/nombre.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadAuditAsync(CourseGroup group)
        {
            CreatedDate = group.CreatedDate;
            LastModifiedDate = group.LastModifiedDate;

            if (group.CreatedById.HasValue)
            {
                CreatedByName = await _context.Users.AsNoTracking()
                    .Where(u => u.Id == group.CreatedById.Value)
                    .Select(u => u.FullName ?? u.UserName)
                    .FirstOrDefaultAsync();
            }
            if (group.ModifiedById.HasValue)
            {
                ModifiedByName = await _context.Users.AsNoTracking()
                    .Where(u => u.Id == group.ModifiedById.Value)
                    .Select(u => u.FullName ?? u.UserName)
                    .FirstOrDefaultAsync();
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

        private bool CourseGroupExists(int id)
        {
            return _context.CourseGroups.Any(e => e.Id == id);
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