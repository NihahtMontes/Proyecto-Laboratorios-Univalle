using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public PaginatedList<Departure> Departures { get; set; } = new PaginatedList<Departure>(new List<Departure>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            Departures = await PaginatedList<Departure>.CreateAsync(
                _context.Departures
                    .Include(d => d.EquipmentUnit)
                        .ThenInclude(u => u!.Equipment)
                    .Include(d => d.Borrower)
                    .OrderByDescending(d => d.DepartureDate),
                pageIndex ?? 1, 20);
        }
    }
}
