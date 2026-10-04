using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 3: compara lo solicitado por artículo con el stock del laboratorio de origen (Economato por defecto).
/// La solicitud solo advierte (D2): se puede aprobar con faltantes; la salida es la que bloquea si no alcanza.
/// </summary>
public class DisponibilidadModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planningService;
    private readonly IL3CircuitService _circuit;
    private readonly ILogger<DisponibilidadModel> _logger;

    public DisponibilidadModel(ApplicationDbContext context, IPlanningService planningService, IL3CircuitService circuit,
        ILogger<DisponibilidadModel> logger)
    {
        _context = context;
        _planningService = planningService;
        _circuit = circuit;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    /// <summary>Laboratorio de origen contra el que se compara el stock (por defecto ECONO).</summary>
    [BindProperty(SupportsGet = true)]
    public int? OriginLabId { get; set; }

    public L3Request? Plan { get; private set; }
    public string Code => L3Codes.Format(PlanId);
    public List<AvailabilityRow> Items { get; private set; } = [];
    public SelectList OriginList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public string? OriginName { get; private set; }
    public bool HasShortage => Items.Any(i => i.State == AvailabilityState.Insufficient);
    public bool HasUnregistered => Items.Any(i => i.State == AvailabilityState.NotRegistered);
    public bool IsApproved => Plan != null && Plan.Status != L3RequestStatus.Planned;

    public enum AvailabilityState { Ok, Insufficient, NotRegistered }

    public sealed record AvailabilityRow(int DetailId, string ArticleCode, string ArticleName, bool ArticlePending, string? Unit,
        decimal Requested, decimal? Available, string? InventoryNumber)
    {
        public decimal? Difference => Available - Requested;
        public AvailabilityState State => Available is null ? AvailabilityState.NotRegistered
            : Available >= Requested ? AvailabilityState.Ok : AvailabilityState.Insufficient;
    }

    /// <summary>Franja superior del wizard: planificación de la práctica y paso actual.</summary>
    public PlanningContextViewModel? Context { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Context = await PlanningContextLoader.ForPracticeAsync(_context, _planningService, PlanId, 3);
        Plan = await _context.L3Requests.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.TeacherPerson)
            .FirstOrDefaultAsync(r => r.Id == PlanId);
        if (Plan == null)
        {
            TempData.Error("La práctica no existe.");
            return RedirectToPage("./Index");
        }

        OriginLabId ??= await _circuit.GetDefaultOriginLaboratoryIdAsync();
        var labs = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo)
            .OrderBy(l => l.Code)
            .Select(l => new { l.Id, Name = l.Code + " · " + l.Name })
            .ToListAsync();
        OriginList = new SelectList(labs, "Id", "Name", OriginLabId);
        OriginName = labs.FirstOrDefault(l => l.Id == OriginLabId)?.Name;

        Items = await BuildRowsAsync(Plan.Id, OriginLabId);
        if (Items.Count == 0)
        {
            TempData.Error("Agrega al menos un requerimiento antes de validar disponibilidad.");
            return RedirectToPage("./Requerimientos", new { planId = PlanId });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        if (!await PlanningContextLoader.IsWritableAsync(_context, PlanId))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return RedirectToPage(new { planId = PlanId });
        }

        try
        {
            var plan = await _context.L3Requests.AsTracking().FirstOrDefaultAsync(r => r.Id == PlanId);
            if (plan == null)
            {
                TempData.Error("La práctica no existe.");
                return RedirectToPage("./Index");
            }
            if (plan.Status != L3RequestStatus.Planned)
            {
                TempData.Warning("La solicitud ya fue aprobada.");
                return RedirectToPage("./L3", new { id = PlanId });
            }

            var details = await _context.L3RequestDetails.AsNoTracking().Where(d => d.L3RequestId == plan.Id).ToListAsync();
            if (details.Count == 0 || details.Any(d => d.RequestedQuantity is not > 0))
            {
                TempData.Error("Todos los requerimientos deben tener una cantidad mayor a 0.");
                return RedirectToPage("./Requerimientos", new { planId = PlanId, conflict = true });
            }

            // La aprobación no exige stock (D2): solo se informa. La salida valida y descuenta.
            var rows = await BuildRowsAsync(plan.Id, OriginLabId ?? await _circuit.GetDefaultOriginLaboratoryIdAsync());
            plan.Status = L3RequestStatus.Approved;
            await _context.SaveChangesAsync();

            var shortages = rows.Count(r => r.State == AvailabilityState.Insufficient);
            var unregistered = rows.Count(r => r.State == AvailabilityState.NotRegistered);
            TempData.Success("Solicitud aprobada. Registra la salida de los artículos en L-3.");
            if (shortages > 0 || unregistered > 0)
                TempData.Warning($"Atención: {shortages} artículo(s) sin stock suficiente y {unregistered} sin registro de stock en el origen.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo aprobar la solicitud L-3 {PlanId}", PlanId);
            TempData.Error("No se pudo aprobar la solicitud. Intente nuevamente.");
            return RedirectToPage(new { planId = PlanId, originLabId = OriginLabId });
        }

        return RedirectToPage("./L3", new { id = PlanId });
    }

    private async Task<List<AvailabilityRow>> BuildRowsAsync(int planId, int? originLabId)
    {
        var details = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => d.L3RequestId == planId)
            .Select(d => new
            {
                d.Id, d.ArticleId, d.Article!.Code, d.Article.Name,
                Pending = d.Article.ReviewStatus == DataReviewStatus.PorConfirmar,
                Unit = d.Article.MeasurementUnit != null ? d.Article.MeasurementUnit.Name : d.Article.UnitOfMeasure,
                Quantity = d.RequestedQuantity ?? 0,
                Inventory = d.EquipmentUnit != null ? d.EquipmentUnit.InventoryNumber : null
            })
            .ToListAsync();
        var articleIds = details.Select(d => d.ArticleId).Distinct().ToList();
        var stock = originLabId.HasValue
            ? await _context.ArticleStocks.AsNoTracking()
                .Where(s => s.LaboratoryId == originLabId && articleIds.Contains(s.ArticleId))
                .ToDictionaryAsync(s => s.ArticleId, s => s.AvailableQuantity)
            : new Dictionary<int, decimal>();

        // Varias líneas del mismo artículo comparten el stock: una fila por artículo con el total pedido.
        return details
            .GroupBy(d => d.ArticleId)
            .Select(g =>
            {
                var first = g.First();
                var units = string.Join(", ", g.Select(d => d.Inventory).Where(i => i != null));
                return new AvailabilityRow(first.Id, first.Code, first.Name, first.Pending, first.Unit, g.Sum(d => d.Quantity),
                    stock.TryGetValue(g.Key, out var available) ? available : null, units.Length > 0 ? units : null);
            })
            .OrderBy(r => r.ArticleName)
            .ToList();
    }
}
