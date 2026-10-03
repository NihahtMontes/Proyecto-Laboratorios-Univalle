using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 1: cabecera de la planificación L-3 persistida en L3Requests.
/// </summary>
public class PlanificacionModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public PlanificacionModel(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Id de L3Requests cuando se edita una planificación existente.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList FacultyList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList CareerList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList TeacherList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList SubjectList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList GroupList { get; private set; } = new(Enumerable.Empty<SelectListItem>());

    public string Code => Id.HasValue ? L3Codes.Format(Id.Value) : "Nueva";
    public string StatusText { get; private set; } = "Nueva planificación";
    public bool IsReadOnly { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id.HasValue)
        {
            var plan = await _context.L3Requests.AsNoTracking()
                .Include(r => r.Laboratory)
                .FirstOrDefaultAsync(r => r.Id == Id.Value);
            if (plan == null)
            {
                TempData.Error("La planificación solicitada no existe.");
                return RedirectToPage("./Index");
            }

            Input = InputModel.From(plan);
            StatusText = plan.Status;
            IsReadOnly = plan.Status != L3RequestStatus.Planned;
        }

        await LoadListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!TryParseTime(Input.StartTime, out var startTime))
            ModelState.AddModelError("Input.StartTime", "Indica una hora de inicio válida.");
        if (!TryParseTime(Input.EndTime, out var endTime))
            ModelState.AddModelError("Input.EndTime", "Indica una hora de finalización válida.");
        if (ModelState.IsValid && endTime <= startTime)
            ModelState.AddModelError("Input.EndTime", "La hora de finalización debe ser posterior a la de inicio.");

        var laboratory = await _context.Laboratories.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == Input.LaboratoryId && l.Status == GeneralStatus.Activo);
        if (laboratory == null || laboratory.FacultyId != Input.FacultyId)
            ModelState.AddModelError("Input.LaboratoryId", "El laboratorio no pertenece a la facultad seleccionada.");

        if (Input.CareerId.HasValue && !await _context.Careers.AsNoTracking()
                .AnyAsync(c => c.Id == Input.CareerId && c.FacultadId == Input.FacultyId && c.Status == GeneralStatus.Activo))
            ModelState.AddModelError("Input.CareerId", "La carrera no pertenece a la facultad seleccionada.");

        if (!await TeacherQuery().AnyAsync(t => t.Id == Input.TeacherPersonId))
            ModelState.AddModelError("Input.TeacherPersonId", "Selecciona un docente activo.");
        if (!await _context.Subjects.AsNoTracking().AnyAsync(s => s.Id == Input.SubjectId))
            ModelState.AddModelError("Input.SubjectId", "Selecciona una materia válida.");
        if (!await _context.Groups.AsNoTracking().AnyAsync(g => g.Id == Input.GroupId))
            ModelState.AddModelError("Input.GroupId", "Selecciona un grupo válido.");

        var start = Input.PracticeDate.Date + startTime;
        var end = Input.PracticeDate.Date + endTime;

        if (ModelState.IsValid)
        {
            // Choque de horario en el mismo laboratorio (planificaciones vigentes).
            var clash = await _context.L3Requests.AsNoTracking()
                .Where(r => r.Id != (Id ?? 0)
                    && r.LaboratoryId == Input.LaboratoryId
                    && r.Status != L3RequestStatus.Cancelled
                    && r.ScheduledDate < end
                    && start < (r.ScheduledEndDate ?? r.ScheduledDate.AddHours(1)))
                .Select(r => new { r.Id, r.PracticeName, r.ScheduledDate, r.ScheduledEndDate })
                .FirstOrDefaultAsync();
            if (clash != null)
            {
                ModelState.AddModelError(string.Empty,
                    $"El laboratorio ya está reservado por {L3Codes.Format(clash.Id)} ({clash.PracticeName}) " +
                    $"entre {clash.ScheduledDate:HH\\:mm} y {(clash.ScheduledEndDate ?? clash.ScheduledDate.AddHours(1)):HH\\:mm}.");
            }
        }

        L3Request? plan = null;
        if (Id.HasValue)
        {
            plan = await _context.L3Requests.AsTracking().FirstOrDefaultAsync(r => r.Id == Id.Value);
            if (plan == null)
            {
                TempData.Error("La planificación ya no existe.");
                return RedirectToPage("./Index");
            }
            if (plan.Status != L3RequestStatus.Planned)
                ModelState.AddModelError(string.Empty, "Solo se puede editar una planificación en estado Planificado.");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return Page();
        }

        var isNew = plan == null;
        plan ??= new L3Request { Status = L3RequestStatus.Planned };
        plan.LaboratoryId = Input.LaboratoryId;
        plan.TeacherPersonId = Input.TeacherPersonId;
        plan.SubjectId = Input.SubjectId;
        plan.GroupId = Input.GroupId;
        plan.PracticeName = Input.PracticeName.Trim();
        plan.AcademicWeek = Input.AcademicWeek;
        plan.ScheduledDate = start;
        plan.ScheduledEndDate = end;
        plan.Observations = string.IsNullOrWhiteSpace(Input.Observations) ? null : Input.Observations.Trim();

        if (isNew) _context.L3Requests.Add(plan);
        await _context.SaveChangesAsync();

        TempData.Success(isNew
            ? $"Planificación {L3Codes.Format(plan.Id)} creada. Ahora define sus requerimientos."
            : "Planificación actualizada correctamente.");
        return RedirectToPage("./Requerimientos", new { planId = plan.Id });
    }

    /// <summary>AJAX: planificaciones existentes para el modal de búsqueda.</summary>
    public async Task<JsonResult> OnGetSearchPlansAsync()
    {
        var plans = await _context.L3Requests.AsNoTracking()
            .OrderByDescending(p => p.ScheduledDate)
            .Take(200)
            .Select(p => new
            {
                p.Id,
                p.PracticeName,
                Lab = p.Laboratory.Name,
                Subject = p.Subject.Name,
                p.TeacherPersonId,
                p.ScheduledDate,
                p.Status
            })
            .ToListAsync();

        var teacherIds = plans.Where(p => p.TeacherPersonId.HasValue).Select(p => p.TeacherPersonId!.Value).Distinct().ToList();
        var teachers = await _context.Interns.AsNoTracking()
            .Where(i => teacherIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Name);

        return new JsonResult(plans.Select(p => new
        {
            p.Id,
            Code = L3Codes.Format(p.Id),
            p.PracticeName,
            p.Lab,
            p.Subject,
            Teacher = p.TeacherPersonId.HasValue && teachers.TryGetValue(p.TeacherPersonId.Value, out var name) ? name : "—",
            Date = p.ScheduledDate.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
            p.Status
        }));
    }

    private IQueryable<Intern> TeacherQuery() => _context.Interns.AsNoTracking()
        .Where(i => i.Category == PersonCategory.Docente
            && i.Status == GeneralStatus.Activo
            && i.InternStatus == GeneralStatus.Activo);

    private async Task LoadListsAsync()
    {
        var faculties = await _context.Faculties.AsNoTracking()
            .Where(f => f.Status == GeneralStatus.Activo).OrderBy(f => f.Name).ToListAsync();
        var careers = await _context.Careers.AsNoTracking()
            .Where(c => c.Status == GeneralStatus.Activo && (Input.FacultyId <= 0 || c.FacultadId == Input.FacultyId))
            .OrderBy(c => c.Name).ToListAsync();
        var laboratories = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo && (Input.FacultyId <= 0 || l.FacultyId == Input.FacultyId))
            .OrderBy(l => l.Name).ToListAsync();
        var teachers = await TeacherQuery().OrderBy(t => t.Name).Select(t => new { t.Id, t.Name }).ToListAsync();
        var subjects = await _context.Subjects.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        var groups = await _context.Groups.AsNoTracking().OrderBy(g => g.Name).ToListAsync();

        FacultyList = new SelectList(faculties, "Id", "Name", Input.FacultyId);
        CareerList = new SelectList(careers, "Id", "Name", Input.CareerId);
        LaboratoryList = new SelectList(laboratories, "Id", "Name", Input.LaboratoryId);
        TeacherList = new SelectList(teachers, "Id", "Name", Input.TeacherPersonId);
        SubjectList = new SelectList(subjects, "Id", "Name", Input.SubjectId);
        GroupList = new SelectList(groups, "Id", "Name", Input.GroupId);
    }

    private static bool TryParseTime(string? value, out TimeSpan result) =>
        TimeSpan.TryParseExact(value, [@"hh\:mm", @"h\:mm", @"hh\:mm\:ss"], CultureInfo.InvariantCulture, out result)
        || TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result);

    public sealed class InputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "La facultad es obligatoria")]
        public int FacultyId { get; set; }

        /// <summary>Solo filtra el formulario; L3Requests no guarda carrera.</summary>
        public int? CareerId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El laboratorio es obligatorio")]
        public int LaboratoryId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El docente es obligatorio")]
        [Display(Name = "Docente responsable")]
        public int TeacherPersonId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La materia es obligatoria")]
        public int SubjectId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El grupo es obligatorio")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "El nombre de la práctica es obligatorio"), StringLength(255)]
        [Display(Name = "Nombre de la práctica")]
        public string PracticeName { get; set; } = string.Empty;

        [Range(1, 30, ErrorMessage = "La semana académica debe estar entre 1 y 30")]
        public int AcademicWeek { get; set; } = 1;

        [Required(ErrorMessage = "La fecha es obligatoria"), DataType(DataType.Date)]
        public DateTime PracticeDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        public string StartTime { get; set; } = "08:00";

        [Required(ErrorMessage = "La hora de finalización es obligatoria")]
        public string EndTime { get; set; } = "11:00";

        [StringLength(1000)]
        public string? Observations { get; set; }

        public static InputModel From(L3Request plan) => new()
        {
            FacultyId = plan.Laboratory?.FacultyId ?? 0,
            LaboratoryId = plan.LaboratoryId,
            TeacherPersonId = plan.TeacherPersonId ?? 0,
            SubjectId = plan.SubjectId,
            GroupId = plan.GroupId,
            PracticeName = plan.PracticeName,
            AcademicWeek = plan.AcademicWeek,
            PracticeDate = plan.ScheduledDate.Date,
            StartTime = plan.ScheduledDate.ToString("HH:mm", CultureInfo.InvariantCulture),
            EndTime = (plan.ScheduledEndDate ?? plan.ScheduledDate.AddHours(1)).ToString("HH:mm", CultureInfo.InvariantCulture),
            Observations = plan.Observations
        };
    }
}

/// <summary>Formato visible del código de planificación a partir del Id persistido.</summary>
public static class L3Codes
{
    public static string Format(int id) => $"PLN-{id:00000}";
}
