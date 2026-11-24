using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services.Occupancy;

/// <summary>Periodo del catálogo con sus horas reales.</summary>
public sealed record LabPeriod(int Id, int Order, TimeSpan StartTime, TimeSpan EndTime)
{
    /// <summary>"08:35 – 09:25".</summary>
    public string Label => $"{StartTime:hh\\:mm} – {EndTime:hh\\:mm}";
}

/// <summary>Estado de una celda (laboratorio, fecha, periodo): libre, ocupada o día no hábil.</summary>
public enum LabCellState
{
    Free,
    Busy,
    Off
}

/// <summary>Celda de un periodo; <paramref name="OccupiedBy"/> es el bloque que la ocupa (null si está libre).</summary>
public sealed record LabCell(LabCellState State, OccupancyBlock? OccupiedBy = null)
{
    public bool IsBusy => State == LabCellState.Busy;
}

/// <summary>Día consultado: hábil o no (con su nota) y el estado de cada periodo del catálogo, en orden.</summary>
public sealed record LabWeekDay(DateTime Date, bool Enabled, string? Note, IReadOnlyList<LabCell> Cells);

/// <summary>
/// Franja de limpieza de 10 minutos de un bloque: el receso posterior a su último periodo si existe,
/// o los últimos 10 minutos de la práctica. Se dibuja como franja delgada; nunca bloquea un periodo.
/// </summary>
public sealed record LabCleaningStrip(TimeSpan From, TimeSpan To)
{
    /// <summary>"12:05–12:15".</summary>
    public string Text => $"{From:hh\\:mm}–{To:hh\\:mm}";
}

/// <summary>
/// Semana (o rango de fechas) de ocupación de un laboratorio. Única fuente de verdad de las celdas:
/// la usan la grilla del admin, la disponibilidad del docente, el asistente de solicitud y el modo
/// reprogramar/reponer, de modo que lo ocupado coincide siempre (cualquier gestión + cupos de práctica libre).
/// </summary>
public sealed record LabWeekOccupancy(
    int LaboratoryId,
    IReadOnlyList<LabPeriod> Periods,
    IReadOnlyList<OccupancyBlock> Blocks,
    IReadOnlyList<LabWeekDay> Days)
{
    /// <summary>Día consultado de la fecha (null si está fuera del rango pedido).</summary>
    public LabWeekDay? Day(DateTime date) => Days.FirstOrDefault(d => d.Date == date.Date);
}

/// <summary>
/// Construye las celdas de un laboratorio por día y periodo a partir de <see cref="IOccupancyService"/>
/// (planificaciones de cualquier gestión + cupos de práctica libre), el catálogo ClassPeriods y los días
/// académicos de la gestión. También calcula la franja de limpieza de cada bloque.
/// </summary>
public interface ILabWeekOccupancyService
{
    /// <summary>Celdas de un laboratorio entre dos fechas (inclusive).</summary>
    /// <param name="managementId">Gestión cuyo calendario decide los días hábiles (null: cualquier día académico de la fecha).</param>
    Task<LabWeekOccupancy> GetAsync(int laboratoryId, DateTime from, DateTime to, int? managementId = null, CancellationToken ct = default);

    /// <summary>Igual que el anterior para varios laboratorios, compartiendo las consultas de periodos, días y ocupación.</summary>
    Task<IReadOnlyDictionary<int, LabWeekOccupancy>> GetAsync(IReadOnlyCollection<int> laboratoryIds, DateTime from, DateTime to, int? managementId = null, CancellationToken ct = default);
}

public sealed class LabWeekOccupancyService : ILabWeekOccupancyService
{
    /// <summary>Duración de la limpieza de cada práctica (minutos).</summary>
    public const int CleaningMinutes = 10;

    private readonly ApplicationDbContext _context;
    private readonly IOccupancyService _occupancy;

    public LabWeekOccupancyService(ApplicationDbContext context, IOccupancyService occupancy)
    {
        _context = context;
        _occupancy = occupancy;
    }

    public async Task<LabWeekOccupancy> GetAsync(int laboratoryId, DateTime from, DateTime to, int? managementId = null, CancellationToken ct = default)
    {
        var weeks = await GetAsync([laboratoryId], from, to, managementId, ct);
        return weeks[laboratoryId];
    }

    public async Task<IReadOnlyDictionary<int, LabWeekOccupancy>> GetAsync(IReadOnlyCollection<int> laboratoryIds, DateTime from, DateTime to, int? managementId = null, CancellationToken ct = default)
    {
        var labIds = laboratoryIds.Distinct().ToList();
        if (labIds.Count == 0) return new Dictionary<int, LabWeekOccupancy>();

        var periods = await _context.ClassPeriods.AsNoTracking()
            .OrderBy(p => p.Order)
            .Select(p => new LabPeriod(p.Id, p.Order, p.StartTime, p.EndTime))
            .ToListAsync(ct);

        var daysQuery = _context.AcademicDays.AsNoTracking()
            .Where(d => d.Date >= from.Date && d.Date <= to.Date);
        if (managementId.HasValue)
            daysQuery = daysQuery.Where(d => d.AcademicWeek.ManagementId == managementId.Value);
        var dayRows = await daysQuery.Select(d => new { d.Date, d.Status, d.Reason }).ToListAsync(ct);
        var dayByDate = dayRows.GroupBy(d => d.Date.Date).ToDictionary(g => g.Key, g => g.First());

        var blocks = await _occupancy.GetAsync(new OccupancyQuery(from, to, labIds), ct);

        var result = new Dictionary<int, LabWeekOccupancy>(labIds.Count);
        foreach (var labId in labIds)
        {
            var labBlocks = blocks
                .Where(b => b.LaboratoryId == labId)
                .OrderBy(b => b.Date).ThenBy(b => b.StartOrder).ThenBy(b => b.Label, StringComparer.Ordinal)
                .ToList();

            var weekDays = new List<LabWeekDay>();
            for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
            {
                var exists = dayByDate.TryGetValue(date, out var day);
                var enabled = exists && day!.Status == AcademicDayStatus.Enabled;
                var note = !exists ? "Sin clases"
                    : enabled ? null
                    : string.IsNullOrWhiteSpace(day!.Reason) ? "Suspendido" : "Suspendido: " + day.Reason.Trim();

                var dayBlocks = labBlocks.Where(b => b.Date == date).ToList();
                var cells = periods.Select(p =>
                {
                    if (!enabled) return new LabCell(LabCellState.Off);
                    var block = dayBlocks.FirstOrDefault(b => p.Order >= b.StartOrder && p.Order <= b.LastOrder);
                    return block is null ? new LabCell(LabCellState.Free) : new LabCell(LabCellState.Busy, block);
                }).ToList();
                weekDays.Add(new LabWeekDay(date, enabled, note, cells));
            }

            result[labId] = new LabWeekOccupancy(labId, periods, labBlocks, weekDays);
        }

        return result;
    }

    /// <summary>
    /// Franja de limpieza que sigue al bloque cuyo último periodo es <paramref name="lastOrder"/>: el receso de
    /// 10 minutos posterior si el catálogo tiene uno, o los últimos <see cref="CleaningMinutes"/> minutos de la
    /// práctica. Se calcula con las horas reales de ClassPeriods (nunca valores fijos).
    /// </summary>
    public static LabCleaningStrip? CleaningAfter(IReadOnlyList<LabPeriod> periods, int lastOrder)
    {
        var last = periods.FirstOrDefault(p => p.Order == lastOrder);
        if (last is null) return null;

        var next = periods.FirstOrDefault(p => p.Order == lastOrder + 1);
        if (next is not null && next.StartTime > last.EndTime)
            return new LabCleaningStrip(last.EndTime, next.StartTime);

        var from = last.EndTime - TimeSpan.FromMinutes(CleaningMinutes);
        if (from < last.StartTime) from = last.StartTime;
        return new LabCleaningStrip(from, last.EndTime);
    }
}
