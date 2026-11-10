using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.Occupancy;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations.Solicitudes;

/// <summary>
/// Revisión de una solicitud docente en una sola pantalla: datos y material (editable), el día del laboratorio
/// con las prácticas aprobadas y las solicitudes que compiten, y el análisis de choques/material.
/// Acciones: aprobar (con o sin cambios de cantidades), observar o rechazar; todo queda en el historial.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ReviewerRoles)]
public class RevisarModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ITeacherRequestService _requests;
    private readonly IOccupancyService _occupancy;

    public RevisarModel(ApplicationDbContext context, ITeacherRequestService requests, IOccupancyService occupancy)
    {
        _context = context;
        _requests = requests;
        _occupancy = occupancy;
    }

    public Planning Plan { get; private set; } = null!;
    public string Code => L3Codes.Format(Plan.Id, Plan.Code);
    public string TeacherName { get; private set; } = "—";
    public int? Students { get; private set; }
    public int? StartOrder { get; private set; }
    public IReadOnlyList<RequestLineView> Lines { get; private set; } = [];
    public IReadOnlyList<MaterialLineCheck> Material { get; private set; } = [];
    public SlotCheckResult Slot { get; private set; } = new([]);
    public IReadOnlyList<ReviewEventView> History { get; private set; } = [];
    public List<ClassPeriod> Periods { get; private set; } = [];
    public List<OccupancyBlock> DayBlocks { get; private set; } = [];
    public List<CompetitorView> Competitors { get; private set; } = [];
    public bool CanReview => Plan.Status == PlanningStatus.Requested;

    public sealed record CompetitorView(int Id, string Code, string Teacher, string Practice, int StartOrder, int PeriodCount, DateTime RequestDate);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id))
        {
            TempData.Error("La solicitud no existe.");
            return RedirectToPage("./Index");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, string? comment, Dictionary<int, decimal>? qty, bool allowShortage)
    {
        var current = await _context.PlanningRequirements.AsNoTracking().Where(r => r.PlanningId == id)
            .Select(r => new { r.ArticleId, Quantity = r.RequestedQuantity ?? r.ExpectedQuantity }).ToListAsync();
        List<RequestLineInput>? adjusted = null;
        if (qty != null && current.Any(c => qty.TryGetValue(c.ArticleId, out var q) && q != c.Quantity))
            adjusted = current.Select(c => new RequestLineInput(c.ArticleId, qty.TryGetValue(c.ArticleId, out var q) ? q : c.Quantity)).ToList();

        // allowShortage viene del formulario («Aprobar igual con faltantes»): aprueba aunque el material no alcance.
        var result = await _requests.ApproveAsync(UserId, id, comment, adjusted, allowShortage);
        if (!result.Ok)
        {
            TempData.Error(result.Message);
            return RedirectToPage(new { id });
        }
        TempData.Success(result.Message + (adjusted != null ? " Se registraron los cambios de cantidades." : ""));
        if (result.Warnings is { Count: > 0 }) TempData.Warning(string.Join(" ", result.Warnings));
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostObserveAsync(int id, string comment)
    {
        var result = await _requests.ObserveAsync(UserId, id, comment);
        if (result.Ok) TempData.Success(result.Message); else TempData.Error(result.Message);
        return result.Ok ? RedirectToPage("./Index") : RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string comment)
    {
        var result = await _requests.RejectAsync(UserId, id, comment);
        if (result.Ok) TempData.Success(result.Message); else TempData.Error(result.Message);
        return result.Ok ? RedirectToPage("./Index") : RedirectToPage(new { id });
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<bool> LoadAsync(int id)
    {
        var plan = await _context.Plannings.AsNoTracking()
            .Include(p => p.Laboratory).Include(p => p.Subject).Include(p => p.CourseGroup)
            .Include(p => p.StartPeriod).Include(p => p.PracticeSchedule)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return false;
        Plan = plan;
        StartOrder = plan.StartPeriod?.Order;
        Students = plan.PracticeSchedule?.StudentCount ?? plan.CourseGroup?.StudentCount;
        TeacherName = await NameAsync(plan.TeacherPersonId);
        Lines = await _requests.GetLinesAsync(id);
        History = await _requests.GetHistoryAsync(id);
        Periods = await _context.ClassPeriods.AsNoTracking().OrderBy(p => p.Order).ToListAsync();

        if (plan.LaboratoryId is int labId && plan.StartPeriodId is int startId && plan.PeriodCount is int count)
        {
            if (plan.CourseGroupId is int groupId && CanReview)
                Slot = await _requests.CheckSlotAsync(plan.TeacherPersonId, new RequestInput
                {
                    CourseGroupId = groupId, Date = plan.ScheduledDate, StartPeriodId = startId, PeriodCount = count, LaboratoryId = labId
                }, plan.Id, forApproval: true);
            Material = await _requests.CheckMaterialAsync(plan.ScheduledDate, startId, count,
                Lines.Select(l => new RequestLineInput(l.ArticleId, l.Quantity)).ToList(), plan.Id);
            DayBlocks = (await _occupancy.GetAsync(new OccupancyQuery(plan.ScheduledDate.Date, plan.ScheduledDate.Date, [labId], null, [plan.Id]))).ToList();

            var competitors = CanReview ? await _requests.FindCompetingRequestsAsync(plan.Id) : [];
            foreach (var c in competitors)
                Competitors.Add(new CompetitorView(c.Id, L3Codes.Format(c.Id, c.Code), await NameAsync(c.TeacherPersonId), c.PracticeName,
                    c.StartPeriod!.Order, c.PeriodCount ?? 1, c.RequestDate));
        }
        return true;
    }

    private async Task<string> NameAsync(int personId) =>
        await _context.Interns.AsNoTracking().Where(i => i.Id == personId)
            .Select(i => string.IsNullOrWhiteSpace(i.Title) ? i.Name : i.Title + " " + i.Name)
            .FirstOrDefaultAsync() ?? "—";
}
