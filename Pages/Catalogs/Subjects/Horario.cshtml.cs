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

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Subjects;

/// <summary>
/// Horario semanal de clases de una materia por grupo (lunes a sábado × periodos 1..12), con el
/// mismo formato visual de la Grilla por laboratorio. Por defecto muestra la gestión actual;
/// con ?managementId= se consulta otra (de solo lectura si está completada).
/// El guardado envía un JSON {groupId: [{weekday, periodOrder}, ...]} y reemplaza el horario
/// de los grupos indicados vía IGroupScheduleService.
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

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IGroupScheduleService _schedules;

    public HorarioModel(ApplicationDbContext context, IAcademicManagementService managements, IGroupScheduleService schedules)
    {
        _context = context;
        _managements = managements;
        _schedules = schedules;
    }

    /// <summary>Identificador de la materia (mismo parámetro que Kit/Edit/Delete).</summary>
    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    /// <summary>Gestión pedida; vacío = la actual.</summary>
    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    /// <summary>Payload del horario enviado por el JS: {"3":[{"weekday":1,"periodOrder":2},...]}.</summary>
    [BindProperty]
    public string? SlotsJson { get; set; }

    public Subject? Subject { get; private set; }
    public AcademicManagementSummary? Management { get; private set; }
    public List<AcademicManagementSummary> Managements { get; private set; } = [];

    /// <summary>True si la gestión mostrada está completada (solo consulta; sin botón de guardar).</summary>
    public bool IsReadOnly => Management is { IsEditable: false };

    public IReadOnlyList<ClassPeriod> Periods { get; private set; } = [];
    public IReadOnlyList<GroupScheduleView> Groups { get; private set; } = [];

    /// <summary>Configuración serializada que consume wwwroot/js/horario-grupos.js.</summary>
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
            return RedirectToPage(new { id = Id });
        }

        if (!Id.HasValue)
        {
            TempData.Error("Debe abrir el horario desde una materia del catálogo.");
            return RedirectToPage("./Index");
        }

        Subject = await _context.Subjects.AsNoTracking()
            .Include(s => s.Career)
            .FirstOrDefaultAsync(s => s.Id == Id.Value);
        if (Subject == null)
        {
            TempData.Error("La materia solicitada no existe.");
            return RedirectToPage("./Index");
        }

        // Sin gestión académica activa: la vista muestra el estado vacío con el selector de gestiones.
        if (Management == null) return Page();

        Periods = await _context.ClassPeriods.AsNoTracking()
            .OrderBy(p => p.Order)
            .ToListAsync();
        Groups = await _schedules.GetSubjectScheduleAsync(Id.Value, Management.Id);

        await BuildConfigJsonAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!Id.HasValue)
        {
            TempData.Error("Debe abrir el horario desde una materia del catálogo.");
            return RedirectToPage("./Index");
        }

        var subject = await _context.Subjects.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == Id.Value, ct);
        if (subject == null)
        {
            TempData.Error("La materia solicitada no existe.");
            return RedirectToPage("./Index");
        }

        Management = await _managements.ResolveAsync(ManagementId);
        if (Management == null)
        {
            TempData.Error("La gestión solicitada no existe.");
            return RedirectToPage(new { id = Id.Value });
        }
        if (!Management.IsEditable)
        {
            TempData.Error($"La gestión {Management.Code} está completada: el horario es de solo lectura.");
            return RedirectToPage(new { id = Id.Value, managementId = Management.Id });
        }

        var slotsByGroup = ParseSlotsJson(SlotsJson);
        if (slotsByGroup == null)
        {
            TempData.Error("No se pudieron leer los datos del horario. Intente nuevamente.");
            return RedirectToPage(new { id = Id.Value, managementId = Management.Id });
        }

        var error = await _schedules.SaveSubjectScheduleAsync(Id.Value, Management.Id, slotsByGroup, ct);
        if (error != null)
        {
            TempData.Error(error);
            return RedirectToPage(new { id = Id.Value, managementId = Management.Id });
        }

        TempData.Success($"Horario de {subject.Name} guardado.");
        return RedirectToPage(new { id = Id.Value, managementId = Management.Id });
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
        if (raw == null || raw.Count > 200) return null;   // una materia no tiene cientos de grupos

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

    /// <summary>Arma el JSON con grupos, horarios, choques con otras materias y nombres de días para el JS.</summary>
    private async Task BuildConfigJsonAsync()
    {
        var groups = new List<object>();
        foreach (var group in Groups)
        {
            var siblings = await _schedules.GetSiblingClassesAsync(group.CourseGroupId);
            groups.Add(new
            {
                id = group.CourseGroupId,
                name = group.Name,
                label = GroupScheduleService.CohortLabel(Subject?.Semester, group.Name),
                teacher = group.TeacherName,
                students = group.StudentCount,
                slots = group.Slots.Select(s => new { s.Weekday, s.PeriodOrder }),
                siblings = siblings.Select(s => new { s.Weekday, s.PeriodOrder, s.SubjectName }),
            });
        }

        var config = new
        {
            readOnly = IsReadOnly,
            subjectName = Subject?.Name ?? string.Empty,
            dayNames = PeriodWeekGridModel.DefaultDayNames,
            groups,
        };
        ConfigJson = JsonSerializer.Serialize(config, ConfigJsonOptions);
    }

    private sealed class SlotDto
    {
        public int Weekday { get; set; }
        public int PeriodOrder { get; set; }
    }
}
