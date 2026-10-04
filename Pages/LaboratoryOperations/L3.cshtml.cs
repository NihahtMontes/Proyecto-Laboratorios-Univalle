using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 4: entrega y devolución L-3. Un faltante solo permite finalizar si está cubierto por incidentes L-5.
/// </summary>
public class L3Model : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planningService;
    private readonly ILogger<L3Model> _logger;

    public L3Model(ApplicationDbContext context, IPlanningService planningService, ILogger<L3Model> logger)
    {
        _context = context;
        _planningService = planningService;
        _logger = logger;
    }

    /// <summary>Id de L3Requests (se conserva el nombre de ruta de RamaDani).</summary>
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public L3Request? Plan { get; private set; }
    public string Code => L3Codes.Format(Id);
    public List<ReturnRow> Items { get; private set; } = [];
    public List<IncidentRow> Incidents { get; private set; } = [];
    public bool IsEditable => Plan?.Status == L3RequestStatus.Approved;
    public bool IsFinalized => Plan?.Status == L3RequestStatus.Finalized;
    public bool BlockFinalization => Items.Any(i => i.Uncovered > 0);

    public sealed record ReturnRow(int DetailId, int EquipmentUnitId, int EquipmentId, string EquipmentName,
        int Delivered, int Returned, int CoveredByIncidents)
    {
        public int Missing => Math.Max(0, Delivered - Returned);
        public int Uncovered => Math.Max(0, Missing - CoveredByIncidents);
    }

    public sealed record IncidentRow(int Id, string EquipmentName, string InventoryNumber, string IncidentType,
        int AffectedQuantity, string ResponsibleName, string RepositionStatus, DateTime RecordDate);

    /// <summary>Franja superior del wizard: planificación de la práctica y paso actual.</summary>
    public PlanningContextViewModel? Context { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Context = await PlanningContextLoader.ForPracticeAsync(_context, _planningService, Id, 4);
        Plan = await _context.L3Requests.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.Group)
            .Include(r => r.TeacherPerson)
            .FirstOrDefaultAsync(r => r.Id == Id);
        if (Plan == null)
        {
            TempData.Error("La planificación no existe.");
            return RedirectToPage("./Index");
        }
        if (Plan.Status == L3RequestStatus.Planned)
        {
            TempData.Warning("Primero confirma la disponibilidad de la planificación.");
            return RedirectToPage("./Disponibilidad", new { planId = Id });
        }

        Items = await BuildRowsAsync(Id);
        Incidents = await _context.L5Incidents.AsNoTracking()
            .Where(i => i.L3RequestId == Id)
            .OrderByDescending(i => i.RecordDate)
            .Select(i => new IncidentRow(i.Id, i.EquipmentUnit.Equipment!.Name, i.EquipmentUnit.InventoryNumber,
                i.IncidentType, i.AffectedQuantity, i.ResponsibleName, i.RepositionStatus, i.RecordDate))
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateReturnAsync(int detailId, int returnedQuantity)
    {
        if (!await PlanningContextLoader.IsWritableAsync(_context, Id))
        {
            TempData.Error(PlanningContextLoader.ReadOnlyMessage);
            return RedirectToPage(new { id = Id });
        }

        var plan = await _context.L3Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == Id);
        if (plan == null || plan.Status != L3RequestStatus.Approved)
        {
            TempData.Error("Solo se registran devoluciones de una planificación aprobada y no finalizada.");
            return RedirectToPage(new { id = Id });
        }

        var detail = await _context.L3RequestDetails.AsTracking()
            .FirstOrDefaultAsync(d => d.Id == detailId && d.L3RequestId == Id);
        if (detail == null)
        {
            TempData.Error("El ítem no pertenece a esta planificación.");
            return RedirectToPage(new { id = Id });
        }
        if (returnedQuantity < 0 || returnedQuantity > detail.DeliveredQuantity)
        {
            TempData.Error($"La cantidad devuelta debe estar entre 0 y {detail.DeliveredQuantity}.");
            return RedirectToPage(new { id = Id });
        }

        detail.ReturnedQuantity = returnedQuantity;
        await _context.SaveChangesAsync();
        TempData.Success("Devolución registrada.");
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
                TempData.Error("La planificación no está en condiciones de finalizarse.");
                return RedirectToPage(new { id = Id });
            }

            var rows = await BuildRowsAsync(Id);
            if (rows.Any(r => r.Uncovered > 0))
            {
                TempData.Error("Hay faltantes sin incidente L-5 registrado. Regístralos antes de finalizar.");
                return RedirectToPage(new { id = Id });
            }

            plan.Status = L3RequestStatus.Finalized;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo finalizar la planificación L-3 {PlanId}", Id);
            TempData.Error("No se pudo finalizar L-3. Intente nuevamente.");
            return RedirectToPage(new { id = Id });
        }

        TempData.Success($"Planificación {Code} finalizada.");
        return RedirectToPage("./Index");
    }

    private async Task<List<ReturnRow>> BuildRowsAsync(int planId)
    {
        var details = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => d.L3RequestId == planId)
            .Select(d => new { d.Id, d.EquipmentUnitId, d.EquipmentUnit.EquipmentId, Name = d.EquipmentUnit.Equipment!.Name,
                d.DeliveredQuantity, d.ReturnedQuantity })
            .ToListAsync();

        // Incidentes del plan agrupados por tipo de equipo (el L-5 registra la unidad concreta afectada).
        var covered = await _context.L5Incidents.AsNoTracking()
            .Where(i => i.L3RequestId == planId)
            .GroupBy(i => i.EquipmentUnit.EquipmentId)
            .Select(g => new { EquipmentId = g.Key, Quantity = g.Sum(i => i.AffectedQuantity) })
            .ToDictionaryAsync(x => x.EquipmentId, x => x.Quantity);

        return details
            .Select(d => new ReturnRow(d.Id, d.EquipmentUnitId, d.EquipmentId, d.Name, d.DeliveredQuantity, d.ReturnedQuantity,
                covered.TryGetValue(d.EquipmentId, out var q) ? q : 0))
            .OrderBy(r => r.EquipmentName)
            .ToList();
    }
}
