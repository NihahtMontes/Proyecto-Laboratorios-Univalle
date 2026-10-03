using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Calendario de planificaciones L-3 persistidas. Solo lectura.
/// Enlaza cada evento con el siguiente paso del flujo según el estado del plan.
/// </summary>
public class CalendarioModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CalendarioModel(ApplicationDbContext context)
    {
        _context = context;
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

        var query = _context.L3Requests.AsNoTracking()
            .Where(plan => plan.Status != L3RequestStatus.Cancelled)
            .Where(plan => !LaboratoryId.HasValue || plan.LaboratoryId == LaboratoryId.Value);

        var plans = await query
            .OrderBy(plan => plan.ScheduledDate)
            .Select(plan => new CalendarProjection
            {
                Id = plan.Id,
                SubjectName = plan.Subject.Name,
                TeacherPersonId = plan.TeacherPersonId,
                LabCode = plan.Laboratory.Code,
                LabName = plan.Laboratory.Name,
                LaboratoryId = plan.LaboratoryId,
                ScheduledDate = plan.ScheduledDate,
                ScheduledEndDate = plan.ScheduledEndDate,
                Status = plan.Status,
                HasDetails = plan.Details.Any()
            })
            .ToListAsync();

        var teacherIds = plans
            .Where(plan => plan.TeacherPersonId.HasValue)
            .Select(plan => plan.TeacherPersonId!.Value)
            .Distinct()
            .ToList();

        var teachers = teacherIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.Interns.AsNoTracking()
                .Where(item => teacherIds.Contains(item.Id))
                .Select(item => new { item.Id, item.Name })
                .ToDictionaryAsync(item => item.Id, item => item.Name);

        var events = plans
            .Where(plan => laboratoryNames.ContainsKey(plan.LaboratoryId))
            .Select(plan =>
            {
                var endDate = plan.ScheduledEndDate ?? plan.ScheduledDate.AddHours(1);
                var labName = laboratoryNames[plan.LaboratoryId];
                var code = L3Codes.Format(plan.Id);
                var teacherName = plan.TeacherPersonId.HasValue
                                  && teachers.TryGetValue(plan.TeacherPersonId.Value, out var teacher)
                    ? teacher
                    : "Sin docente";

                return new CalendarEvent
                {
                    Title = $"{code} · {plan.SubjectName} · {labName}",
                    Start = plan.ScheduledDate.ToString("s", CultureInfo.InvariantCulture),
                    End = endDate.ToString("s", CultureInfo.InvariantCulture),
                    Color = GetColor(plan.Status),
                    Url = GetNextStepUrl(plan.Status, plan.HasDetails, plan.Id),
                    Lab = labName,
                    Responsible = teacherName,
                    Status = GetStatusText(plan.Status)
                };
            })
            .ToList();

        EventsJson = JsonSerializer.Serialize(events, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    /// <summary>
    /// Siguiente paso: Planificado sin detalles -> Requerimientos;
    /// Planificado con detalles -> Disponibilidad; Aprobado/Finalizado -> L3.
    /// </summary>
    private static string GetNextStepUrl(string status, bool hasDetails, int id) => status switch
    {
        L3RequestStatus.Planned when !hasDetails => $"/LaboratoryOperations/Requerimientos?planId={id}",
        L3RequestStatus.Planned => $"/LaboratoryOperations/Disponibilidad?planId={id}",
        L3RequestStatus.Approved => $"/LaboratoryOperations/L3?id={id}",
        L3RequestStatus.Finalized => $"/LaboratoryOperations/L3?id={id}",
        _ => $"/LaboratoryOperations/Planificacion?id={id}"
    };

    private static string GetColor(string status) => status switch
    {
        L3RequestStatus.Approved => "#28a745",
        L3RequestStatus.Finalized => "#343a40",
        L3RequestStatus.Cancelled => "#6c757d",
        L3RequestStatus.Planned => "#ffc107",
        _ => "#17a2b8"
    };

    private static string GetStatusText(string status) => status switch
    {
        L3RequestStatus.Planned => "Planificada",
        L3RequestStatus.Approved => "Aprobada",
        L3RequestStatus.Finalized => "Finalizada",
        L3RequestStatus.Cancelled => "Cancelada",
        _ => status
    };

    private sealed class CalendarProjection
    {
        public int Id { get; init; }
        public string SubjectName { get; init; } = string.Empty;
        public int? TeacherPersonId { get; init; }
        public string LabCode { get; init; } = string.Empty;
        public string LabName { get; init; } = string.Empty;
        public int LaboratoryId { get; init; }
        public DateTime ScheduledDate { get; init; }
        public DateTime? ScheduledEndDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public bool HasDetails { get; init; }
    }

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
