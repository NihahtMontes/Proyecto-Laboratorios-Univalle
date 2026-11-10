using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations.Solicitudes;

/// <summary>
/// Bandeja de solicitudes de docentes (Supervisor / Administrador / SuperAdmin): por urgencia, con banderas de
/// choque, competencia y material; permite aprobar en lote las que no tienen ningún aviso.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ReviewerRoles)]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ITeacherRequestService _requests;

    public IndexModel(ApplicationDbContext context, ITeacherRequestService requests)
    {
        _context = context;
        _requests = requests;
    }

    public sealed record InboxRow(
        int Id, string Code, string Teacher, string Practice, string? Subject, string? Group, DateTime Date, TimeSpan? Start, TimeSpan? End,
        string? LabCode, int Items, string Status, DateTime RequestDate, int Blocks, int Competitors, int MaterialShort, int MaterialNoStock,
        string? Comment)
    {
        public int DaysLeft => (Date.Date - DateTime.Today).Days;
        public bool Urgent => DaysLeft <= 2;
        public bool Soon => !Urgent && DaysLeft <= 5;
        public bool Clean => Blocks == 0 && Competitors == 0 && MaterialShort == 0;
        public string TimeText => Start.HasValue ? $"{Start.Value:hh\\:mm}–{(End ?? Start.Value):hh\\:mm}" : "Sin horario";
    }

    public string Tab { get; private set; } = "revisar";
    public int ToReview { get; private set; }
    public int Observed { get; private set; }
    public List<InboxRow> Rows { get; private set; } = [];
    /// <summary>Control semanal (esta semana y las 3 siguientes): prácticas programadas frente a solicitudes recibidas.</summary>
    public IReadOnlyList<WeekCoverage> Coverage { get; private set; } = [];

    public async Task OnGetAsync(string? tab)
    {
        Tab = tab is "observadas" or "resueltas" ? tab : "revisar";
        ToReview = await _context.Plannings.AsNoTracking().CountAsync(p => !p.IsDeleted && p.Status == PlanningStatus.Requested);
        Observed = await _context.Plannings.AsNoTracking().CountAsync(p => !p.IsDeleted && p.Status == PlanningStatus.Observed);
        Coverage = await _requests.GetWeeklyCoverageAsync(DateTime.Today, 4);

        IQueryable<Planning> query = _context.Plannings.AsNoTracking().Where(p => !p.IsDeleted);
        query = Tab switch
        {
            "observadas" => query.Where(p => p.Status == PlanningStatus.Observed),
            // Resueltas: solicitudes de docentes aprobadas/rechazadas en los últimos 30 días.
            "resueltas" => query.Where(p => p.Reviews.Any(r => (r.Action == PlanningReviewAction.Approved || r.Action == PlanningReviewAction.ApprovedWithChanges
                || r.Action == PlanningReviewAction.Rejected) && r.CreatedDate >= DateTime.UtcNow.AddDays(-30))),
            _ => query.Where(p => p.Status == PlanningStatus.Requested)
        };
        var rows = await (Tab == "resueltas" ? query.OrderByDescending(p => p.Reviews.Max(r => r.CreatedDate)) : query.OrderBy(p => p.ScheduledDate))
            .Take(150)
            .Select(p => new
            {
                p.Id, p.Code, p.TeacherPersonId, p.PracticeName, Subject = p.Subject != null ? p.Subject.Name : null,
                Group = p.CourseGroup != null ? p.CourseGroup.Name : p.GroupName, p.ScheduledDate, p.StartTime, p.EndTime,
                Lab = p.Laboratory != null ? p.Laboratory.Code : null, p.LaboratoryId, p.CourseGroupId, p.StartPeriodId, p.PeriodCount,
                Items = p.Requirements.Count, p.Status, p.RequestDate,
                Comment = p.Reviews.OrderByDescending(r => r.CreatedDate).Select(r => r.Comment).FirstOrDefault()
            })
            .ToListAsync();

        var teacherIds = rows.Select(r => r.TeacherPersonId).Distinct().ToList();
        var names = await _context.Interns.AsNoTracking().Where(i => teacherIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => string.IsNullOrWhiteSpace(i.Title) ? i.Name : i.Title + " " + i.Name);

        // Material de las pendientes: requerimientos en UNA consulta y evaluación por día (un solo rango/stock por fecha).
        var pendingRows = rows
            .Where(r => r.Status == PlanningStatus.Requested && r.LaboratoryId != null && r.StartPeriodId != null
                && r.PeriodCount != null && r.CourseGroupId != null)
            .ToList();
        var pendingIds = pendingRows.Select(r => r.Id).ToList();
        var linesByPlan = new Dictionary<int, IReadOnlyList<RequestLineInput>>();
        if (pendingIds.Count > 0)
        {
            var allLines = await _context.PlanningRequirements.AsNoTracking()
                .Where(l => pendingIds.Contains(l.PlanningId))
                .Select(l => new { l.PlanningId, l.ArticleId, Quantity = l.RequestedQuantity ?? l.ExpectedQuantity })
                .ToListAsync();
            foreach (var group in allLines.GroupBy(l => l.PlanningId))
                linesByPlan[group.Key] = group.Select(l => new RequestLineInput(l.ArticleId, l.Quantity)).ToList();
        }
        var materialByPlan = new Dictionary<int, IReadOnlyList<MaterialLineCheck>>();
        foreach (var dayGroup in pendingRows.GroupBy(r => r.ScheduledDate.Date))
        {
            var batch = dayGroup
                .Select(r => new MaterialDayRequest(r.Id, r.StartPeriodId!.Value, r.PeriodCount!.Value, linesByPlan.GetValueOrDefault(r.Id) ?? []))
                .ToList();
            foreach (var check in await _requests.CheckMaterialForDayAsync(dayGroup.Key, batch))
                materialByPlan[check.PlanningId] = check.Lines;
        }

        foreach (var r in rows)
        {
            int blocks = 0, competitors = 0, shortCount = 0, noStock = 0;
            // Solo las pendientes se analizan (las resueltas ya no compiten).
            if (r.Status == PlanningStatus.Requested && r.LaboratoryId is int labId && r.StartPeriodId is int startId && r.PeriodCount is int count && r.CourseGroupId is int groupId)
            {
                var check = await _requests.CheckSlotAsync(r.TeacherPersonId, new RequestInput
                {
                    CourseGroupId = groupId, Date = r.ScheduledDate, StartPeriodId = startId, PeriodCount = count, LaboratoryId = labId
                }, r.Id, forApproval: true);
                blocks = check.Blocks.Count();
                competitors = check.Issues.Count(i => i.Code == "PENDING_COMPETITION");
                if (materialByPlan.TryGetValue(r.Id, out var material))
                {
                    shortCount = material.Count(m => m.State == MaterialState.Short);
                    noStock = material.Count(m => m.State == MaterialState.NoStock);
                }
            }
            Rows.Add(new InboxRow(r.Id, Pages.LaboratoryOperations.L3Codes.Format(r.Id, r.Code), names.GetValueOrDefault(r.TeacherPersonId) ?? "—",
                r.PracticeName, r.Subject, r.Group, r.ScheduledDate, r.StartTime, r.EndTime, r.Lab, r.Items, r.Status, r.RequestDate,
                blocks, competitors, shortCount, noStock, r.Comment));
        }
    }

    /// <summary>Aprueba en lote las solicitudes indicadas que siguen sin ningún aviso (se revalidan una por una).</summary>
    public async Task<IActionResult> OnPostApproveCleanAsync(int[] ids)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        int ok = 0;
        var failed = new List<string>();
        foreach (var id in ids.Distinct())
        {
            // El lote nunca aprueba con faltantes: solo entra lo que el semáforo marcó como limpio.
            var result = await _requests.ApproveAsync(userId, id, "Aprobada en lote: sin choques ni faltantes.", null, allowShortage: false);
            if (result.Ok) ok++; else failed.Add(result.Message);
        }
        if (ok > 0) TempData.Success($"{ok} solicitud(es) aprobada(s). Los docentes ya fueron notificados.");
        if (failed.Count > 0) TempData.Warning($"{failed.Count} no se aprobaron: {string.Join(" ", failed.Take(3))}");
        return RedirectToPage();
    }
}
