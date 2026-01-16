using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.MaintenanceTypes
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<MaintenanceType> MaintenanceTypes { get; set; } = default!;

        public async Task OnGetAsync()
        {
            MaintenanceTypes = await _context.MaintenanceTypes
                .Include(mt => mt.CreatedBy)
                .Include(mt => mt.ModifiedBy)
                .ToListAsync();
        }
    }
}
