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
/// Paso 1 del asistente: planificación de una sesión de laboratorio (Planning, código PRA-{Id}).
/// Se elige materia, práctica del catálogo, laboratorio, docente y horario por periodos académicos
/// (fecha + periodo inicial + cantidad); la programación del catálogo (PracticeSchedule) se guarda en la
/// misma transacción. La carrera ya no se hereda de un contenedor: sale de la materia elegida.
/// </summary>
public class PlanificacionModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly ILogger<PlanificacionModel> _logger;

    public PlanificacionModel(ApplicationDbContext context, IAcademicManagementService managements, ILogger<PlanificacionModel> logger)
    {
        _context = context;
        _managements = managements;
        _logger = logger;
    }

    /// <summary>Id de Plannings al editar una planificación.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    /// <summary>Gestión en la que se crea (en edición es la de la planificación).</summary>
    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    /// <summary>Precarga de fecha (yyyy-MM-dd), p. ej. desde el calendario.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Date { get; set; }

    /// <summary>Precarga del periodo inicial, p. ej. desde el calendario.</summary>
    [BindProperty(SupportsGet = true)]
    public int? PeriodId { get; set; }

    /// <summary>Precarga del laboratorio, p. ej. desde el calendario.</summary>
    [BindProperty(SupportsGet = true)]
    public int? LaboratoryId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public AcademicManagementSummary? Management { get; private set; }

    /// <summary>Franja superior del wizard (gestión + planificación + paso 1).</summary>
    public PlanningContextViewModel Context => new(Management, Id, Title, 1);
    public string Title => Id.HasValue ? $"Planificación {Code}" : "Nueva planificación";
    public string Code => Id.HasValue ? L3Codes.Format(Id.Value) : "—";
    public string? ScheduleCode { get; private set; }
    public string StatusText { get; private set; } = PlanningStatus.Planned;
    /// <summary>En edición: solo se modifica en estado Planificada y con gestión editable.</summary>
    public bool IsReadOnly { get; private set; }

    public List<SelectListItem> SubjectList { get; private set; } = [];
    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList TeacherList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList PeriodList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList PeriodCountList { get; private set; } = new(Enumerable.Empty<SelectListItem>());

    /// <summary>Catálogo de periodos (para el rango resultante en el cliente).</summary>
    public List<ClassPeriod> Periods { get; private set; } = [];

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
            var planning = await _context.Plannings.AsNoTracking()
                .Include(p => p.PracticeSchedule)
                .FirstOrDefaultAsync(p => p.Id == Id.Value);
            if (planning == null)
            {
                TempData.Error("La planificación no existe.");
                return RedirectToPage("./Index");
            }

            ManagementId = planning.ManagementId;
            Management = await _managements.GetSummaryAsync(planning.ManagementId);
            if (Management == null)
            {
                TempData.Warning("La gestión de esta planificación no está disponible.");
                return RedirectToPage("./Index");
            }
            Input = InputModel.From(planning);
            ScheduleCode = planning.PracticeSchedule?.Code;
            StatusText = PlanningStatus.Label(planning.Status);
            IsReadOnly = planning.Status != PlanningStatus.Planned || !Management.IsEditable;
        }
        else
        {
            Management = await _managements.ResolveAsync(ManagementId);
            if (Management == null)
            {
                TempData.Warning("No hay una gestión académica disponible. Configure la gestión antes de registrar planificaciones.");
                return RedirectToPage("./Index");
            }
            if (!Management.IsEditable)
            {
                TempData.Warning($"La gestión {Management.Code} está completada: solo se puede consultar.");
                return RedirectToPage("./Index");
            }
            ManagementId = Management.Id;

            if (!string.IsNullOrWhiteSpace(Date)
                && DateTime.TryParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                Input.Date = Date;
            if (PeriodId is > 0) Input.StartPeriodId = PeriodId.Value;
            if (LaboratoryId is > 0) Input.LaboratoryId = LaboratoryId.Value;
        }

        await LoadListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Planning? existing = null;
        if (Id.HasValue)
        {
            existing = await _context.Plannings.AsNoTracking()
                .Include(p => p.PracticeSchedule)
                .FirstOrDefaultAsync(p => p.Id == Id.Value);
            if (existing == null)
            {
                TempData.Error("La planificación ya no existe.");
                return RedirectToPage("./Index");
            }
            ManagementId = existing.ManagementId;
            ScheduleCode = existing.PracticeSchedule?.Code;
            StatusText = PlanningStatus.Label(existing.Status);
            if (existing.Status != PlanningStatus.Planned)
                ModelState.AddModelError(string.Empty, "Solo se puede editar una planificación en estado Planificada.");
        }

        Management = await _managements.ResolveAsync(ManagementId);
        if (Management == null)
        {
            TempData.Error("La gestión académica no existe.");
            return RedirectToPage("./Index");
        }
        if (!Management.IsEditable)
        {
            TempData.Error(AcademicManagementService.ReadOnlyMessage);
            return Id.HasValue ? RedirectToPage(new { id = Id }) : RedirectToPage("./Index");
        }

        Input.GroupName = (Input.GroupName ?? string.Empty).Trim().ToUpperInvariant();

        DateTime date = default;
        var hasDate = DateTime.TryParseExact(Input.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        if (!hasDate)
            ModelState.AddModelError("Input.Date", "Indica una fecha válida (lunes a sábado).");
        else if (PlanningSlot.WeekdayOf(date) == 0)
            ModelState.AddModelError("Input.Date", "Los domingos no hay actividades académicas: elige un día de lunes a sábado.");

        Periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        ClassPeriod? startPeriod = null;
        if (Periods.Count == 0)
            ModelState.AddModelError("Input.StartPeriodId", "El catálogo de periodos académicos no está configurado.");
        else
        {
            startPeriod = Periods.FirstOrDefault(p => p.Id == Input.StartPeriodId);
            if (startPeriod == null)
                ModelState.AddModelError("Input.StartPeriodId", "Selecciona el periodo de inicio.");
            else if (!PlanningSlot.Fits(startPeriod.Order, Input.PeriodCount))
                ModelState.AddModelError("Input.PeriodCount",
                    $"El bloque no cabe en la jornada: el periodo {startPeriod.Order} con {Input.PeriodCount} periodos supera el periodo {PlanningSlot.MaxPeriods}.");
        }

        var laboratory = await _context.Laboratories.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == Input.LaboratoryId && l.Status == GeneralStatus.Activo);
        if (laboratory == null)
            ModelState.AddModelError("Input.LaboratoryId", "Selecciona un laboratorio activo.");
        if (!await TeacherQuery().AnyAsync(t => t.Id == Input.TeacherPersonId))
            ModelState.AddModelError("Input.TeacherPersonId", "Selecciona un docente activo.");
        if (!await _context.Subjects.AsNoTracking().AnyAsync(s => s.Id == Input.SubjectId && s.Status == GeneralStatus.Activo))
            ModelState.AddModelError("Input.SubjectId", "Selecciona una materia activa.");

        var catalogPractice = await _context.Practices.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == Input.PracticeId && p.Status == GeneralStatus.Activo);
        if (catalogPractice == null || catalogPractice.SubjectId != Input.SubjectId)
            ModelState.AddModelError("Input.PracticeId", "Selecciona una práctica del catálogo de la materia.");

        // El día debe existir en el calendario académico de la gestión (semanas lunes a sábado).
        AcademicDay? day = null;
        var weekNumber = 0;
        if (hasDate)
        {
            var row = await _context.AcademicDays.AsNoTracking()
                .Where(d => d.Date == date && d.AcademicWeek.ManagementId == Management.Id)
                .Select(d => new { Day = d, WeekNumber = d.AcademicWeek.Number })
                .FirstOrDefaultAsync();
            if (row == null)
                ModelState.AddModelError("Input.Date",
                    $"La fecha no pertenece al calendario académico de la gestión {Management.Code}. Genere las semanas en Calendario académico.");
            else
            {
                day = row.Day;
                weekNumber = row.WeekNumber;
                if (day.Status == AcademicDayStatus.Suspended)
                    ModelState.AddModelError("Input.Date",
                        $"El día {date:dd/MM} está suspendido{(string.IsNullOrWhiteSpace(day.Reason) ? "." : $": {day.Reason.Trim()}")}");
            }
        }

        // Una programación por gestión + práctica + grupo (índice único). Si ya existe (importada del Excel o
        // liberada por otra planificación), se reutiliza; solo es un choque si otra planificación que ocupa la usa.
        int? adoptScheduleId = null;
        if (catalogPractice != null && !string.IsNullOrEmpty(Input.GroupName))
        {
            var currentScheduleId = existing?.PracticeScheduleId ?? 0;
            var currentPlanningId = Id ?? 0;
            var match = await _context.PracticeSchedules.AsNoTracking()
                .Where(s => s.ManagementId == Management.Id && s.PracticeId == catalogPractice.Id
                    && s.GroupName == Input.GroupName && s.Id != currentScheduleId)
                .Select(s => new
                {
                    s.Id,
                    UsedBy = _context.Plannings
                        .Where(r => r.PracticeScheduleId == s.Id && r.Id != currentPlanningId
                            && !PlanningStatus.NonOccupying.Contains(r.Status))
                        .Select(r => (int?)r.Id)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();
            if (match?.UsedBy is int usedBy)
                ModelState.AddModelError("Input.GroupName", $"El grupo {Input.GroupName} ya tiene planificada esta práctica en la gestión ({L3Codes.Format(usedBy)}).");
            else if (match != null)
                adoptScheduleId = match.Id;
        }

        // Choque de ambiente (provisional; TODO fase 4: servicio de ocupación por periodos).
        if (ModelState.IsValid && laboratory != null && startPeriod != null && day != null)
        {
            var lastOrder = PlanningSlot.LastOrder(startPeriod.Order, Input.PeriodCount);
            var lastPeriod = Periods.FirstOrDefault(p => p.Order == lastOrder);
            if (lastPeriod == null)
            {
                ModelState.AddModelError("Input.PeriodCount", $"El catálogo de periodos no contiene el periodo {lastOrder}.");
            }
            else
            {
                var blockStart = date.Date + startPeriod.StartTime;
                var blockEnd = date.Date + lastPeriod.EndTime;
                var sameDay = await _context.Plannings.AsNoTracking()
                    .Where(r => r.Id != (Id ?? 0)
                        && r.LaboratoryId == Input.LaboratoryId
                        && r.ScheduledDate.Date == date.Date
                        && !PlanningStatus.NonOccupying.Contains(r.Status))
                    .Select(r => new
                    {
                        r.Id, r.PracticeName, r.StartPeriodId,
                        StartOrder = r.StartPeriod != null ? r.StartPeriod.Order : 0,
                        r.PeriodCount, r.ScheduledDate, r.ScheduledEndDate, r.StartTime, r.EndTime
                    })
                    .ToListAsync();
                var hit = sameDay.FirstOrDefault(r =>
                    r.StartPeriodId.HasValue && r.StartOrder >= 1 && r.PeriodCount is >= 1
                        ? PlanningSlot.Overlaps(startPeriod.Order, Input.PeriodCount, r.StartOrder, r.PeriodCount.Value)
                        : r.ScheduledDate < blockEnd && blockStart < (r.ScheduledEndDate ?? r.ScheduledDate.AddHours(1)));
                if (hit != null)
                {
                    var from = hit.StartTime ?? hit.ScheduledDate.TimeOfDay;
                    var to = hit.EndTime ?? (hit.ScheduledEndDate ?? hit.ScheduledDate.AddHours(1)).TimeOfDay;
                    var range = hit.StartPeriodId.HasValue && hit.StartOrder >= 1 && hit.PeriodCount is >= 1
                        ? $" (periodos P{hit.StartOrder}–{PlanningSlot.LastOrder(hit.StartOrder, hit.PeriodCount.Value)}, {from:hh\\:mm} – {to:hh\\:mm})"
                        : $" ({from:hh\\:mm} – {to:hh\\:mm})";
                    ModelState.AddModelError(string.Empty,
                        $"El laboratorio ya está reservado por {L3Codes.Format(hit.Id)} ({hit.PracticeName}){range}.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return Page();
        }

        var courseGroupId = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.ManagementId == Management.Id && g.SubjectId == Input.SubjectId && g.Name == Input.GroupName)
            .Select(g => (int?)g.Id)
            .FirstOrDefaultAsync();
        var practiceLabel = PracticeLabel(catalogPractice!);
        var observations = string.IsNullOrWhiteSpace(Input.Observations) ? null : Input.Observations.Trim();
        var isNew = existing == null;
        int planningId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var planning = isNew
                ? new Planning { Status = PlanningStatus.Planned, RequestDate = DateTime.UtcNow }
                : await _context.Plannings.AsTracking().FirstAsync(p => p.Id == existing!.Id);
            // Programación existente de la gestión + práctica + grupo: se adopta (la anterior, si había, queda libre).
            var scheduleId = adoptScheduleId ?? planning.PracticeScheduleId;
            var schedule = scheduleId.HasValue
                ? await _context.PracticeSchedules.AsTracking().FirstOrDefaultAsync(s => s.Id == scheduleId.Value)
                : null;
            var newSchedule = schedule == null;
            schedule ??= new PracticeSchedule { Code = TemporaryCode(), Status = PracticeScheduleStatus.Scheduled };

            planning.ManagementId = Management.Id;
            planning.AcademicWeekId = day!.AcademicWeekId;
            planning.AcademicWeek = weekNumber;
            planning.LaboratoryId = Input.LaboratoryId;
            planning.CampusId = laboratory!.CampusId;
            planning.AllLaboratories = false;
            planning.TeacherPersonId = Input.TeacherPersonId;
            planning.SubjectId = Input.SubjectId;
            planning.CourseGroupId = courseGroupId;
            planning.GroupName = Input.GroupName;
            planning.PracticeName = practiceLabel;
            planning.PracticeNumber = catalogPractice!.Number;
            planning.Observations = observations;
            PlanningSlot.Apply(planning, date, Input.StartPeriodId, Input.PeriodCount, Periods);

            schedule.ManagementId = Management.Id;
            schedule.SubjectId = Input.SubjectId;
            schedule.PracticeId = catalogPractice!.Id;
            schedule.CourseGroupId = courseGroupId;
            schedule.GroupName = Input.GroupName;
            schedule.TeacherPersonId = Input.TeacherPersonId;
            schedule.LaboratoryId = Input.LaboratoryId;
            schedule.AcademicWeekId = day!.AcademicWeekId;
            schedule.WeekNumber = weekNumber is >= 1 and <= 30 ? weekNumber : null;
            schedule.ExecutionDate = planning.ScheduledDate.Date;
            schedule.StartTime = planning.StartTime;
            schedule.EndTime = planning.EndTime;
            // Sin dato en el formulario se conserva el de la programación (p. ej. el importado del Excel).
            schedule.StudentCount = Input.StudentCount ?? schedule.StudentCount;
            schedule.ScalingFactor = Input.ScalingFactor ?? schedule.ScalingFactor;
            schedule.IsDemonstrative = Input.IsDemonstrative;
            if (newSchedule) _context.PracticeSchedules.Add(schedule);

            planning.PracticeSchedule = schedule;
            if (isNew) _context.Plannings.Add(planning);

            await _context.SaveChangesAsync();
            if (newSchedule) schedule.Code = $"PRG-{schedule.Id:00000}";
            planning.Code ??= L3Codes.Format(planning.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            planningId = planning.Id;
        }
        catch (ArgumentException ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadListsAsync();
            return Page();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo guardar la planificación {PlanningId}", Id);
            ModelState.AddModelError(string.Empty, "No se pudo guardar la planificación. Verifica que el grupo no tenga ya programada esta práctica en la gestión.");
            await LoadListsAsync();
            return Page();
        }

        TempData.Success(isNew
            ? $"Planificación {L3Codes.Format(planningId)} registrada. Ahora define sus requerimientos."
            : "Planificación actualizada correctamente.");
        return RedirectToPage("./Requerimientos", new { planId = planningId });
    }

    /// <summary>Quien puede crear prácticas del catálogo (mismos roles que Catálogos › Prácticas › Crear).</summary>
    public bool CanCreatePractice => User.IsInRole(AuthorizationHelper.RoleAdministrator) || User.IsInRole(AuthorizationHelper.RoleSuperAdmin);

    /// <summary>AJAX: crea una práctica del catálogo para una materia activa de cualquier carrera, con gestión editable.</summary>
    public async Task<IActionResult> OnPostCreatePracticeAsync(int subjectId, string? name, string? description)
    {
        if (!CanCreatePractice)
            return new JsonResult(new { ok = false, message = "No tiene permisos para registrar prácticas del catálogo." }) { StatusCode = StatusCodes.Status403Forbidden };

        var management = await _managements.ResolveAsync(ManagementId);
        if (management == null || !management.IsEditable)
            return new JsonResult(new { ok = false, message = "Solo se registran prácticas del catálogo en una gestión activa o inactiva." }) { StatusCode = StatusCodes.Status400BadRequest };
        Management = management;
        ManagementId = management.Id;

        var cleanName = name.Clean();
        var cleanDescription = description.Clean();
        if (string.IsNullOrWhiteSpace(cleanName) || cleanName.Length > 200)
            return new JsonResult(new { ok = false, message = "El nombre de la práctica es obligatorio (máximo 200 caracteres)." }) { StatusCode = StatusCodes.Status400BadRequest };
        if (cleanDescription?.Length > 2000)
            return new JsonResult(new { ok = false, message = "La descripción no puede superar los 2000 caracteres." }) { StatusCode = StatusCodes.Status400BadRequest };

        if (!await _context.Subjects.AsNoTracking().AnyAsync(s => s.Id == subjectId && s.Status == GeneralStatus.Activo))
            return new JsonResult(new { ok = false, message = "La materia no está activa." }) { StatusCode = StatusCodes.Status400BadRequest };

        var practice = new Practice
        {
            Code = string.Empty,
            SubjectId = subjectId,
            Name = cleanName,
            Description = string.IsNullOrWhiteSpace(cleanDescription) ? null : cleanDescription,
            Status = GeneralStatus.Activo
        };
        _context.Practices.Add(practice);
        try
        {
            await _context.SaveWithCodeAsync(
                _context.Practices.IgnoreQueryFilters().Select(p => (string?)p.Code),
                CatalogCodes.Practice,
                code => practice.Code = code);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "No se pudo crear la práctica del catálogo para la materia {SubjectId}", subjectId);
            return new JsonResult(new { ok = false, message = "No se pudo registrar la práctica. Intente nuevamente." }) { StatusCode = StatusCodes.Status500InternalServerError };
        }

        return new JsonResult(new
        {
            ok = true,
            practice = new { id = practice.Id, subjectId, code = practice.Code, label = $"{practice.Code} · {PracticeLabel(practice)}", requirementCount = 0 }
        });
    }

    public static string PracticeLabel(Practice practice)
    {
        var label = string.IsNullOrWhiteSpace(practice.Name) ? practice.Code : practice.Name.Trim();
        return label.Length <= 255 ? label : label[..255];
    }

    private IQueryable<Intern> TeacherQuery() => _context.Interns.AsNoTracking()
        .Where(i => i.Category == PersonCategory.Docente
            && i.Status == GeneralStatus.Activo
            && i.InternStatus == GeneralStatus.Activo);

    private async Task LoadListsAsync()
    {
        var laboratories = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo || l.Id == Input.LaboratoryId)
            .OrderBy(l => l.Name)
            .Select(l => new { l.Id, Name = l.Code + " · " + l.Name })
            .ToListAsync();
        var teachers = await TeacherQuery().OrderBy(t => t.Name).Select(t => new { t.Id, t.Name }).ToListAsync();

        // Materias activas (cualquier carrera) con optgroup por carrera.
        var subjects = (await _context.Subjects.AsNoTracking()
                .Where(s => s.Status == GeneralStatus.Activo || s.Id == Input.SubjectId)
                .Select(s => new { s.Id, Name = s.Code + " · " + s.Name, CareerName = s.Career != null ? s.Career.Name : null })
                .ToListAsync())
            .OrderBy(s => string.IsNullOrWhiteSpace(s.CareerName) ? "zzz" : s.CareerName)
            .ThenBy(s => s.Name)
            .ToList();
        var careerGroups = new Dictionary<string, SelectListGroup>();
        SubjectList = subjects.Select(s =>
        {
            var groupName = string.IsNullOrWhiteSpace(s.CareerName) ? "Sin carrera" : s.CareerName!;
            if (!careerGroups.TryGetValue(groupName, out var group))
                careerGroups[groupName] = group = new SelectListGroup { Name = groupName };
            return new SelectListItem { Value = s.Id.ToString(), Text = s.Name, Group = group, Selected = s.Id == Input.SubjectId };
        }).ToList();

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
            .Where(g => g.ManagementId == Management!.Id && subjectIds.Contains(g.SubjectId) && g.Status == GeneralStatus.Activo)
            .OrderBy(g => g.Name)
            .Select(g => new GroupOption(g.SubjectId, g.Name, g.StudentCount, g.ScalingFactor))
            .ToListAsync();

        Periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        if (Input.StartPeriodId == 0 && Periods.Count > 0)
            Input.StartPeriodId = Periods[0].Id;
        PeriodList = new SelectList(
            Periods.Select(p => new { p.Id, Name = $"P{p.Order} · {p.StartTime:hh\\:mm} – {p.EndTime:hh\\:mm}" }),
            "Id", "Name", Input.StartPeriodId);
        PeriodCountList = new SelectList(
            Enumerable.Range(1, 12).Select(n => new { Id = n, Name = n.ToString() }),
            "Id", "Name", Input.PeriodCount);

        LaboratoryList = new SelectList(laboratories, "Id", "Name", Input.LaboratoryId);
        TeacherList = new SelectList(teachers, "Id", "Name", Input.TeacherPersonId);
    }

    private static string TemporaryCode() => "TMP-" + Guid.NewGuid().ToString("N")[..20];

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
        [Display(Name = "Práctica del catálogo")]
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
        public bool IsDemonstrative { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Indica una fecha válida.")]
        [Display(Name = "Fecha")]
        public string? Date { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El periodo de inicio es obligatorio")]
        [Display(Name = "Periodo de inicio")]
        public int StartPeriodId { get; set; }

        [Range(1, 12, ErrorMessage = "La cantidad de periodos debe estar entre 1 y 12")]
        [Display(Name = "Cantidad de periodos")]
        public int PeriodCount { get; set; } = 1;

        [StringLength(1000)]
        [Display(Name = "Observaciones")]
        public string? Observations { get; set; }

        public static InputModel From(Planning planning) => new()
        {
            LaboratoryId = planning.LaboratoryId ?? 0,
            TeacherPersonId = planning.TeacherPersonId,
            SubjectId = planning.SubjectId ?? 0,
            PracticeId = planning.PracticeSchedule?.PracticeId ?? 0,
            GroupName = planning.GroupName ?? string.Empty,
            StudentCount = planning.PracticeSchedule?.StudentCount,
            ScalingFactor = planning.PracticeSchedule?.ScalingFactor,
            IsDemonstrative = planning.PracticeSchedule?.IsDemonstrative ?? false,
            Date = planning.ScheduledDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            StartPeriodId = planning.StartPeriodId ?? 0,
            PeriodCount = Math.Max(1, planning.PeriodCount ?? 1),
            Observations = planning.Observations
        };
    }
}
