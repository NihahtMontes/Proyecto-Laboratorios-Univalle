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
/// Paso 3 (último del wizard): compara lo solicitado con el stock (existencias del origen o unidades físicas
/// del equipo vinculado) y muestra la semana académica por días con los conflictos y rotaciones entre
/// planificaciones. Solo advierte (D2): se puede aprobar con faltantes si la persona confirma que está consciente.
/// </summary>
public class DisponibilidadModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IAcademicManagementService _managements;
    private readonly IL3CircuitService _circuit;
    private readonly IArticleAvailabilityService _availability;
    private readonly ILogger<DisponibilidadModel> _logger;

    private readonly IPlanningLock _lock;

    public DisponibilidadModel(ApplicationDbContext context, IAcademicManagementService managements, IL3CircuitService circuit,
        IArticleAvailabilityService availability, ILogger<DisponibilidadModel> logger, IPlanningLock planningLock)
    {
        _lock = planningLock;
        _context = context;
        _managements = managements;
        _circuit = circuit;
        _availability = availability;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    /// <summary>Laboratorio de origen contra el que se compara el stock (por defecto ECONO).</summary>
    [BindProperty(SupportsGet = true)]
    public int? OriginLabId { get; set; }

    public Planning? Plan { get; private set; }
    public string Code => L3Codes.Format(PlanId);
    public List<AvailabilityRow> Items { get; private set; } = [];
    public SelectList OriginList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public string? OriginName { get; private set; }
    public bool HasShortage => Items.Any(i => i.State == AvailabilityState.Insufficient);
    public bool HasUnregistered => Items.Any(i => i.State == AvailabilityState.NotRegistered);
    public bool IsApproved => Plan != null && Plan.Status != PlanningStatus.Planned;

    /// <summary>Semana académica de la planificación con la demanda de todas las sesiones por día.</summary>
    public WeekAvailability? Week { get; private set; }

    /// <summary>Conflictos y rotaciones de la semana en los que participa esta planificación.</summary>
    public List<ArticleDayDemand> PracticeIssues { get; private set; } = [];

    /// <summary>La persona confirma que conoce los conflictos/rotaciones y aprueba igual.</summary>
    [BindProperty]
    public bool Acknowledged { get; set; }

    public enum AvailabilityState { Ok, Insufficient, NotRegistered }

    public sealed record AvailabilityRow(int DetailId, string ArticleCode, string ArticleName, bool ArticlePending, string? Unit,
        decimal Requested, ArticleStockInfo Stock, string? InventoryNumber)
    {
        public decimal? Available => Stock.Available;
        public decimal? Difference => Available - Requested;
        public AvailabilityState State => Available is null ? AvailabilityState.NotRegistered
            : Available >= Requested ? AvailabilityState.Ok : AvailabilityState.Insufficient;
    }

    /// <summary>Franja superior del wizard: gestión de la planificación y paso actual.</summary>
    public PlanningContextViewModel? Context { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Context = await PlanningContextLoader.ForPlanningAsync(_context, _managements, PlanId, 3);
        Plan = await _context.Plannings.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.TeacherPerson)
            .FirstOrDefaultAsync(r => r.Id == PlanId);
        if (Plan == null)
        {
            TempData.Error("La planificación no existe.");
            return RedirectToPage("./Index");
        }

        if (OriginLabId is 0) OriginLabId = null; // null = total institucional (D12)
        var labs = await _context.Laboratories.AsNoTracking()
            .Where(l => l.Status == GeneralStatus.Activo)
            .OrderBy(l => l.Code)
            .Select(l => new { l.Id, Name = l.Code + " · " + l.Name })
            .ToListAsync();
        OriginList = new SelectList(labs, "Id", "Name", OriginLabId);
        OriginName = OriginLabId.HasValue ? labs.FirstOrDefault(l => l.Id == OriginLabId)?.Name : "Total institucional (Economato + laboratorios)";

        Items = await BuildRowsAsync(Plan.Id, OriginLabId);
        if (Items.Count == 0)
        {
            TempData.Error("Agrega al menos un requerimiento antes de validar disponibilidad.");
            return RedirectToPage("./Requerimientos", new { planId = PlanId });
        }

        // La semana compara siempre contra el total: el material se mueve entre laboratorios (D12).
        Week = await _availability.GetWeekForPracticeAsync(Plan.Id);
        PracticeIssues = Week?.Issues.Where(d => d.Practices.Any(p => p.PracticeId == Plan.Id)).ToList() ?? [];

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        if (!await PlanningContextLoader.IsWritableAsync(_managements, PlanId))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return RedirectToPage(new { planId = PlanId });
        }

        // Mismo bloqueo que la reprogramación: aprobar no puede pisar un origen que se está reubicando.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (!await _lock.TryAcquireAsync([PlanningLockResources.Planning(PlanId)]))
            {
                TempData.Error(PlanningLockResources.BusyMessage + ".");
                return RedirectToPage(new { planId = PlanId });
            }
            var plan = await _context.Plannings.AsTracking().FirstOrDefaultAsync(r => r.Id == PlanId);
            if (plan == null)
            {
                TempData.Error("La planificación no existe.");
                return RedirectToPage("./Index");
            }
            if (plan.Status != PlanningStatus.Planned)
            {
                TempData.Warning(plan.Status == PlanningStatus.Approved
                    ? "Esta planificación ya fue aprobada."
                    : $"La planificación está {PlanningStatus.Label(plan.Status)}: no se puede aprobar.");
                return RedirectToPage(new { planId = PlanId });
            }

            var details = await _context.PlanningRequirements.AsNoTracking().Where(d => d.PlanningId == plan.Id).ToListAsync();
            if (details.Count == 0 || details.Any(d => d.RequestedQuantity is not > 0))
            {
                TempData.Error("Todos los requerimientos deben tener una cantidad mayor a 0.");
                return RedirectToPage("./Requerimientos", new { planId = PlanId, conflict = true });
            }

            // No se aprueba lo que no tiene stock registrado (o en 0): se vuelve a Requerimientos para registrarlo.
            // Un stock insuficiente (pero existente) solo se informa: la salida valida y descuenta.
            var missing = await PlanningStockGate.FindAsync(_context, _availability, plan.Id);
            if (missing.Count > 0)
            {
                var names = string.Join(", ", missing.Take(5).Select(m => m.ArticleName)) + (missing.Count > 5 ? "…" : "");
                TempData.Error($"No se puede aprobar: hay artículos sin stock ({names}). Registre su existencia primero.");
                return RedirectToPage("./Requerimientos", new { planId = PlanId });
            }

            var originId = OriginLabId is 0 ? null : OriginLabId;
            var rows = await BuildRowsAsync(plan.Id, originId);
            var week = await _availability.GetWeekForPracticeAsync(plan.Id);
            var issues = week?.Issues.Count(d => d.Practices.Any(p => p.PracticeId == plan.Id)) ?? 0;
            var shortages = rows.Count(r => r.State == AvailabilityState.Insufficient);
            var unregistered = rows.Count(r => r.State == AvailabilityState.NotRegistered);
            if ((issues > 0 || shortages > 0) && !Acknowledged)
            {
                TempData.Warning("Confirma que conoces los faltantes, conflictos o rotaciones de la semana antes de aprobar.");
                return RedirectToPage(new { planId = PlanId, originLabId = OriginLabId });
            }

            plan.Status = PlanningStatus.Approved;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData.Success("Planificación aprobada.");
            if (shortages > 0 || unregistered > 0 || issues > 0)
                TempData.Warning($"Atención: {shortages} artículo(s) sin stock suficiente, {unregistered} sin stock registrado y {issues} conflicto(s)/rotación(es) en la semana.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo aprobar la solicitud L-3 {PlanId}", PlanId);
            TempData.Error("No se pudo aprobar la solicitud. Intente nuevamente.");
            return RedirectToPage(new { planId = PlanId, originLabId = OriginLabId });
        }

        return RedirectToPage(new { planId = PlanId, originLabId = OriginLabId });
    }

    private async Task<List<AvailabilityRow>> BuildRowsAsync(int planId, int? originLabId)
    {
        var details = await _context.PlanningRequirements.AsNoTracking()
            .Where(d => d.PlanningId == planId)
            .Select(d => new
            {
                d.Id, d.ArticleId, d.Article!.Code, d.Article.Name,
                Pending = d.Article.ReviewStatus == DataReviewStatus.PorConfirmar,
                Unit = d.Article.MeasurementUnit != null ? d.Article.MeasurementUnit.Name : d.Article.UnitOfMeasure,
                Quantity = d.RequestedQuantity ?? 0,
                Inventory = d.EquipmentUnit != null ? d.EquipmentUnit.InventoryNumber : null
            })
            .ToListAsync();
        var stock = await _availability.GetStockAsync(details.Select(d => d.ArticleId), originLabId);

        // Varias líneas del mismo artículo comparten el stock: una fila por artículo con el total pedido.
        return details
            .GroupBy(d => d.ArticleId)
            .Select(g =>
            {
                var first = g.First();
                var units = string.Join(", ", g.Select(d => d.Inventory).Where(i => i != null));
                return new AvailabilityRow(first.Id, first.Code, first.Name, first.Pending, first.Unit, g.Sum(d => d.Quantity),
                    stock.TryGetValue(g.Key, out var info) ? info : ArticleStockInfo.NotRegistered(g.Key), units.Length > 0 ? units : null);
            })
            .OrderBy(r => r.ArticleName)
            .ToList();
    }
}
