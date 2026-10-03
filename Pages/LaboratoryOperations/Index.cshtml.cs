using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly LaboratoryPlanningDemoService _demoService;

    public IndexModel(ApplicationDbContext context, LaboratoryPlanningDemoService demoService)
    {
        _context = context;
        _demoService = demoService;
    }

    [BindProperty(SupportsGet = true)]
    public string Filter { get; set; } = "hoy";

    public IReadOnlyList<PlanListItem> Plans { get; private set; } = [];
    public int TotalPlans { get; private set; }
    public int ReadyPlans { get; private set; }
    public int ConflictPlans { get; private set; }
    public int ClosedPlans { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadPlansAsync();
    }

    public async Task<IActionResult> OnPostDemoAsync()
    {
        var laboratory = await _context.Laboratories
            .AsNoTracking()
            .Where(item => item.Status == GeneralStatus.Activo)
            .OrderBy(item => item.Name)
            .FirstOrDefaultAsync();
        var career = laboratory == null
            ? null
            : await _context.Careers
                .AsNoTracking()
                .Where(item => item.Status == GeneralStatus.Activo && item.FacultadId == laboratory.FacultyId)
                .OrderBy(item => item.Name)
                .FirstOrDefaultAsync();

        if (laboratory == null || career == null)
        {
            TempData.Error("No existen catálogos suficientes para crear el ejemplo local.");
            return RedirectToPage();
        }

        var number = _demoService.GetPlans().Count + 1;
        var plan = new LaboratoryPlanningDemo
        {
            Id = Guid.NewGuid().ToString("N"),
            Code = $"PLN-DEMO-{number:000}",
            FacultyId = laboratory.FacultyId,
            CareerId = career.Id,
            LaboratoryId = laboratory.Id,
            ResponsibleName = "Docente de demostración",
            Subject = "Práctica demostrativa de laboratorio",
            Group = "A",
            PracticeDate = DateTime.Today,
            StartTime = new TimeSpan(14, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            MaterialCategory = "Gastronomía (Utensilios)",
            PracticeNumber = 1,
            Observations = "Registro creado desde el escenario demo local.",
            Status = LaboratoryPlanningDemoStatus.Approved,
            Requirements =
            [
                CreateRequirement("SHOT", 4),
                CreateRequirement("COPA-MARTINI", 2)
            ]
        };

        _demoService.Save(plan);
        TempData.Success("Ejemplo de planificación cargado. Puedes editarlo o recorrer su flujo.");
        return RedirectToPage(new { filter = "todos" });
    }

    private async Task LoadPlansAsync()
    {
        var source = _demoService.GetPlans()
            .Where(plan => plan.Status != LaboratoryPlanningDemoStatus.Cancelled)
            .Where(plan => MatchesFilter(plan.PracticeDate, Filter))
            .OrderBy(plan => plan.PracticeDate)
            .ThenBy(plan => plan.StartTime)
            .ToList();

        var laboratoryIds = source.Select(plan => plan.LaboratoryId).Distinct().ToList();
        var careerIds = source.Select(plan => plan.CareerId).Distinct().ToList();
        var laboratories = await _context.Laboratories
            .AsNoTracking()
            .Where(item => laboratoryIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id);
        var careers = await _context.Careers
            .AsNoTracking()
            .Where(item => careerIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id);

        Plans = source.Select(plan => new PlanListItem
        {
            Id = plan.Id,
            Code = plan.Code,
            Subject = plan.Subject,
            ResponsibleName = plan.ResponsibleName,
            Group = plan.Group,
            PracticeDate = plan.PracticeDate,
            StartTime = plan.StartTime,
            EndTime = plan.EndTime,
            LaboratoryName = laboratories.TryGetValue(plan.LaboratoryId, out var lab)
                ? $"{lab.Code} · {lab.Name}"
                : "Laboratorio no disponible",
            CareerName = careers.TryGetValue(plan.CareerId, out var career)
                ? career.Name
                : "Carrera no disponible",
            StatusText = GetStatusText(plan.Status),
            StatusCss = GetStatusCss(plan.Status),
            HasConflict = HasAvailabilityConflict(plan),
            HasIncident = plan.Incident != null
        }).ToList();

        TotalPlans = Plans.Count;
        ReadyPlans = Plans.Count(item => item.StatusText is "Aprobada" or "En operación");
        ConflictPlans = Plans.Count(item => item.HasConflict);
        ClosedPlans = Plans.Count(item => item.StatusText == "Cerrada");
    }

    private static LaboratoryPlanningDemoRequirement CreateRequirement(string code, int quantity)
    {
        var item = LaboratoryPlanningDemoCatalog.Find(code)
            ?? throw new InvalidOperationException($"Catálogo demo inexistente: {code}");
        return new LaboratoryPlanningDemoRequirement
        {
            Code = item.Code,
            Name = item.Name,
            Unit = item.Unit,
            Category = item.Category,
            RequestedQuantity = quantity,
            AvailableQuantity = item.AvailableStock
        };
    }

    private static bool HasAvailabilityConflict(LaboratoryPlanningDemo plan) =>
        plan.Requirements.Any(item => item.RequestedQuantity > item.AvailableQuantity);

    private static bool MatchesFilter(DateTime date, string? filter)
    {
        var value = (filter ?? "hoy").Trim().ToLowerInvariant();
        var today = DateTime.Today;
        return value switch
        {
            "todos" => true,
            "semana" => date.Date >= StartOfWeek(today) && date.Date < StartOfWeek(today).AddDays(7),
            "mes" => date.Year == today.Year && date.Month == today.Month,
            _ => date.Date == today
        };
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }

    private static string GetStatusText(LaboratoryPlanningDemoStatus status) => status switch
    {
        LaboratoryPlanningDemoStatus.PendingAvailability => "Pendiente",
        LaboratoryPlanningDemoStatus.Approved => "Aprobada",
        LaboratoryPlanningDemoStatus.InProgress => "En operación",
        LaboratoryPlanningDemoStatus.Completed => "Cerrada",
        LaboratoryPlanningDemoStatus.Incident => "Incidencia L-5",
        _ => "Borrador"
    };

    private static string GetStatusCss(LaboratoryPlanningDemoStatus status) => status switch
    {
        LaboratoryPlanningDemoStatus.Approved => "success",
        LaboratoryPlanningDemoStatus.InProgress => "info",
        LaboratoryPlanningDemoStatus.Completed => "dark",
        LaboratoryPlanningDemoStatus.Incident => "danger",
        LaboratoryPlanningDemoStatus.PendingAvailability => "warning",
        _ => "secondary"
    };

    public sealed class PlanListItem
    {
        public string Id { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string ResponsibleName { get; init; } = string.Empty;
        public string Group { get; init; } = string.Empty;
        public DateTime PracticeDate { get; init; }
        public TimeSpan StartTime { get; init; }
        public TimeSpan EndTime { get; init; }
        public string LaboratoryName { get; init; } = string.Empty;
        public string CareerName { get; init; } = string.Empty;
        public string StatusText { get; init; } = string.Empty;
        public string StatusCss { get; init; } = "secondary";
        public bool HasConflict { get; init; }
        public bool HasIncident { get; init; }
    }
}
