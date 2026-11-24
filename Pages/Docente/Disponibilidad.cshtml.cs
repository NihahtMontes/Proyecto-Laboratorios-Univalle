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
/// Consulta ( solo lectura) de disponibilidad: mapa de laboratorios × periodos de un día y stock libre de artículos
/// en un horario. Las celdas salen de la misma fuente de ocupación que la grilla del administrador
/// (<see cref="ILabWeekOccupancyService"/>): lo ocupado coincide para todos los roles, con el nombre de la práctica.
/// </summary>
public class DisponibilidadModel : TeacherPageBase
{
    private readonly ILabWeekOccupancyService _week;

    public DisponibilidadModel(ApplicationDbContext db, ITeacherRequestService requests, ILabWeekOccupancyService week) : base(db, requests)
        => _week = week;

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

    /// <summary>
    /// Estado visible de una celda: free, busy, slot (práctica libre de estudiantes), pending, mine, minepending, off.
    /// <paramref name="PlanningId"/> solo en las celdas propias; Label/Title llevan el nombre de la práctica y su detalle.
    /// </summary>
    public sealed record CellInfo(int PeriodId, string State, int? PlanningId = null, string? Label = null, string? Title = null, string? Clean = null);

    public sealed record WeekDay(DateTime Date, bool Enabled, string? Note, IReadOnlyList<CellInfo> Cells);
    public sealed record WeekLab(int Id, string Code, string Name, IReadOnlyList<WeekDay> Days)
    {
        public int FreeCount => Days.Sum(d => d.Cells.Count(c => c.State == "free"));
        public int MineCount => Days.Sum(d => d.Cells.Count(c => c.State is "mine" or "minepending"));
    }

    /// <summary>Etiqueta de reserva cuando la celda no trae nombre propio.</summary>
    public static string CellLabel(string state) => state switch
    {
        "busy" => "Ocupado", "slot" => "Práctica libre", "pending" => "En revisión", "mine" => "Tu práctica",
        "minepending" => "Tu solicitud", "off" => "", _ => "Libre"
    };

    public sealed record LabRow(int Id, string Code, string Name, IReadOnlyList<CellInfo> Cells)
    {
        public int FreeCount => Cells.Count(c => c.State == "free");
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
        var occupancy = await _week.GetAsync(labs.Select(l => l.Id).ToList(), Date, Date, managementId);
        var pending = await LoadPendingAsync(Date, Date);

        Labs = labs.Select(l =>
        {
            var labDay = occupancy[l.Id].Days.FirstOrDefault();
            var cells = BuildCells(occupancy[l.Id], labDay, pending.Where(x => x.AllLaboratories || x.LabId == l.Id).ToList());
            return new LabRow(l.Id, l.Code, l.Name, cells);
        }).ToList();

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
    /// Semana académica de la gestión: por cada laboratorio solicitable, lunes a sábado × periodos con el estado
    /// de cada celda desde la fuente única de ocupación (igual que la grilla del admin), con el nombre de la práctica.
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

        var occupancy = await _week.GetAsync(shown.Select(l => l.Id).ToList(), Week.Start, Week.End, managementId);
        var pending = await LoadPendingAsync(Week.Start, Week.End);

        var dates = Enumerable.Range(0, (Week.End - Week.Start).Days + 1).Select(i => Week.Start.AddDays(i))
            .Where(d => d.DayOfWeek != DayOfWeek.Sunday).ToList();
        foreach (var lab in shown)
        {
            var labDays = new List<WeekDay>();
            foreach (var d in dates)
            {
                var labDay = occupancy[lab.Id].Day(d);
                var cells = BuildCells(occupancy[lab.Id], labDay, pending.Where(x => x.Date == d && (x.AllLaboratories || x.LabId == lab.Id)).ToList());
                labDays.Add(new WeekDay(d, labDay?.Enabled ?? false, labDay?.Note, cells));
            }
            WeekLabs.Add(new WeekLab(lab.Id, lab.Code, lab.Name, labDays));
        }
    }

    /// <summary>Solicitudes en revisión (no ocupan): avisan el interés por el horario.</summary>
    private async Task<List<PendingRow>> LoadPendingAsync(DateTime from, DateTime to)
    {
        var end = to.Date.AddDays(1);
        return await Db.Plannings.AsNoTracking()
            .Where(p => !p.IsDeleted && PlanningStatus.PendingReview.Contains(p.Status) && p.ScheduledDate >= from && p.ScheduledDate < end
                && (p.LaboratoryId != null || p.AllLaboratories) && p.StartPeriod != null && p.PeriodCount != null)
            .Select(p => new PendingRow(
                p.Id, p.LaboratoryId, p.AllLaboratories, p.TeacherPersonId, p.ScheduledDate.Date, p.StartPeriod!.Order, p.PeriodCount!.Value))
            .ToListAsync();
    }

    // Registrado como tipo concreto para poder proyectarlo en EF.
    public sealed record PendingRow(int Id, int? LabId, bool AllLaboratories, int TeacherPersonId, DateTime Date, int Start, int Count);

    /// <summary>
    /// Convierte un día de la fuente de ocupación en celdas de la vista: ocupado/práctica libre/tu práctica con su
    /// nombre y detalle, pendientes (tuyas o de otros) solo cuando la celda está libre, y la franja de limpieza
    /// de 10 minutos en el último periodo de cada bloque.
    /// </summary>
    private List<CellInfo> BuildCells(LabWeekOccupancy week, LabWeekDay? day, List<PendingRow> dayPending)
    {
        if (day is null) return Periods.Select(p => new CellInfo(p.Id, "off")).ToList();

        var cleanByOrder = new Dictionary<int, string>();
        foreach (var block in week.Blocks.Where(b => b.Date == day.Date))
        {
            if (cleanByOrder.ContainsKey(block.LastOrder)) continue;
            if (LabWeekOccupancyService.CleaningAfter(week.Periods, block.LastOrder)?.Text is { } text)
                cleanByOrder[block.LastOrder] = text;
        }

        var cells = new List<CellInfo>(Periods.Count);
        for (var i = 0; i < Periods.Count; i++)
        {
            var period = Periods[i];
            var cell = day.Cells.ElementAtOrDefault(i);
            if (!day.Enabled || cell is null || cell.State == LabCellState.Off)
            {
                cells.Add(new CellInfo(period.Id, "off"));
                continue;
            }

            if (cell.State == LabCellState.Busy && cell.OccupiedBy is { } block)
            {
                var clean = block.LastOrder == period.Order && cleanByOrder.TryGetValue(block.LastOrder, out var strip) ? strip : null;
                if (block.IsFreePracticeSlot)
                {
                    cells.Add(new CellInfo(period.Id, "slot", null, "Práctica libre",
                        $"Cupo de práctica libre para estudiantes{(block.Capacity is int cap ? $" · cupo {cap}" : "")}{(string.IsNullOrWhiteSpace(block.Code) ? "" : " · " + block.Code.Trim())} · {block.PeriodsText}"));
                    continue;
                }

                var label = block.ShortLabel();
                var detail = block.Detail();
                cells.Add(block.TeacherPersonId == Teacher.PersonId
                    ? new CellInfo(period.Id, "mine", block.PlanningId, label == "Ocupado" ? "Tu práctica" : label, $"Tu práctica · {detail}", clean)
                    : new CellInfo(period.Id, "busy", null, label, detail, clean));
                continue;
            }

            // Libre: las solicitudes en revisión no ocupan, solo avisan.
            var covering = dayPending.Where(x => period.Order >= x.Start && period.Order < x.Start + x.Count).ToList();
            var mine = covering.FirstOrDefault(x => x.TeacherPersonId == Teacher.PersonId);
            if (mine != null)
                cells.Add(new CellInfo(period.Id, "minepending", mine.Id, "Tu solicitud", "Tu solicitud en revisión: aún no reserva el laboratorio"));
            else if (covering.Count > 0)
                cells.Add(new CellInfo(period.Id, "pending", null, "En revisión", "Solicitud de otro docente en revisión: aún no reserva el laboratorio"));
            else
                cells.Add(new CellInfo(period.Id, "free"));
        }
        return cells;
    }
}
