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
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 1b: práctica (L3Request) dentro de una planificación académica.
/// Facultad, carrera y gestión se heredan de la planificación y no se editan aquí.
/// </summary>
public class PracticaModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planning;

    public PracticaModel(ApplicationDbContext context, IPlanningService planning)
    {
        _context = context;
        _planning = planning;
    }

    /// <summary>Id de L3Requests al editar una práctica.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PlanningId { get; set; }

    /// <summary>Precarga desde el calendario.</summary>
    [BindProperty(SupportsGet = true)]
    public int? WeekId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Day { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public PlanningSummary? Planning { get; private set; }

    /// <summary>Franja superior del wizard (planificación + práctica + paso 1).</summary>
    public PlanningContextViewModel Context => new(Planning, Id, Id.HasValue ? $"Práctica {Code}" : "Nueva práctica", 1);
    public string Code => Id.HasValue ? L3Codes.Format(Id.Value) : "Nueva práctica";
    public string StatusText { get; private set; } = L3RequestStatus.Planned;
    public bool IsReadOnly { get; private set; }

    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList TeacherList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList SubjectList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList GroupList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList WeekList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList DayList { get; private set; } = new(Enumerable.Empty<SelectListItem>());


    public async Task<IActionResult> OnGetAsync()
    {
        if (Id.HasValue)
        {
            var practice = await _context.L3Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == Id.Value);
            if (practice == null)
            {
                TempData.Error("La práctica no existe.");
                return RedirectToPage("./Index");
            }
            if (!practice.AcademicPlanningId.HasValue)
            {
                TempData.Warning("Esta práctica es anterior a las planificaciones académicas y no se puede editar aquí.");
                return RedirectToPage("./Requerimientos", new { planId = practice.Id });
            }

            PlanningId = practice.AcademicPlanningId;
            Input = InputModel.From(practice);
            StatusText = practice.Status;
            IsReadOnly = practice.Status != L3RequestStatus.Planned;
        }
        else
        {
            PlanningId ??= (await _planning.GetActiveAsync())?.Id;
            if (WeekId.HasValue) Input.AcademicWeekId = WeekId.Value;
            if (Day is >= 1 and <= 5) Input.Weekday = Day.Value;
        }

        if (!await LoadPlanningAsync())
        {
            TempData.Warning("No tiene ninguna planificación activa. Crea o activa una antes de registrar prácticas.");
            return RedirectToPage("./Index");
        }

        if (!Planning!.IsActive)
        {
            if (!Id.HasValue)
            {
                TempData.Warning("Solo se registran prácticas en la planificación activa.");
                return RedirectToPage("./Index");
            }
            IsReadOnly = true;
        }

        await LoadListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        L3Request? practice = null;
        if (Id.HasValue)
        {
            practice = await _context.L3Requests.AsTracking().FirstOrDefaultAsync(r => r.Id == Id.Value);
            if (practice == null || !practice.AcademicPlanningId.HasValue)
            {
                TempData.Error("La práctica ya no existe o no pertenece a una planificación.");
                return RedirectToPage("./Index");
            }
            PlanningId = practice.AcademicPlanningId;
            if (practice.Status != L3RequestStatus.Planned)
                ModelState.AddModelError(string.Empty, "Solo se puede editar una práctica en estado Planificado.");
        }

        if (!await LoadPlanningAsync())
        {
            TempData.Error("La planificación no existe.");
            return RedirectToPage("./Index");
        }
        if (!Planning!.IsActive)
        {
            TempData.Error("La planificación de esta práctica no está activa: solo se puede consultar.");
            return Id.HasValue ? RedirectToPage(new { id = Id }) : RedirectToPage("./Index");
        }

        if (!TryParseTime(Input.StartTime, out var startTime))
            ModelState.AddModelError("Input.StartTime", "Indica una hora de inicio válida.");
        if (!TryParseTime(Input.EndTime, out var endTime))
            ModelState.AddModelError("Input.EndTime", "Indica una hora de finalización válida.");
        if (ModelState.IsValid && endTime <= startTime)
            ModelState.AddModelError("Input.EndTime", "La hora de finalización debe ser posterior a la de inicio.");

        if (!await _context.Laboratories.AsNoTracking()
                .AnyAsync(l => l.Id == Input.LaboratoryId && l.FacultyId == Planning!.FacultyId && l.Status == GeneralStatus.Activo))
            ModelState.AddModelError("Input.LaboratoryId", "El laboratorio no pertenece a la facultad de la planificación.");
        if (!await TeacherQuery().AnyAsync(t => t.Id == Input.TeacherPersonId))
            ModelState.AddModelError("Input.TeacherPersonId", "Selecciona un docente activo.");
        if (!await _context.Subjects.AsNoTracking().AnyAsync(s => s.Id == Input.SubjectId))
            ModelState.AddModelError("Input.SubjectId", "Selecciona una materia válida.");
        if (!await _context.Groups.AsNoTracking().AnyAsync(g => g.Id == Input.GroupId))
            ModelState.AddModelError("Input.GroupId", "Selecciona un grupo válido.");

        var week = await _context.AcademicWeeks.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == Input.AcademicWeekId && w.AcademicPlanningId == Planning!.Id);
        if (week == null)
            ModelState.AddModelError("Input.AcademicWeekId", "La semana no pertenece a esta planificación.");
        else if (!week.IsLective)
            ModelState.AddModelError("Input.AcademicWeekId", $"La semana {week.Number} es no lectiva{(string.IsNullOrWhiteSpace(week.Note) ? "" : $" ({week.Note})")}.");

        DateTime start = default, end = default;
        if (ModelState.IsValid && week != null)
        {
            start = AcademicWeekGenerator.PracticeDate(week.StartDate, Input.Weekday, startTime);
            end = AcademicWeekGenerator.PracticeDate(week.StartDate, Input.Weekday, endTime);

            // Choque: mismo laboratorio con horario superpuesto (prácticas vigentes de cualquier planificación).
            var clash = await _context.L3Requests.AsNoTracking()
                .Where(r => r.Id != (Id ?? 0)
                    && r.LaboratoryId == Input.LaboratoryId
                    && r.Status != L3RequestStatus.Cancelled
                    && r.ScheduledDate < end
                    && start < (r.ScheduledEndDate ?? r.ScheduledDate.AddHours(1)))
                .Select(r => new { r.Id, r.PracticeName, r.ScheduledDate, r.ScheduledEndDate })
                .FirstOrDefaultAsync();
            if (clash != null)
                ModelState.AddModelError(string.Empty,
                    $"El laboratorio ya está reservado por {L3Codes.Format(clash.Id)} ({clash.PracticeName}) " +
                    $"entre {clash.ScheduledDate:HH\\:mm} y {(clash.ScheduledEndDate ?? clash.ScheduledDate.AddHours(1)):HH\\:mm}.");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return Page();
        }

        var isNew = practice == null;
        practice ??= new L3Request { Status = L3RequestStatus.Planned };
        practice.AcademicPlanningId = Planning!.Id;
        practice.AcademicWeekId = week!.Id;
        practice.AcademicWeek = week.Number;
        practice.LaboratoryId = Input.LaboratoryId;
        practice.TeacherPersonId = Input.TeacherPersonId;
        practice.SubjectId = Input.SubjectId;
        practice.GroupId = Input.GroupId;
        practice.PracticeName = Input.PracticeName.Trim();
        practice.ScheduledDate = start;
        practice.ScheduledEndDate = end;
        practice.Observations = string.IsNullOrWhiteSpace(Input.Observations) ? null : Input.Observations.Trim();

        if (isNew) _context.L3Requests.Add(practice);
        await _context.SaveChangesAsync();

        TempData.Success(isNew
            ? $"Práctica {L3Codes.Format(practice.Id)} registrada. Ahora define sus requerimientos."
            : "Práctica actualizada correctamente.");
        return RedirectToPage("./Requerimientos", new { planId = practice.Id });
    }

    private IQueryable<Intern> TeacherQuery() => _context.Interns.AsNoTracking()
        .Where(i => i.Category == PersonCategory.Docente
            && i.Status == GeneralStatus.Activo
            && i.InternStatus == GeneralStatus.Activo);

    private async Task<bool> LoadPlanningAsync()
    {
        if (!PlanningId.HasValue) return false;
        Planning = await _planning.GetSummaryAsync(PlanningId.Value);
        return Planning != null;
    }

    private async Task LoadListsAsync()
    {
        var laboratories = await _context.Laboratories.AsNoTracking()
            .Where(l => l.FacultyId == Planning!.FacultyId && l.Status == GeneralStatus.Activo)
            .OrderBy(l => l.Name).Select(l => new { l.Id, Name = l.Code + " · " + l.Name }).ToListAsync();
        var teachers = await TeacherQuery().OrderBy(t => t.Name).Select(t => new { t.Id, t.Name }).ToListAsync();
        var subjects = await _context.Subjects.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        var groups = await _context.Groups.AsNoTracking().OrderBy(g => g.Name).ToListAsync();
        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Where(w => w.AcademicPlanningId == Planning!.Id && (w.IsLective || w.Id == Input.AcademicWeekId))
            .OrderBy(w => w.Number)
            .ToListAsync();

        LaboratoryList = new SelectList(laboratories, "Id", "Name", Input.LaboratoryId);
        TeacherList = new SelectList(teachers, "Id", "Name", Input.TeacherPersonId);
        SubjectList = new SelectList(subjects, "Id", "Name", Input.SubjectId);
        GroupList = new SelectList(groups, "Id", "Name", Input.GroupId);
        WeekList = new SelectList(
            weeks.Select(w => new
            {
                w.Id,
                Name = $"Semana {w.Number} ({w.StartDate:dd/MM} – {w.EndDate:dd/MM}){(w.IsLective ? "" : " · No lectiva")}"
            }),
            "Id", "Name", Input.AcademicWeekId);
        DayList = new SelectList(
            Enumerable.Range(1, 5).Select(d => new { Id = d, Name = AcademicWeekGenerator.WeekdayNames[d] }),
            "Id", "Name", Input.Weekday);
    }

    private static bool TryParseTime(string? value, out TimeSpan result) =>
        TimeSpan.TryParseExact(value, [@"hh\:mm", @"h\:mm", @"hh\:mm\:ss"], CultureInfo.InvariantCulture, out result)
        || TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result);

    public sealed class InputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "El laboratorio es obligatorio")]
        [Display(Name = "Laboratorio")]
        public int LaboratoryId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El docente es obligatorio")]
        [Display(Name = "Docente responsable")]
        public int TeacherPersonId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La materia es obligatoria")]
        [Display(Name = "Materia")]
        public int SubjectId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El grupo es obligatorio")]
        [Display(Name = "Grupo")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "El nombre de la práctica es obligatorio"), StringLength(255)]
        [Display(Name = "Nombre de la práctica")]
        public string PracticeName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "La semana académica es obligatoria")]
        [Display(Name = "Semana académica")]
        public int AcademicWeekId { get; set; }

        [Range(1, 5, ErrorMessage = "Selecciona un día de lunes a viernes")]
        [Display(Name = "Día")]
        public int Weekday { get; set; } = 1;

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        [Display(Name = "Hora de inicio")]
        public string StartTime { get; set; } = "08:00";

        [Required(ErrorMessage = "La hora de finalización es obligatoria")]
        [Display(Name = "Hora de fin")]
        public string EndTime { get; set; } = "11:00";

        [StringLength(1000)]
        [Display(Name = "Observaciones")]
        public string? Observations { get; set; }

        public static InputModel From(L3Request practice) => new()
        {
            LaboratoryId = practice.LaboratoryId,
            TeacherPersonId = practice.TeacherPersonId ?? 0,
            SubjectId = practice.SubjectId,
            GroupId = practice.GroupId,
            PracticeName = practice.PracticeName,
            AcademicWeekId = practice.AcademicWeekId ?? 0,
            Weekday = Math.Max(1, AcademicWeekGenerator.WeekdayOf(practice.ScheduledDate)),
            StartTime = practice.ScheduledDate.ToString("HH:mm", CultureInfo.InvariantCulture),
            EndTime = (practice.ScheduledEndDate ?? practice.ScheduledDate.AddHours(1)).ToString("HH:mm", CultureInfo.InvariantCulture),
            Observations = practice.Observations
        };
    }
}
