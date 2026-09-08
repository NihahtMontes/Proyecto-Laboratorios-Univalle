using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;

namespace Proyecto_Laboratorios_Univalle.Pages.Migration
{
    [Authorize(Roles = AuthorizationHelper.RoleSuperAdmin)]
    public class ImportModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
