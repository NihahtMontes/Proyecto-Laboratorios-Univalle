using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.TeacherRequests;

namespace Proyecto_Laboratorios_Univalle.Pages.Docente;

/// <summary>
/// «Pedir cambio de material» (L-3) de una práctica YA aprobada: el docente ajusta cantidades, agrega o quita
/// artículos, recalcule el kit para sus estudiantes y envía el pedido con un motivo; la planificación sigue
/// «Aprobada» y la Inge decide desde la bandeja «Cambios de material». El envío revalida todo en el servidor.
/// </summary>
public class CambioMaterialModel : TeacherPageBase
{
    public CambioMaterialModel(ApplicationDbContext db, ITeacherRequestService requests) : base(db, requests) { }

    /// <summary>Línea enviada por el formulario: artículo y cantidad deseada (0 = quitarlo del L-3).</summary>
    public sealed class LineInput
    {
        public int ArticleId { get; set; }
        public decimal Quantity { get; set; }
    }

    /// <summary>Fila renderizada: el L-3 vigente junto con la cantidad escrita por el docente.</summary>
    public sealed record CambioLine(int ArticleId, string Code, string Name, string? Category, string? Unit,
        decimal Current, decimal Quantity);

    public TeacherPlanningRow Row { get; private set; } = null!;
    public string Code => CodeOf(Row);
    public int? Students { get; private set; }

    /// <summary>Filas de la tabla editable (L-3 vigente; si el envío falló, con lo escrito por el docente).</summary>
    public IReadOnlyList<CambioLine> Rows { get; private set; } = [];

    /// <summary>Días entre hoy (hora Bolivia) y la fecha de la práctica.</summary>
    public int DaysAhead { get; private set; }
    public bool LastMinute => DaysAhead < MaterialChangeRules.OnTimeDays;

    /// <summary>Motivo escrito por el docente (se conserva si el servidor rechaza el envío).</summary>
    public string? Reason { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id))
        {
            TempData.Error("La solicitud no existe.");
            return RedirectToPage("./Solicitudes");
        }
        var eligibility = await Requests.CanRequestMaterialChangeAsync(Teacher, id);
        if (!eligibility.Allowed)
        {
            TempData.Warning(eligibility.Reason!);
            return RedirectToPage("./Solicitud", new { id });
        }
        Rows = (await Requests.GetLinesAsync(id))
            .Select(l => new CambioLine(l.ArticleId, l.Code, l.Name, l.Category, l.Unit, l.Quantity, l.Quantity))
            .ToList();
        return Page();
    }

    /// <summary>
    /// Kit de ESA práctica (la programación de esta planificación) escalado a la cantidad de estudiantes. JSON.
    /// A diferencia del endpoint de NuevaSolicitud, no limita a los grupos de la ventana de solicitud:
    /// la práctica ya está aprobada y puede estar en cualquier gestión.
    /// </summary>
    public async Task<IActionResult> OnGetKitAsync(int id, int students)
    {
        var plan = await Mine().Where(p => p.Id == id)
            .Select(p => new
            {
                p.SubjectId,
                ScalingFactor = p.CourseGroup != null ? p.CourseGroup.ScalingFactor : null,
                PracticeId = p.PracticeSchedule != null ? (int?)p.PracticeSchedule.PracticeId : null
            })
            .FirstOrDefaultAsync();
        if (plan == null || plan.PracticeId == null) return new JsonResult(Array.Empty<object>());
        var studentsSafe = Math.Clamp(students, 1, 500);
        var kit = await Db.PracticeRequirements.AsNoTracking()
            .Where(r => r.PracticeId == plan.PracticeId && !r.IsRemoved && r.Article!.Status == GeneralStatus.Activo
                && r.Practice!.SubjectId == plan.SubjectId)
            .Select(r => new
            {
                r.ArticleId,
                Code = r.Article!.Code,
                Name = r.Article.Name,
                r.Quantity,
                r.IsScalable,
                Category = r.Article.ArticleCategory != null ? r.Article.ArticleCategory.Name : r.Article.Category,
                Unit = r.Article.UnitOfMeasure
            })
            .ToListAsync();
        var escalado = RequirementPickerService.ScaleKit(
            kit.Select(k => new RequirementPickerService.KitLine(k.ArticleId, k.Quantity, k.IsScalable)),
            studentsSafe, plan.ScalingFactor);
        var infoPorArticulo = kit.GroupBy(k => k.ArticleId).ToDictionary(g => g.Key, g => g.First());
        var lines = escalado.OrderBy(kv => infoPorArticulo[kv.Key].Name).Select(kv =>
        {
            var first = infoPorArticulo[kv.Key];
            return new { articleId = first.ArticleId, code = first.Code, name = first.Name, category = first.Category, unit = first.Unit, quantity = kv.Value.Quantity };
        });
        return new JsonResult(lines);
    }

    public async Task<IActionResult> OnPostAsync(int id, List<LineInput>? lines, string? reason)
    {
        if (!await LoadAsync(id))
        {
            TempData.Error("La solicitud no existe.");
            return RedirectToPage("./Solicitudes");
        }
        var posted = lines ?? [];
        var result = await Requests.RequestMaterialChangeAsync(Teacher, id,
            posted.Select(l => new RequestLineInput(l.ArticleId, l.Quantity)).ToList(), reason ?? string.Empty);
        if (result.Ok)
        {
            TempData.Success(result.Message);
            if (result.Warnings is { Count: > 0 }) TempData.Warning(string.Join(" ", result.Warnings));
            return RedirectToPage("./Solicitud", new { id });
        }
        // El pedido no salió: se vuelve a mostrar el formulario con lo que escribió el docente.
        TempData.Error(result.Message);
        Reason = reason;
        await RepopulateAsync(id, posted);
        return Page();
    }

    private async Task<bool> LoadAsync(int id)
    {
        var row = await Project(Mine().Where(p => p.Id == id)).FirstOrDefaultAsync();
        if (row == null) return false;
        Row = row;
        var extra = await Mine().Where(p => p.Id == id)
            .Select(p => new { Students = p.StudentCount ?? (p.PracticeSchedule != null ? p.PracticeSchedule.StudentCount : null) })
            .FirstAsync();
        Students = extra.Students;
        DaysAhead = (Row.Date.Date - BoliviaTime.ToLocal(DateTime.UtcNow).Date).Days;
        return true;
    }

    /// <summary>Reconstruye las filas del formulario tras un envío rechazado: L-3 vigente con las cantidades enviadas.</summary>
    private async Task RepopulateAsync(int id, IReadOnlyList<LineInput> posted)
    {
        var current = await Requests.GetLinesAsync(id);
        var byArticle = posted.GroupBy(l => l.ArticleId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var extraIds = byArticle.Keys.Where(a => current.All(l => l.ArticleId != a)).ToList();
        var extras = extraIds.Count == 0
            ? []
            : await Db.Articles.AsNoTracking()
                .Where(a => extraIds.Contains(a.Id))
                .Select(a => new
                {
                    a.Id,
                    a.Code,
                    a.Name,
                    Unit = a.UnitOfMeasure,
                    Category = a.ArticleCategory != null ? a.ArticleCategory.Name : a.Category
                })
                .ToListAsync();
        var rows = current.Select(l => new CambioLine(l.ArticleId, l.Code, l.Name, l.Category, l.Unit, l.Quantity,
            byArticle.GetValueOrDefault(l.ArticleId, l.Quantity))).ToList();
        rows.AddRange(extras.Select(a => new CambioLine(a.Id, a.Code, a.Name, a.Category, a.Unit, 0m, byArticle.GetValueOrDefault(a.Id))));
        Rows = rows;
    }
}
