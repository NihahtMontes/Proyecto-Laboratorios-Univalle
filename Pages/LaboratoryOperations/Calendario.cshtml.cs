using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>El calendario ahora vive en el panel de la planificación (Index); se conserva la ruta por enlaces antiguos.</summary>
public class CalendarioModel : PageModel
{
    public IActionResult OnGet(int? planningId, string? month) =>
        RedirectToPage("./Index", new { planningId, month });
}
