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

    public sealed record LabRow(int Id, string Code, string Name, string?[] Cells)
    {
        public int FreeCount => Cells.Count(c => c == "free");
    }

    public async Task OnGetAsync(DateTime? date, int? fromPeriodId, int? periodCount, string? term)
    {
        Date = (date ?? Window.MinDate).Date;
        Periods = await Db.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();
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
}
