using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Pages.Shared;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.CourseGroups;

/// <summary>
/// Horario semanal de clases POR COHORTE (semestre-grupo): todas las materias de la misma gestión,
/// carrera, semestre y nombre de grupo comparten una sola grilla (cohorte virtual, sin tabla).
/// El guardado envía un JSON {courseGroupId: [{weekday, periodOrder}, ...]} y reemplaza el horario
/// de los grupos indicados vía IGroupScheduleService.SaveCohortScheduleAsync.
/// </summary>
[Authorize(Roles = AuthorizationHelper.AdminRoles)]
public class HorarioModel : PageModel
{
    private static readonly JsonSerializerOptions SlotsJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions ConfigJsonOptions = new()
    {
        // El encoder por defecto escapa <, > y &, lo que hace seguro inyectarlo en <script type="application/json">.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Carrera del filtro (select de carreras con cohortes).</summary>
    public sealed record CareerOption(int Id, string Name);

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IGroupScheduleService _schedules;

    public HorarioModel(ApplicationDbContext context, IAcademicManagementService managements, IGroupScheduleService schedules)
    {
        _context = context;
        _managements = managements;
        _schedules = schedules;
    }

    /// <summary>Gestión pedida; vacío = la actual.</summary>
    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    /// <summary>Carrera del cohorte (opcional hasta elegir una).</summary>
    [BindProperty(SupportsGet = true)]
    public int? CareerId { get; set; }

    /// <summary>Cohorte pedido con el formato "semestre|grupo" (p. ej. "3|A").</summary>
    [BindProperty(SupportsGet = true)]
    public string? Cohort { get; set; }

    /// <summary>Payload del horario enviado por el JS: {"601":[{"weekday":1,"periodOrder":2},...]}.</summary>
    [BindProperty]
    public string? SlotsJson { get; set; }

    public AcademicManagementSummary? Management { get; private set; }
    public List<AcademicManagementSummary> Managements { get; private set; } = [];

    /// <summary>True si la gestión mostrada está completada (solo consulta; sin botón de guardar).</summary>
    public bool IsReadOnly => Management is { IsEditable: false };

    public IReadOnlyList<ClassPeriod> Periods { get; private set; } = [];
    public IReadOnlyList<CohortOption> Cohorts { get; private set; } = [];
    public IReadOnlyList<CareerOption> CareerOptions { get; private set; } = [];

    /// <summary>Cohorte seleccionado (con su horario); null si falta elegir gestión/carrera/cohorte.</summary>
    public CohortScheduleView? Selected { get; private set; }

    /// <summary>Opción de cohorte correspondiente a Selected (para la cabecera de la hoja).</summary>
    public CohortOption? SelectedOption { get; private set; }

    /// <summary>Configuración serializada que consume wwwroot/js/horario-cohorte.js.</summary>
    public string ConfigJson { get; private set; } = "{}";

    /// <summary>Modelo del partial _PeriodWeekGrid (esquina, encabezados de día y columna de periodos).</summary>
    public PeriodWeekGridModel GridModel => new(
        Periods.Select(p => new PeriodWeekGridPeriod(p.Order, p.Label)).ToList(),
        PeriodWeekGridModel.DefaultDayNames);

    public async Task<IActionResult> OnGetAsync()
    {
        Managements = await _managements.ListAsync();
        Management = await _managements.ResolveAsync(ManagementId);
        if (ManagementId.HasValue && Management == null)
        {
            TempData.Error("La gestión solicitada no existe.");
            return RedirectToPage(new { careerId = CareerId });
        }

        Periods = await _context.ClassPeriods.AsNoTracking()
            .OrderBy(p => p.Order)
            .ToListAsync();

        // Sin gestión académica activa: la vista muestra el estado vacío con el selector de gestiones.
        if (Management == null) return Page();

        var allCohorts = await _schedules.ListCohortsAsync(Management.Id);
        CareerOptions = allCohorts
            .GroupBy(c => new { c.CareerId, c.CareerName })
            .Select(g => new CareerOption(g.Key.CareerId, g.Key.CareerName))
            .OrderBy(c => c.Name).ThenBy(c => c.Id)
            .ToList();

        // Carrera pedida inexistente en la gestión: se ignora el filtro en vez de mostrar una lista vacía.
        if (CareerId.HasValue && CareerOptions.All(c => c.Id != CareerId.Value))
            CareerId = null;

        Cohorts = CareerId.HasValue
            ? allCohorts.Where(c => c.CareerId == CareerId.Value).ToList()
            : allCohorts;

        var parsed = ParseCohort(Cohort);
        if (parsed is { } cohort)
        {
            var option = Cohorts.FirstOrDefault(c =>
                c.Semester == cohort.Semester
                && string.Equals(c.GroupName, cohort.GroupName, StringComparison.OrdinalIgnoreCase));
            if (option == null)
            {
                TempData.Warning("El cohorte solicitado no existe en esta gestión.");
                return RedirectToPage(new { managementId = Management.Id, careerId = CareerId });
            }

            SelectedOption = option;
            Selected = await _schedules.GetCohortScheduleAsync(
                Management.Id, option.CareerId, option.Semester, option.GroupName);
        }

        await BuildConfigJsonAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var parsed = ParseCohort(Cohort);
        if (ManagementId == null || CareerId == null || parsed == null)
        {
            TempData.Error("Faltan datos del cohorte: vuelva a seleccionarlo.");
            return RedirectToPage(new { managementId = ManagementId, careerId = CareerId });
        }

        Management = await _managements.ResolveAsync(ManagementId);
        if (Management == null)
        {
            TempData.Error("La gestión solicitada no existe.");
            return RedirectToPage(new { careerId = CareerId });
        }
        if (!Management.IsEditable)
        {
            TempData.Error($"La gestión {Management.Code} está completada: el horario es de solo lectura.");
            return RedirectToPage(new { managementId = Management.Id, careerId = CareerId, cohort = Cohort });
        }

        var slotsByGroup = ParseSlotsJson(SlotsJson);
        if (slotsByGroup == null)
        {
            TempData.Error("No se pudieron leer los datos del horario. Intente nuevamente.");
            return RedirectToPage(new { managementId = Management.Id, careerId = CareerId, cohort = Cohort });
        }

        var (semester, groupName) = parsed.Value;
        var error = await _schedules.SaveCohortScheduleAsync(
            Management.Id, CareerId.Value, semester, groupName, slotsByGroup, ct);
        if (error != null)
        {
            TempData.Error(error);
            return RedirectToPage(new { managementId = Management.Id, careerId = CareerId, cohort = Cohort });
        }

        TempData.Success($"Horario del cohorte {GroupScheduleService.CohortLabel(semester, groupName)} guardado.");
        return RedirectToPage(new { managementId = Management.Id, careerId = CareerId, cohort = Cohort });
    }

    /// <summary>Interpreta el filtro "semestre|grupo"; null si falta o es inválido.</summary>
    private static (int Semester, string GroupName)? ParseCohort(string? cohort)
    {
        if (string.IsNullOrWhiteSpace(cohort)) return null;
        var parts = cohort.Split('|');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var semester) || semester is < 1 or > 12) return null;
        var groupName = parts[1].Trim();
        if (groupName.Length == 0 || groupName.Length > 20) return null;
        return (semester, groupName);
    }

    /// <summary>Deserializa el payload del JS; null = JSON inválido. Acepta "{}" (no cambia nada) y deduplica celdas.</summary>
    private static IReadOnlyDictionary<int, IReadOnlyCollection<WeekSlot>>? ParseSlotsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<int, IReadOnlyCollection<WeekSlot>>();

        Dictionary<int, List<SlotDto>>? raw;
        try
        {
            raw = JsonSerializer.Deserialize<Dictionary<int, List<SlotDto>>>(json, SlotsJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        if (raw == null || raw.Count > 100) return null;   // un cohorte no tiene cientos de grupos

        var result = new Dictionary<int, IReadOnlyCollection<WeekSlot>>();
        foreach (var (groupId, slots) in raw)
        {
            if (groupId <= 0 || slots == null) continue;
            if (slots.Count > 6 * 12) return null;   // más celdas que la semana completa: carga inválida
            var parsed = new HashSet<WeekSlot>();
            foreach (var slot in slots)
            {
                if (slot is null || slot.Weekday is < 1 or > 6 || slot.PeriodOrder is < 1 or > 12) continue;
                parsed.Add(new WeekSlot(slot.Weekday, slot.PeriodOrder));
            }
            result[groupId] = parsed.ToList();
        }
        return result;
    }

    /// <summary>Arma el JSON con materias, horarios, choques de docente y nombres de días para el JS.</summary>
    private Task BuildConfigJsonAsync()
    {
        var config = new
        {
            readOnly = IsReadOnly,
            careerName = Selected?.CareerName ?? string.Empty,
            semester = Selected?.Semester,
            groupName = Selected?.GroupName ?? string.Empty,
            cohortLabel = SelectedOption?.Label ?? Selected?.Label ?? string.Empty,
            subjectCount = Selected?.Subjects.Count ?? 0,
            dayNames = PeriodWeekGridModel.DefaultDayNames,
            subjects = (Selected?.Subjects ?? []).Select(s => new
            {
                courseGroupId = s.CourseGroupId,
                subjectId = s.SubjectId,
                name = s.SubjectName,
                code = s.SubjectCode,
                group = s.GroupName,
                teacher = s.TeacherName,
                color = s.ColorIndex,
                slots = s.Slots.Select(x => new { x.Weekday, x.PeriodOrder }),
                busy = (Selected?.TeacherBusy ?? [])
                    .Where(b => b.CourseGroupId == s.CourseGroupId)
                    .Select(b => new { b.Weekday, b.PeriodOrder, otherSubject = b.OtherSubjectName }),
            }).ToList(),
        };
        ConfigJson = JsonSerializer.Serialize(config, ConfigJsonOptions);
        return Task.CompletedTask;
    }

    private sealed class SlotDto
    {
        public int Weekday { get; set; }
        public int PeriodOrder { get; set; }
    }
}
