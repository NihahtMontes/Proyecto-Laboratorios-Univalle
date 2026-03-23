using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Helpers;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Management> ManagementList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Load all management periods, ordered by most recent first
            if (_context.Managements != null)
            {
                ManagementList = await _context.Managements
                    .Include(m => m.ManagementPlans) // Include plans to calculate progress
                    .OrderByDescending(m => m.CreatedDate)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostCloseManagementAsync(int id)
        {
            var management = await _context.Managements.FindAsync(id);
            if (management == null)
            {
                return NotFound();
            }

            management.Status = Models.Enums.ManagementStatus.Closed;
            management.ActualClosedDate = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "La gestión administrativa ha sido cerrada exitosamente. Ya no se admiten modificaciones.";
            return RedirectToPage("./Index");
        }
    }
}
