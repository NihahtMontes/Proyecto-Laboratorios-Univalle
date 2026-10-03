using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Paso 5: incidente L-5 sobre una unidad concreta de un tipo de equipo prestado en la planificación.
/// El historial y el estado de la unidad los actualiza ApplicationDbContext al guardar.
/// </summary>
public class L5Model : PageModel
{
    public static readonly string[] IncidentTypes = ["Daño", "Rotura", "Pérdida", "Faltante", "Otro"];
    public static readonly string[] ResponsibleTypes = ["ESTUDIANTE", "DOCENTE"];

    private readonly ApplicationDbContext _context;

    public L5Model(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public int PlanId { get; set; }

    /// <summary>Tipo de equipo preseleccionado desde L-3 (opcional).</summary>
    [BindProperty(SupportsGet = true)]
    public int? EquipmentId { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public L3Request? Plan { get; private set; }
    public string Code => L3Codes.Format(PlanId);
    public List<SelectListItem> UnitOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var redirect = await LoadAsync();
        if (redirect != null) return redirect;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var redirect = await LoadAsync();
        if (redirect != null) return redirect;

        var allowedUnitIds = UnitOptions.Select(o => int.Parse(o.Value)).ToHashSet();
        if (!allowedUnitIds.Contains(Input.EquipmentUnitId))
            ModelState.AddModelError("Input.EquipmentUnitId", "La unidad no corresponde a los equipos prestados en esta planificación.");
        if (!IncidentTypes.Contains(Input.IncidentType))
            ModelState.AddModelError("Input.IncidentType", "Selecciona un tipo de incidente válido.");
        if (!ResponsibleTypes.Contains(Input.ResponsibleType))
            ModelState.AddModelError("Input.ResponsibleType", "Selecciona si el involucrado es estudiante o docente.");

        if (!ModelState.IsValid) return Page();

        _context.L5Incidents.Add(new L5Incident
        {
            L3RequestId = PlanId,
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

        TempData.Success("Incidente L-5 registrado. La unidad quedó marcada como averiada.");
        return RedirectToPage("./L3", new { id = PlanId });
    }

    private async Task<IActionResult?> LoadAsync()
    {
        Plan = await _context.L3Requests.AsNoTracking()
            .Include(r => r.Laboratory)
            .Include(r => r.Subject)
            .Include(r => r.Group)
            .FirstOrDefaultAsync(r => r.Id == PlanId);
        if (Plan == null)
        {
            TempData.Error("La planificación no existe.");
            return RedirectToPage("./Index");
        }
        if (Plan.Status != L3RequestStatus.Approved)
        {
            TempData.Error("Solo se registran incidentes L-5 de una planificación aprobada y no finalizada.");
            return RedirectToPage("./L3", new { id = PlanId });
        }

        // Unidades concretas de los tipos de equipo prestados, dentro del laboratorio del plan.
        var lentEquipmentIds = await _context.L3RequestDetails.AsNoTracking()
            .Where(d => d.L3RequestId == PlanId && d.DeliveredQuantity > 0)
            .Select(d => d.EquipmentUnit.EquipmentId)
            .Distinct()
            .ToListAsync();

        var units = await _context.EquipmentUnits.AsNoTracking()
            .Where(u => u.LaboratoryId == Plan.LaboratoryId && lentEquipmentIds.Contains(u.EquipmentId)
                && u.CurrentStatus != EquipmentStatus.Deleted)
            .OrderBy(u => u.Equipment!.Name).ThenBy(u => u.InventoryNumber)
            .Select(u => new { u.Id, u.EquipmentId, Name = u.Equipment!.Name, u.InventoryNumber })
            .ToListAsync();

        UnitOptions = units.Select(u => new SelectListItem(
                $"{u.Name} (INV: {u.InventoryNumber})",
                u.Id.ToString(),
                u.Id == Input.EquipmentUnitId || (Input.EquipmentUnitId == 0 && EquipmentId == u.EquipmentId && u == units.First(x => x.EquipmentId == EquipmentId))))
            .ToList();
        return null;
    }

    public sealed class InputModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona la unidad afectada")]
        public int EquipmentUnitId { get; set; }

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
