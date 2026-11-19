using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

namespace Proyecto_Laboratorios_Univalle.Services.Inicio;

/// <summary>
/// Datos del panel de control del Inicio: prácticas de la semana en curso (lunes a sábado)
/// y serie de prácticas por semana. Todo de solo lectura (AsNoTracking) y con la misma
/// presentación que Planificaciones/Calendario (PlanningStatusView).
/// </summary>
public interface IInicioPanelService
{
    /// <summary>Prácticas que ocupan ambiente en la semana lunes–sábado que contiene <paramref name="today"/>, agrupadas por día.</summary>
    Task<InicioWeekPanel> GetWeekAsync(DateTime today, CancellationToken cancellationToken = default);

    /// <summary>Cantidad de prácticas que ocupan ambiente por semana (lunes–domingo): 5 anteriores, la actual y 2 siguientes.</summary>
    Task<IReadOnlyList<InicioWeekPoint>> GetWeeklySeriesAsync(DateTime today, CancellationToken cancellationToken = default);
}

/// <summary>Fila de práctica para la tabla de la semana del Inicio.</summary>
public sealed record InicioPracticeRow(
    int Id,
    string PracticeName,
    string? PracticeNumber,
    string SubjectName,
    string GroupLabel,
    string LaboratoryName,
    string TeacherName,
    string TimeText,
    string Status,
    string StatusLabel,
    string StatusCss,
    string NextUrl);

/// <summary>Un día de la semana con sus prácticas (aunque esté vacío).</summary>
public sealed record InicioDayGroup(
    DateTime Date,
    int Weekday,
    string DayName,
    string HeaderText,
    bool IsToday,
    bool IsPast,
    IReadOnlyList<InicioPracticeRow> Practices);

/// <summary>Conteo por estado visible de la semana (chips del pie de la tabla).</summary>
public sealed record InicioStatusCount(string Label, string Css, int Count);

/// <summary>Panel de la semana: días lunes a sábado, conteo de hoy y conteos por estado.</summary>
public sealed record InicioWeekPanel(
    DateTime Monday,
    DateTime Saturday,
    DateTime Today,
    IReadOnlyList<InicioDayGroup> Days,
    int TodayCount,
    IReadOnlyList<InicioStatusCount> StatusCounts);

/// <summary>Un punto de la serie semanal: «16–22 nov», 4 prácticas, true si es la semana actual.</summary>
public sealed record InicioWeekPoint(string Label, DateTime Start, DateTime End, int Count, bool IsCurrent);

public sealed class InicioPanelService : IInicioPanelService
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-BO");

    private readonly ApplicationDbContext _context;

    public InicioPanelService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InicioWeekPanel> GetWeekAsync(DateTime today, CancellationToken cancellationToken = default)
    {
        today = today.Date;
        var monday = MondayOf(today);
        var saturday = monday.AddDays(5);
        var to = saturday.AddDays(1);

        var plannings = await _context.Plannings.AsNoTracking()
            .Where(p => !p.IsDeleted && p.ScheduledDate >= monday && p.ScheduledDate < to)
            .OrderBy(p => p.ScheduledDate)
            .Select(p => new
            {
                p.Id,
                p.PracticeName,
                p.PracticeNumber,
                p.ScheduledDate,
                p.ScheduledEndDate,
                p.Status,
                StartOrder = p.StartPeriod != null ? (int?)p.StartPeriod.Order : null,
                p.PeriodCount,
                SubjectName = p.Subject != null ? p.Subject.Name : "Sin materia",
                CourseGroupName = p.CourseGroup != null ? p.CourseGroup.Name : null,
                p.GroupName,
                LegacyGroupName = p.Group != null ? p.Group.Name : null,
                p.AllLaboratories,
                LaboratoryCode = p.Laboratory != null ? p.Laboratory.Code : null,
                LaboratoryName = p.Laboratory != null ? p.Laboratory.Name : null,
                p.TeacherPersonId,
                HasRequirements = p.Requirements.Any()
            })
            .ToListAsync(cancellationToken);

        // Occupies no es traducible a SQL: se filtra en memoria con la regla canónica de PlanningStatus.
        var occupying = plannings.Where(p => PlanningStatus.Occupies(p.Status)).ToList();

        var teacherIds = occupying.Select(p => p.TeacherPersonId).Distinct().ToList();
        var teachers = teacherIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.Interns.AsNoTracking()
                .Where(i => teacherIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Name, cancellationToken);

        var days = Enumerable.Range(0, 6).Select(offset =>
        {
            var date = monday.AddDays(offset);
            var practices = occupying
                .Where(p => p.ScheduledDate.Date == date)
                .OrderBy(p => p.StartOrder ?? int.MaxValue)
                .ThenBy(p => p.ScheduledDate)
                .Select(p => new InicioPracticeRow(
                    p.Id,
                    p.PracticeName,
                    string.IsNullOrEmpty(p.PracticeNumber) ? null : p.PracticeNumber,
                    p.SubjectName,
                    GroupLabelOf(p.CourseGroupName, p.GroupName, p.LegacyGroupName),
                    p.AllLaboratories
                        ? "Todos los laboratorios"
                        : string.IsNullOrEmpty(p.LaboratoryCode)
                            ? "—"
                            : string.IsNullOrEmpty(p.LaboratoryName)
                                ? p.LaboratoryCode
                                : $"{p.LaboratoryCode} · {p.LaboratoryName}",
                    teachers.TryGetValue(p.TeacherPersonId, out var teacher) ? teacher : "Sin docente",
                    PlanningStatusView.TimeText(p.StartOrder, p.PeriodCount, p.ScheduledDate, p.ScheduledEndDate),
                    p.Status,
                    PlanningStatus.Label(p.Status),
                    PlanningStatusView.Css(p.Status),
                    PlanningStatusView.NextUrl(p.Id, p.Status, p.HasRequirements)))
                .ToList();

            // Lunes = 1 … sábado = 6 (mismo índice que AcademicWeekGenerator.WeekdayNames).
            var weekday = ((int)date.DayOfWeek + 6) % 7 + 1;
            var dayName = AcademicWeekGenerator.WeekdayNames[weekday];
            var isToday = date == today;
            return new InicioDayGroup(
                date,
                weekday,
                dayName,
                isToday ? $"Hoy · {dayName} {date.Day}" : $"{dayName} {date.Day}",
                isToday,
                date < today,
                practices);
        }).ToList();

        var statusCounts = occupying
            .GroupBy(p => new { Label = PlanningStatus.Label(p.Status), Css = PlanningStatusView.Css(p.Status) })
            .Select(g => new InicioStatusCount(g.Key.Label, g.Key.Css, g.Count()))
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Label, StringComparer.Create(Es, false))
            .ToList();

        return new InicioWeekPanel(
            monday,
            saturday,
            today,
            days,
            days.FirstOrDefault(d => d.IsToday)?.Practices.Count ?? 0,
            statusCounts);
    }

    public async Task<IReadOnlyList<InicioWeekPoint>> GetWeeklySeriesAsync(DateTime today, CancellationToken cancellationToken = default)
    {
        today = today.Date;
        var monday = MondayOf(today);
        var from = monday.AddDays(-35); // 5 semanas anteriores
        var to = monday.AddDays(21);    // + la semana actual + 2 siguientes

        // Se traen solo fecha y estado del rango: el filtro de Occupies y el agrupado son en memoria.
        var plannings = await _context.Plannings.AsNoTracking()
            .Where(p => !p.IsDeleted && p.ScheduledDate >= from && p.ScheduledDate < to)
            .Select(p => new { p.ScheduledDate, p.Status })
            .ToListAsync(cancellationToken);

        var buckets = new int[8];
        foreach (var planning in plannings)
        {
            if (!PlanningStatus.Occupies(planning.Status))
            {
                continue;
            }

            // Índice relativo al inicio de la serie (5 semanas antes del lunes actual): 0..7.
            var days = (planning.ScheduledDate.Date - from).Days;
            if (days >= 0 && days < 56)
            {
                buckets[days / 7]++;
            }
        }

        return Enumerable.Range(0, 8).Select(i =>
        {
            var start = monday.AddDays(i * 7 - 35);
            return new InicioWeekPoint(WeekLabel(start), start, start.AddDays(6), buckets[i], i == 5);
        }).ToList();
    }

    /// <summary>Grupo visible: CourseGroup (nuevo) → GroupName → Group legado → «—», como Planificaciones.</summary>
    private static string GroupLabelOf(string? courseGroupName, string? groupName, string? legacyGroupName)
    {
        if (!string.IsNullOrEmpty(courseGroupName)) return courseGroupName;
        if (!string.IsNullOrEmpty(groupName)) return groupName;
        if (!string.IsNullOrEmpty(legacyGroupName)) return legacyGroupName;
        return "—";
    }

    private static DateTime MondayOf(DateTime date) => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    /// <summary>Etiqueta corta de la semana: «16–22 nov» o, si cruza mes, «27 oct–2 nov».</summary>
    private static string WeekLabel(DateTime monday)
    {
        var sunday = monday.AddDays(6);
        if (monday.Month == sunday.Month)
        {
            return $"{monday.Day:0}–{sunday.Day:0} {ShortMonth(monday)}";
        }
        return $"{monday.Day:0} {ShortMonth(monday)}–{sunday.Day:0} {ShortMonth(sunday)}";
    }

    private static string ShortMonth(DateTime date)
    {
        return date.ToString("MMM", Es).TrimEnd('.', ' ').ToLowerInvariant();
    }
}
