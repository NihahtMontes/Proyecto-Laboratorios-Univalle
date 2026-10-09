using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>El calendario vive en el panel de planificación (Index); se conserva la ruta por enlaces antiguos.</summary>
public class CalendarioModel : PageModel
{
    public IActionResult OnGet(int? managementId, string? month) =>
        RedirectToPage("./Index", new { managementId, month });
}
