using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 2: requerimientos L-3 por artículo. Se precargan desde la receta de la práctica (cantidad escalada por
/// estudiantes ÷ factor en las líneas escalables) y se pueden agregar artículos sueltos, con unidad física opcional.
/// Los artículos "Por confirmar" se pueden pedir (D1) y se muestran con su etiqueta.
/// </summary>
public class RequerimientosModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planningService;

    public RequerimientosModel(ApplicationDbContext context, IPlanningService planningService)
    {
        _context = context;
        _planningService = planningService;
    }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Conflict { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public L3Request? Plan { get; private set; }
    public string Code => L3Codes.Format(PlanId);
    public bool IsEditable => Plan?.Status == L3RequestStatus.Planned;
    public List<RequirementRow> Items { get; private set; } = [];
    public List<ArticleOption> Articles { get; private set; } = [];
    public List<UnitOption> Units { get; private set; } = [];

    /// <summary>Líneas de la receta de la práctica (para mostrar qué se precargará).</summary>
    public List<RecipeRow> Recipe { get; private set; } = [];
    public int? StudentCount { get; private set; }
    public int? ScalingFactor { get; private set; }

    public sealed record RequirementRow(int DetailId, int ArticleId, string ArticleCode, string ArticleName, bool ArticlePending,
        string? Unit, decimal? Quantity, string? InventoryNumber, string? Observations);
    public sealed record ArticleOption(int Id, string Code, string Name, string? Category, string? Unit, bool Pending);
    public sealed record UnitOption(int Id, string InventoryNumber, string EquipmentName);
    public sealed record RecipeRow(string ArticleCode, string ArticleName, bool ArticlePending, decimal? BaseQuantity,
        bool IsScalable, decimal? ComputedQuantity, string? Note);

    /// <summary>Franja superior del wizard: planificación de la práctica y paso actual.</summary>
    public PlanningContextViewModel? Context { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Context = await PlanningContextLoader.ForPracticeAsync(_context, _planningService, PlanId, 2);
        return await LoadPageAsync();
    }

    /// <summary>Precarga la receta: agrega las líneas que aún no están en la solicitud.</summary>
    public async Task<IActionResult> OnPostLoadRecipeAsync()
    {
        var plan = await GuardAsync();
        if (plan == null) return RedirectToPage(new { planId = PlanId });

        var schedule = plan.PracticeScheduleId.HasValue
            ? await _context.PracticeSchedules.AsNoTracking().FirstOrDefaultAsync(s => s.Id == plan.PracticeScheduleId.Value)
            : null;
        if (schedule == null)
        {
            TempData.Warning("La práctica no está vinculada a una práctica del catálogo: agrega los artículos manualmente.");
            return RedirectToPage(new { planId = PlanId });
        }

        var recipe = await _context.PracticeRequirements.AsNoTracking()
            .Include(r => r.Article)
            .Where(r => r.PracticeId == schedule.PracticeId && !r.IsRemoved && r.Article!.Status == GeneralStatus.Activo)
            .ToListAsync();
        if (recipe.Count == 0)
        {
            TempData.Warning("La práctica del catálogo no tiene receta registrada.");
            return RedirectToPage(new { planId = PlanId });
        }

        var present = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => d.L3RequestId == plan.Id).Select(d => d.ArticleId).ToListAsync();
        int added = 0, skipped = 0;
        var warnings = new List<string>();
        foreach (var line in recipe.Where(r => !present.Contains(r.ArticleId)))
        {
            var (quantity, note) = Scale(line, schedule.StudentCount, schedule.ScalingFactor);
            if (quantity == null)
            {
                skipped++;
                warnings.Add($"{line.Article!.Code}: {note}");
                continue;
            }
            _context.L3RequestDetails.Add(new L3RequestDetail
            {
                L3RequestId = plan.Id,
                ArticleId = line.ArticleId,
                RequestedQuantity = quantity,
                ExpectedQuantity = (int)Math.Ceiling(quantity.Value),
                Observations = note
            });
            added++;
        }
        await _context.SaveChangesAsync();

        if (added > 0) TempData.Success($"Receta cargada: {added} artículo(s) agregados.");
        else TempData.Info("Todos los artículos de la receta ya estaban en la solicitud.");
        if (skipped > 0) TempData.Warning($"{skipped} línea(s) sin cantidad en la receta no se cargaron: {string.Join("; ", warnings.Take(5))}");
        return RedirectToPage(new { planId = PlanId });
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        var plan = await GuardAsync();
        if (plan == null) return RedirectToPage(new { planId = PlanId });

        if (Input.Quantity is decimal q && decimal.Round(q, 2) != q)
            ModelState.AddModelError("Input.Quantity", "La cantidad admite hasta 2 decimales.");
        var article = await _context.Articles.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == Input.ArticleId && a.Status == GeneralStatus.Activo);
        if (article == null)
            ModelState.AddModelError("Input.ArticleId", "Selecciona un artículo activo.");
        if (Input.EquipmentUnitId.HasValue && !await _context.EquipmentUnits.AsNoTracking().AnyAsync(u =>
                u.Id == Input.EquipmentUnitId && u.LaboratoryId == plan.LaboratoryId && u.CurrentStatus != EquipmentStatus.Deleted))
            ModelState.AddModelError("Input.EquipmentUnitId", "La unidad física no pertenece al laboratorio de la práctica.");
        if (!ModelState.IsValid) return await LoadPageAsync();

        var observations = string.IsNullOrWhiteSpace(Input.Observations) ? null : Input.Observations.Trim();
        var existing = await _context.L3RequestDetails.AsTracking()
            .FirstOrDefaultAsync(d => d.L3RequestId == plan.Id && d.ArticleId == Input.ArticleId && d.EquipmentUnitId == Input.EquipmentUnitId);
        if (existing != null)
        {
            existing.RequestedQuantity = (existing.RequestedQuantity ?? 0) + Input.Quantity;
            existing.ExpectedQuantity = (int)Math.Ceiling(existing.RequestedQuantity ?? 0);
            if (observations != null) existing.Observations = observations;
        }
        else
        {
            _context.L3RequestDetails.Add(new L3RequestDetail
            {
                L3RequestId = plan.Id,
                ArticleId = Input.ArticleId,
                EquipmentUnitId = Input.EquipmentUnitId,
                RequestedQuantity = Input.Quantity,
                ExpectedQuantity = (int)Math.Ceiling(Input.Quantity ?? 0),
                Observations = observations
            });
        }

        await _context.SaveChangesAsync();
        TempData.Success(article!.ReviewStatus == DataReviewStatus.PorConfirmar
            ? $"Requerimiento agregado. {article.Code} es un artículo \"Por confirmar\"."
            : "Requerimiento agregado.");
        return RedirectToPage(new { planId = PlanId });
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int detailId, decimal newQuantity)
    {
        var plan = await GuardAsync();
        if (plan == null) return RedirectToPage(new { planId = PlanId });

        if (newQuantity <= 0 || newQuantity > 999999 || decimal.Round(newQuantity, 2) != newQuantity)
        {
            TempData.Error("La cantidad debe ser mayor a 0 y admite hasta 2 decimales.");
            return RedirectToPage(new { planId = PlanId });
        }

        var detail = await _context.L3RequestDetails.AsTracking()
            .FirstOrDefaultAsync(d => d.Id == detailId && d.L3RequestId == plan.Id);
        if (detail == null)
        {
            TempData.Error("El requerimiento no pertenece a esta práctica.");
            return RedirectToPage(new { planId = PlanId });
        }

        detail.RequestedQuantity = newQuantity;
        detail.ExpectedQuantity = (int)Math.Ceiling(newQuantity);
        await _context.SaveChangesAsync();
        TempData.Success("Cantidad actualizada.");
        return RedirectToPage(new { planId = PlanId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(int detailId)
    {
        var plan = await GuardAsync();
        if (plan == null) return RedirectToPage(new { planId = PlanId });

        var detail = await _context.L3RequestDetails.AsTracking()
            .FirstOrDefaultAsync(d => d.Id == detailId && d.L3RequestId == plan.Id);
        if (detail != null)
        {
            // Línea de borrador de una solicitud aún no aprobada: no tiene salidas ni historial que preservar.
            _context.L3RequestDetails.Remove(detail);
            await _context.SaveChangesAsync();
            TempData.Success("Requerimiento retirado.");
        }
        return RedirectToPage(new { planId = PlanId });
    }

    public async Task<IActionResult> OnPostContinueAsync()
    {
        var plan = await GuardAsync();
        if (plan == null) return RedirectToPage(new { planId = PlanId });

        if (!await _context.L3RequestDetails.AsNoTracking().AnyAsync(d => d.L3RequestId == plan.Id))
        {
            TempData.Error("Agrega al menos un requerimiento antes de validar disponibilidad.");
            return RedirectToPage(new { planId = PlanId });
        }

        return RedirectToPage("./Disponibilidad", new { planId = PlanId });
    }

    /// <summary>Cantidad de una línea de receta: base, o base × estudiantes ÷ factor (redondeada hacia arriba) si es escalable.</summary>
    public static (decimal? Quantity, string? Note) Scale(PracticeRequirement line, int? students, int? factor)
    {
        if (line.Quantity is not decimal baseQuantity)
            return (null, "la receta no trae cantidad");
        if (!line.IsScalable)
            return (baseQuantity, null);
        if (students is not int s || s <= 0 || factor is not int f || f <= 0)
            return (baseQuantity, "Escalable sin estudiantes o factor en la programación: se usa la cantidad base.");
        return (Math.Ceiling(baseQuantity * s / f), $"Escalada: {baseQuantity:0.##} × {s} estudiantes ÷ {f}.");
    }

    private async Task<L3Request?> GuardAsync()
    {
        if (!await PlanningContextLoader.IsWritableAsync(_context, PlanId))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return null;
        }
        var plan = await _context.L3Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == PlanId);
        if (plan == null)
        {
            TempData.Error("La práctica no existe.");
            return null;
        }
        if (plan.Status != L3RequestStatus.Planned)
        {
            TempData.Error("La solicitud ya fue aprobada; sus requerimientos no se pueden modificar.");
            return null;
        }
        return plan;
    }

    private async Task<IActionResult> LoadPageAsync()
    {
        Plan = await _context.L3Requests.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.TeacherPerson)
            .Include(r => r.PracticeSchedule)
            .FirstOrDefaultAsync(r => r.Id == PlanId);
        if (Plan == null)
        {
            TempData.Error("La práctica no existe.");
            return RedirectToPage("./Index");
        }

        StudentCount = Plan.PracticeSchedule?.StudentCount;
        ScalingFactor = Plan.PracticeSchedule?.ScalingFactor;

        Items = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => d.L3RequestId == Plan.Id)
            .OrderBy(d => d.Article!.Name)
            .Select(d => new RequirementRow(d.Id, d.ArticleId, d.Article!.Code, d.Article.Name,
                d.Article.ReviewStatus == DataReviewStatus.PorConfirmar,
                d.Article.MeasurementUnit != null ? d.Article.MeasurementUnit.Name : d.Article.UnitOfMeasure,
                d.RequestedQuantity, d.EquipmentUnit != null ? d.EquipmentUnit.InventoryNumber : null, d.Observations))
            .ToListAsync();

        if (IsEditable)
        {
            Articles = await _context.Articles.AsNoTracking()
                .Where(a => a.Status == GeneralStatus.Activo)
                .OrderBy(a => a.Name)
                .Select(a => new ArticleOption(a.Id, a.Code, a.Name,
                    a.ArticleCategory != null ? a.ArticleCategory.Name : a.Category,
                    a.MeasurementUnit != null ? a.MeasurementUnit.Name : a.UnitOfMeasure,
                    a.ReviewStatus == DataReviewStatus.PorConfirmar))
                .ToListAsync();
            Units = Plan.LaboratoryId.HasValue
                ? await _context.EquipmentUnits.AsNoTracking()
                    .Where(u => u.LaboratoryId == Plan.LaboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                    .OrderBy(u => u.InventoryNumber)
                    .Select(u => new UnitOption(u.Id, u.InventoryNumber, u.Equipment != null ? u.Equipment.Name : ""))
                    .ToListAsync()
                : [];
        }

        if (Plan.PracticeSchedule != null)
        {
            var recipe = await _context.PracticeRequirements.AsNoTracking()
                .Include(r => r.Article)
                .Where(r => r.PracticeId == Plan.PracticeSchedule.PracticeId && !r.IsRemoved)
                .OrderBy(r => r.Article!.Name)
                .ToListAsync();
            Recipe = recipe.Select(r =>
            {
                var (quantity, note) = Scale(r, StudentCount, ScalingFactor);
                return new RecipeRow(r.Article!.Code, r.Article.Name, r.Article.ReviewStatus == DataReviewStatus.PorConfirmar,
                    r.Quantity, r.IsScalable, quantity, note);
            }).ToList();
        }

        return Page();
    }

    public sealed class InputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un artículo")]
        [Display(Name = "Artículo")]
        public int ArticleId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(0.01, 999999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [Display(Name = "Cantidad")]
        public decimal? Quantity { get; set; } = 1;

        [Display(Name = "Unidad física (opcional)")]
        public int? EquipmentUnitId { get; set; }

        [StringLength(1000)]
        [Display(Name = "Observaciones")]
        public string? Observations { get; set; }
    }
}
