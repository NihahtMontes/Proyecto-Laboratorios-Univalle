using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 5: incidente L-5 sobre un artículo entregado en la salida L-3 (y, si corresponde, la unidad física concreta).
/// Con unidad física, ApplicationDbContext actualiza su historial y estado al guardar.
/// </summary>
public class L5Model : PageModel
{
    public static readonly string[] IncidentTypes = ["Daño", "Rotura", "Pérdida", "Faltante", "Otro"];
    public static readonly string[] ResponsibleTypes = ["ESTUDIANTE", "DOCENTE"];

    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planningService;

    public L5Model(ApplicationDbContext context, IPlanningService planningService)
    {
        _context = context;
        _planningService = planningService;
    }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    /// <summary>Artículo preseleccionado desde L-3 (opcional).</summary>
    [BindProperty(SupportsGet = true)]
    public int? ArticleId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public L3Request? Plan { get; private set; }
    public string Code => L3Codes.Format(PlanId);
    public List<SelectListItem> ArticleOptions { get; private set; } = [];
    public List<SelectListItem> UnitOptions { get; private set; } = [];

    /// <summary>Cantidad entregada por artículo (tope de la cantidad afectada).</summary>
    public Dictionary<int, decimal> DeliveredByArticle { get; private set; } = [];

    /// <summary>Franja superior del wizard: planificación de la práctica y paso actual.</summary>
    public PlanningContextViewModel? Context { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Context = await PlanningContextLoader.ForPracticeAsync(_context, _planningService, PlanId, 5);
        if (ArticleId.HasValue) Input.ArticleId = ArticleId.Value;
        var redirect = await LoadAsync();
        if (redirect != null) return redirect;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await PlanningContextLoader.IsWritableAsync(_context, PlanId))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return RedirectToPage(new { planId = PlanId });
        }

        var redirect = await LoadAsync();
        if (redirect != null) return redirect;

        if (!DeliveredByArticle.TryGetValue(Input.ArticleId, out var delivered))
            ModelState.AddModelError("Input.ArticleId", "El artículo no fue entregado en la salida de esta práctica.");
        else if (Input.AffectedQuantity > Math.Ceiling(delivered))
            ModelState.AddModelError("Input.AffectedQuantity", $"La cantidad afectada no puede superar lo entregado ({delivered:0.##}).");
        if (Input.EquipmentUnitId.HasValue && !UnitOptions.Any(o => o.Value == Input.EquipmentUnitId.Value.ToString()))
            ModelState.AddModelError("Input.EquipmentUnitId", "La unidad no corresponde a las solicitadas en esta práctica.");
        if (!IncidentTypes.Contains(Input.IncidentType))
            ModelState.AddModelError("Input.IncidentType", "Selecciona un tipo de incidente válido.");
        if (!ResponsibleTypes.Contains(Input.ResponsibleType))
            ModelState.AddModelError("Input.ResponsibleType", "Selecciona si el involucrado es estudiante o docente.");

        if (!ModelState.IsValid) return Page();

        _context.L5Incidents.Add(new L5Incident
        {
            L3RequestId = PlanId,
            ArticleId = Input.ArticleId,
            EquipmentUnitId = Input.EquipmentUnitId,
            ResponsibleName = Input.ResponsibleName.Trim(),
            ResponsibleType = Input.ResponsibleType,
            IncidentType = Input.IncidentType,
            DamageDescription = Input.DamageDescription.Trim(),
            AffectedQuantity = Input.AffectedQuantity,
            RepositionStatus = "Pendiente de Reposición",
            RecordDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData.Success(Input.EquipmentUnitId.HasValue
            ? "Incidente L-5 registrado. La unidad quedó marcada como averiada."
            : "Incidente L-5 registrado.");
        return RedirectToPage("./L3", new { id = PlanId });
    }

    private async Task<IActionResult?> LoadAsync()
    {
        Plan = await _context.L3Requests.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .FirstOrDefaultAsync(r => r.Id == PlanId);
        if (Plan == null)
        {
            TempData.Error("La práctica no existe.");
            return RedirectToPage("./Index");
        }
        if (Plan.Status != L3RequestStatus.Approved)
        {
            TempData.Error("Solo se registran incidentes L-5 de una solicitud aprobada y no finalizada.");
            return RedirectToPage("./L3", new { id = PlanId });
        }

        // Artículos entregados en la salida de esta práctica.
        var delivered = await _context.DepartureItems.AsNoTracking()
            .Where(i => i.Departure!.L3RequestId == PlanId && i.ArticleId != null)
            .GroupBy(i => new { ArticleId = i.ArticleId!.Value, i.Article!.Code, i.Article.Name })
            .Select(g => new { g.Key.ArticleId, g.Key.Code, g.Key.Name, Quantity = g.Sum(i => i.Quantity ?? 0) })
            .ToListAsync();
        if (delivered.Count == 0)
        {
            TempData.Error("Primero registra la salida de los artículos en L-3.");
            return RedirectToPage("./L3", new { id = PlanId });
        }
        DeliveredByArticle = delivered.ToDictionary(d => d.ArticleId, d => d.Quantity);
        ArticleOptions = delivered.OrderBy(d => d.Name)
            .Select(d => new SelectListItem($"{d.Code} · {d.Name} (entregado {d.Quantity:0.##})", d.ArticleId.ToString(), d.ArticleId == Input.ArticleId))
            .ToList();

        // Unidades físicas indicadas en los requerimientos de la práctica.
        var units = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => d.L3RequestId == PlanId && d.EquipmentUnitId != null)
            .Select(d => new
            {
                Id = d.EquipmentUnitId!.Value,
                d.EquipmentUnit!.InventoryNumber,
                Name = d.EquipmentUnit.Equipment != null ? d.EquipmentUnit.Equipment.Name : ""
            })
            .Distinct()
            .ToListAsync();
        UnitOptions = units.OrderBy(u => u.InventoryNumber)
            .Select(u => new SelectListItem($"{u.Name} (INV: {u.InventoryNumber})", u.Id.ToString(), u.Id == Input.EquipmentUnitId))
            .ToList();
        return null;
    }

    public sealed class InputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona el artículo afectado")]
        public int ArticleId { get; set; }

        /// <summary>Unidad física concreta (opcional).</summary>
        public int? EquipmentUnitId { get; set; }

        [Required(ErrorMessage = "Indica el nombre del involucrado"), StringLength(200)]
        public string ResponsibleName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string ResponsibleType { get; set; } = "ESTUDIANTE";

        [Required(ErrorMessage = "Selecciona el tipo de incidente"), StringLength(100)]
        public string IncidentType { get; set; } = "Daño";

        [Required(ErrorMessage = "Describe el daño o la pérdida"), StringLength(2000)]
        public string DamageDescription { get; set; } = string.Empty;

        [Range(1, 999, ErrorMessage = "La cantidad afectada debe estar entre 1 y 999")]
        public int AffectedQuantity { get; set; } = 1;
    }
}
