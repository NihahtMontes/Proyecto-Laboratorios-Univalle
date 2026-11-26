using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Services.Occupancy;

/// <summary>
/// Cupos de práctica libre abiertos: ocupan el laboratorio (no a un docente) en sus periodos, así que ninguna
/// planificación, solicitud ni importación puede caer encima, y la Inge no puede abrir dos cupos superpuestos.
/// </summary>
public sealed class FreePracticeSlotOccupancySource : IOccupancySource
{
    public const string SourceName = "Práctica libre";

    private readonly ApplicationDbContext _context;

    public FreePracticeSlotOccupancySource(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<OccupancyBlock>> GetAsync(OccupancyQuery query, CancellationToken ct = default)
    {
        // Un cupo no ocupa docente: si la consulta es solo por docentes, no aporta bloques.
        if (query.TeacherPersonIds is { Count: > 0 }) return [];

        var from = query.From.Date;
        var toExclusive = query.To.Date.AddDays(1);
        var slots = _context.FreePracticeSlots.AsNoTracking()
            .Where(s => s.Status == FreePracticeSlotStatus.Open && s.Date >= from && s.Date < toExclusive);
        if (query.LaboratoryIds is { Count: > 0 } labs)
            slots = slots.Where(s => labs.Contains(s.LaboratoryId));

        var rows = await slots
            .Select(s => new { s.Id, s.Code, s.LaboratoryId, s.Date, StartOrder = s.StartPeriod!.Order, s.PeriodCount, s.Capacity })
            .ToListAsync(ct);
        return rows.Select(s => new OccupancyBlock(SourceName, null, $"{SourceName} {s.Code} (cupo {s.Capacity})",
            s.LaboratoryId, null, s.Date.Date, s.StartOrder, s.PeriodCount,
            Code: s.Code, PracticeName: SourceName, FreePracticeSlotId: s.Id, Capacity: s.Capacity)).ToList();
    }
}
