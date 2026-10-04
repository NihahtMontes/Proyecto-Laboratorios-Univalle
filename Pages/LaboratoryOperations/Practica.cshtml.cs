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
/// Paso 1: programación de una práctica del catálogo (PracticeSchedule) dentro de la planificación activa
/// y su solicitud L-3 (L3Request), que se guardan juntas. Facultad, carrera y gestión se heredan de la planificación.
/// </summary>
public class PracticaModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planning;
    private readonly ILogger<PracticaModel> _logger;

    public PracticaModel(ApplicationDbContext context, IPlanningService planning, ILogger<PracticaModel> logger)
    {
        _context = context;
        _planning = planning;
        _logger = logger;
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
    public string? ScheduleCode { get; private set; }
    public string StatusText { get; private set; } = L3RequestStatus.Planned;
    public bool IsReadOnly { get; private set; }

    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList TeacherList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList SubjectList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList WeekList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList DayList { get; private set; } = new(Enumerable.Empty<SelectListItem>());

    /// <summary>Prácticas del catálogo de las materias listadas; la vista filtra por materia.</summary>
    public List<PracticeOption> PracticeOptions { get; private set; } = [];

    /// <summary>Grupos de materia de la gestión (sugerencias para GroupName).</summary>
    public List<GroupOption> GroupOptions { get; private set; } = [];

    public sealed record PracticeOption(int Id, int SubjectId, string Code, string Label, int RequirementCount);
    public sealed record GroupOption(int SubjectId, string Name, int? StudentCount, int? ScalingFactor);

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id.HasValue)
        {
            var practice = await _context.L3Requests.AsNoTracking()
                .Include(r => r.PracticeSchedule)
                .FirstOrDefaultAsync(r => r.Id == Id.Value);
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
            ScheduleCode = practice.PracticeSchedule?.Code;
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
        L3Request? existing = null;
        if (Id.HasValue)
        {
            existing = await _context.L3Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == Id.Value);
            if (existing == null || !existing.AcademicPlanningId.HasValue)
            {
                TempData.Error("La práctica ya no existe o no pertenece a una planificación.");
                return RedirectToPage("./Index");
            }
            PlanningId = existing.AcademicPlanningId;
            if (existing.Status != L3RequestStatus.Planned)
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

        var planning = await _context.AcademicPlannings.AsNoTracking().FirstAsync(p => p.Id == Planning.Id);
        Input.GroupName = (Input.GroupName ?? string.Empty).Trim().ToUpperInvariant();

        if (!TryParseTime(Input.StartTime, out var startTime))
            ModelState.AddModelError("Input.StartTime", "Indica una hora de inicio válida.");
        if (!TryParseTime(Input.EndTime, out var endTime))
            ModelState.AddModelError("Input.EndTime", "Indica una hora de finalización válida.");
        if (ModelState.IsValid && endTime <= startTime)
            ModelState.AddModelError("Input.EndTime", "La hora de finalización debe ser posterior a la de inicio.");

        if (!await _context.Laboratories.AsNoTracking()
                .AnyAsync(l => l.Id == Input.LaboratoryId && l.FacultyId == Planning.FacultyId && l.Status == GeneralStatus.Activo))
            ModelState.AddModelError("Input.LaboratoryId", "El laboratorio no pertenece a la facultad de la planificación.");
        if (!await TeacherQuery().AnyAsync(t => t.Id == Input.TeacherPersonId))
            ModelState.AddModelError("Input.TeacherPersonId", "Selecciona un docente activo.");
        if (!await SubjectQuery(planning.CareerId).AnyAsync(s => s.Id == Input.SubjectId))
            ModelState.AddModelError("Input.SubjectId", "Selecciona una materia activa de la carrera de la planificación.");

        var catalogPractice = await _context.Practices.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == Input.PracticeId && p.Status == GeneralStatus.Activo);
        if (catalogPractice == null || catalogPractice.SubjectId != Input.SubjectId)
            ModelState.AddModelError("Input.PracticeId", "Selecciona una práctica activa del catálogo de la materia.");

        var week = await _context.AcademicWeeks.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == Input.AcademicWeekId && w.AcademicPlanningId == Planning.Id);
        if (week == null)
            ModelState.AddModelError("Input.AcademicWeekId", "La semana no pertenece a esta planificación.");
        else if (!week.IsLective)
            ModelState.AddModelError("Input.AcademicWeekId", $"La semana {week.Number} es no lectiva{(string.IsNullOrWhiteSpace(week.Note) ? "" : $" ({week.Note})")}.");

        // Una programación por gestión + práctica + grupo (índice único).
        if (catalogPractice != null && !string.IsNullOrEmpty(Input.GroupName)
            && await _context.PracticeSchedules.AsNoTracking().AnyAsync(s =>
                s.ManagementId == planning.ManagementId && s.PracticeId == catalogPractice.Id && s.GroupName == Input.GroupName
                && s.Id != (existing != null ? existing.PracticeScheduleId ?? 0 : 0)))
            ModelState.AddModelError("Input.GroupName", $"El grupo {Input.GroupName} ya tiene programada esta práctica en la gestión.");

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

        var courseGroupId = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.ManagementId == planning.ManagementId && g.SubjectId == Input.SubjectId && g.Name == Input.GroupName)
            .Select(g => (int?)g.Id)
            .FirstOrDefaultAsync();
        var practiceLabel = PracticeLabel(catalogPractice!);
        var observations = string.IsNullOrWhiteSpace(Input.Observations) ? null : Input.Observations.Trim();
        var isNew = existing == null;
        int requestId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var request = isNew
                ? new L3Request { Status = L3RequestStatus.Planned, RequestDate = DateTime.UtcNow }
                : await _context.L3Requests.AsTracking().FirstAsync(r => r.Id == existing!.Id);
            var schedule = request.PracticeScheduleId.HasValue
                ? await _context.PracticeSchedules.AsTracking().FirstOrDefaultAsync(s => s.Id == request.PracticeScheduleId.Value)
                : null;
            var newSchedule = schedule == null;
            schedule ??= new PracticeSchedule { Code = TemporaryCode(), Status = PracticeScheduleStatus.Scheduled };

            schedule.ManagementId = planning.ManagementId;
            schedule.SubjectId = Input.SubjectId;
            schedule.PracticeId = catalogPractice!.Id;
            schedule.CourseGroupId = courseGroupId;
            schedule.GroupName = Input.GroupName;
            schedule.TeacherPersonId = Input.TeacherPersonId;
            schedule.LaboratoryId = Input.LaboratoryId;
            schedule.AcademicWeekId = week!.Id;
            schedule.WeekNumber = week.Number is >= 1 and <= 30 ? week.Number : null;
            schedule.ExecutionDate = start.Date;
            schedule.StartTime = startTime;
            schedule.EndTime = endTime;
            schedule.StudentCount = Input.StudentCount;
            schedule.ScalingFactor = Input.ScalingFactor;
            schedule.IsDemonstrative = Input.IsDemonstrative;
            if (newSchedule) _context.PracticeSchedules.Add(schedule);

            request.ManagementId = planning.ManagementId;
            request.AcademicPlanningId = planning.Id;
            request.CareerId = planning.CareerId;
            request.AcademicWeekId = week.Id;
            request.AcademicWeek = week.Number;
            request.LaboratoryId = Input.LaboratoryId;
            request.AllLaboratories = false;
            request.TeacherPersonId = Input.TeacherPersonId;
            request.SubjectId = Input.SubjectId;
            request.CourseGroupId = courseGroupId;
            request.GroupName = Input.GroupName;
            request.PracticeName = practiceLabel;
            request.PracticeNumber = catalogPractice.Number;
            request.ScheduledDate = start;
            request.ScheduledEndDate = end;
            request.StartTime = startTime;
            request.EndTime = endTime;
            request.Observations = observations;
            request.PracticeSchedule = schedule;
            if (isNew) _context.L3Requests.Add(request);

            await _context.SaveChangesAsync();
            if (newSchedule) schedule.Code = $"PRG-{schedule.Id:00000}";
            request.Code ??= L3Codes.Format(request.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            requestId = request.Id;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo guardar la práctica {PracticeId}", Id);
            ModelState.AddModelError(string.Empty, "No se pudo guardar la práctica. Verifica que el grupo no tenga ya programada esta práctica.");
            await LoadListsAsync();
            return Page();
        }

        TempData.Success(isNew
            ? $"Práctica {L3Codes.Format(requestId)} programada. Ahora define sus requerimientos."
            : "Práctica actualizada correctamente.");
        return RedirectToPage("./Requerimientos", new { planId = requestId });
    }

    public static string PracticeLabel(Practice practice)
    {
        var label = string.Join(" · ", new[] { practice.Number, practice.Description }.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (string.IsNullOrWhiteSpace(label)) label = practice.Code;
        return label.Length <= 255 ? label : label[..255];
    }

    private IQueryable<Intern> TeacherQuery() => _context.Interns.AsNoTracking()
        .Where(i => i.Category == PersonCategory.Docente
            && i.Status == GeneralStatus.Activo
            && i.InternStatus == GeneralStatus.Activo);

    private IQueryable<Subject> SubjectQuery(int careerId) => _context.Subjects.AsNoTracking()
        .Where(s => s.Status == GeneralStatus.Activo && s.CareerId == careerId);

    private async Task<bool> LoadPlanningAsync()
    {
        if (!PlanningId.HasValue) return false;
        Planning = await _planning.GetSummaryAsync(PlanningId.Value);
        return Planning != null;
    }

    private async Task LoadListsAsync()
    {
        var planning = await _context.AcademicPlannings.AsNoTracking().FirstAsync(p => p.Id == Planning!.Id);
        var laboratories = await _context.Laboratories.AsNoTracking()
            .Where(l => l.FacultyId == Planning!.FacultyId && l.Status == GeneralStatus.Activo)
            .OrderBy(l => l.Name).Select(l => new { l.Id, Name = l.Code + " · " + l.Name }).ToListAsync();
        var teachers = await TeacherQuery().OrderBy(t => t.Name).Select(t => new { t.Id, t.Name }).ToListAsync();
        var subjects = await _context.Subjects.AsNoTracking()
            .Where(s => (s.Status == GeneralStatus.Activo && s.CareerId == planning.CareerId) || s.Id == Input.SubjectId)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, Name = s.Code + " · " + s.Name })
            .ToListAsync();
        var subjectIds = subjects.Select(s => s.Id).ToList();
        var practices = await _context.Practices.AsNoTracking()
            .Where(p => subjectIds.Contains(p.SubjectId) && (p.Status == GeneralStatus.Activo || p.Id == Input.PracticeId))
            .OrderBy(p => p.Code)
            .Select(p => new { Practice = p, Requirements = p.Requirements.Count(r => !r.IsRemoved) })
            .ToListAsync();
        PracticeOptions = practices
            .Select(p => new PracticeOption(p.Practice.Id, p.Practice.SubjectId, p.Practice.Code,
                $"{p.Practice.Code} · {PracticeLabel(p.Practice)}", p.Requirements))
            .ToList();
        GroupOptions = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.ManagementId == planning.ManagementId && subjectIds.Contains(g.SubjectId) && g.Status == GeneralStatus.Activo)
            .OrderBy(g => g.Name)
            .Select(g => new GroupOption(g.SubjectId, g.Name, g.StudentCount, g.ScalingFactor))
            .ToListAsync();
        var weeks = await _context.AcademicWeeks.AsNoTracking()
            .Where(w => w.AcademicPlanningId == Planning!.Id && (w.IsLective || w.Id == Input.AcademicWeekId))
            .OrderBy(w => w.Number)
            .ToListAsync();

        LaboratoryList = new SelectList(laboratories, "Id", "Name", Input.LaboratoryId);
        TeacherList = new SelectList(teachers, "Id", "Name", Input.TeacherPersonId);
        SubjectList = new SelectList(subjects, "Id", "Name", Input.SubjectId);
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

    private static string TemporaryCode() => "TMP-" + Guid.NewGuid().ToString("N")[..20];

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

        [Range(1, int.MaxValue, ErrorMessage = "La práctica del catálogo es obligatoria")]
        [Display(Name = "Práctica")]
        public int PracticeId { get; set; }

        [Required(ErrorMessage = "El grupo es obligatorio"), StringLength(20)]
        [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "El grupo admite letras, números y guion (A1, B, LND-2...).")]
        [Display(Name = "Grupo")]
        public string GroupName { get; set; } = string.Empty;

        [Range(0, 500, ErrorMessage = "Los estudiantes deben estar entre 0 y 500")]
        [Display(Name = "Estudiantes")]
        public int? StudentCount { get; set; }

        [Range(1, 100, ErrorMessage = "El factor debe estar entre 1 y 100")]
        [Display(Name = "Estudiantes por juego (factor)")]
        public int? ScalingFactor { get; set; }

        [Display(Name = "Demostrativa")]
        public bool? IsDemonstrative { get; set; }

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
            LaboratoryId = practice.LaboratoryId ?? 0,
            TeacherPersonId = practice.TeacherPersonId,
            SubjectId = practice.SubjectId ?? 0,
            PracticeId = practice.PracticeSchedule?.PracticeId ?? 0,
            GroupName = practice.GroupName ?? string.Empty,
            StudentCount = practice.PracticeSchedule?.StudentCount,
            ScalingFactor = practice.PracticeSchedule?.ScalingFactor,
            IsDemonstrative = practice.PracticeSchedule?.IsDemonstrative,
            AcademicWeekId = practice.AcademicWeekId ?? 0,
            Weekday = Math.Max(1, AcademicWeekGenerator.WeekdayOf(practice.ScheduledDate)),
            StartTime = practice.ScheduledDate.ToString("HH:mm", CultureInfo.InvariantCulture),
            EndTime = (practice.ScheduledEndDate ?? practice.ScheduledDate.AddHours(1)).ToString("HH:mm", CultureInfo.InvariantCulture),
            Observations = practice.Observations
        };
    }
}
