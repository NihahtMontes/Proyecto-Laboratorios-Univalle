using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Resumen del flujo de Operación de Laboratorios (solo lectura).
/// Las planificaciones vienen de L3Requests; las métricas se calculan en memoria
/// a partir de detalles, incidentes y stock operativo.
/// </summary>
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
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

    private async Task LoadPlansAsync()
    {
        var today = DateTime.Today;
        var periodQuery = _context.L3Requests.AsNoTracking()
            .Where(r => r.Status != L3RequestStatus.Cancelled);

        periodQuery = (Filter ?? "hoy").Trim().ToLowerInvariant() switch
        {
            "todos" => periodQuery,
            "semana" => periodQuery.Where(r => r.ScheduledDate.Date >= StartOfWeek(today)
                                              && r.ScheduledDate.Date < StartOfWeek(today).AddDays(7)),
            "mes" => periodQuery.Where(r => r.ScheduledDate.Year == today.Year && r.ScheduledDate.Month == today.Month),
            _ => periodQuery.Where(r => r.ScheduledDate.Date == today),
        };

        // Proyección plana para evitar N+1 al armar la tabla.
        var rows = await periodQuery
            .OrderBy(r => r.ScheduledDate)
            .ThenBy(r => r.ScheduledEndDate ?? r.ScheduledDate.AddHours(1))
            .Select(r => new PlanRowProjection
            {
                    Id = r.Id,
                    SubjectName = r.Subject.Name,
                    GroupName = r.Group.Name,
                    TeacherPersonId = r.TeacherPersonId,
                    LabCode = r.Laboratory.Code,
                    LabName = r.Laboratory.Name,
                    FacultyName = r.Laboratory.Faculty!.Name,
                    ScheduledDate = r.ScheduledDate,
                    ScheduledEndDate = r.ScheduledEndDate,
                    Status = r.Status,
                    AcademicPlanningId = r.AcademicPlanningId,
                    ManagementCode = r.AcademicPlanning!.Management.Code,
                    CareerCode = r.AcademicPlanning.Career.Code ?? string.Empty
                })
            .ToListAsync();

        if (rows.Count == 0)
        {
            Plans = [];
            TotalPlans = 0;
            ReadyPlans = 0;
            ConflictPlans = 0;
            ClosedPlans = 0;
            return;
        }

        var planIds = rows.Select(r => r.Id).ToList();
        var teacherIds = rows
            .Where(r => r.TeacherPersonId.HasValue)
            .Select(r => r.TeacherPersonId!.Value)
            .Distinct()
            .ToList();

        var teachers = teacherIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.Interns.AsNoTracking()
                .Where(i => teacherIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Name })
                .ToDictionaryAsync(i => i.Id, i => i.Name);

        // Planes con al menos un L5Incident vinculado.
        var incidentSet = (await _context.L5Incidents.AsNoTracking()
            .Where(i => i.L3RequestId.HasValue && planIds.Contains(i.L3RequestId.Value))
            .Select(i => i.L3RequestId!.Value)
            .Distinct()
            .ToListAsync()).ToHashSet();

        // Detalles de planes Planificado: comparamos contra stock operativo del laboratorio de cada detalle.
        var conflictByPlan = new HashSet<int>();
        var plannedRows = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => planIds.Contains(d.L3RequestId)
                        && d.EquipmentUnit.LaboratoryId.HasValue)
            .Select(d => new
            {
                d.L3RequestId,
                LabId = d.EquipmentUnit.LaboratoryId!.Value,
                EquipmentId = d.EquipmentUnit.EquipmentId,
                d.ExpectedQuantity,
                PlanStatus = d.L3Request!.Status
            })
            .ToListAsync();

        var plannedPairs = plannedRows
            .Where(d => d.PlanStatus == L3RequestStatus.Planned)
            .Select(d => (d.LabId, d.EquipmentId))
            .Distinct()
            .ToList();

        if (plannedPairs.Count > 0)
        {
            var labIds = plannedPairs.Select(p => p.LabId).Distinct().ToList();
            var equipmentIds = plannedPairs.Select(p => p.EquipmentId).Distinct().ToList();

            var stockMap = await _context.EquipmentUnits.AsNoTracking()
                .Where(u => u.LaboratoryId.HasValue
                            && labIds.Contains(u.LaboratoryId.Value)
                            && equipmentIds.Contains(u.EquipmentId)
                            && u.CurrentStatus == EquipmentStatus.Operational)
                .GroupBy(u => new { u.LaboratoryId, u.EquipmentId })
                .Select(g => new { LabId = g.Key.LaboratoryId!.Value, g.Key.EquipmentId, Count = g.Count() })
                .ToDictionaryAsync(x => (x.LabId, x.EquipmentId), x => x.Count);

            foreach (var d in plannedRows)
            {
                if (d.PlanStatus != L3RequestStatus.Planned) continue;
                var key = (d.LabId, d.EquipmentId);
                var stock = stockMap.TryGetValue(key, out var c) ? c : 0;
                if (d.ExpectedQuantity > stock)
                    conflictByPlan.Add(d.L3RequestId);
            }
        }

        var items = rows.Select(r =>
        {
            var scheduled = r.ScheduledDate;
            var scheduledEnd = r.ScheduledEndDate ?? r.ScheduledDate.AddHours(1);
            var teacherName = r.TeacherPersonId.HasValue
                              && teachers.TryGetValue(r.TeacherPersonId.Value, out var t)
                ? t
                : "Sin docente";

            return new PlanListItem
            {
                Id = r.Id,
                Code = L3Codes.Format(r.Id),
                Subject = r.SubjectName,
                ResponsibleName = teacherName,
                Group = r.GroupName,
                PracticeDate = scheduled.Date,
                StartTime = scheduled.TimeOfDay,
                EndTime = scheduledEnd.TimeOfDay,
                LaboratoryName = $"{r.LabCode} · {r.LabName}",
                FacultyName = r.FacultyName,
                PlanningCode = r.AcademicPlanningId.HasValue
                    ? L3Codes.Planning(r.ManagementCode, r.CareerCode, r.AcademicPlanningId.Value)
                    : "Sin planificación",
                StatusText = GetStatusText(r.Status),
                StatusCss = GetStatusCss(r.Status),
                HasConflict = conflictByPlan.Contains(r.Id),
                HasIncident = incidentSet.Contains(r.Id)
            };
        }).ToList();

        Plans = items;
        TotalPlans = items.Count;
        ReadyPlans = items.Count(i => i.StatusText == "Aprobada");
        ClosedPlans = items.Count(i => i.StatusText == "Finalizada");
        ConflictPlans = items.Count(i => i.HasConflict);
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }

    private static string GetStatusText(string status) => status switch
    {
        L3RequestStatus.Planned => "Planificada",
        L3RequestStatus.Approved => "Aprobada",
        L3RequestStatus.Finalized => "Finalizada",
        L3RequestStatus.Cancelled => "Cancelada",
        _ => status
    };

    private static string GetStatusCss(string status) => status switch
    {
        L3RequestStatus.Planned => "warning",
        L3RequestStatus.Approved => "success",
        L3RequestStatus.Finalized => "dark",
        L3RequestStatus.Cancelled => "secondary",
        _ => "secondary"
    };

    private sealed class PlanRowProjection
    {
        public int Id { get; init; }
        public string SubjectName { get; init; } = string.Empty;
        public string GroupName { get; init; } = string.Empty;
        public int? TeacherPersonId { get; init; }
        public string LabCode { get; init; } = string.Empty;
        public string LabName { get; init; } = string.Empty;
        public string FacultyName { get; init; } = string.Empty;
        public DateTime ScheduledDate { get; init; }
        public DateTime? ScheduledEndDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public int? AcademicPlanningId { get; init; }
        public string ManagementCode { get; init; } = string.Empty;
        public string CareerCode { get; init; } = string.Empty;
    }

    public sealed class PlanListItem
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string ResponsibleName { get; init; } = string.Empty;
        public string Group { get; init; } = string.Empty;
        public DateTime PracticeDate { get; init; }
        public TimeSpan StartTime { get; init; }
        public TimeSpan EndTime { get; init; }
        public string LaboratoryName { get; init; } = string.Empty;
        public string FacultyName { get; init; } = string.Empty;
        public string PlanningCode { get; init; } = string.Empty;
        public string StatusText { get; init; } = string.Empty;
        public string StatusCss { get; init; } = "secondary";
        public bool HasConflict { get; init; }
        public bool HasIncident { get; init; }
    }
}
