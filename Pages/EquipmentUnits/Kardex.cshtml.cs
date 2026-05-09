using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentUnits
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class KardexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public KardexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public EquipmentUnit Unit { get; set; } = default!;
        public List<Maintenance> History { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public bool IsWizard { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ReturnStep { get; set; } = 5;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Unit = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (Unit == null) return NotFound();

            History = await _context.Maintenances
                .Include(m => m.Technician)
                .Include(m => m.CostDetails)
                .Include(m => m.Tasks)
                .Include(m => m.CreatedBy)
                .Where(m => m.EquipmentUnitId == id)
                .OrderByDescending(m => m.EndDate ?? m.ScheduledDate)
                .ToListAsync();

            return Page();
        }
    }
}
