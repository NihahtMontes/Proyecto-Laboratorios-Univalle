using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Demanda agregada por (Laboratorio del plan, Equipo): suma ExpectedQuantity de
/// los detalles de planes Planificado/Aprobado con ScheduledDate >= hoy y compara
/// contra las unidades operativas del mismo tipo de equipo en ese laboratorio.
/// </summary>
public class DemandaModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DemandaModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = "todos";

    [BindProperty(SupportsGet = true)]
    public string Order { get; set; } = "mas";

    [BindProperty(SupportsGet = true)]
    public int? LaboratoryId { get; set; }

    public SelectList LaboratoryList { get; private set; } = new(Enumerable.Empty<SelectListItem>());
    public IReadOnlyList<DemandRow> Rows { get; private set; } = [];
    public IReadOnlyList<string> Categories { get; private set; } = [];
    public int TotalRequested { get; private set; }
    public int DeficitCount { get; private set; }
    public int SurplusCount { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;

        // Laboratorios activos: alimenta el filtro y la resolución de nombres.
        var laboratories = await _context.Laboratories.AsNoTracking()
            .Where(item => item.Status == GeneralStatus.Activo)
            .OrderBy(item => item.Name)
            .Select(item => new { item.Id, item.Code, item.Name })
            .ToListAsync();
        LaboratoryList = new SelectList(laboratories, "Id", "Name", LaboratoryId);
        var laboratoryNames = laboratories.ToDictionary(item => item.Id, item => $"{item.Code} · {item.Name}");

        // Demanda agregada por (Laboratorio del plan, Equipo). Una sola consulta.
        var demandRows = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => (d.L3Request!.Status == L3RequestStatus.Planned
                         || d.L3Request!.Status == L3RequestStatus.Approved)
                        && d.L3Request!.ScheduledDate >= today
                        && (!LaboratoryId.HasValue || d.L3Request!.LaboratoryId == LaboratoryId.Value))
            .GroupBy(d => new { LabId = d.L3Request!.LaboratoryId, EqId = d.EquipmentUnit.EquipmentId })
            .Select(g => new
            {
                LaboratoryId = g.Key.LabId,
                EquipmentId = g.Key.EqId,
                Requested = g.Sum(d => (int?)d.ExpectedQuantity) ?? 0,
                PlanCount = g.Count()
            })
            .ToListAsync();

        // Stock operativo por (Laboratorio, Equipo). Una sola consulta, acotada a los laboratorios relevantes.
        var relevantLabIds = LaboratoryId.HasValue
            ? new List<int> { LaboratoryId.Value }
            : demandRows.Select(row => row.LaboratoryId).Distinct().ToList();

        var stockMap = relevantLabIds.Count == 0
            ? new Dictionary<(int Lab, int Eq), int>()
            : (await _context.EquipmentUnits.AsNoTracking()
                .Where(u => u.LaboratoryId.HasValue
                            && relevantLabIds.Contains(u.LaboratoryId.Value)
                            && u.CurrentStatus == EquipmentStatus.Operational)
                .GroupBy(u => new { LabId = u.LaboratoryId!.Value, u.EquipmentId })
                .Select(g => new { g.Key.LabId, g.Key.EquipmentId, Count = g.Count() })
                .ToListAsync())
            .ToDictionary(x => (x.LabId, x.EquipmentId), x => x.Count);

        // Catálogo de equipos referenciados (un lote).
        var equipmentIds = demandRows.Select(row => row.EquipmentId).Distinct().ToList();
        var equipmentMap = equipmentIds.Count == 0
            ? new Dictionary<int, EquipmentProjection>()
            : (await _context.Equipments.AsNoTracking()
                .Where(item => equipmentIds.Contains(item.Id))
                .Select(item => new EquipmentProjection
                {
                    Id = item.Id,
                    Name = item.Name,
                    Category = item.Category,
                    CatalogCode = item.CatalogCode
                })
                .ToListAsync())
            .ToDictionary(item => item.Id);

        var source = demandRows.Select(d =>
        {
            equipmentMap.TryGetValue(d.EquipmentId, out var eq);
            var key = (d.LaboratoryId, d.EquipmentId);
            var available = stockMap.TryGetValue(key, out var s) ? s : 0;
            var labName = laboratoryNames.TryGetValue(d.LaboratoryId, out var lab)
                ? lab
                : $"Laboratorio #{d.LaboratoryId}";
            return new DemandRow
            {
                Code = eq?.CatalogCode ?? $"EQ-{d.EquipmentId:0000}",
                Name = eq?.Name ?? "Equipo sin nombre",
                Category = GetCategoryName(eq?.Category ?? EquipmentCategory.Equipment),
                LaboratoryId = d.LaboratoryId,
                LaboratoryName = labName,
                Requested = d.Requested,
                Available = available,
                PlanCount = d.PlanCount
            };
        }).ToList();

        Categories = source
            .Select(item => item.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToList();

        var category = (Category ?? "todos").Trim();
        var filtered = string.Equals(category, "todos", StringComparison.OrdinalIgnoreCase)
            ? source
            : source.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

        Rows = string.Equals(Order, "menos", StringComparison.OrdinalIgnoreCase)
            ? filtered.OrderBy(item => item.Requested).ThenBy(item => item.LaboratoryName).ThenBy(item => item.Name).ToList()
            : filtered.OrderByDescending(item => item.Requested).ThenBy(item => item.LaboratoryName).ThenBy(item => item.Name).ToList();

        TotalRequested = Rows.Sum(item => item.Requested);
        DeficitCount = Rows.Count(item => item.Balance < 0);
        SurplusCount = Rows.Count(item => item.Balance >= 0);
    }

    private static string GetCategoryName(EquipmentCategory category) => category switch
    {
        EquipmentCategory.Equipment => "Equipo",
        EquipmentCategory.Utensil => "Utensilio",
        EquipmentCategory.Other => "Otro",
        _ => category.ToString()
    };

    private sealed class EquipmentProjection
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public EquipmentCategory Category { get; init; }
        public string? CatalogCode { get; init; }
    }

    public sealed class DemandRow
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public int LaboratoryId { get; init; }
        public string LaboratoryName { get; init; } = string.Empty;
        public int Requested { get; init; }
        public int Available { get; init; }
        public int Balance => Available - Requested;
        public int PlanCount { get; init; }
    }
}
