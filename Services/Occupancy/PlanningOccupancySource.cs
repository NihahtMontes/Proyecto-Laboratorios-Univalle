using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Services.Occupancy;

/// <summary>
/// Ocupación generada por planificaciones vigentes (no borradas y con estado que ocupa, ver <see cref="PlanningStatus.Occupies"/>).
/// </summary>
/// <remarks>
/// - Planificaciones con periodo: el bloque es StartPeriod.Order + PeriodCount.
/// - Legadas sin periodo pero con horas: se convierten al rango de periodos del catálogo que se solapan con [StartTime, EndTime)
///   (sin EndTime válido se toma el periodo que contiene StartTime).
/// - Sin periodo ni horas, o sin laboratorio (AllLaboratories): no se pueden ubicar y se omiten.
/// </remarks>
public sealed class PlanningOccupancySource : IOccupancySource
{
    public const string SourceName = "Planificación";

    private readonly ApplicationDbContext _context;

    public PlanningOccupancySource(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<OccupancyBlock>> GetAsync(OccupancyQuery query, CancellationToken ct = default)
    {
        var from = query.From.Date;
        var toExclusive = query.To.Date.AddDays(1);

        var plannings = _context.Plannings.AsNoTracking()
            .Where(p => !p.IsDeleted && !PlanningStatus.NonOccupying.Contains(p.Status)
                && p.LaboratoryId != null
                && p.ScheduledDate >= from && p.ScheduledDate < toExclusive);

        if (query.LaboratoryIds is { Count: > 0 } labs)
            plannings = plannings.Where(p => labs.Contains(p.LaboratoryId!.Value));
        if (query.TeacherPersonIds is { Count: > 0 } teachers)
            plannings = plannings.Where(p => teachers.Contains(p.TeacherPersonId));
        if (query.ExcludePlanningIds is { Count: > 0 } excluded)
            plannings = plannings.Where(p => !excluded.Contains(p.Id));

        var rows = await plannings
            .OrderBy(p => p.ScheduledDate).ThenBy(p => p.Id)
            .Select(p => new
            {
                p.Id,
                LaboratoryId = p.LaboratoryId!.Value,
                p.TeacherPersonId,
                p.ScheduledDate,
                StartOrder = p.StartPeriod != null ? (int?)p.StartPeriod.Order : null,
                p.PeriodCount,
                p.StartTime,
                p.EndTime,
                p.PracticeName,
                Subject = p.Subject != null ? p.Subject.Name : null,
                Group = p.CourseGroup != null ? p.CourseGroup.Name : p.GroupName
            })
            .ToListAsync(ct);

        if (rows.Count == 0) return [];

        List<ClassPeriod>? catalog = null;
        var blocks = new List<OccupancyBlock>(rows.Count);
        foreach (var row in rows)
        {
            int startOrder, count;
            if (row.StartOrder is int order && row.PeriodCount is int periods && periods >= 1)
            {
                (startOrder, count) = (order, periods);
            }
            else if (row.StartTime is TimeSpan startTime)
            {
                catalog ??= await _context.ClassPeriods.AsNoTracking().OrderBy(c => c.Order).ToListAsync(ct);
                if (FromTimes(startTime, row.EndTime, catalog) is not { } range) continue;
                (startOrder, count) = range;
            }
            else
            {
                continue;
            }

            blocks.Add(new OccupancyBlock(
                SourceName,
                row.Id,
                BuildLabel(row.Id, row.Subject ?? row.PracticeName, row.Group),
                row.LaboratoryId,
                row.TeacherPersonId,
                row.ScheduledDate.Date,
                startOrder,
                count));
        }

        return blocks;
    }

    /// <summary>"PRA-00024 · Materia (Grupo)".</summary>
    public static string BuildLabel(int planningId, string? title, string? group)
    {
        var label = $"{Pages.LaboratoryOperations.L3Codes.Format(planningId)} · {(string.IsNullOrWhiteSpace(title) ? "Sin materia" : title)}";
        return string.IsNullOrWhiteSpace(group) ? label : $"{label} ({group})";
    }

    /// <summary>
    /// Rango de periodos del catálogo que se solapan con [start, end). Sin fin válido se usa el periodo que contiene start
    /// (o el siguiente si start cae en un recreo). Null si el horario queda fuera de la jornada.
    /// </summary>
    public static (int StartOrder, int Count)? FromTimes(TimeSpan start, TimeSpan? end, IReadOnlyList<ClassPeriod> catalog)
    {
        var effectiveEnd = end is TimeSpan e && e > start ? e : start.Add(TimeSpan.FromMinutes(1));
        var overlapping = catalog
            .Where(c => c.StartTime < effectiveEnd && c.EndTime > start)
            .Select(c => c.Order)
            .ToList();

        if (overlapping.Count == 0 && !(end is TimeSpan e2 && e2 > start))
        {
            var next = catalog.Where(c => c.StartTime >= start).OrderBy(c => c.Order).FirstOrDefault();
            if (next != null) overlapping.Add(next.Order);
        }

        if (overlapping.Count == 0) return null;
        var first = overlapping.Min();
        return (first, overlapping.Max() - first + 1);
    }
}
