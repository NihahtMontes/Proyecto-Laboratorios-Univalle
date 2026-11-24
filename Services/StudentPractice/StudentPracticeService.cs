using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;

namespace Proyecto_Laboratorios_Univalle.Services.StudentPractice;

public sealed record StudentResult(bool Ok, string Message, int? Id = null);

public sealed record ReservationLine(int ArticleId, decimal Quantity);

/// <summary>Cupo con lugares ocupados (aprobadas + pendientes) para listarlo.</summary>
public sealed record SlotView(
    int Id, string Code, int LaboratoryId, string LaboratoryCode, string LaboratoryName, DateTime Date,
    int StartOrder, int PeriodCount, TimeSpan? StartTime, TimeSpan? EndTime, int Capacity, int Approved, int Pending,
    FreePracticeSlotStatus Status, string? Notes)
{
    public int Taken => Approved + Pending;
    public int Free => Math.Max(0, Capacity - Taken);
    public DateTime StartsAt => Date.Date + (StartTime ?? TimeSpan.Zero);
    public string PeriodsText => PlanningSlot.PeriodsText(StartOrder, PeriodCount);
    public string TimeText => StartTime is TimeSpan s && EndTime is TimeSpan e ? $"{s:hh\\:mm}–{e:hh\\:mm}" : PeriodsText;
}

/// <summary>
/// Reglas de la práctica libre de estudiantes (decisiones del usuario, 2026-11-23):
/// registro con correo @est.univalle.edu y aprobación de la cuenta; cupos que abre la Inge donde el laboratorio está libre;
/// reservas que la Inge aprueba; máximo 2 por semana; cancelar hasta 24 h antes; asistencia y faltas; suspensión y baja manuales.
/// </summary>
public sealed class StudentPracticeService
{
    public const int WeeklyLimit = 2;
    public static readonly TimeSpan CancelWindow = TimeSpan.FromHours(24);
    public static readonly string[] Careers = ["LGS", "LCG", "LND", "LTH"];

    private static readonly SlotReservationStatus[] Holding = [SlotReservationStatus.Pending, SlotReservationStatus.Approved];

    private readonly ApplicationDbContext _context;
    private readonly IOccupancyService _occupancy;
    private readonly Func<DateTime> _now;

    public StudentPracticeService(ApplicationDbContext context, IOccupancyService occupancy)
        : this(context, occupancy, () => DateTime.Now) { }

    public StudentPracticeService(ApplicationDbContext context, IOccupancyService occupancy, Func<DateTime> now)
    {
        _context = context;
        _occupancy = occupancy;
        _now = now;
    }

    // ───────────────────────────── avisos

    private void Notify(int userId, string title, string message, string url, string icon = "mdi mdi-school") =>
        _context.Notifications.Add(new Notification
        {
            UserId = userId, Title = title, Message = message, ActionUrl = url, IconClass = icon, Scope = "PracticaLibre"
        });

    private async Task NotifyRolesAsync(string[] roles, string title, string message, string url, CancellationToken ct)
    {
        var ids = await _context.UserRoles
            .Where(ur => _context.Roles.Any(r => r.Id == ur.RoleId && roles.Contains(r.Name!)))
            .Select(ur => ur.UserId).Distinct().ToListAsync(ct);
        var active = await _context.Users.Where(u => ids.Contains(u.Id) && u.Status == GeneralStatus.Activo).Select(u => u.Id).ToListAsync(ct);
        foreach (var id in active) Notify(id, title, message, url);
    }

    // ───────────────────────────── cuentas

    /// <summary>Perfil de estudiante recién registrado (la cuenta de usuario ya fue creada inactiva por la página).</summary>
    public async Task<StudentResult> AddProfileAsync(int userId, string career, int semester, CancellationToken ct = default)
    {
        if (!Careers.Contains(career)) return new(false, "Elija una carrera válida.");
        if (semester is < 1 or > 10) return new(false, "El semestre debe estar entre 1 y 10.");
        var profile = new StudentProfile { UserId = userId, Career = career, Semester = semester };
        _context.StudentProfiles.Add(profile);
        await NotifyRolesAsync([AuthorizationHelper.RoleAdministrator, AuthorizationHelper.RoleSuperAdmin],
            "Nueva cuenta de estudiante", "Un estudiante se registró y espera la aprobación de su cuenta.", "/PracticaLibre/Cuentas", ct);
        await _context.SaveChangesAsync(ct);
        return new(true, "Registro enviado.", profile.Id);
    }

    public async Task<StudentResult> ReviewAccountAsync(int profileId, bool approve, string? note, int reviewerId, CancellationToken ct = default)
    {
        var profile = await _context.StudentProfiles.AsTracking().Include(p => p.User).FirstOrDefaultAsync(p => p.Id == profileId, ct);
        if (profile?.User is not User user) return new(false, "La cuenta no existe.");
        if (profile.AccountStatus != StudentAccountStatus.Pending) return new(false, "La cuenta ya fue revisada.");
        if (!approve && string.IsNullOrWhiteSpace(note)) return new(false, "Indique el motivo del rechazo.");

        profile.AccountStatus = approve ? StudentAccountStatus.Approved : StudentAccountStatus.Rejected;
        profile.ReviewedById = reviewerId;
        profile.ReviewedDate = DateTime.UtcNow;
        profile.ReviewNote = note?.Trim();
        user.Status = approve ? GeneralStatus.Activo : GeneralStatus.Inactivo;
        if (approve)
            Notify(user.Id, "Tu cuenta fue aprobada", "Ya puedes reservar cupos de práctica libre.", "/Estudiante");
        await _context.SaveChangesAsync(ct);
        return new(true, approve ? $"Cuenta de {user.FullName} aprobada." : $"Cuenta de {user.FullName} rechazada.");
    }

    public async Task<StudentResult> SuspendAsync(int profileId, DateTime until, string reason, int byId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return new(false, "Indique el motivo de la suspensión.");
        if (until.Date < _now().Date) return new(false, "La fecha de fin de la suspensión ya pasó.");
        var profile = await _context.StudentProfiles.AsTracking().FirstOrDefaultAsync(p => p.Id == profileId, ct);
        if (profile == null) return new(false, "El estudiante no existe.");
        profile.SuspendedUntil = until.Date;
        profile.SuspensionReason = reason.Trim();
        Notify(profile.UserId, "Suspensión de reservas", $"No puedes reservar hasta el {until:dd/MM/yyyy}. Motivo: {reason.Trim()}", "/Estudiante", "mdi mdi-alert");
        await _context.SaveChangesAsync(ct);
        return new(true, $"Estudiante suspendido hasta el {until:dd/MM/yyyy}.");
    }

    public async Task<StudentResult> LiftSuspensionAsync(int profileId, CancellationToken ct = default)
    {
        var profile = await _context.StudentProfiles.AsTracking().FirstOrDefaultAsync(p => p.Id == profileId, ct);
        if (profile == null) return new(false, "El estudiante no existe.");
        profile.SuspendedUntil = null;
        profile.SuspensionReason = null;
        await _context.SaveChangesAsync(ct);
        return new(true, "Suspensión levantada.");
    }

    /// <summary>Baja (soft): la cuenta queda inactiva y sus reservas futuras se cancelan.</summary>
    public async Task<StudentResult> DeactivateAsync(int profileId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return new(false, "Indique el motivo de la baja.");
        var profile = await _context.StudentProfiles.AsTracking().Include(p => p.User).FirstOrDefaultAsync(p => p.Id == profileId, ct);
        if (profile?.User is not User user) return new(false, "El estudiante no existe.");
        profile.AccountStatus = StudentAccountStatus.Deactivated;
        profile.ReviewNote = reason.Trim();
        user.Status = GeneralStatus.Inactivo;
        var today = _now().Date;
        foreach (var r in await _context.SlotReservations.AsTracking()
                     .Where(r => r.StudentProfileId == profileId && Holding.Contains(r.Status) && r.Slot!.Date >= today).ToListAsync(ct))
        {
            r.Status = SlotReservationStatus.Cancelled;
            r.CancelledDate = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync(ct);
        return new(true, $"{user.FullName} fue dado de baja.");
    }

    // ───────────────────────────── cupos

    public async Task<StudentResult> CreateSlotAsync(int laboratoryId, DateTime date, int startPeriodId, int periodCount, int capacity,
        string? notes, int byId, CancellationToken ct = default)
    {
        date = date.Date;
        if (date < _now().Date) return new(false, "La fecha ya pasó.");
        if (date.DayOfWeek == DayOfWeek.Sunday) return new(false, "Los domingos no hay actividades.");
        if (capacity is < 1 or > 30) return new(false, "El cupo debe estar entre 1 y 30 estudiantes.");
        var lab = await _context.Laboratories.AsNoTracking().Where(l => l.Id == laboratoryId && l.Status == GeneralStatus.Activo)
            .Select(l => new { l.Id, l.Code }).FirstOrDefaultAsync(ct);
        if (lab == null) return new(false, "El laboratorio no existe o está inactivo.");
        var start = await _context.ClassPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == startPeriodId, ct);
        var maxOrder = await _context.ClassPeriods.AsNoTracking().MaxAsync(p => (int?)p.Order, ct) ?? PlanningSlot.MaxPeriods;
        if (start == null || !PlanningSlot.Fits(start.Order, periodCount, maxOrder)) return new(false, "Los periodos no caben en la jornada.");
        var suspended = await _context.AcademicDays.AsNoTracking()
            .AnyAsync(d => d.Date == date && d.Status != AcademicDayStatus.Enabled, ct);
        if (suspended) return new(false, $"El {date:dd/MM/yyyy} está suspendido en el calendario académico.");

        var conflicts = await _occupancy.FindConflictsAsync(laboratoryId, date, start.Order, periodCount, null, ct);
        if (conflicts.Count > 0)
            return new(false, $"El laboratorio {lab.Code} está ocupado en esos periodos: {string.Join("; ", conflicts.Select(c => $"{c.Label} ({c.PeriodsText})"))}.");

        var count = await _context.FreePracticeSlots.CountAsync(ct);
        var slot = new FreePracticeSlot
        {
            Code = $"PL-{date:yyMMdd}-{count + 1:D3}", LaboratoryId = laboratoryId, Date = date, StartPeriodId = start.Id,
            PeriodCount = periodCount, Capacity = capacity, Notes = notes?.Trim(), CreatedById = byId
        };
        _context.FreePracticeSlots.Add(slot);
        await _context.SaveChangesAsync(ct);
        return new(true, $"Cupo {slot.Code} abierto en {lab.Code} el {date:dd/MM/yyyy} ({PlanningSlot.PeriodsText(start.Order, periodCount)}) para {capacity} estudiantes.", slot.Id);
    }

    /// <summary>Cancela el cupo (soft) y sus reservas vigentes, avisando a cada estudiante.</summary>
    public async Task<StudentResult> CancelSlotAsync(int slotId, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return new(false, "Indique el motivo de la cancelación.");
        var slot = await _context.FreePracticeSlots.AsTracking().Include(s => s.Reservations).ThenInclude(r => r.Student)
            .FirstOrDefaultAsync(s => s.Id == slotId, ct);
        if (slot == null || slot.Status == FreePracticeSlotStatus.Cancelled) return new(false, "El cupo no existe o ya fue cancelado.");
        slot.Status = FreePracticeSlotStatus.Cancelled;
        slot.Notes = $"{slot.Notes} [Cancelado: {reason.Trim()}]".Trim();
        foreach (var r in slot.Reservations.Where(r => Holding.Contains(r.Status)))
        {
            r.Status = SlotReservationStatus.Cancelled;
            r.CancelledDate = DateTime.UtcNow;
            r.ReviewNote = "Cupo cancelado: " + reason.Trim();
            Notify(r.Student!.UserId, "Cupo cancelado", $"El cupo {slot.Code} del {slot.Date:dd/MM/yyyy} fue cancelado. Motivo: {reason.Trim()}", "/Estudiante", "mdi mdi-calendar-remove");
        }
        await _context.SaveChangesAsync(ct);
        return new(true, $"Cupo {slot.Code} cancelado.");
    }

    public async Task<IReadOnlyList<SlotView>> ListSlotsAsync(DateTime from, DateTime? to = null, bool includeCancelled = false, CancellationToken ct = default)
    {
        var query = _context.FreePracticeSlots.AsNoTracking().Where(s => s.Date >= from.Date);
        if (to is DateTime end) query = query.Where(s => s.Date <= end.Date);
        if (!includeCancelled) query = query.Where(s => s.Status == FreePracticeSlotStatus.Open);
        return await query.OrderBy(s => s.Date).ThenBy(s => s.StartPeriod!.Order)
            .Select(s => new SlotView(s.Id, s.Code, s.LaboratoryId, s.Laboratory!.Code, s.Laboratory.Name, s.Date,
                s.StartPeriod!.Order, s.PeriodCount, s.StartPeriod.StartTime,
                _context.ClassPeriods.Where(p => p.Order == s.StartPeriod.Order + s.PeriodCount - 1).Select(p => (TimeSpan?)p.EndTime).FirstOrDefault(),
                s.Capacity,
                s.Reservations.Count(r => r.Status == SlotReservationStatus.Approved || r.Status == SlotReservationStatus.Attended),
                s.Reservations.Count(r => r.Status == SlotReservationStatus.Pending),
                s.Status, s.Notes))
            .ToListAsync(ct);
    }

    // ───────────────────────────── reservas

    private static DateTime WeekStart(DateTime date) => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public async Task<StudentResult> ReserveAsync(int userId, int slotId, string purpose, IReadOnlyList<ReservationLine> lines, CancellationToken ct = default)
    {
        var profile = await _context.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile == null || profile.AccountStatus != StudentAccountStatus.Approved) return new(false, "Tu cuenta no está aprobada.");
        if (profile.SuspendedUntil is DateTime until && until.Date >= _now().Date)
            return new(false, $"Estás suspendido hasta el {until:dd/MM/yyyy}: no puedes reservar.");
        if (string.IsNullOrWhiteSpace(purpose)) return new(false, "Cuenta qué vas a practicar.");

        var slot = (await ListSlotsAsync(DateTime.MinValue, null, false, ct)).FirstOrDefault(s => s.Id == slotId);
        if (slot == null) return new(false, "El cupo no existe o fue cancelado.");
        if (slot.StartsAt <= _now()) return new(false, "El cupo ya empezó o ya pasó.");
        if (slot.Free <= 0) return new(false, "El cupo está lleno.");

        var mine = await _context.SlotReservations.AsNoTracking()
            .Where(r => r.StudentProfileId == profile.Id && Holding.Contains(r.Status))
            .Select(r => new { r.FreePracticeSlotId, r.Slot!.Date }).ToListAsync(ct);
        if (mine.Any(r => r.FreePracticeSlotId == slotId)) return new(false, "Ya pediste un lugar en este cupo.");
        var week = WeekStart(slot.Date);
        if (mine.Count(r => WeekStart(r.Date) == week) >= WeeklyLimit)
            return new(false, $"Ya tienes {WeeklyLimit} reservas esa semana (el máximo).");

        var clean = lines.Where(l => l.Quantity > 0).GroupBy(l => l.ArticleId)
            .Select(g => new ReservationLine(g.Key, g.Sum(x => x.Quantity))).ToList();
        if (clean.Count > 0)
        {
            var ids = clean.Select(l => l.ArticleId).ToList();
            var valid = await _context.Articles.AsNoTracking().CountAsync(a => ids.Contains(a.Id) && a.Status == GeneralStatus.Activo, ct);
            if (valid != ids.Count) return new(false, "Algún artículo del material no existe o está inactivo.");
        }

        var reservation = new SlotReservation
        {
            FreePracticeSlotId = slotId, StudentProfileId = profile.Id, Purpose = purpose.Trim(),
            Items = clean.Select(l => new SlotReservationItem { ArticleId = l.ArticleId, Quantity = l.Quantity }).ToList()
        };
        _context.SlotReservations.Add(reservation);
        await NotifyRolesAsync(AuthorizationHelper.ManagementRoles.Split(','), "Nueva reserva de práctica libre",
            $"Un estudiante pidió un lugar en el cupo {slot.Code} del {slot.Date:dd/MM/yyyy}.", "/PracticaLibre/Reservas", ct);
        await _context.SaveChangesAsync(ct);
        return new(true, $"Pediste un lugar en el cupo {slot.Code} del {slot.Date:dd/MM/yyyy} ({slot.TimeText}). La Inge revisará tu reserva.", reservation.Id);
    }

    public async Task<StudentResult> CancelReservationAsync(int userId, int reservationId, CancellationToken ct = default)
    {
        var reservation = await _context.SlotReservations.AsTracking().Include(r => r.Student).Include(r => r.Slot).ThenInclude(s => s!.StartPeriod)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.Student!.UserId == userId, ct);
        if (reservation == null) return new(false, "La reserva no existe.");
        if (!Holding.Contains(reservation.Status)) return new(false, "La reserva ya no está vigente.");
        var startsAt = reservation.Slot!.Date.Date + (reservation.Slot.StartPeriod?.StartTime ?? TimeSpan.Zero);
        if (startsAt - _now() < CancelWindow)
            return new(false, "Solo se puede cancelar hasta 24 horas antes. Si no puedes ir, avisa a la Inge: si no te presentas, cuenta como falta.");
        reservation.Status = SlotReservationStatus.Cancelled;
        reservation.CancelledDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return new(true, "Reserva cancelada.");
    }

    public async Task<StudentResult> ReviewReservationAsync(int reservationId, bool approve, string? note, int reviewerId, CancellationToken ct = default)
    {
        var reservation = await _context.SlotReservations.AsTracking().Include(r => r.Student).Include(r => r.Slot)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);
        if (reservation == null) return new(false, "La reserva no existe.");
        if (reservation.Status != SlotReservationStatus.Pending) return new(false, "La reserva ya fue revisada o cancelada.");
        if (!approve && string.IsNullOrWhiteSpace(note)) return new(false, "Indique el motivo del rechazo.");
        if (approve)
        {
            var approved = await _context.SlotReservations.CountAsync(r => r.FreePracticeSlotId == reservation.FreePracticeSlotId
                && (r.Status == SlotReservationStatus.Approved || r.Status == SlotReservationStatus.Attended), ct);
            if (approved >= reservation.Slot!.Capacity) return new(false, "El cupo ya está completo con reservas aprobadas.");
        }
        reservation.Status = approve ? SlotReservationStatus.Approved : SlotReservationStatus.Rejected;
        reservation.ReviewedById = reviewerId;
        reservation.ReviewedDate = DateTime.UtcNow;
        reservation.ReviewNote = note?.Trim();
        Notify(reservation.Student!.UserId,
            approve ? "Reserva aprobada" : "Reserva rechazada",
            approve ? $"Tu lugar en el cupo {reservation.Slot!.Code} del {reservation.Slot.Date:dd/MM/yyyy} está confirmado."
                    : $"Tu reserva del cupo {reservation.Slot!.Code} fue rechazada. Motivo: {note!.Trim()}",
            "/Estudiante", approve ? "mdi mdi-check-circle" : "mdi mdi-close-circle");
        await _context.SaveChangesAsync(ct);
        return new(true, approve ? "Reserva aprobada." : "Reserva rechazada.");
    }

    /// <summary>Asistencia de una reserva aprobada: asistió, no se presentó o mal uso (estos dos quedan como falta en la ficha).</summary>
    public async Task<StudentResult> MarkAttendanceAsync(int reservationId, SlotReservationStatus result, string? note, int byId, CancellationToken ct = default)
    {
        if (result is not (SlotReservationStatus.Attended or SlotReservationStatus.NoShow or SlotReservationStatus.Misuse))
            return new(false, "Resultado de asistencia no válido.");
        if (result == SlotReservationStatus.Misuse && string.IsNullOrWhiteSpace(note))
            return new(false, "Describa el mal uso.");
        var reservation = await _context.SlotReservations.AsTracking().Include(r => r.Slot).Include(r => r.Student)
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);
        if (reservation == null) return new(false, "La reserva no existe.");
        if (reservation.Status is not (SlotReservationStatus.Approved or SlotReservationStatus.Attended or SlotReservationStatus.NoShow or SlotReservationStatus.Misuse))
            return new(false, "Solo se toma asistencia de reservas aprobadas.");
        if (reservation.Slot!.Date.Date > _now().Date) return new(false, "Todavía no es el día de la práctica.");

        var hadIncident = reservation.Status is SlotReservationStatus.NoShow or SlotReservationStatus.Misuse;
        reservation.Status = result;
        reservation.AttendanceById = byId;
        reservation.AttendanceDate = DateTime.UtcNow;
        reservation.AttendanceNote = note?.Trim();
        if (result is SlotReservationStatus.NoShow or SlotReservationStatus.Misuse && !hadIncident)
            _context.StudentIncidents.Add(new StudentIncident
            {
                StudentProfileId = reservation.StudentProfileId, SlotReservationId = reservation.Id,
                Type = result == SlotReservationStatus.NoShow ? StudentIncidentType.NoShow : StudentIncidentType.Misuse,
                Note = note?.Trim(), CreatedById = byId
            });
        await _context.SaveChangesAsync(ct);
        return new(true, "Asistencia registrada.");
    }

    public async Task<StudentResult> AddIncidentAsync(int profileId, StudentIncidentType type, string note, int byId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(note)) return new(false, "Describa la falta.");
        if (!await _context.StudentProfiles.AnyAsync(p => p.Id == profileId, ct)) return new(false, "El estudiante no existe.");
        _context.StudentIncidents.Add(new StudentIncident { StudentProfileId = profileId, Type = type, Note = note.Trim(), CreatedById = byId });
        await _context.SaveChangesAsync(ct);
        return new(true, "Falta registrada en la ficha.");
    }

    public static string Label<T>(T value) where T : struct, Enum =>
        typeof(T).GetField(value.ToString())?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
            .Cast<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault()?.Name ?? value.ToString();
}
