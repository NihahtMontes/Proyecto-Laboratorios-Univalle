using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 3: compara lo solicitado con las unidades operativas del mismo tipo en el laboratorio del plan.
/// </summary>
public class DisponibilidadModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IPlanningService _planningService;
    private readonly ILogger<DisponibilidadModel> _logger;

    public DisponibilidadModel(ApplicationDbContext context, IPlanningService planningService, ILogger<DisponibilidadModel> logger)
    {
        _context = context;
        _planningService = planningService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    public L3Request? Plan { get; private set; }
    public string Code => L3Codes.Format(PlanId);
    public List<AvailabilityRow> Items { get; private set; } = [];
    public bool HasConflict => Items.Any(i => !i.Ok);
    public bool IsApproved => Plan != null && Plan.Status != L3RequestStatus.Planned;

    public sealed record AvailabilityRow(int DetailId, string EquipmentName, int Requested, int Available)
    {
        public int Difference => Available - Requested;
        public bool Ok => Difference >= 0;
    }

    /// <summary>Franja superior del wizard: planificación de la práctica y paso actual.</summary>
    public PlanningContextViewModel? Context { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Context = await PlanningContextLoader.ForPracticeAsync(_context, _planningService, PlanId, 3);
        Plan = await _context.L3Requests.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.Group)
            .Include(r => r.TeacherPerson)
            .FirstOrDefaultAsync(r => r.Id == PlanId);
        if (Plan == null)
        {
            TempData.Error("La planificación no existe.");
            return RedirectToPage("./Index");
        }

        Items = await BuildRowsAsync(Plan.Id, Plan.LaboratoryId);
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

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var plan = await _context.L3Requests.AsTracking()
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == PlanId);
            if (plan == null)
            {
                TempData.Error("La planificación no existe.");
                return RedirectToPage("./Index");
            }
            if (plan.Status != L3RequestStatus.Planned)
            {
                TempData.Warning("La planificación ya fue aprobada.");
                return RedirectToPage("./L3", new { id = PlanId });
            }

            // Revalidar en el servidor: la vista no es autoridad.
            var rows = await BuildRowsAsync(plan.Id, plan.LaboratoryId);
            if (rows.Count == 0 || rows.Any(r => !r.Ok))
            {
                await transaction.RollbackAsync();
                TempData.Error("Hay requerimientos sin stock suficiente en el laboratorio. Ajusta las cantidades.");
                return RedirectToPage("./Requerimientos", new { planId = PlanId, conflict = true });
            }

            foreach (var detail in plan.Details)
                detail.DeliveredQuantity = detail.ExpectedQuantity;
            plan.Status = L3RequestStatus.Approved;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "No se pudo aprobar la planificación L-3 {PlanId}", PlanId);
            TempData.Error("No se pudo aprobar la planificación. Intente nuevamente.");
            return RedirectToPage(new { planId = PlanId });
        }

        TempData.Success("Disponibilidad confirmada. Registra la entrega y devolución en L-3.");
        return RedirectToPage("./L3", new { id = PlanId });
    }

    private async Task<List<AvailabilityRow>> BuildRowsAsync(int planId, int laboratoryId)
    {
        var details = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => d.L3RequestId == planId)
            .Select(d => new { d.Id, d.EquipmentUnit.EquipmentId, Name = d.EquipmentUnit.Equipment!.Name, d.ExpectedQuantity })
            .ToListAsync();
        var equipmentIds = details.Select(d => d.EquipmentId).Distinct().ToList();

        var operational = await _context.EquipmentUnits.AsNoTracking()
            .Where(u => u.LaboratoryId == laboratoryId && equipmentIds.Contains(u.EquipmentId)
                && u.CurrentStatus == EquipmentStatus.Operational)
            .GroupBy(u => u.EquipmentId)
            .Select(g => new { EquipmentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EquipmentId, x => x.Count);

        return details
            .Select(d => new AvailabilityRow(d.Id, d.Name, d.ExpectedQuantity,
                operational.TryGetValue(d.EquipmentId, out var count) ? count : 0))
            .OrderBy(r => r.EquipmentName)
            .ToList();
    }
}
