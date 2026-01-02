using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Equipment
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Proyecto_Laboratorios_Univalle.Models.Equipment Equipment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipment = await _context.Equipments
                .Include(e => e.Laboratory)
                    .ThenInclude(l => l.Faculty)
                .Include(e => e.EquipmentType)
                .Include(e => e.City)
                    .ThenInclude(c => c.Country)
                .Include(e => e.CreatedBy)
                .Include(e => e.ModifiedBy)
                .Include(e => e.StateHistory)
                    .ThenInclude(h => h.RegisteredBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipment == null)
            {
                return NotFound();
            }

            Equipment = equipment;
            return Page();
        }
    }
}