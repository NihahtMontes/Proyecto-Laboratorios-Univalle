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

/// <summary>
/// Paso 4: salida L-3 por artículo (descuenta el stock del origen) y devoluciones parciales o totales (lo reponen).
/// Lo que no vuelve es faltante y solo permite finalizar si está cubierto por incidentes L-5.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ClosureRoles)]
public class L3Model : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planningService;
    private readonly IL3CircuitService _circuit;
    private readonly ILogger<L3Model> _logger;

    public L3Model(ApplicationDbContext context, IPlanningService planningService, IL3CircuitService circuit, ILogger<L3Model> logger)
    {
        _context = context;
        _planningService = planningService;
        _circuit = circuit;
        _logger = logger;
    }

    /// <summary>Id de L3Requests (se conserva el nombre de ruta de RamaDani).</summary>
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    public DepartureInput Departure { get; set; } = new();

    [BindProperty]
    public ReturnInput Return { get; set; } = new();

    public L3Request? Plan { get; private set; }
    public string Code => L3Codes.Format(Id);
    public DepartureView? CurrentDeparture { get; private set; }
    public List<DeliveryRow> DeliveryRows { get; private set; } = [];
    public List<ReturnRow> Items { get; private set; } = [];
    public List<ReturnView> Returns { get; private set; } = [];
    public List<IncidentRow> Incidents { get; private set; } = [];
    public SelectList OriginList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public SelectList ResponsibleList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public bool IsEditable => Plan?.Status == L3RequestStatus.Approved;
    public bool IsFinalized => Plan?.Status == L3RequestStatus.Finalized;
    public bool HasDeparture => CurrentDeparture != null;
    public bool CanReturn => IsEditable && HasDeparture && Items.Any(i => i.Pending > 0);
    public bool BlockFinalization => !HasDeparture || Items.Any(i => i.Uncovered > 0);

    public sealed record DeliveryRow(int DetailId, string ArticleCode, string ArticleName, bool ArticlePending, string? Unit,
        decimal Requested, decimal? Available, string? InventoryNumber);
    public sealed record DepartureView(int Id, string Code, string OriginName, DateTime DepartureDate, LoanStatus Status);
    public sealed record ReturnRow(int DepartureItemId, int ArticleId, string ArticleCode, string ArticleName, string? Unit,
        decimal Delivered, decimal Returned, int CoveredByIncidents)
    {
        public decimal Pending => Math.Max(0, Delivered - Returned);
        public decimal Uncovered => Math.Max(0, Pending - CoveredByIncidents);
    }
    public sealed record ReturnView(string Code, DateTime? ReturnDate, string ResponsibleName, DepartureReturnStatus Status, string Lines);
    public sealed record IncidentRow(int Id, string Target, string IncidentType, int AffectedQuantity, string ResponsibleName,
        string RepositionStatus, DateTime RecordDate);

    /// <summary>Franja superior del wizard: planificación de la práctica y paso actual.</summary>
    public PlanningContextViewModel? Context { get; private set; }

    /// <summary>El rol de cierre solo opera prácticas del día o con devoluciones pendientes (GET y POST).</summary>
    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var denied = await PracticeClosureAccess.DenyReasonAsync(_context, User, Id);
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
        Context = await PlanningContextLoader.ForPracticeAsync(_context, _planningService, Id, 4);
        var redirect = await LoadAsync();
        return redirect ?? Page();
    }

    public async Task<IActionResult> OnPostDepartureAsync()
    {
        if (!await PlanningContextLoader.IsWritableAsync(_context, Id))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return RedirectToPage(new { id = Id });
        }

        var lines = Departure.Lines.Select(l => new DepartureLine(l.DetailId, l.Quantity ?? 0)).ToList();
        var result = await _circuit.CreateDepartureAsync(Id, Departure.OriginLaboratoryId, lines, Departure.Observations);
        Report(result);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostReturnAsync()
    {
        if (!await PlanningContextLoader.IsWritableAsync(_context, Id))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return RedirectToPage(new { id = Id });
        }

        var departureId = await _context.Departures.AsNoTracking()
            .Where(d => d.L3RequestId == Id).Select(d => (int?)d.Id).FirstOrDefaultAsync();
        if (departureId == null)
        {
            TempData.Error("Primero registra la salida de los artículos.");
            return RedirectToPage(new { id = Id });
        }

        var lines = Return.Lines.Select(l => new ReturnLine(l.DepartureItemId, l.Quantity ?? 0)).ToList();
        var result = await _circuit.RegisterReturnAsync(departureId.Value, Return.ResponsiblePersonId,
            Return.ReturnDate ?? DateTime.Today, lines, Return.Observations);
        Report(result);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostFinalizeAsync()
    {
        if (!await PlanningContextLoader.IsWritableAsync(_context, Id))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return RedirectToPage(new { id = Id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var plan = await _context.L3Requests.AsTracking().FirstOrDefaultAsync(r => r.Id == Id);
            if (plan == null || plan.Status != L3RequestStatus.Approved)
            {
                TempData.Error("La solicitud no está en condiciones de finalizarse.");
                return RedirectToPage(new { id = Id });
            }

            var departure = await _context.Departures.AsTracking().FirstOrDefaultAsync(d => d.L3RequestId == Id);
            if (departure == null)
            {
                TempData.Error("No se puede finalizar sin registrar la salida de los artículos.");
                return RedirectToPage(new { id = Id });
            }

            var rows = await BuildReturnRowsAsync(departure.Id);
            if (rows.Any(r => r.Uncovered > 0))
            {
                TempData.Error("Hay faltantes sin incidente L-5 registrado. Regístralos antes de finalizar.");
                return RedirectToPage(new { id = Id });
            }

            // Lo que no volvió quedó cubierto por L-5: la salida se cierra.
            if (departure.Status == LoanStatus.Active || departure.Status == LoanStatus.Overdue)
            {
                departure.Status = LoanStatus.Returned;
                departure.ActualReturnDate ??= DateTime.UtcNow;
            }
            plan.Status = L3RequestStatus.Finalized;
            if (plan.PracticeScheduleId.HasValue)
            {
                var schedule = await _context.PracticeSchedules.AsTracking().FirstOrDefaultAsync(s => s.Id == plan.PracticeScheduleId);
                if (schedule != null) schedule.Status = PracticeScheduleStatus.Executed;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo finalizar la solicitud L-3 {PlanId}", Id);
            TempData.Error("No se pudo finalizar L-3. Intente nuevamente.");
            return RedirectToPage(new { id = Id });
        }

        TempData.Success($"Práctica {Code} finalizada.");
        return RedirectToHome();
    }

    private void Report(L3CircuitResult result)
    {
        if (result.Ok) TempData.Success(result.Message);
        else TempData.Error(result.Message);
        if (result.Warnings is { Count: > 0 })
            TempData.Warning(string.Join(" ", result.Warnings.Take(5)));
    }

    private async Task<IActionResult?> LoadAsync()
    {
        Plan = await _context.L3Requests.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.TeacherPerson)
            .FirstOrDefaultAsync(r => r.Id == Id);
        if (Plan == null)
        {
            TempData.Error("La práctica no existe.");
            return RedirectToHome();
        }
        if (Plan.Status == L3RequestStatus.Planned)
        {
            TempData.Warning("Primero aprueba la solicitud en Disponibilidad.");
            return RedirectToPage("./Disponibilidad", new { planId = Id });
        }

        var departure = await _context.Departures.AsNoTracking()
            .Include(d => d.OriginLaboratory)
            .FirstOrDefaultAsync(d => d.L3RequestId == Id);

        if (departure == null)
        {
            var originId = await _circuit.GetDefaultOriginLaboratoryIdAsync();
            Departure.OriginLaboratoryId = originId ?? 0;
            var labs = await _context.Laboratories.AsNoTracking()
                .Where(l => l.Status == GeneralStatus.Activo).OrderBy(l => l.Code)
                .Select(l => new { l.Id, Name = l.Code + " · " + l.Name }).ToListAsync();
            OriginList = new SelectList(labs, "Id", "Name", originId);

            var details = await _context.L3RequestDetails.AsNoTracking()
                .Where(d => d.L3RequestId == Id)
                .OrderBy(d => d.Article!.Name)
                .Select(d => new
                {
                    d.Id, d.ArticleId, d.Article!.Code, d.Article.Name,
                    Pending = d.Article.ReviewStatus == DataReviewStatus.PorConfirmar,
                    Unit = d.Article.MeasurementUnit != null ? d.Article.MeasurementUnit.Name : d.Article.UnitOfMeasure,
                    Quantity = d.RequestedQuantity ?? 0,
                    Inventory = d.EquipmentUnit != null ? d.EquipmentUnit.InventoryNumber : null
                })
                .ToListAsync();
            var articleIds = details.Select(d => d.ArticleId).ToList();
            var stock = originId.HasValue
                ? await _context.ArticleStocks.AsNoTracking()
                    .Where(s => s.LaboratoryId == originId && articleIds.Contains(s.ArticleId))
                    .ToDictionaryAsync(s => s.ArticleId, s => s.AvailableQuantity)
                : new Dictionary<int, decimal>();
            DeliveryRows = details
                .Select(d => new DeliveryRow(d.Id, d.Code, d.Name, d.Pending, d.Unit, d.Quantity,
                    stock.TryGetValue(d.ArticleId, out var available) ? available : null, d.Inventory))
                .ToList();
            Departure.Lines = DeliveryRows.Select(r => new DepartureInput.Line { DetailId = r.DetailId, Quantity = r.Requested }).ToList();
        }
        else
        {
            CurrentDeparture = new DepartureView(departure.Id, departure.Code ?? $"SAL-{departure.Id:00000}",
                departure.OriginLaboratory != null ? $"{departure.OriginLaboratory.Code} · {departure.OriginLaboratory.Name}" : "Sin origen",
                departure.DepartureDate, departure.Status);
            Items = await BuildReturnRowsAsync(departure.Id);
            Return.Lines = Items.Where(i => i.Pending > 0)
                .Select(i => new ReturnInput.Line { DepartureItemId = i.DepartureItemId, Quantity = i.Pending }).ToList();
            Return.ResponsiblePersonId = Plan.TeacherPersonId;
            Return.ReturnDate = DateTime.Today;
            var people = await _context.Interns.AsNoTracking()
                .Where(i => i.Status == GeneralStatus.Activo && i.InternStatus == GeneralStatus.Activo)
                .OrderBy(i => i.Name).Select(i => new { i.Id, i.Name }).ToListAsync();
            ResponsibleList = new SelectList(people, "Id", "Name", Plan.TeacherPersonId);

            var returns = await _context.DepartureReturns.AsNoTracking()
                .Where(r => r.DepartureId == departure.Id)
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Code, r.ReturnDate, r.Status, r.ResponsiblePersonId,
                    Lines = r.Items.Select(i => new { i.DepartureItem!.ProductName, i.ReturnedQuantity }).ToList()
                })
                .ToListAsync();
            var personIds = returns.Select(r => r.ResponsiblePersonId).Distinct().ToList();
            var names = await _context.Interns.AsNoTracking().Where(i => personIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Name }).ToDictionaryAsync(i => i.Id, i => i.Name);
            foreach (var ext in await _context.Externs.AsNoTracking().Where(e => personIds.Contains(e.Id)).Select(e => new { e.Id, e.Name }).ToListAsync())
                names.TryAdd(ext.Id, ext.Name);
            Returns = returns.Select(r => new ReturnView(r.Code, r.ReturnDate, names.GetValueOrDefault(r.ResponsiblePersonId, ""), r.Status,
                string.Join(", ", r.Lines.Select(l => $"{l.ProductName} × {l.ReturnedQuantity:0.##}")))).ToList();
        }

        Incidents = await _context.L5Incidents.AsNoTracking()
            .Where(i => i.L3RequestId == Id)
            .OrderByDescending(i => i.RecordDate)
            .Select(i => new IncidentRow(i.Id,
                i.Article != null ? i.Article.Code + " · " + i.Article.Name
                    : i.EquipmentUnit != null ? i.EquipmentUnit.InventoryNumber + " · " + (i.EquipmentUnit.Equipment != null ? i.EquipmentUnit.Equipment.Name : "") : "",
                i.IncidentType, i.AffectedQuantity, i.ResponsibleName, i.RepositionStatus, i.RecordDate))
            .ToListAsync();
        return null;
    }

    private async Task<List<ReturnRow>> BuildReturnRowsAsync(int departureId)
    {
        var items = await _context.DepartureItems.AsNoTracking()
            .Where(i => i.DepartureId == departureId && i.ArticleId != null)
            .Select(i => new
            {
                i.Id, ArticleId = i.ArticleId!.Value, i.Article!.Code, i.Article.Name,
                Unit = i.Article.MeasurementUnit != null ? i.Article.MeasurementUnit.Name : i.Article.UnitOfMeasure,
                Delivered = i.Quantity ?? 0
            })
            .ToListAsync();
        var itemIds = items.Select(i => i.Id).ToList();
        var returned = await _context.DepartureReturnItems.AsNoTracking()
            .Where(r => itemIds.Contains(r.DepartureItemId))
            .GroupBy(r => r.DepartureItemId)
            .Select(g => new { g.Key, Total = g.Sum(r => r.ReturnedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Total);
        // Faltantes cubiertos por incidentes L-5 del artículo en esta práctica.
        var covered = await _context.L5Incidents.AsNoTracking()
            .Where(i => i.L3RequestId == Id && i.ArticleId != null)
            .GroupBy(i => i.ArticleId!.Value)
            .Select(g => new { g.Key, Quantity = g.Sum(i => i.AffectedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity);

        // Si un artículo aparece en varias líneas, su cobertura L-5 se reparte en orden.
        var remaining = new Dictionary<int, int>(covered);
        var rows = new List<ReturnRow>();
        foreach (var i in items.OrderBy(i => i.Name))
        {
            var back = returned.GetValueOrDefault(i.Id);
            var pending = Math.Max(0, i.Delivered - back);
            var available = remaining.GetValueOrDefault(i.ArticleId);
            var use = (int)Math.Min(available, Math.Ceiling(pending));
            remaining[i.ArticleId] = available - use;
            rows.Add(new ReturnRow(i.Id, i.ArticleId, i.Code, i.Name, i.Unit, i.Delivered, back, use));
        }
        return rows;
    }

    public sealed class DepartureInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona el laboratorio de origen")]
        public int OriginLaboratoryId { get; set; }

        [StringLength(500)]
        public string? Observations { get; set; }

        public List<Line> Lines { get; set; } = [];

        public sealed class Line
        {
            public int DetailId { get; set; }

            [Range(0, 999999)]
            public decimal? Quantity { get; set; }
        }
    }

    public sealed class ReturnInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona el responsable de la devolución")]
        public int ResponsiblePersonId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ReturnDate { get; set; }

        [StringLength(1000)]
        public string? Observations { get; set; }

        public List<Line> Lines { get; set; } = [];

        public sealed class Line
        {
            public int DepartureItemId { get; set; }

            [Range(0, 999999)]
            public decimal? Quantity { get; set; }
        }
    }
}
