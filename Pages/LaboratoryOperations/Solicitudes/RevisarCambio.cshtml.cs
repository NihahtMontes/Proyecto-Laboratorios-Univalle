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
/// Revisión de un pedido de cambio de material (L-3) sobre una práctica YA aprobada: qué cambió
/// (Antes → Después por línea), si el material alcanza con el pedido aplicado, el motivo del docente
/// y el historial. El L-3 vigente solo se mueve cuando el pedido se aprueba; rechazarlo no cambia nada.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ReviewerRoles)]
public class RevisarCambioModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ITeacherRequestService _requests;

    public RevisarCambioModel(ApplicationDbContext context, ITeacherRequestService requests)
    {
        _context = context;
        _requests = requests;
    }

    public Planning Plan { get; private set; } = null!;
    public string Code => L3Codes.Format(Plan.Id, Plan.Code);
    public string TeacherName { get; private set; } = "—";
    public int? StartOrder => Plan.StartPeriod?.Order;
    public int? Students => Plan.StudentCount ?? Plan.CourseGroup?.StudentCount;

    /// <summary>Pedido de cambio pendiente (siempre presente cuando se llega a la vista).</summary>
    public PendingMaterialChangeView Pending { get; private set; } = null!;
    public IReadOnlyList<RequestLineView> Lines { get; private set; } = [];
    public IReadOnlyList<ReviewEventView> History { get; private set; } = [];

    /// <summary>Stock de las líneas finales del pedido (vigente + pedido aplicado), por artículo.</summary>
    public IReadOnlyList<MaterialLineCheck> Material { get; private set; } = [];

    /// <summary>Faltantes del L-3 final: solo con ellos aparece «Aprobar igual con faltantes».</summary>
    public int ShortCount => Material.Count(m => m.State == MaterialState.Short);

    /// <summary>Línea del pedido con el estado de material de su artículo (Antes → Después + Material).</summary>
    public sealed record LineReview(MaterialChangeLineView Line, MaterialLineCheck? Check)
    {
        /// <summary>«Alcanza», «Falta X» o «Sin stock registrado» según el semáforo del L-3 final.</summary>
        public string MaterialText => Check == null
            ? "—"
            : Check.State == MaterialState.NoStock
                ? "Sin stock registrado"
                : Check.State == MaterialState.Short
                    ? $"Falta {Check.Requested - (Check.Free ?? Check.Stock ?? 0):0.##}"
                    : "Alcanza";

        public string MaterialCss => Check == null ? "secondary" : Check.StateCss;
    }

    public IReadOnlyList<LineReview> ReviewLines { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadPlanningAsync(id))
        {
            TempData.Error("La práctica no existe.");
            return RedirectToPage("./Index", new { tab = "cambios" });
        }
        var pending = await _requests.GetPendingMaterialChangeAsync(id);
        if (pending == null)
        {
            TempData.Info("No hay un pedido de cambio pendiente para esta práctica.");
            return RedirectToPage("./Index", new { tab = "cambios" });
        }
        Pending = pending;
        Lines = await _requests.GetLinesAsync(id);
        History = await _requests.GetHistoryAsync(id);
        Material = await _requests.CheckPendingMaterialChangeAsync(id);
        var materialByArticle = Material.ToDictionary(m => m.ArticleId);
        ReviewLines = Pending.Lines.Select(l => new LineReview(l, materialByArticle.GetValueOrDefault(l.ArticleId))).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id, string? comment, bool allowShortage)
    {
        var result = await _requests.ApproveMaterialChangeAsync(UserId, id, comment, allowShortage);
        if (!result.Ok)
        {
            TempData.Error(result.Message);
            return RedirectToPage(new { id });
        }
        TempData.Success(result.Message);
        if (result.Warnings is { Count: > 0 }) TempData.Warning(string.Join(" ", result.Warnings));
        return RedirectToPage("./Index", new { tab = "cambios" });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id, string comment)
    {
        var result = await _requests.RejectMaterialChangeAsync(UserId, id, comment);
        if (result.Ok) TempData.Success(result.Message); else TempData.Error(result.Message);
        return result.Ok ? RedirectToPage("./Index", new { tab = "cambios" }) : RedirectToPage(new { id });
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<bool> LoadPlanningAsync(int id)
    {
        var plan = await _context.Plannings.AsNoTracking()
            .Include(p => p.Laboratory).Include(p => p.Subject)
            .Include(p => p.StartPeriod).Include(p => p.CourseGroup)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (plan == null) return false;
        Plan = plan;
        TeacherName = await _context.Interns.AsNoTracking().Where(i => i.Id == plan.TeacherPersonId)
            .Select(i => string.IsNullOrWhiteSpace(i.Title) ? i.Name : i.Title + " " + i.Name)
            .FirstOrDefaultAsync() ?? "—";
        return true;
    }
}
