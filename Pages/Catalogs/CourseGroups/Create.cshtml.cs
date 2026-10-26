using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
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
        private readonly IAcademicManagementService _managements;
        private readonly IGroupScheduleService _schedules;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            IAcademicManagementService managements,
            IGroupScheduleService schedules,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _managements = managements;
            _schedules = schedules;
            _logger = logger;
        }

        /// <summary>Catálogo de periodos de clase (filas de la grilla de horario).</summary>
        public IReadOnlyList<ClassPeriod> Periods { get; private set; } = [];

        public GroupScheduleField ScheduleField => new(Periods, Input.SlotsJson, "Input.SlotsJson");

        public SelectList ManagementOptions { get; set; } = new(Enumerable.Empty<Management>(), "Id", "Code");
        public SelectList TeacherOptions { get; set; } = new(Enumerable.Empty<Intern>(), "Id", "Name");
        public SelectList CareerOptions { get; set; } = new(Enumerable.Empty<CareerEntity>(), "Id", "Name");
        public SelectList LaboratoryOptions { get; set; } = new(Enumerable.Empty<Laboratory>(), "Id", "Name");

        /// <summary>Lista de materias activas con su CareerId para soportar el filtro opcional por carrera.</summary>
        public List<SubjectOptionItem> SubjectOptions { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        /// <summary>Gestión pedida por query (pestañas de Materias); vacío = la gestión actual.</summary>
        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        /// <summary>Materia pedida por query (crear grupo desde el horario de la materia).</summary>
        [BindProperty(SupportsGet = true)]
        public int? SubjectId { get; set; }

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

            /// <summary>Horario semanal por periodos: JSON [{"weekday":1,"order":3}, ...].</summary>
            [Display(Name = "Horario de clases")]
            public string? SlotsJson { get; set; }

            [Range(0, 1000, ErrorMessage = "El número de estudiantes debe estar entre 0 y 1000")]
            [Display(Name = "Estudiantes")]
            public int? StudentCount { get; set; }

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

        /// <summary>Los grupos se registran completos (carrera + semestre + letra, con todas sus materias).</summary>
        public IActionResult OnGet() =>
            RedirectToPage("/Catalogs/StudentGroups/Create", new { managementId = ManagementId, subjectId = SubjectId });

        /// <summary>Formulario anterior (grupo de una sola materia); se conserva por compatibilidad del POST.</summary>
        public async Task<IActionResult> OnGetLegacyAsync()
        {
            // ?managementId= (pestañas de Materias) preselecciona esa gestión; si falta, la gestión actual.
            var management = await _managements.ResolveAsync(ManagementId);
            if (ManagementId.HasValue && management == null)
            {
                TempData.Error("La gestión solicitada no existe.");
                return RedirectToPage("./Index");
            }
            Input.ManagementId ??= management?.Id;

            // ?subjectId= (crear grupo desde el horario de una materia) preselecciona la materia.
            if (SubjectId > 0)
            {
                Input.SubjectId ??= SubjectId;
            }

            await LoadListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Horario por periodos (grilla día × periodo).
            var slots = GroupScheduleField.Parse(Input.SlotsJson);
            if (slots == null)
                ModelState.AddModelError("Input.SlotsJson", "El horario enviado no es válido: vuelva a marcar los periodos.");

            if (!ModelState.IsValid)
            {
                await LoadListsAsync();
                return Page();
            }

            // Normalización
            var nameNormalized = Regex.Replace(Input.Name.Trim().ToUpper(), @"\s+", "");

            // Validación de FK: gestión válida y no Deleted
            var management = await _context.Managements.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == Input.ManagementId && (m.Status == ManagementStatus.Active || m.Status == ManagementStatus.Inactive) && m.Semester > 0 && m.Type != ManagementType.Corrective);
            if (management == null)
            {
                ModelState.AddModelError("Input.ManagementId", "La gestión seleccionada no existe o está completada (solo lectura).");
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
                               && g.Name.Replace(" ", "").ToUpper() == nameNormalized
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
                StudentCount = Input.StudentCount,
                ScalingFactor = Input.ScalingFactor,
                Status = GeneralStatus.Activo
            };

            _context.CourseGroups.Add(courseGroup);

            try
            {
                // Grupo y horario se guardan juntos: si el horario choca con otra materia del mismo cohorte, no se crea nada.
                await using var transaction = await _context.Database.BeginTransactionAsync();
                await _context.SaveWithCodeAsync(
                    _context.CourseGroups.IgnoreQueryFilters().Select(x => (string?)x.Code),
                    CatalogCodes.CourseGroup,
                    code => courseGroup.Code = code);

                if (slots!.Count > 0)
                {
                    var scheduleError = await _schedules.SaveSubjectScheduleAsync(
                        courseGroup.SubjectId, courseGroup.ManagementId,
                        new Dictionary<int, IReadOnlyCollection<WeekSlot>> { [courseGroup.Id] = slots });
                    if (scheduleError != null)
                    {
                        await transaction.RollbackAsync();
                        _context.ChangeTracker.Clear();
                        ModelState.AddModelError("Input.SlotsJson", scheduleError);
                        await LoadListsAsync();
                        return Page();
                    }
                }

                await transaction.CommitAsync();
                TempData.Success($"Grupo '{courseGroup.Code}' ({courseGroup.Name}) registrado exitosamente.");
                return RedirectToPage("./Index", new { ManagementFilter = courseGroup.ManagementId });
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
                .Where(m => m.Status != ManagementStatus.Deleted && m.Semester > 0 && m.Type != ManagementType.Corrective)
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

            // Solo carreras con materias activas: filtrar por una carrera vacía dejaba la lista de materias sin opciones.
            var careerIds = subjects.Where(s => s.CareerId.HasValue).Select(s => s.CareerId!.Value).Distinct().ToList();
            var careers = await _context.Careers.AsNoTracking()
                .Where(c => c.Status == GeneralStatus.Activo && careerIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            Periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();

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
    }
}