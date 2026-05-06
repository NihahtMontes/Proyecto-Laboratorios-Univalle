using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Departures
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Departure Departure { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var departure = await _context.Departures
                .Include(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(d => d.EquipmentUnit)
                    .ThenInclude(eu => eu!.Laboratory)
                .Include(d => d.Borrower)
                .Include(d => d.CreatedBy)
                .Include(d => d.ModifiedBy)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (departure == null) return NotFound();

            Departure = departure;
            return Page();
        }
    }
}
