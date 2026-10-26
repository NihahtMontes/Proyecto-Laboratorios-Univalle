using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;

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
    private readonly IOccupancyService _occupancy;
    private readonly IPlanningLock _planningLock;
    private readonly IGroupScheduleService _schedules;

    public PlanificacionModel(ApplicationDbContext context, IAcademicManagementService managements, ILogger<PlanificacionModel> logger,
        IOccupancyService occupancy, IPlanningLock planningLock, IGroupScheduleService schedules)
    {
        _context = context;
        _managements = managements;
        _logger = logger;
        _occupancy = occupancy;
        _planningLock = planningLock;
        _schedules = schedules;
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
    public PlanningContextViewModel Context => new(Management, Id, Title, 1, CurrentStatus);
    /// <summary>Estado real de la planificación editada (null al crear).</summary>
    public string? CurrentStatus { get; private set; }
    public string Title => Id.HasValue ? $"Planificación {Code}" : "Nueva planificación";
    /// <summary>Code persistido de la planificación cargada (null al crear o antes de cargar).</summary>
    public string? PlanningCode { get; private set; }
    public string Code => Id.HasValue ? L3Codes.Format(Id.Value, PlanningCode) : "—";
    public string? ScheduleCode { get; private set; }
    public string StatusText { get; private set; } = PlanningStatus.Planned;
    /// <summary>En edición: solo se modifica en estado Planificada y con gestión editable.</summary>
    public bool IsReadOnly { get; private set; }

    /// <summary>
    /// True cuando una planificación aprobada sin periodo asignado puede recibir horario desde esta página
    /// (O10): estado Aprobada, StartPeriodId null y gestión editable.
    /// </summary>
    public bool CanAssignPeriod { get; private set; }

    /// <summary>Periodo inicial sugerido para la asignación de horario (null sin sugerencia).</summary>
    public int? SuggestedStartPeriodId { get; private set; }

    /// <summary>Cantidad de periodos sugerida para la asignación de horario (mínimo 1).</summary>
    public int SuggestedPeriodCount { get; private set; } = 1;

    /// <summary>Choques de docente detectados al guardar ("{Label} ({PeriodsText})"); vacío sin choques.</summary>
    public List<string> TeacherConflicts { get; private set; } = [];

    /// <summary>Huella de los choques de docente actuales: debe acompañar a AcceptTeacherConflict para insistir.</summary>
    public string? TeacherConflictFingerprint { get; private set; }

    /// <summary>Choques de docente del intento de asignación de horario (leídos de TempData en OnGet).</summary>
    public List<string> AssignTeacherConflicts { get; private set; } = [];

    /// <summary>Huella de los choques de docente del intento de asignación de horario.</summary>
    public string? AssignTeacherConflictFingerprint { get; private set; }

    /// <summary>Hora importada de la planificación sin periodo: base de la sugerencia de horario.</summary>
    private TimeSpan? _planningStartTime;
    private TimeSpan? _planningEndTime;

    public List<SelectListItem> SubjectList { get; private set; } = [];
    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList TeacherList { get; private set; } = new(Enumerable.Empty<SelectListItem>());

    /// <summary>Catálogo de periodos (para el rango resultante en el cliente).</summary>
    public List<ClassPeriod> Periods { get; private set; } = [];

    /// <summary>Prácticas del catálogo de las materias listadas; la vista filtra por materia.</summary>
    public List<PracticeOption> PracticeOptions { get; private set; } = [];

    /// <summary>Grupos de materia de la gestión (sugerencias para GroupName).</summary>
    public List<GroupOption> GroupOptions { get; private set; } = [];

    public sealed record PracticeOption(int Id, int SubjectId, string Code, string Label, int RequirementCount);
    public sealed record GroupOption(int SubjectId, string Name, int? StudentCount, int? ScalingFactor, int? Semester = null)
    {
        /// <summary>Etiqueta visible "Sem N · Grupo X" (el valor enviado sigue siendo Name).</summary>
        public string Label => GroupScheduleService.CohortLabel(Semester, Name);
    }

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
            PlanningCode = planning.Code;
            StatusText = PlanningStatus.Label(planning.Status);
            CurrentStatus = planning.Status;
            IsReadOnly = planning.Status != PlanningStatus.Planned || !Management.IsEditable;
            // O10: una aprobada sin periodo reservado puede asignar horario desde aquí (fecha y demás datos fijos).
            CanAssignPeriod = planning.Status == PlanningStatus.Approved
                && planning.StartPeriodId == null
                && Management.IsEditable;
            _planningStartTime = planning.StartTime;
            _planningEndTime = planning.EndTime;
        }
        else
        {
            Management = await _managements.ResolveAsync(ManagementId);
            if (Management == null)
            {
                TempData.Warning("No hay un semestre académico disponible. Pida al administrador que active la gestión del semestre.");
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

        if (CanAssignPeriod && Periods.Count > 0)
            (SuggestedStartPeriodId, SuggestedPeriodCount) = SuggestSlot(_planningStartTime, _planningEndTime);

        // Intento de asignación de horario rechazado por choque de docente (OnPostAssignPeriod): se
        // muestran los choques y se precarga el mismo bloque para insistir o elegir otro horario.
        if (TempData["AssignTeacherConflicts"] is string assignConflictsJson && assignConflictsJson.Length > 0)
        {
            try
            {
                AssignTeacherConflicts = JsonSerializer.Deserialize<List<string>>(assignConflictsJson) ?? [];
            }
            catch (JsonException)
            {
                AssignTeacherConflicts = [];
            }
            AssignTeacherConflictFingerprint = TempData["AssignTeacherConflictFingerprint"] as string;
            if (TempData["AssignStartPeriodId"] is int retryStartPeriodId && Periods.Any(p => p.Id == retryStartPeriodId))
                SuggestedStartPeriodId = retryStartPeriodId;
            if (TempData["AssignPeriodCount"] is int retryPeriodCount and >= 1)
                SuggestedPeriodCount = Math.Min(retryPeriodCount, PlanningSlot.MaxPeriods);
        }

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
            PlanningCode = existing.Code;
            StatusText = PlanningStatus.Label(existing.Status);
            CurrentStatus = existing.Status;
            if (existing.Status != PlanningStatus.Planned)
            {
                IsReadOnly = true;
                ModelState.AddModelError(string.Empty, "Solo se puede editar una planificación en estado Planificada.");
            }
        }

        Management = await _managements.ResolveAsync(ManagementId);
        if (Management == null)
        {
            TempData.Error("El semestre académico no existe.");
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
                        .Select(r => new { r.Id, r.Code })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();
            if (match is { UsedBy: not null })
                ModelState.AddModelError("Input.GroupName", $"El grupo {Input.GroupName} ya tiene planificada esta práctica en la gestión ({L3Codes.Format(match.UsedBy.Id, match.UsedBy.Code)}).");
            else if (match != null)
                adoptScheduleId = match.Id;
        }

        // Aforo: los estudiantes del grupo no pueden superar la capacidad del laboratorio (mismo texto y
        // criterio que la reprogramación, SlotValidationService). Sin alguno de los dos datos no se bloquea.
        if (laboratory != null)
        {
            var students = Input.StudentCount;
            if (students is null)
            {
                var capacityScheduleId = adoptScheduleId ?? existing?.PracticeScheduleId;
                students = capacityScheduleId is int scheduleId
                    ? await _context.PracticeSchedules.AsNoTracking()
                        .Where(s => s.Id == scheduleId)
                        .Select(s => s.StudentCount)
                        .FirstOrDefaultAsync()
                    : null;
            }
            if (students is null && catalogPractice != null && !string.IsNullOrEmpty(Input.GroupName))
                students = await _context.CourseGroups.AsNoTracking()
                    .Where(g => g.ManagementId == Management.Id && g.SubjectId == Input.SubjectId && g.Name == Input.GroupName)
                    .Select(g => g.StudentCount)
                    .FirstOrDefaultAsync();
            if (PlanningAssignmentRules.CapacityError(students, laboratory.Capacity, laboratory.Code) is string capacityError)
                ModelState.AddModelError("Input.StudentCount", capacityError);
        }

        // Choque de ambiente: servicio único de ocupación (mismo criterio que la reprogramación y la reposición).
        if (ModelState.IsValid && laboratory != null && startPeriod != null && day != null)
        {
            var conflicts = await _occupancy.FindConflictsAsync(Input.LaboratoryId, date.Date, startPeriod.Order, Input.PeriodCount,
                Id.HasValue ? [Id.Value] : null);
            if (conflicts.Count > 0)
                ModelState.AddModelError(string.Empty,
                    $"El laboratorio está ocupado o no queda un periodo libre entre prácticas: {string.Join("; ", conflicts.Select(c => $"{c.Label} ({c.PeriodsText})"))}.");
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

        // Horario de clases del grupo de estudiantes: el destino no puede coincidir con clases de OTRAS
        // materias del mismo grupo (las clases propias de la materia solo se sugieren, no bloquean).
        // Mismo criterio que la reprogramación (SlotValidationService → GROUP_IN_CLASS).
        // Al editar solo se revisa si cambia el horario (fecha, periodo inicial o cantidad): una clase cargada
        // después no debe impedir corregir observaciones o docente de una sesión que ya existía.
        var scheduleChanged = existing == null
            || existing.ScheduledDate.Date != date.Date
            || existing.StartPeriodId != Input.StartPeriodId
            || existing.PeriodCount != Input.PeriodCount
            || existing.CourseGroupId != courseGroupId;
        if (scheduleChanged && courseGroupId is int groupId && hasDate && startPeriod != null)
        {
            var groupCheck = await _schedules.CheckDestinationAsync(groupId, date.Date, startPeriod.Order, Input.PeriodCount);
            if (groupCheck.Collisions.Count > 0)
            {
                var subjects = groupCheck.Collisions
                    .Select(c => c.SubjectName)
                    .Distinct(StringComparer.CurrentCulture)
                    .ToList();
                ModelState.AddModelError(string.Empty,
                    $"El grupo {groupCheck.Collisions[0].GroupName} tiene clase de {string.Join(", ", subjects)} en ese horario: elija otro periodo.");
                await LoadListsAsync();
                return Page();
            }
        }

        // Choque de docente: bloquea, pero se puede insistir aceptando la advertencia (queda constancia en
        // las observaciones). Va después de los bloqueos duros (ambiente y horario del grupo) para no pedir
        // la aceptación de algo que igual se rechazaría. La huella asegura que se aceptan estos choques, no otros.
        // Al editar solo se revisa si cambia el horario o el docente: un choque que ya existía (p. ej. importado)
        // no debe impedir corregir observaciones ni sumar una constancia nueva en cada guardado.
        var teacherSlotChanged = existing == null
            || existing.ScheduledDate.Date != date.Date
            || existing.StartPeriodId != Input.StartPeriodId
            || existing.PeriodCount != Input.PeriodCount
            || existing.TeacherPersonId != Input.TeacherPersonId;
        List<string>? acceptedTeacherConflicts = null;
        if (teacherSlotChanged && ModelState.IsValid && laboratory != null && startPeriod != null && day != null)
        {
            await LoadTeacherConflictsAsync(Input.TeacherPersonId, date, startPeriod.Order, Input.PeriodCount,
                Id.HasValue ? [Id.Value] : null);
            if (TeacherConflicts.Count > 0)
            {
                if (Input.AcceptTeacherConflict && Input.TeacherConflictFingerprint == TeacherConflictFingerprint)
                    acceptedTeacherConflicts = [.. TeacherConflicts];
                else
                    ModelState.AddModelError(string.Empty,
                        $"El docente ya tiene otra práctica en esos periodos: {string.Join("; ", TeacherConflicts)}.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return Page();
        }

        var practiceLabel = PracticeLabel(catalogPractice!);
        var observations = string.IsNullOrWhiteSpace(Input.Observations) ? null : Input.Observations.Trim();
        var isNew = existing == null;
        int planningId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var resources = new List<string>
            {
                PlanningLockResources.ManagementDay(Management.Id, date),
                PlanningLockResources.LaboratoryDay(Input.LaboratoryId, date),
                PlanningLockResources.TeacherDay(Input.TeacherPersonId, date)
            };
            if (existing != null)
            {
                resources.Add(PlanningLockResources.Planning(existing.Id));
                resources.Add(PlanningLockResources.ManagementDay(Management.Id, existing.ScheduledDate.Date));
            }
            if (!await _planningLock.TryAcquireAsync(resources))
                ModelState.AddModelError(string.Empty, PlanningLockResources.BusyMessage);

            // No confiar en la consulta anterior a la transacción: suspender el día o reservar el ambiente
            // puede haber ocurrido mientras el formulario estaba abierto.
            if (ModelState.IsValid)
            {
                var currentStatus = await _context.Managements.AsNoTracking()
                    .Where(m => m.Id == Management.Id)
                    .Select(m => (ManagementStatus?)m.Status).FirstOrDefaultAsync();
                if (currentStatus is not (ManagementStatus.Active or ManagementStatus.Inactive))
                    ModelState.AddModelError(string.Empty, AcademicManagementService.ReadOnlyMessage);

                var currentDay = await _context.AcademicDays.AsNoTracking()
                    .Where(d => d.Date == date && d.AcademicWeek.ManagementId == Management.Id)
                    .Select(d => new { d.Id, d.Status, WeekNumber = d.AcademicWeek.Number })
                    .FirstOrDefaultAsync();
                if (currentDay == null || currentDay.Status != AcademicDayStatus.Enabled)
                    ModelState.AddModelError("Input.Date", "El día académico ya no está habilitado; actualice el formulario.");
                else
                {
                    day = await _context.AcademicDays.AsNoTracking().FirstAsync(d => d.Id == currentDay.Id);
                    weekNumber = currentDay.WeekNumber;
                }

                if (existing != null && !await _context.Plannings.AsNoTracking()
                    .AnyAsync(p => p.Id == existing.Id && p.Status == PlanningStatus.Planned))
                    ModelState.AddModelError(string.Empty, "La planificación ya no está en estado Planificada.");

                if (await _occupancy.FindConflictsAsync(Input.LaboratoryId, date, startPeriod!.Order, Input.PeriodCount,
                    existing != null ? [existing.Id] : null) is { Count: > 0 } conflicts)
                    ModelState.AddModelError(string.Empty,
                        $"El laboratorio se ocupó mientras se guardaba (ocupado o sin periodo libre entre prácticas): {string.Join("; ", conflicts.Select(c => $"{c.Label} ({c.PeriodsText})"))}.");

                // Choque de docente bajo bloqueo: si apareció uno nuevo la huella ya no coincide y se bloquea.
                if (teacherSlotChanged)
                    await LoadTeacherConflictsAsync(Input.TeacherPersonId, date, startPeriod.Order, Input.PeriodCount,
                        existing != null ? [existing.Id] : null);
                if (teacherSlotChanged && TeacherConflicts.Count > 0)
                {
                    if (Input.AcceptTeacherConflict && Input.TeacherConflictFingerprint == TeacherConflictFingerprint)
                        acceptedTeacherConflicts = [.. TeacherConflicts];
                    else
                        ModelState.AddModelError(string.Empty,
                            $"El docente ya tiene otra práctica en esos periodos: {string.Join("; ", TeacherConflicts)}.");
                }
            }
            if (!ModelState.IsValid)
            {
                await LoadListsAsync();
                return Page();
            }

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
            // Choque de docente aceptado: se deja constancia en las observaciones (recorta el texto previo si hace falta).
            planning.Observations = acceptedTeacherConflicts is { Count: > 0 }
                ? PlanningAssignmentRules.AppendTeacherConflictNote(observations,
                    $"Choque de docente aceptado por {User.Identity?.Name ?? "usuario"} el {DateTime.Now:dd/MM/yyyy HH:mm}: {string.Join("; ", acceptedTeacherConflicts)}")
                : observations;
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
            PlanningCode = planning.Code;
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
            ? $"Planificación {L3Codes.Format(planningId, PlanningCode)} registrada. Ahora define sus requerimientos."
            : "Planificación actualizada correctamente.");
        if (acceptedTeacherConflicts is { Count: > 0 })
            TempData.Warning("Se guardó con choque de docente; quedó constancia en las observaciones.");
        return RedirectToPage("./Requerimientos", new { planId = planningId });
    }

    /// <summary>
    /// Asigna el bloque de periodos a una planificación aprobada sin periodo asignado (O10): no cambia la
    /// fecha, el laboratorio, el docente, la materia, los requerimientos ni el estado; solo escribe
    /// StartPeriodId/PeriodCount y los campos derivados del horario (PlanningSlot.Apply), y sincroniza la
    /// programación vinculada. Mismas validaciones de ambiente y horario de grupo que el paso 1.
    /// </summary>
    public async Task<IActionResult> OnPostAssignPeriodAsync(int startPeriodId, int periodCount,
        bool acceptTeacherConflict = false, string? teacherConflictFingerprint = null)
    {
        if (Id is not int planningId)
            return RedirectToPage("./Index");

        var planning = await _context.Plannings.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planningId);
        if (planning == null)
        {
            TempData.Error("La planificación no existe.");
            return RedirectToPage("./Index");
        }
        var management = await _managements.GetSummaryAsync(planning.ManagementId);
        if (management == null || !management.IsEditable)
        {
            TempData.Error(AcademicManagementService.ReadOnlyMessage);
            return RedirectToPage(new { id = planningId });
        }
        if (planning.Status != PlanningStatus.Approved)
        {
            TempData.Error("Solo se asigna horario a planificaciones aprobadas sin periodo asignado.");
            return RedirectToPage(new { id = planningId });
        }
        if (planning.StartPeriodId != null)
        {
            TempData.Warning("Esta planificación ya tiene periodo asignado.");
            return RedirectToPage(new { id = planningId });
        }
        if (planning.LaboratoryId is not int laboratoryId)
        {
            TempData.Error("La planificación no tiene laboratorio asignado: no se puede reservar el horario.");
            return RedirectToPage(new { id = planningId });
        }
        if (periodCount < 1 || periodCount > PlanningSlot.MaxPeriods)
        {
            TempData.Error($"La cantidad de periodos debe estar entre 1 y {PlanningSlot.MaxPeriods}.");
            return RedirectToPage(new { id = planningId });
        }

        var periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        var start = periods.FirstOrDefault(p => p.Id == startPeriodId);
        if (start == null)
        {
            TempData.Error("Selecciona el periodo de inicio.");
            return RedirectToPage(new { id = planningId });
        }
        if (!PlanningSlot.Fits(start.Order, periodCount))
        {
            TempData.Error($"El bloque no cabe en la jornada: el periodo {start.Order} con {periodCount} periodos supera el periodo {PlanningSlot.MaxPeriods}.");
            return RedirectToPage(new { id = planningId });
        }

        // La fecha de la sesión no cambia: debe ser un día habilitado del calendario académico de la gestión.
        var date = planning.ScheduledDate.Date;
        if (PlanningSlot.WeekdayOf(date) == 0)
        {
            TempData.Error("Los domingos no hay actividades académicas: no se puede asignar el horario.");
            return RedirectToPage(new { id = planningId });
        }
        var dayEnabled = await _context.AcademicDays.AsNoTracking()
            .AnyAsync(d => d.Date == date && d.AcademicWeek.ManagementId == planning.ManagementId
                && d.Status == AcademicDayStatus.Enabled);
        if (!dayEnabled)
        {
            TempData.Error($"El día {date:dd/MM} no está habilitado en el calendario académico: no se puede asignar el horario.");
            return RedirectToPage(new { id = planningId });
        }

        // Aforo: los estudiantes de la sesión no pueden superar la capacidad del laboratorio de la
        // planificación (mismo texto y criterio que el guardado del paso 1).
        int? assignStudents = null;
        if (planning.PracticeScheduleId is int capacityScheduleId)
            assignStudents = await _context.PracticeSchedules.AsNoTracking()
                .Where(s => s.Id == capacityScheduleId)
                .Select(s => s.StudentCount)
                .FirstOrDefaultAsync();
        if (assignStudents is null && planning.CourseGroupId is int capacityGroupId)
            assignStudents = await _context.CourseGroups.AsNoTracking()
                .Where(g => g.Id == capacityGroupId)
                .Select(g => g.StudentCount)
                .FirstOrDefaultAsync();
        var capacityLab = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Id == laboratoryId)
            .Select(l => new { l.Code, l.Capacity })
            .FirstOrDefaultAsync();
        if (PlanningAssignmentRules.CapacityError(assignStudents, capacityLab?.Capacity, capacityLab?.Code ?? string.Empty) is string assignCapacityError)
        {
            TempData.Error(assignCapacityError);
            return RedirectToPage(new { id = planningId });
        }

        // Choque de ambiente: mismo criterio que el guardado del paso 1.
        var conflicts = await _occupancy.FindConflictsAsync(laboratoryId, date, start.Order, periodCount, [planningId]);
        if (conflicts.Count > 0)
        {
            TempData.Error($"El laboratorio está ocupado o no queda un periodo libre entre prácticas: {string.Join("; ", conflicts.Select(c => $"{c.Label} ({c.PeriodsText})"))}.");
            return RedirectToPage(new { id = planningId });
        }

        // Horario de clases del grupo: el destino no puede coincidir con clases de otras materias del mismo grupo.
        if (planning.CourseGroupId is int groupId)
        {
            var groupCheck = await _schedules.CheckDestinationAsync(groupId, date, start.Order, periodCount);
            if (groupCheck.Collisions.Count > 0)
            {
                var subjects = groupCheck.Collisions
                    .Select(c => c.SubjectName)
                    .Distinct(StringComparer.CurrentCulture)
                    .ToList();
                TempData.Error($"El grupo {groupCheck.Collisions[0].GroupName} tiene clase de {string.Join(", ", subjects)} en ese horario: elija otro periodo.");
                return RedirectToPage(new { id = planningId });
            }
        }

        // Choque de docente: se puede insistir aceptando la advertencia (queda constancia en observaciones).
        // Después de los bloqueos duros (ambiente y horario del grupo): no se pide aceptar algo que igual se rechazaría.
        // Se guarda en TempData para que la página muestre la alerta y precargue el intento.
        List<string>? acceptedAssignConflicts = null;
        var teacherBlocks = await _occupancy.FindTeacherConflictsAsync(planning.TeacherPersonId, date, start.Order, periodCount, [planningId]);
        if (teacherBlocks.Count > 0)
        {
            var conflictLabels = teacherBlocks.Select(c => $"{c.Label} ({c.PeriodsText})").ToList();
            var conflictFingerprint = PlanningAssignmentRules.Fingerprint(teacherBlocks.Select(c => c.PlanningId));
            if (!(acceptTeacherConflict && teacherConflictFingerprint == conflictFingerprint))
            {
                TempData["AssignTeacherConflicts"] = JsonSerializer.Serialize(conflictLabels);
                TempData["AssignTeacherConflictFingerprint"] = conflictFingerprint;
                TempData["AssignStartPeriodId"] = startPeriodId;
                TempData["AssignPeriodCount"] = periodCount;
                return RedirectToPage(new { id = planningId });
            }
            acceptedAssignConflicts = conflictLabels;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var resources = new List<string>
        {
            PlanningLockResources.ManagementDay(planning.ManagementId, date),
            PlanningLockResources.LaboratoryDay(laboratoryId, date),
            PlanningLockResources.Planning(planningId)
        };
        if (planning.TeacherPersonId > 0)
            resources.Add(PlanningLockResources.TeacherDay(planning.TeacherPersonId, date));
        if (!await _planningLock.TryAcquireAsync(resources))
        {
            TempData.Error(PlanningLockResources.BusyMessage + ".");
            return RedirectToPage(new { id = planningId });
        }

        try
        {
            // No confiar en las consultas previas: revalidar dentro de la transacción.
            var current = await _context.Plannings.AsNoTracking()
                .Where(p => p.Id == planningId)
                .Select(p => new { p.Status, p.StartPeriodId })
                .FirstAsync();
            if (current.Status != PlanningStatus.Approved || current.StartPeriodId != null)
            {
                TempData.Error("La planificación cambió de estado mientras se asignaba el horario; revisa la pantalla e intente nuevamente.");
                return RedirectToPage(new { id = planningId });
            }
            if (await _occupancy.FindConflictsAsync(laboratoryId, date, start.Order, periodCount, [planningId]) is { Count: > 0 } revalidados)
            {
                TempData.Error($"El laboratorio se ocupó mientras se guardaba (ocupado o sin periodo libre entre prácticas): {string.Join("; ", revalidados.Select(c => $"{c.Label} ({c.PeriodsText})"))}.");
                return RedirectToPage(new { id = planningId });
            }

            // Choque de docente bajo bloqueo: si apareció uno nuevo la huella ya no coincide y se vuelve a pedir.
            var teacherRecheck = await _occupancy.FindTeacherConflictsAsync(planning.TeacherPersonId, date, start.Order, periodCount, [planningId]);
            if (teacherRecheck.Count > 0)
            {
                var recheckLabels = teacherRecheck.Select(c => $"{c.Label} ({c.PeriodsText})").ToList();
                var recheckFingerprint = PlanningAssignmentRules.Fingerprint(teacherRecheck.Select(c => c.PlanningId));
                if (!(acceptTeacherConflict && teacherConflictFingerprint == recheckFingerprint))
                {
                    TempData["AssignTeacherConflicts"] = JsonSerializer.Serialize(recheckLabels);
                    TempData["AssignTeacherConflictFingerprint"] = recheckFingerprint;
                    TempData["AssignStartPeriodId"] = startPeriodId;
                    TempData["AssignPeriodCount"] = periodCount;
                    return RedirectToPage(new { id = planningId });
                }
                acceptedAssignConflicts = recheckLabels;
            }

            var tracked = await _context.Plannings.AsTracking().FirstAsync(p => p.Id == planningId);
            PlanningSlot.Apply(tracked, date, startPeriodId, periodCount, periods);
            if (acceptedAssignConflicts is { Count: > 0 })
            {
                tracked.Observations = PlanningAssignmentRules.AppendTeacherConflictNote(tracked.Observations,
                    $"Choque de docente aceptado por {User.Identity?.Name ?? "usuario"} el {DateTime.Now:dd/MM/yyyy HH:mm}: {string.Join("; ", acceptedAssignConflicts)}");
            }
            if (tracked.PracticeScheduleId.HasValue)
            {
                var schedule = await _context.PracticeSchedules.AsTracking()
                    .FirstOrDefaultAsync(s => s.Id == tracked.PracticeScheduleId.Value);
                if (schedule != null)
                {
                    schedule.ExecutionDate = tracked.ScheduledDate.Date;
                    schedule.StartTime = tracked.StartTime;
                    schedule.EndTime = tracked.EndTime;
                }
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var lastOrder = PlanningSlot.LastOrder(start.Order, periodCount);
            TempData.Success($"Horario asignado a {L3Codes.Format(tracked.Id, tracked.Code)}: periodos {start.Order}–{lastOrder} ({tracked.StartTime:hh\\:mm}–{tracked.EndTime:hh\\:mm}).");
            if (acceptedAssignConflicts is { Count: > 0 })
                TempData.Warning("Se asignó el horario con choque de docente; quedó constancia en las observaciones.");
        }
        catch (ArgumentException ex)
        {
            await transaction.RollbackAsync();
            TempData.Error(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo asignar el horario de la planificación {PlanningId}", planningId);
            TempData.Error("No se pudo asignar el horario. Intente nuevamente.");
        }
        return RedirectToPage(new { id = planningId });
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

    /// <summary>Recarga los choques de docente de la ventana elegida y su huella (para la vista y la aceptación).</summary>
    private async Task LoadTeacherConflictsAsync(int teacherPersonId, DateTime date, int startOrder, int periodCount,
        IReadOnlyCollection<int>? excludePlanningIds)
    {
        var blocks = await _occupancy.FindTeacherConflictsAsync(teacherPersonId, date.Date, startOrder, periodCount, excludePlanningIds);
        TeacherConflicts = blocks.Select(b => $"{b.Label} ({b.PeriodsText})").ToList();
        TeacherConflictFingerprint = PlanningAssignmentRules.Fingerprint(blocks.Select(b => b.PlanningId));
    }

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
            .Select(g => new GroupOption(g.SubjectId, g.Name, g.StudentCount, g.ScalingFactor, g.Subject!.Semester))
            .ToListAsync();

        Periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        if (Input.StartPeriodId == 0 && Periods.Count > 0)
            Input.StartPeriodId = Periods[0].Id;

        LaboratoryList = new SelectList(laboratories, "Id", "Name", Input.LaboratoryId);
        TeacherList = new SelectList(teachers, "Id", "Name", Input.TeacherPersonId);
    }

    /// <summary>
    /// Sugerencia de bloque de periodos para una planificación sin periodo (O10): el periodo cuyo rango
    /// contiene la hora de inicio (o el más cercano) y la cantidad de periodos hasta cubrir la hora de fin
    /// (mínimo 1), acotada a la jornada del catálogo.
    /// </summary>
    private (int StartPeriodId, int PeriodCount) SuggestSlot(TimeSpan? startTime, TimeSpan? endTime)
    {
        var ordered = Periods.OrderBy(p => p.Order).ToList();
        ClassPeriod start;
        if (startTime is TimeSpan desde)
            // Primer periodo que termina después de la hora de inicio: la contiene o es el más cercano.
            start = ordered.FirstOrDefault(p => p.EndTime > desde) ?? ordered[^1];
        else
            start = ordered.FirstOrDefault(p => endTime is TimeSpan hasta && p.EndTime > hasta) ?? ordered[0];

        var count = 1;
        if (endTime is TimeSpan fin)
        {
            var last = ordered.FirstOrDefault(p => p.EndTime >= fin) ?? ordered[^1];
            count = last.Order - start.Order + 1;
        }
        if (!PlanningSlot.Fits(start.Order, count))
            count = PlanningSlot.MaxPeriods - start.Order + 1;
        return (start.Id, Math.Max(1, count));
    }

    /// <summary>
    /// AJAX (GET): bloques ocupados del ambiente en la fecha para pintar las fichas del selector de
    /// periodos (ocupadas, gris) y las de separación contiguas (rayado). Solo lectura; al editar se
    /// excluye la planificación actual para no marcarse a sí misma.
    /// </summary>
    public async Task<IActionResult> OnGetOccupancyAsync(int laboratoryId, string date, int? excludeId)
    {
        if (laboratoryId < 1
            || !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return new JsonResult(Array.Empty<object>());

        var blocks = await _occupancy.GetAsync(new OccupancyQuery(day.Date, day.Date, [laboratoryId], null,
            excludeId is > 0 ? [excludeId.Value] : null));

        return new JsonResult(blocks.Select(b => new
        {
            startOrder = b.StartOrder,
            periodCount = b.PeriodCount,
            label = b.Label
        }));
    }

    /// <summary>
    /// AJAX (GET): horario semanal del grupo de la materia para marcar las fichas del selector de periodos:
    /// own = clases de esta misma materia (sugeridas, verde) y busy = clases de OTRAS materias del mismo
    /// grupo de estudiantes (bloquean, rojo). Resuelve el grupo por gestión + materia + nombre, el mismo
    /// criterio del guardado (nombre en mayúsculas y sin espacios extremos). Solo lectura.
    /// </summary>
    public async Task<IActionResult> OnGetGroupScheduleAsync(int subjectId, string? groupName, int? managementId)
    {
        var empty = new { hasSchedule = false, own = Array.Empty<object>(), busy = Array.Empty<object>() };
        var management = await _managements.ResolveAsync(managementId ?? ManagementId);
        var name = (groupName ?? string.Empty).Trim().ToUpperInvariant();
        if (management == null || subjectId < 1 || name.Length == 0)
            return new JsonResult(empty);

        var courseGroupId = await _context.CourseGroups.AsNoTracking()
            .Where(g => g.ManagementId == management.Id && g.SubjectId == subjectId && g.Name == name)
            .Select(g => (int?)g.Id)
            .FirstOrDefaultAsync();
        if (courseGroupId is not int groupId)
            return new JsonResult(empty);

        var own = await _schedules.GetGroupSlotsAsync(groupId);
        var busy = await _schedules.GetSiblingClassesAsync(groupId);

        return new JsonResult(new
        {
            hasSchedule = own.Count > 0,
            own = own.Select(s => new { weekday = s.Weekday, order = s.PeriodOrder }),
            busy = busy.Select(s => new { weekday = s.Weekday, order = s.PeriodOrder, subject = s.SubjectName })
        });
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

        /// <summary>True cuando la persona acepta guardar con el choque de docente (queda constancia en observaciones).</summary>
        public bool AcceptTeacherConflict { get; set; }

        /// <summary>Huella de los choques aceptados: debe coincidir con los vigentes al procesar el formulario.</summary>
        public string? TeacherConflictFingerprint { get; set; }

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
