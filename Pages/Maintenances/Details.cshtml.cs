using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Maintenance Maintenance { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.Maintenances
                .Include(m => m.Equipment)
                .Include(m => m.MaintenanceType)
                .Include(m => m.Technician)
                .Include(m => m.Request)
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .Include(m => m.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenance == null)
            {
                return NotFound();
            }
            else
            {
                Maintenance = maintenance;
            }
            return Page();
        }
    }
}
