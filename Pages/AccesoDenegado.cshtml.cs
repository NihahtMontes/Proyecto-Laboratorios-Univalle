using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;

namespace Proyecto_Laboratorios_Univalle.Pages
{
    /// <summary>
    /// Pantalla de acceso denegado (403): destino de AccessDeniedPath para cualquier rol.
    /// [AllowAnonymous] evita el bucle de autenticación si llega sin sesión iniciada.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public class AccesoDenegadoModel : PageModel
    {
        /// <summary>Destino del botón «Ir al inicio»: el rol de cierre inicia en su pantalla del día.</summary>
        public string HomeUrl => PracticeClosureAccess.IsClosureOnly(User)
            ? PracticeClosureAccess.HomePage
            : "/Index";

        public void OnGet()
        {
        }
    }
}
