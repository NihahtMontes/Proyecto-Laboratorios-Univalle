using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{

    //[Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<User> User { get; set; } = default!;

        public async Task OnGetAsync()
        {
            User = await _context.Users
            .Include(u => u.CreatedBy)
            .Include(u => u.ModifiedBy)
            .Where(u => u.Status != Proyecto_Laboratorios_Univalle.Models.Enums.GeneralStatus.Eliminado)
            .ToListAsync();
        }
    }
}
