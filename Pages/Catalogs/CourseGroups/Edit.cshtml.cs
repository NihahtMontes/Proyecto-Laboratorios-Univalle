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
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly IGroupScheduleService _schedules;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            IGroupScheduleService schedules,
            ILogger<EditModel> logger)
        {
            _context = context;
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

            /// <summary>Código asignado por el sistema. Solo se conserva para mostrarlo en la UI.</summary>
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

            /// <summary>Horario semanal por periodos: JSON [{"weekday":1,"order":3}, ...].</summary>
            [Display(Name = "Horario de clases")]
            public string? SlotsJson { get; set; }

            [Range(0, 1000, ErrorMessage = "El número de estudiantes debe estar entre 0 y 1000")]
            [Display(Name = "Estudiantes")]
            public int? StudentCount { get; set; }

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

            if (group.StudentGroupId is int studentGroupId)
            {
                TempData.Info($"La materia es parte del grupo {group.Name}: su docente y datos se editan desde el grupo.");
                return RedirectToPage("/Catalogs/StudentGroups/Edit", new { id = studentGroupId });
            }

            Input = new InputModel
            {
                Id = group.Id,
                Code = group.Code,
                ManagementId = group.ManagementId,
                SubjectId = group.SubjectId,
                Name = group.Name,
                TeacherPersonId = group.TeacherPersonId,
                LaboratoryId = group.LaboratoryId,
                StudentCount = group.StudentCount,
                ScalingFactor = group.ScalingFactor,
                Status = group.Status
            };

            await LoadAuditAsync(group);
            await LoadListsAsync();

            // Horario por periodos; un horario antiguo por horas (día + inicio/fin) se muestra convertido
            // a los periodos que cubre y se guarda así al confirmar.
            var slots = (await _schedules.GetGroupSlotsAsync(group.Id)).ToList();
            if (slots.Count == 0 && group.Weekday is >= 1 and <= 6 && group.StartTime.HasValue && group.EndTime.HasValue)
            {
                slots = Periods
                    .Where(p => p.StartTime < group.EndTime.Value && p.EndTime > group.StartTime.Value)
                    .Select(p => new WeekSlot(group.Weekday.Value, p.Order))
                    .ToList();
            }
            Input.SlotsJson = GroupScheduleField.Serialize(slots);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Cargar el original con tracking para modificar.
            var groupToUpdate = await _context.CourseGroups
                .AsTracking()
                .FirstOrDefaultAsync(g => g.Id == Input.Id);
            if (groupToUpdate == null) return NotFound();

            // Una materia que forma parte de un grupo de estudiantes se edita desde el grupo (igual que el GET):
            // un POST directo la desincronizaba de su grupo.
            if (groupToUpdate.StudentGroupId is int linkedGroupId)
            {
                TempData.Info($"La materia es parte del grupo {groupToUpdate.Name}: su docente y datos se editan desde el grupo.");
                return RedirectToPage("/Catalogs/StudentGroups/Edit", new { id = linkedGroupId });
            }

            // El código no se envía desde el formulario: repoblar desde BD para mantenerlo visible.
            Input.Code = groupToUpdate.Code;

            // Horario por periodos (grilla día × periodo).
            var slots = GroupScheduleField.Parse(Input.SlotsJson);
            if (slots == null)
                ModelState.AddModelError("Input.SlotsJson", "El horario enviado no es válido: vuelva a marcar los periodos.");

            // El estado Eliminado solo se asigna con la acción de baja (antes un POST manual lo aceptaba y el registro desaparecía).
            if (Input.Status == GeneralStatus.Eliminado)
            {
                ModelState.AddModelError("Input.Status", "El estado Eliminado solo se gestiona desde la opción Dar de Baja.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Normalización
            var nameNormalized = Regex.Replace(Input.Name.Trim().ToUpper(), @"\s+", "");

            // Validación de FK: gestión
            var management = await _context.Managements.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == Input.ManagementId && (m.Status == ManagementStatus.Active || m.Status == ManagementStatus.Inactive) && m.Semester > 0 && m.Type != ManagementType.Corrective);
            if (management == null)
            {
                ModelState.AddModelError("Input.ManagementId", "La gestión seleccionada no existe o está completada (solo lectura).");
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

            // Unicidad: (ManagementId, SubjectId, Name) (excluyendo el actual)
            var nameExists = await _context.CourseGroups
                .IgnoreQueryFilters()
                .AnyAsync(g => g.Id != Input.Id
                               && g.ManagementId == Input.ManagementId
                               && g.SubjectId == Input.SubjectId
                               && g.Name.Replace(" ", "").ToUpper() == nameNormalized
                               && g.Status != GeneralStatus.Eliminado);
            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe otro grupo con este nombre para la gestión y materia seleccionadas.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            // Entity Mapping (preserva audit CreatedDate/CreatedById)
            // Code no se modifica: lo asigna el sistema.
            groupToUpdate.ManagementId = Input.ManagementId!.Value;
            groupToUpdate.SubjectId = Input.SubjectId!.Value;
            groupToUpdate.Name = Input.Name.Clean();
            groupToUpdate.TeacherPersonId = Input.TeacherPersonId!.Value;
            groupToUpdate.LaboratoryId = Input.LaboratoryId;
            groupToUpdate.StudentCount = Input.StudentCount;
            groupToUpdate.ScalingFactor = Input.ScalingFactor;
            groupToUpdate.Status = Input.Status;

            // El horario vive en los periodos: el día/hora antiguos dejan de usarse para no mostrar dos horarios.
            var updatesSchedule = Input.Status == GeneralStatus.Activo;
            if (updatesSchedule)
            {
                groupToUpdate.Weekday = null;
                groupToUpdate.StartTime = null;
                groupToUpdate.EndTime = null;
            }

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                await _context.SaveChangesAsync();

                // Un grupo inactivo conserva su horario tal cual (el servicio solo edita grupos activos).
                if (updatesSchedule)
                {
                    var scheduleError = await _schedules.SaveSubjectScheduleAsync(
                        groupToUpdate.SubjectId, groupToUpdate.ManagementId,
                        new Dictionary<int, IReadOnlyCollection<WeekSlot>> { [groupToUpdate.Id] = slots! });
                    if (scheduleError != null)
                    {
                        await transaction.RollbackAsync();
                        _context.ChangeTracker.Clear();
                        ModelState.AddModelError("Input.SlotsJson", scheduleError);
                        await LoadAuditAsync(groupToUpdate);
                        await LoadListsAsync();
                        return Page();
                    }
                }

                await transaction.CommitAsync();
                TempData.Success($"Grupo '{groupToUpdate.Code}' ({groupToUpdate.Name}) actualizado correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseGroupExists(Input.Id)) return NotFound();
                throw;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al actualizar grupo {Id}", Input.Id);
                ModelState.AddModelError(string.Empty, "Ya existe otro grupo con esa combinación de gestión/materia/nombre.");
                await LoadAuditAsync(groupToUpdate);
                await LoadListsAsync();
                return Page();
            }

            return RedirectToPage("./Index", new { ManagementFilter = groupToUpdate.ManagementId });
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
    }
}