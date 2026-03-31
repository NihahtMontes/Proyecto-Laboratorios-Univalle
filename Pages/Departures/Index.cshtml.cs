using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Departures
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Departure> Departures { get; set; } = new List<Departure>();

        public async Task OnGetAsync()
        {
            Departures = await _context.Departures
                .Include(d => d.EquipmentUnit)
                    .ThenInclude(u => u!.Equipment)
                .Include(d => d.Borrower)
                .OrderByDescending(d => d.DepartureDate)
                .ToListAsync();
        }
    }
}
