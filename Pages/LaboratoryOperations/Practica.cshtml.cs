using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>La práctica ahora es la planificación (Paso 1); se conserva la ruta por enlaces antiguos.</summary>
public class PracticaModel : PageModel
{
    public IActionResult OnGet(int? id) => RedirectToPage("./Planificacion", new { id });
}
