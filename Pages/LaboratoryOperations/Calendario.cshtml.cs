using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

public class CalendarioModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly LaboratoryPlanningDemoService _demoService;

    public CalendarioModel(ApplicationDbContext context, LaboratoryPlanningDemoService demoService)
    {
        _context = context;
        _demoService = demoService;
    }

    [BindProperty(SupportsGet = true)]
    public int? LaboratoryId { get; set; }

    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public string EventsJson { get; private set; } = "[]";

    public async Task OnGetAsync()
    {
        var laboratories = await _context.Laboratories
            .AsNoTracking()
            .Where(item => item.Status == GeneralStatus.Activo)
            .OrderBy(item => item.Name)
            .ToListAsync();
        LaboratoryList = new SelectList(laboratories, "Id", "Name", LaboratoryId);
        var laboratoryNames = laboratories.ToDictionary(item => item.Id, item => $"{item.Code} · {item.Name}");

        var events = _demoService.GetPlans()
            .Where(plan => plan.Status != LaboratoryPlanningDemoStatus.Cancelled)
            .Where(plan => !LaboratoryId.HasValue || plan.LaboratoryId == LaboratoryId.Value)
            .Where(plan => laboratoryNames.ContainsKey(plan.LaboratoryId))
            .Select(plan => new CalendarEvent
            {
                Title = $"{plan.Subject} · {laboratoryNames[plan.LaboratoryId]}",
                Start = plan.PracticeDate.Date.Add(plan.StartTime).ToString("s", CultureInfo.InvariantCulture),
                End = plan.PracticeDate.Date.Add(plan.EndTime).ToString("s", CultureInfo.InvariantCulture),
                Color = GetColor(plan.Status),
                Url = $"/LaboratoryOperations/Requerimientos?id={Uri.EscapeDataString(plan.Id)}",
                Lab = laboratoryNames[plan.LaboratoryId],
                Responsible = plan.ResponsibleName,
                Status = GetStatusText(plan.Status)
            })
            .ToList();

        EventsJson = JsonSerializer.Serialize(events, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static string GetColor(LaboratoryPlanningDemoStatus status) => status switch
    {
        LaboratoryPlanningDemoStatus.Approved => "#28a745",
        LaboratoryPlanningDemoStatus.Completed => "#343a40",
        LaboratoryPlanningDemoStatus.Incident => "#dc3545",
        LaboratoryPlanningDemoStatus.PendingAvailability => "#ffc107",
        _ => "#17a2b8"
    };

    private static string GetStatusText(LaboratoryPlanningDemoStatus status) => status switch
    {
        LaboratoryPlanningDemoStatus.Approved => "Aprobada",
        LaboratoryPlanningDemoStatus.Completed => "Cerrada",
        LaboratoryPlanningDemoStatus.Incident => "Incidencia L-5",
        LaboratoryPlanningDemoStatus.PendingAvailability => "Pendiente",
        _ => "Borrador"
    };

    public sealed class CalendarEvent
    {
        public string Title { get; init; } = string.Empty;
        public string Start { get; init; } = string.Empty;
        public string End { get; init; } = string.Empty;
        public string Color { get; init; } = "#17a2b8";
        public string Url { get; init; } = string.Empty;
        public string Lab { get; init; } = string.Empty;
        public string Responsible { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }
}
