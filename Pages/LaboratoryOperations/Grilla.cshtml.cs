using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>
/// Grilla semanal por laboratorio (formulario RE-10-VAC-007 "Registro de horarios – Horario por aula").
/// Solo lectura: lunes a sábado × 12 periodos, con las planificaciones del ambiente elegido.
/// </summary>
[Authorize(Roles = AuthorizationHelper.ManagementRoles)]
public class GrillaModel : PageModel
{
    private readonly ILaboratoryGridService _grid;

    public GrillaModel(ILaboratoryGridService grid) => _grid = grid;

    [BindProperty(SupportsGet = true)]
    public int? ManagementId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? LaboratoryId { get; set; }

    /// <summary>Número de semana académica de la gestión.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Week { get; set; }

    /// <summary>Fecha yyyy-MM-dd: muestra la semana que la contiene (tiene prioridad sobre Week).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Date { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool TwoWeeks { get; set; }

    /// <summary>Grilla armada (null si no hay gestión pedida ni actual).</summary>
    public LaboratoryGrid? Grid { get; private set; }

    public async Task OnGetAsync()
    {
        Grid = await _grid.BuildAsync(ManagementId, LaboratoryId, Week, Date, TwoWeeks);
    }
}
