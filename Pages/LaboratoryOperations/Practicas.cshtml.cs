using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proyecto_Laboratorios_Univalle.Pages.LaboratoryOperations;

/// <summary>El listado de prácticas ahora es el de planificaciones; se conserva la ruta por enlaces antiguos.</summary>
public class PracticasModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("./Planificaciones");
}
