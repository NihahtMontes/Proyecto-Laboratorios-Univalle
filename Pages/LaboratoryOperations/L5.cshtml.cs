using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>Tope de incidentes L-5 pendientes por artículo: lo entregado menos lo devuelto y lo ya registrado en L-5.</summary>
public static class L5Rules
{
    public static int MaxAdditional(decimal delivered, decimal returned, int alreadyRegistered) =>
        Math.Max(0, (int)Math.Ceiling(Math.Max(0, delivered - returned)) - alreadyRegistered);
}

/// <summary>
/// Paso 5: incidente L-5 sobre un artículo entregado en la salida L-3 de una planificación (y, si corresponde,
/// la unidad física concreta). Con unidad física, ApplicationDbContext actualiza su historial y estado al guardar.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ClosureRoles)]
public class L5Model : PageModel
{
    public static readonly string[] IncidentTypes = ["Daño", "Rotura", "Pérdida", "Faltante", "Otro"];
    public static readonly string[] ResponsibleTypes = ["ESTUDIANTE", "DOCENTE"];

    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;

    public L5Model(ApplicationDbContext context, IAcademicManagementService managements)
    {
        _context = context;
        _managements = managements;
    }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    /// <summary>Artículo preseleccionado desde L-3 (opcional).</summary>
    [BindProperty(SupportsGet = true)]
    public int? ArticleId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public Planning? Plan { get; private set; }
    public string Code => L3Codes.Format(PlanId, Plan?.Code);
    public List<SelectListItem> ArticleOptions { get; private set; } = [];
    public List<SelectListItem> UnitOptions { get; private set; } = [];

    /// <summary>Cantidad entregada por artículo (tope de la cantidad afectada).</summary>
    public Dictionary<int, decimal> DeliveredByArticle { get; private set; } = [];

    /// <summary>Cantidad ya registrada en incidentes L-5 por artículo de esta planificación.</summary>
    public Dictionary<int, int> RegisteredByArticle { get; private set; } = [];

    /// <summary>Cantidad ya devuelta por artículo de la salida de esta planificación.</summary>
    public Dictionary<int, decimal> ReturnedByArticle { get; private set; } = [];

    /// <summary>Franja superior del wizard: gestión de la planificación y paso actual.</summary>
    public PlanningContextViewModel? Context { get; private set; }

    /// <summary>El rol de cierre solo opera planificaciones del día o con devoluciones pendientes (GET y POST).</summary>
    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var denied = await PracticeClosureAccess.DenyReasonAsync(_context, User, PlanId);
        if (denied != null)
        {
            TempData.Error(denied);
            context.Result = RedirectToPage(PracticeClosureAccess.HomePage);
            return;
        }
        await next();
    }

    /// <summary>Pantalla a la que se vuelve al terminar: el cierre del día o el panel de planificación.</summary>
    private IActionResult RedirectToHome() =>
        PracticeClosureAccess.IsClosureOnly(User) ? RedirectToPage(PracticeClosureAccess.HomePage) : RedirectToPage("./Index");

    public async Task<IActionResult> OnGetAsync()
    {
        Context = await PlanningContextLoader.ForPlanningAsync(_context, _managements, PlanId, 5);
        if (ArticleId.HasValue) Input.ArticleId = ArticleId.Value;
        var redirect = await LoadAsync();
        if (redirect != null) return redirect;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await PlanningContextLoader.IsWritableAsync(_managements, PlanId))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return RedirectToPage(new { planId = PlanId });
        }

        var redirect = await LoadAsync();
        if (redirect != null) return redirect;

        if (!DeliveredByArticle.TryGetValue(Input.ArticleId, out var delivered))
            ModelState.AddModelError("Input.ArticleId", "El artículo no fue entregado en la salida de esta planificación.");
        else
        {
            // Tope pendiente: lo entregado menos lo devuelto y los incidentes L-5 ya registrados para este artículo.
            var registrado = RegisteredByArticle.GetValueOrDefault(Input.ArticleId);
            var devuelto = ReturnedByArticle.GetValueOrDefault(Input.ArticleId);
            var max = L5Rules.MaxAdditional(delivered, devuelto, registrado);
            if (max == 0)
                ModelState.AddModelError("Input.AffectedQuantity",
                    $"No queda nada pendiente de este artículo: entregado {delivered:0.##}, devuelto {devuelto:0.##}, ya registrado en incidentes {registrado}.");
            else if (Input.AffectedQuantity > max)
                ModelState.AddModelError("Input.AffectedQuantity",
                    $"La cantidad supera lo pendiente: entregado {delivered:0.##}, devuelto {devuelto:0.##}, ya registrado en incidentes {registrado}; como máximo {max} más.");
            else if (Input.AffectedQuantity < 1)
                ModelState.AddModelError("Input.AffectedQuantity", "La cantidad afectada debe ser al menos 1.");
        }
        if (Input.EquipmentUnitId.HasValue && !UnitOptions.Any(o => o.Value == Input.EquipmentUnitId.Value.ToString()))
            ModelState.AddModelError("Input.EquipmentUnitId", "La unidad no corresponde a las solicitadas en esta planificación.");
        if (!IncidentTypes.Contains(Input.IncidentType))
            ModelState.AddModelError("Input.IncidentType", "Selecciona un tipo de incidente válido.");
        if (!ResponsibleTypes.Contains(Input.ResponsibleType))
            ModelState.AddModelError("Input.ResponsibleType", "Selecciona si el involucrado es estudiante o docente.");

        if (!ModelState.IsValid)
        {
            // Sin la franja de contexto la vista no dibuja el formulario y los errores quedarían ocultos.
            Context = await PlanningContextLoader.ForPlanningAsync(_context, _managements, PlanId, 5);
            return Page();
        }

        _context.L5Incidents.Add(new L5Incident
        {
            PlanningId = PlanId,
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
            ? "Incidente de material registrado. La unidad quedó marcada como averiada."
            : "Incidente de material registrado.");
        return RedirectToPage("./L3", new { id = PlanId });
    }

    private async Task<IActionResult?> LoadAsync()
    {
        Plan = await _context.Plannings.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .FirstOrDefaultAsync(r => r.Id == PlanId);
        if (Plan == null)
        {
            TempData.Error("La planificación no existe.");
            return RedirectToHome();
        }
        var hasDeparture = await _context.Departures.AsNoTracking()
            .AnyAsync(d => d.PlanningId == PlanId && d.Status != LoanStatus.Cancelled);
        if (!PlanningStatus.AllowsIncidents(Plan.Status, hasDeparture))
        {
            TempData.Error("Solo se registran incidentes de material de una planificación aprobada (o reubicada con salida de material) y no finalizada.");
            return RedirectToPage("./L3", new { id = PlanId });
        }

        // Artículos entregados en la salida de esta planificación (sin salidas anuladas). Las líneas de unidad
        // física llevan EquipmentUnitId (sin ArticleId, por integridad del ítem): se atribuyen al artículo vinculado a su equipo.
        var deliveredItems = await _context.DepartureItems.AsNoTracking()
            .Where(i => i.Departure!.PlanningId == PlanId && i.Departure.Status != LoanStatus.Cancelled)
            .Select(i => new { i.Id, i.ArticleId, i.EquipmentUnitId, i.Quantity })
            .ToListAsync();
        var unitIds = deliveredItems.Where(i => i.EquipmentUnitId != null).Select(i => i.EquipmentUnitId!.Value).Distinct().ToList();
        var unitEquipment = unitIds.Count == 0
            ? new Dictionary<int, int>()
            : await _context.EquipmentUnits.AsNoTracking()
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.EquipmentId);
        var equipmentIds = unitEquipment.Values.Distinct().ToList();
        var articleByEquipment = equipmentIds.Count == 0
            ? new Dictionary<int, int>()
            : (await _context.Articles.AsNoTracking()
                    .Where(a => a.EquipmentId != null && equipmentIds.Contains(a.EquipmentId.Value) && a.Status == GeneralStatus.Activo)
                    .OrderBy(a => a.Id)
                    .ToListAsync())
                .GroupBy(a => a.EquipmentId!.Value)
                .ToDictionary(g => g.Key, g => g.First().Id);

        int? UnitArticle(int? unitId) =>
            unitId.HasValue && unitEquipment.TryGetValue(unitId.Value, out var equipmentId)
            && articleByEquipment.TryGetValue(equipmentId, out var linkedArticleId)
                ? linkedArticleId
                : null;

        var quantities = deliveredItems
            .GroupBy(i => i.ArticleId ?? UnitArticle(i.EquipmentUnitId))
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!.Value, g => g.Sum(i => i.Quantity ?? 0));
        if (quantities.Count == 0)
        {
            TempData.Error("Primero registra la entrega de los artículos.");
            return RedirectToPage("./L3", new { id = PlanId });
        }

        // Devuelto por ítem (acumulado de las devoluciones registradas), para cruzarlo por artículo con UnitArticle.
        var deliveredItemIds = deliveredItems.Select(i => i.Id).ToList();
        var returnedByItem = deliveredItemIds.Count == 0
            ? new Dictionary<int, decimal>()
            : (await _context.DepartureReturnItems.AsNoTracking()
                    .Where(r => deliveredItemIds.Contains(r.DepartureItemId))
                    .GroupBy(r => r.DepartureItemId)
                    .Select(g => new { g.Key, Total = g.Sum(r => r.ReturnedQuantity) })
                    .ToListAsync())
                .ToDictionary(x => x.Key, x => x.Total);
        ReturnedByArticle = deliveredItems
            .GroupBy(i => i.ArticleId ?? UnitArticle(i.EquipmentUnitId))
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!.Value, g => g.Sum(i => returnedByItem.GetValueOrDefault(i.Id)));

        var deliveredIds = quantities.Keys.ToList();
        var articleInfo = await _context.Articles.AsNoTracking()
            .Where(a => deliveredIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => new { a.Id, a.Code, a.Name });
        DeliveredByArticle = quantities;

        // Incidentes L-5 ya registrados por artículo: la cantidad afectada no puede superar lo pendiente.
        RegisteredByArticle = (await _context.L5Incidents.AsNoTracking()
                .Where(i => i.PlanningId == PlanId && i.ArticleId != null)
                .GroupBy(i => i.ArticleId!.Value)
                .Select(g => new { ArticleId = g.Key, Total = g.Sum(i => i.AffectedQuantity) })
                .ToListAsync())
            .ToDictionary(r => r.ArticleId, r => r.Total);

        ArticleOptions = articleInfo.Values.OrderBy(a => a.Name)
            .Select(a =>
            {
                var text = $"{a.Code} · {a.Name} (entregado {quantities[a.Id]:0.##})";
                if (ReturnedByArticle.TryGetValue(a.Id, out var devuelto) && devuelto > 0)
                    text += $" · devuelto: {devuelto:0.##}";
                if (RegisteredByArticle.TryGetValue(a.Id, out var conL5) && conL5 > 0)
                    text += $" · con incidentes: {conL5}";
                return new SelectListItem(text, a.Id.ToString(), a.Id == Input.ArticleId);
            })
            .ToList();

        // Unidades físicas indicadas en los requerimientos de la planificación.
        var units = await _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.PlanningId == PlanId && d.EquipmentUnitId != null)
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
