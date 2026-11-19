using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.Docente;

/// <summary>
/// Consulta (solo lectura) de disponibilidad: mapa de laboratorios × periodos de un día y stock libre de artículos
/// en un horario. No muestra datos de prácticas de otros docentes, solo "ocupado".
/// </summary>
public class DisponibilidadModel : TeacherPageBase
{
    private readonly IOccupancyService _occupancy;

    public DisponibilidadModel(ApplicationDbContext db, ITeacherRequestService requests, IOccupancyService occupancy) : base(db, requests)
        => _occupancy = occupancy;

    public DateTime Date { get; private set; }
    public List<ClassPeriod> Periods { get; private set; } = [];
    public List<LabRow> Labs { get; private set; } = [];
    public string? DayNote { get; private set; }
    public int? FromPeriodId { get; private set; }
    public int PeriodCount { get; private set; } = 2;
    public string? Term { get; private set; }
    public IReadOnlyList<MaterialLineCheck> Articles { get; private set; } = [];

    // ── Vista por semana académica ──
    /// <summary>"dia" (mapa de un día, todos los laboratorios) o "semana" (semana académica, por laboratorio).</summary>
    public string View { get; private set; } = "dia";
    public IReadOnlyList<WeekOption> Weeks { get; private set; } = [];
    public WeekOption? Week { get; private set; }
    public int? LabId { get; private set; }
    public IReadOnlyList<(int Id, string Label)> LabOptions { get; private set; } = [];
    public List<WeekLab> WeekLabs { get; private set; } = [];

    public sealed record WeekOption(int Number, DateTime Start, DateTime End)
    {
        public string Label => $"Semana {Number} · {Start:dd/MM} al {End:dd/MM}";
    }
    /// <summary>State: free, busy, gap, pending, mine, minepending, off. PlanningId solo en las celdas propias.</summary>
    public sealed record WeekCell(int PeriodId, string State, int? PlanningId);
    public sealed record WeekDay(DateTime Date, bool Enabled, string? Note, IReadOnlyList<WeekCell> Cells);
    public sealed record WeekLab(int Id, string Code, string Name, IReadOnlyList<WeekDay> Days)
    {
        public int FreeCount => Days.Sum(d => d.Cells.Count(c => c.State == "free"));
        public int MineCount => Days.Sum(d => d.Cells.Count(c => c.State is "mine" or "minepending"));
    }

    public static string CellLabel(string state) => state switch
    {
        "busy" => "Ocupado", "gap" => "Limpieza", "pending" => "Revisión", "mine" => "Tuya",
        "minepending" => "Tu solicitud", "off" => "", _ => "Libre"
    };

    public sealed record LabRow(int Id, string Code, string Name, string?[] Cells)
    {
        public int FreeCount => Cells.Count(c => c == "free");
    }

    public async Task OnGetAsync(DateTime? date, int? fromPeriodId, int? periodCount, string? term, string? view, int? week, int? labId)
    {
        Date = (date ?? Window.MinDate).Date;
        Periods = await Db.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
        if (view == "semana")
        {
            View = "semana";
            await BuildWeekAsync(week, date, labId);
            return;
        }
        FromPeriodId = fromPeriodId ?? Periods.FirstOrDefault()?.Id;
        PeriodCount = Math.Clamp(periodCount ?? 2, 1, 6);
        Term = string.IsNullOrWhiteSpace(term) ? null : term.Trim();

        var managementId = Window.ManagementId ?? 0;
        var day = await Db.AcademicDays.AsNoTracking()
            .Where(d => d.AcademicWeek.ManagementId == managementId && d.Date == Date)
            .Select(d => new { d.Status, d.Reason })
            .FirstOrDefaultAsync();
        DayNote = day == null ? "Ese día no tiene clases en el calendario académico."
            : day.Status != AcademicDayStatus.Enabled ? $"Día suspendido{(string.IsNullOrWhiteSpace(day.Reason) ? "" : $": {day.Reason}")}." : null;

        var labs = await Db.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo && (l.Type == null || !TeacherRequestService.NonRequestableTypes.Contains(l.Type)))
            .OrderBy(l => l.Code)
            .Select(l => new { l.Id, l.Code, l.Name })
            .ToListAsync();
        var blocks = await _occupancy.GetAsync(new OccupancyQuery(Date, Date));
        var pending = await Db.Plannings.AsNoTracking()
            .Where(p => !p.IsDeleted && PlanningStatus.PendingReview.Contains(p.Status) && p.ScheduledDate >= Date && p.ScheduledDate < Date.AddDays(1)
                && p.LaboratoryId != null && p.StartPeriod != null && p.PeriodCount != null)
            .Select(p => new { LabId = p.LaboratoryId!.Value, Start = p.StartPeriod!.Order, Count = p.PeriodCount!.Value })
            .ToListAsync();

        Labs = labs.Select(l => new LabRow(l.Id, l.Code, l.Name, Periods.Select(p =>
        {
            if (DayNote != null) return "off";
            var labBlocks = blocks.Where(b => b.LaboratoryId == l.Id).ToList();
            if (labBlocks.Any(b => b.TeacherPersonId == Teacher.PersonId && p.Order >= b.StartOrder && p.Order <= b.LastOrder)) return "mine";
            if (labBlocks.Any(b => p.Order >= b.StartOrder && p.Order <= b.LastOrder)) return "busy";
            if (labBlocks.Any(b => p.Order == b.StartOrder - 1 || p.Order == b.LastOrder + 1)) return "gap";
            if (pending.Any(x => x.LabId == l.Id && p.Order >= x.Start && p.Order < x.Start + x.Count)) return "pending";
            return "free";
        }).ToArray<string?>())).ToList();

        if (Term != null && FromPeriodId is int startId)
        {
            var query = Db.Articles.AsNoTracking().Where(a => a.Status == GeneralStatus.Activo);
            foreach (var word in Term.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(4))
                query = query.Where(a => a.Name.Contains(word) || a.Code.Contains(word));
            var ids = await query.OrderBy(a => a.Name).Take(25).Select(a => a.Id).ToListAsync();
            // Cantidad 1 por artículo: interesa cuánto queda libre en ese horario.
            Articles = await Requests.CheckMaterialAsync(Date, startId, PeriodCount, ids.Select(id => new RequestLineInput(id, 1)).ToList(), null);
        }
    }

    /// <summary>
    /// Semana académica de la gestión: por cada laboratorio solicitable, lunes a sábado × periodos con el estado de cada
    /// celda. De las prácticas ajenas solo se sabe que el espacio está ocupado; las propias llevan a su solicitud.
    /// </summary>
    private async Task BuildWeekAsync(int? weekNumber, DateTime? date, int? labId)
    {
        var managementId = Window.ManagementId ?? 0;
        Weeks = (await Db.AcademicWeeks.AsNoTracking()
            .Where(w => w.ManagementId == managementId)
            .OrderBy(w => w.Number)
            .Select(w => new { w.Number, w.StartDate, w.EndDate })
            .ToListAsync())
            .Select(w => new WeekOption(w.Number, w.StartDate.Date, w.EndDate.Date)).ToList();
        if (Weeks.Count == 0) return;

        var reference = (date ?? DateTime.Today).Date;
        Week = Weeks.FirstOrDefault(w => w.Number == weekNumber)
            ?? Weeks.FirstOrDefault(w => reference >= w.Start && reference <= w.End)
            ?? Weeks.FirstOrDefault(w => w.End >= reference)
            ?? Weeks[^1];

        var labs = await Db.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo && (l.Type == null || !TeacherRequestService.NonRequestableTypes.Contains(l.Type)))
            .OrderBy(l => l.Code)
            .Select(l => new { l.Id, l.Code, l.Name })
            .ToListAsync();
        LabOptions = labs.Select(l => (l.Id, l.Code + " · " + l.Name)).ToList();
        LabId = labs.Any(l => l.Id == labId) ? labId : null;
        var shown = LabId is int only ? labs.Where(l => l.Id == only).ToList() : labs;

        var from = Week.Start;
        var to = Week.End;
        var days = await Db.AcademicDays.AsNoTracking()
            .Where(d => d.AcademicWeek.ManagementId == managementId && d.Date >= from && d.Date <= to)
            .Select(d => new { d.Date, d.Status, d.Reason })
            .ToListAsync();
        var blocks = await _occupancy.GetAsync(new OccupancyQuery(from, to));
        var end = to.AddDays(1);
        var pending = await Db.Plannings.AsNoTracking()
            .Where(p => !p.IsDeleted && PlanningStatus.PendingReview.Contains(p.Status) && p.ScheduledDate >= from && p.ScheduledDate < end
                && p.LaboratoryId != null && p.StartPeriod != null && p.PeriodCount != null)
            .Select(p => new { p.Id, p.TeacherPersonId, p.ScheduledDate, LabId = p.LaboratoryId!.Value, Start = p.StartPeriod!.Order, Count = p.PeriodCount!.Value })
            .ToListAsync();

        var dates = Enumerable.Range(0, (to - from).Days + 1).Select(i => from.AddDays(i))
            .Where(d => d.DayOfWeek != DayOfWeek.Sunday).ToList();
        foreach (var lab in shown)
        {
            var labDays = new List<WeekDay>();
            foreach (var d in dates)
            {
                var info = days.FirstOrDefault(x => x.Date.Date == d);
                var enabled = info != null && info.Status == AcademicDayStatus.Enabled;
                var note = info == null ? "Sin clases"
                    : info.Status != AcademicDayStatus.Enabled ? (string.IsNullOrWhiteSpace(info.Reason) ? "Suspendido" : "Suspendido: " + info.Reason.Trim())
                    : null;
                var dayBlocks = blocks.Where(b => b.LaboratoryId == lab.Id && b.Date.Date == d).ToList();
                var dayPending = pending.Where(x => x.LabId == lab.Id && x.ScheduledDate.Date == d).ToList();
                var cells = Periods.Select(p =>
                {
                    if (!enabled) return new WeekCell(p.Id, "off", null);
                    var mine = dayBlocks.FirstOrDefault(b => b.TeacherPersonId == Teacher.PersonId && p.Order >= b.StartOrder && p.Order <= b.LastOrder);
                    if (mine != null) return new WeekCell(p.Id, "mine", mine.PlanningId);
                    if (dayBlocks.Any(b => p.Order >= b.StartOrder && p.Order <= b.LastOrder)) return new WeekCell(p.Id, "busy", null);
                    var myPending = dayPending.FirstOrDefault(x => x.TeacherPersonId == Teacher.PersonId && p.Order >= x.Start && p.Order < x.Start + x.Count);
                    if (myPending != null) return new WeekCell(p.Id, "minepending", myPending.Id);
                    if (dayBlocks.Any(b => p.Order == b.StartOrder - 1 || p.Order == b.LastOrder + 1)) return new WeekCell(p.Id, "gap", null);
                    if (dayPending.Any(x => p.Order >= x.Start && p.Order < x.Start + x.Count)) return new WeekCell(p.Id, "pending", null);
                    return new WeekCell(p.Id, "free", null);
                }).ToList();
                labDays.Add(new WeekDay(d, enabled, note, cells));
            }
            WeekLabs.Add(new WeekLab(lab.Id, lab.Code, lab.Name, labDays));
        }
    }
}
