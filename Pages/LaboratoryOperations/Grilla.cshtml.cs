using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Grilla semanal por laboratorio (formulario RE-10-VAC-007 "Registro de horarios – Horario por aula"):
/// lunes a sábado × 12 periodos, con las planificaciones del ambiente elegido.
/// Con ?reprogramar={planningId} (solo SuperAdmin) entra en modo reprogramación: se arrastra el bloque origen a su
/// nuevo destino, se previsualizan las validaciones del servidor y se confirma con motivo (OnPostReprogramarAsync).
/// Con ?reponer={planningId}&recoveryId={id} (Supervisor/Administrator/SuperAdmin) repone un bloque de un día
/// suspendido; con ?recoveryId= (sin reponer) muestra el panel de afectados de la reposición en curso.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ManagementRoles)]
public class GrillaModel : PageModel
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-BO");

    private readonly ILaboratoryGridService _grid;
    private readonly IReschedulingService _rescheduling;
    private readonly IGroupScheduleService _schedules;
    private readonly ApplicationDbContext _context;

    public GrillaModel(ILaboratoryGridService grid, IReschedulingService rescheduling, IGroupScheduleService schedules,
        ApplicationDbContext context)
    {
        _grid = grid;
        _rescheduling = rescheduling;
        _schedules = schedules;
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? LaboratoryId { get; set; }

    /// <summary>Número de semana académica de la gestión.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Week { get; set; }

    /// <summary>Fecha yyyy-MM-dd: muestra la semana que la contiene (tiene prioridad sobre Week).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Date { get; set; }

    /// <summary>Ver dos semanas. Sin valor: no en la vista normal, sí en el modo reprogramación.</summary>
    [BindProperty(SupportsGet = true)]
    public bool? TwoWeeks { get; set; }

    /// <summary>Planificación a reprogramar (activa el modo reprogramación).</summary>
    [BindProperty(SupportsGet = true, Name = "reprogramar")]
    public int? Reprogramar { get; set; }

    [BindProperty(SupportsGet = true, Name = "reponer")]
    public int? Reponer { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? RecoveryId { get; set; }

    /// <summary>Destino a previsualizar al abrir el modo (tras un intento fallido).</summary>
    [BindProperty(SupportsGet = true)]
    public string? DestDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? DestPeriodId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? DestTeacherId { get; set; }

    /// <summary>Planificación a resaltar (p. ej. el destino recién creado).</summary>
    [BindProperty(SupportsGet = true)]
    public int? Resaltar { get; set; }

    [BindProperty]
    public RescheduleInput Move { get; set; } = new();

    /// <summary>Grilla armada (null si no hay gestión pedida ni actual).</summary>
    public LaboratoryGrid? Grid { get; private set; }

    public bool IsSuperAdmin => User.IsInRole(AuthorizationHelper.RoleSuperAdmin);

    /// <summary>True para Supervisor/Administrator/SuperAdmin: pueden reponer días suspendidos (no reprogramar a mano).</summary>
    public bool CanRecover => AuthorizationHelper.ManagementRoles
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(User.IsInRole);

    /// <summary>Origen del modo reprogramación (null = grilla normal).</summary>
    public RescheduleOrigin? Origin { get; private set; }

    public bool IsRescheduleMode => Origin != null;
    public bool IsRecoveryMode => Origin != null && Reponer.HasValue;

    /// <summary>Reposición de día en curso mostrada sin planificación elegida (?recoveryId= sin reponer).</summary>
    public RecoveryOverviewInfo? RecoveryOverview { get; private set; }

    public sealed record RecoveryOverviewInfo(int Id, DateTime Date, string Reason, int Recovered, int Pending)
    {
        public int Total => Recovered + Pending;
    }

    /// <summary>True si el bloque origen está en la vista actual (ambiente y semanas visibles).</summary>
    public bool OriginInView => Origin != null && Grid != null
        && Grid.Weeks.Any(w => w.Days.Any(d => d.Blocks.Any(b => b.PlanningId == Origin.Id)));

    /// <summary>Docentes activos para el selector (solo en modo reprogramación).</summary>
    public IReadOnlyList<TeacherOption> Teachers { get; private set; } = [];
    public IReadOnlyList<RecoverySibling> RecoveryPending { get; private set; } = [];

    /// <summary>Horario semanal del grupo de la planificación origen (solo en modo reprogramación).</summary>
    public IReadOnlyList<WeekSlot> GroupSlots { get; private set; } = [];

    /// <summary>Clases de OTRAS materias del mismo grupo de estudiantes (solo en modo reprogramación).</summary>
    public IReadOnlyList<SiblingClass> GroupSiblings { get; private set; } = [];

    /// <summary>True si el grupo tiene celdas de horario propias registradas en esta materia.</summary>
    public bool HasGroupSchedule { get; private set; }

    public sealed record RecoverySibling(int Id, string Code, string Laboratory, int? LaboratoryId, string Subject,
        string Group, string Teacher, int PeriodCount, string Periods, string PreviousStatus);

    /// <summary>Motivo escrito en un intento fallido (se devuelve al formulario).</summary>
    public string? PreviousReason { get; private set; }

    public bool EffectiveTwoWeeks => TwoWeeks ?? IsRescheduleMode;

    public sealed record TeacherOption(int Id, string Name);

    public sealed record RescheduleOrigin(
        int Id,
        string Code,
        int ManagementId,
        string SubjectName,
        string GroupName,
        string PracticeName,
        DateTime Date,
        int StartPeriodId,
        int StartOrder,
        int PeriodCount,
        string TimeText,
        int? LaboratoryId,
        string LaboratoryLabel,
        int TeacherPersonId,
        string TeacherName,
        string Status,
        int? CourseGroupId,
        int? SubjectId)
    {
        public string PeriodsText => PlanningSlot.PeriodsText(StartOrder, PeriodCount);
        public bool IsCompleted => Status == PlanningStatus.Finalized;
    }

    public sealed class RescheduleInput
    {
        public int PlanningId { get; set; }
        /// <summary>yyyy-MM-dd.</summary>
        public string? Date { get; set; }
        public int StartPeriodId { get; set; }
        public int LaboratoryId { get; set; }
        public int TeacherPersonId { get; set; }
        public string? Reason { get; set; }
        /// <summary>Códigos de advertencia que la persona aceptó (los envía el cliente tras la previsualización).</summary>
        public List<string> AcceptedWarnings { get; set; } = [];
        public int? DayRecoveryId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // La fecha destino precargada viene de la URL: solo se acepta yyyy-MM-dd (el JS la usa en selectores).
        if (DestDate != null && !DateTime.TryParseExact(DestDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            DestDate = null;

        if (Reprogramar.HasValue || Reponer.HasValue)
        {
            if (Reprogramar.HasValue && Reponer.HasValue)
                return BadRequest();
            if (Reponer.HasValue ? !CanRecover : !IsSuperAdmin)
            {
                TempData.Error(Reponer.HasValue
                    ? "No tiene permiso para reponer planificaciones de un día suspendido."
                    : "Solo el SuperAdmin puede reprogramar planificaciones.");
                return RedirectToPage(new { managementId = ManagementId, laboratoryId = LaboratoryId, week = Week, date = Date, twoWeeks = TwoWeeks });
            }

            var (origin, error) = await LoadOriginAsync(Reponer ?? Reprogramar!.Value, Reponer.HasValue, RecoveryId);
            if (origin == null)
            {
                TempData.Error(error!);
                return RedirectToPage(new { managementId = ManagementId, laboratoryId = LaboratoryId, week = Week, date = Date, twoWeeks = TwoWeeks });
            }

            Origin = origin;
            // Por defecto: gestión, ambiente y semana del origen. La gestión no cambia dentro del modo.
            ManagementId = origin.ManagementId;
            LaboratoryId ??= origin.LaboratoryId;
            if (Week is null && string.IsNullOrWhiteSpace(Date))
                Date = origin.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            Teachers = await LoadTeachersAsync(origin.TeacherPersonId);
            // Horario de clases del grupo de la planificación: marca las celdas de la grilla (clase propia
            // sugerida en verde, clase de otra materia del grupo en rojo) y alimenta el rechazo local del JS.
            if (origin.CourseGroupId is int courseGroupId)
            {
                GroupSlots = await _schedules.GetGroupSlotsAsync(courseGroupId);
                GroupSiblings = await _schedules.GetSiblingClassesAsync(courseGroupId);
                HasGroupSchedule = GroupSlots.Count > 0;
            }
            if (Reponer.HasValue)
            {
                // Afectados del día suspendido: alimentan la bandeja lateral y el marcador de ambientes con pendientes.
                RecoveryPending = await LoadRecoveryPendingAsync(origin.ManagementId, origin.Date.Date);
            }
            PreviousReason = TempData["RescheduleReason"] as string;
        }
        else if (RecoveryId.HasValue)
        {
            // Modo reposición general: ?recoveryId= sin reponer muestra los afectados del día para elegir a cuál saltar.
            var recovery = await _context.DayRecoveries.AsNoTracking()
                .Where(r => r.Id == RecoveryId && r.Status == Models.Enums.DayRecoveryStatus.InProgress)
                .Select(r => new { r.Id, r.ManagementId, r.Date, r.Reason })
                .FirstOrDefaultAsync();
            if (recovery == null)
            {
                TempData.Error("La reposición indicada no existe o ya no está en curso.");
                return RedirectToPage(new { managementId = ManagementId, laboratoryId = LaboratoryId, week = Week, date = Date, twoWeeks = TwoWeeks });
            }
            ManagementId ??= recovery.ManagementId;
            RecoveryPending = await LoadRecoveryPendingAsync(recovery.ManagementId, recovery.Date.Date);
            var recovered = await _context.Reschedulings.AsNoTracking()
                .CountAsync(x => x.DayRecoveryId == recovery.Id);
            RecoveryOverview = new RecoveryOverviewInfo(recovery.Id, recovery.Date.Date, recovery.Reason, recovered, RecoveryPending.Count);
        }

        Grid = await _grid.BuildAsync(ManagementId, LaboratoryId, Week, Date, EffectiveTwoWeeks);
        return Page();
    }

    public async Task<IActionResult> OnPostReprogramarAsync(CancellationToken ct)
    {
        if (Move.DayRecoveryId.HasValue ? !CanRecover : !IsSuperAdmin)
        {
            TempData.Error(Move.DayRecoveryId.HasValue
                ? "No tiene permiso para reponer planificaciones de un día suspendido."
                : "Solo el SuperAdmin puede reprogramar planificaciones.");
            return RedirectToPage(new { managementId = ManagementId, laboratoryId = LaboratoryId, date = Date });
        }

        if (Move.PlanningId <= 0 || (Reprogramar.HasValue && Reprogramar.Value != Move.PlanningId)
            || (Reponer.HasValue && Reponer.Value != Move.PlanningId)
            || (Reponer.HasValue != Move.DayRecoveryId.HasValue)
            || (RecoveryId.HasValue && RecoveryId != Move.DayRecoveryId))
        {
            TempData.Error("La planificación de origen no es válida.");
            return RedirectToPage("./Grilla");
        }

        if (string.IsNullOrWhiteSpace(Move.Date)
            || !DateTime.TryParseExact(Move.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            TempData.Error("Elija una fecha de destino válida.");
            return BackToMode(null);
        }

        var result = await _rescheduling.ExecuteAsync(new RescheduleCommand(
            Move.PlanningId, date, Move.StartPeriodId, Move.LaboratoryId, Move.TeacherPersonId,
            Move.Reason ?? string.Empty, Move.AcceptedWarnings ?? [], Move.DayRecoveryId), ct);

        if (!result.Ok)
        {
            // El origen ya no se puede mover (otro usuario lo reubicó o cambió su estado): volver al modo fallaría
            // al cargar el origen y taparía este mensaje, así que se vuelve a la grilla normal.
            var originGone = result.Message == ReschedulingService.AlreadyRescheduledMessage
                || (result.Validation?.Issues.Any(i => i.Code is SlotValidationService.Codes.OriginNotMovable
                    or SlotValidationService.Codes.OriginNotFound) ?? false);
            if (originGone)
            {
                TempData.Error(result.Message);
                return RedirectToPage(new { managementId = ManagementId, laboratoryId = LaboratoryId, date = Date });
            }

            var details = result.Validation?.Issues
                .Where(i => i.Severity != SlotSeverity.Info)
                .Select(i => i.Message)
                .Where(m => !result.Message.Contains(m, StringComparison.Ordinal))
                .ToList() ?? [];
            TempData.Error(details.Count == 0 ? result.Message : $"{result.Message} {string.Join(" · ", details)}");
            return BackToMode(date);
        }

        TempData.Success(result.Message);
        var destinationManagementId = await _context.Plannings.AsNoTracking()
            .Where(p => p.Id == result.DestinationPlanningId)
            .Select(p => p.ManagementId).FirstAsync(ct);
        if (Move.DayRecoveryId.HasValue)
        {
            // Reposición de día: seguir en la grilla con el siguiente pendiente (mismo ambiente primero).
            var next = await FindNextRecoveryPendingAsync(destinationManagementId, Move.DayRecoveryId.Value, Move.LaboratoryId, ct);
            return next == null
                ? RedirectToPage("./Grilla", new { managementId = destinationManagementId, recoveryId = Move.DayRecoveryId })
                : RedirectToPage("./Grilla", new
                {
                    managementId = destinationManagementId,
                    recoveryId = Move.DayRecoveryId,
                    reponer = next.Id,
                    laboratoryId = next.LaboratoryId
                });
        }
        return RedirectToPage(new
        {
            managementId = destinationManagementId,
            laboratoryId = Move.LaboratoryId,
            date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            resaltar = result.DestinationPlanningId
        });
    }

    /// <summary>Planificaciones PendienteReposicion del día suspendido, agrupadas por ambiente y con su contexto.</summary>
    private async Task<IReadOnlyList<RecoverySibling>> LoadRecoveryPendingAsync(int managementId, DateTime date, CancellationToken ct = default)
    {
        var from = date.Date;
        var to = from.AddDays(1);
        var rows = await _context.Plannings.AsNoTracking()
            .Where(p => p.ManagementId == managementId && p.Status == PlanningStatus.PendingRecovery
                && p.ScheduledDate >= from && p.ScheduledDate < to)
            .OrderBy(p => p.Laboratory!.Code).ThenBy(p => p.ScheduledDate)
            .Select(p => new
            {
                p.Id,
                p.GroupName,
                p.TeacherPersonId,
                p.PeriodCount,
                p.StatusBeforeRecovery,
                LaboratoryId = (int?)p.LaboratoryId,
                Laboratory = p.Laboratory != null ? p.Laboratory.Code : "Sin ambiente",
                Subject = p.Subject != null ? p.Subject.Name : p.PracticeName,
                StartOrder = p.StartPeriod != null ? (int?)p.StartPeriod.Order : null
            })
            .ToListAsync(ct);
        var teacherIds = rows.Select(p => p.TeacherPersonId).Distinct().ToList();
        var teachers = teacherIds.Count == 0
            ? []
            : await _context.Interns.AsNoTracking().Where(t => teacherIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct);
        return rows.Select(p => new RecoverySibling(p.Id, L3Codes.Format(p.Id), p.Laboratory, p.LaboratoryId,
            p.Subject, p.GroupName ?? "—", teachers.GetValueOrDefault(p.TeacherPersonId, "—"), p.PeriodCount ?? 0,
            p.StartOrder is int order && p.PeriodCount is int count ? PlanningSlot.PeriodsText(order, count) : "Sin periodos",
            PlanningStatus.Label(p.StatusBeforeRecovery ?? PlanningStatus.PendingRecovery))).ToList();
    }

    /// <summary>Siguiente planificación pendiente de la reposición (primero del ambiente recién usado, si no el primero).</summary>
    private async Task<RecoverySibling?> FindNextRecoveryPendingAsync(int managementId, int recoveryId, int preferredLabId, CancellationToken ct)
    {
        var date = await _context.DayRecoveries.AsNoTracking()
            .Where(r => r.Id == recoveryId)
            .Select(r => (DateTime?)r.Date)
            .FirstOrDefaultAsync(ct);
        if (date == null) return null;
        var pendings = await LoadRecoveryPendingAsync(managementId, date.Value.Date, ct);
        return pendings
            .OrderBy(p => p.LaboratoryId == preferredLabId ? 0 : 1)
            .ThenBy(p => p.Laboratory).ThenBy(p => p.Id)
            .FirstOrDefault();
    }

    /// <summary>Vuelve al modo reprogramación mostrando el destino intentado (y conserva el motivo escrito).</summary>
    private RedirectToPageResult BackToMode(DateTime? date)
    {
        if (!string.IsNullOrWhiteSpace(Move.Reason))
        {
            var reason = Move.Reason.Trim();
            TempData["RescheduleReason"] = reason.Length > ReschedulingService.MaxReasonLength ? reason[..ReschedulingService.MaxReasonLength] : reason;
        }

        var dateText = date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return RedirectToPage(new
        {
            reprogramar = Move.DayRecoveryId.HasValue ? (int?)null : Move.PlanningId,
            reponer = Move.DayRecoveryId.HasValue ? Move.PlanningId : (int?)null,
            recoveryId = Move.DayRecoveryId,
            managementId = ManagementId,
            laboratoryId = Move.LaboratoryId > 0 ? Move.LaboratoryId : LaboratoryId,
            date = dateText ?? Date,
            twoWeeks = TwoWeeks,
            destDate = dateText,
            destPeriodId = date.HasValue && Move.StartPeriodId > 0 ? Move.StartPeriodId : (int?)null,
            destTeacherId = Move.TeacherPersonId > 0 ? Move.TeacherPersonId : (int?)null
        });
    }

    private async Task<(RescheduleOrigin? Origin, string? Error)> LoadOriginAsync(int planningId, bool isRecovery, int? recoveryId)
    {
        var p = await _context.Plannings.AsNoTracking()
            .Where(x => x.Id == planningId)
            .Select(x => new
            {
                x.Id,
                x.ManagementId,
                ManagementStatus = x.Management != null ? (Models.Enums.ManagementStatus?)x.Management.Status : null,
                x.Status,
                x.StatusBeforeRecovery,
                x.PracticeName,
                SubjectName = x.Subject != null ? x.Subject.Name : null,
                GroupLabel = !string.IsNullOrEmpty(x.GroupName) ? x.GroupName : (x.Group != null ? x.Group.Name : null),
                x.CourseGroupId,
                x.SubjectId,
                x.ScheduledDate,
                x.ScheduledEndDate,
                x.StartPeriodId,
                StartOrder = x.StartPeriod != null ? (int?)x.StartPeriod.Order : null,
                x.PeriodCount,
                x.LaboratoryId,
                LabCode = x.Laboratory != null ? x.Laboratory.Code : null,
                LabName = x.Laboratory != null ? x.Laboratory.Name : null,
                x.TeacherPersonId
            })
            .FirstOrDefaultAsync();

        if (p == null)
            return (null, "La planificación a reprogramar no existe o fue eliminada.");
        if (!isRecovery && p.Status == PlanningStatus.PendingRecovery)
            return (null, $"{L3Codes.Format(p.Id)} está pendiente de reposición: se reubica desde la reposición del día suspendido.");
        if (isRecovery && (recoveryId is not > 0 || p.Status != PlanningStatus.PendingRecovery
            || p.StatusBeforeRecovery is null
            || !await _context.AcademicDays.AsNoTracking().AnyAsync(d => d.Date == p.ScheduledDate.Date
                && d.AcademicWeek.ManagementId == p.ManagementId && d.Status == Models.Enums.AcademicDayStatus.Suspended
                && d.DayRecoveryId == recoveryId && d.DayRecovery!.Status == Models.Enums.DayRecoveryStatus.InProgress)))
            return (null, "El bloque ya no está pendiente de esta reposición de día.");
        if (!isRecovery && !GridRelocation.IsReschedulableStatus(p.Status))
            return (null, $"{L3Codes.Format(p.Id)} está en estado \"{PlanningStatus.Label(p.Status)}\" y no se puede reprogramar.");
        if (p.StartPeriodId is not int startPeriodId || p.StartOrder is not int startOrder || p.PeriodCount is not int count)
            return (null, $"{L3Codes.Format(p.Id)} no tiene periodos asignados: complete su horario antes de reprogramarla.");
        if (p.ManagementStatus is not (Models.Enums.ManagementStatus.Active or Models.Enums.ManagementStatus.Inactive))
            return (null, AcademicManagementService.ReadOnlyMessage);

        var teacherName = await _context.Interns.AsNoTracking()
            .Where(i => i.Id == p.TeacherPersonId)
            .Select(i => i.Name)
            .FirstOrDefaultAsync();

        return (new RescheduleOrigin(
            p.Id,
            L3Codes.Format(p.Id),
            p.ManagementId,
            string.IsNullOrWhiteSpace(p.SubjectName) ? "Sin materia" : p.SubjectName!,
            string.IsNullOrWhiteSpace(p.GroupLabel) ? "—" : p.GroupLabel!,
            p.PracticeName,
            p.ScheduledDate.Date,
            startPeriodId,
            startOrder,
            count,
            PlanningStatusView.TimeText(startOrder, count, p.ScheduledDate, p.ScheduledEndDate),
            p.LaboratoryId,
            p.LabCode != null ? $"{p.LabCode} – {p.LabName}" : "Todos los laboratorios",
            p.TeacherPersonId,
            string.IsNullOrWhiteSpace(teacherName) ? "—" : teacherName!,
            p.Status,
            p.CourseGroupId,
            p.SubjectId), null);
    }

    private async Task<IReadOnlyList<TeacherOption>> LoadTeachersAsync(int currentTeacherId)
    {
        var teachers = await ReschedulingService.ActiveTeachers(_context)
            .OrderBy(t => t.Name)
            .Select(t => new TeacherOption(t.Id, t.Name))
            .ToListAsync();
        if (teachers.All(t => t.Id != currentTeacherId))
        {
            // El docente actual puede estar inactivo: se ofrece igual para conservarlo.
            var current = await _context.Interns.AsNoTracking()
                .Where(i => i.Id == currentTeacherId)
                .Select(i => new TeacherOption(i.Id, i.Name + " (actual)"))
                .FirstOrDefaultAsync();
            if (current != null) teachers.Insert(0, current);
        }
        return teachers;
    }

    /// <summary>"martes 06/10/2026".</summary>
    public static string LongDate(DateTime date) => date.ToString("dddd dd/MM/yyyy", Es);
}
