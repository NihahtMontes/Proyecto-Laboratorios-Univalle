using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;

namespace Proyecto_Laboratorios_Univalle.Pages.Equipment
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Proyecto_Laboratorios_Univalle.Models.Equipment> Equipment { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Equipment = await _context.Equipments
                .Include(e => e.City)
                .Include(e => e.CreatedBy)
                .Include(e => e.Laboratory)
                .Include(e => e.ModifiedBy)
                .Include(e => e.Country)
                .Include(e => e.EquipmentType).ToListAsync();
        }
    }
}
