using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Verification> Verifications { get; set; } = default!;

        public string CurrentFilter { get; set; }

        public async Task OnGetAsync(string searchString)
        {
            CurrentFilter = searchString;

            IQueryable<Verification> verificationIQ = _context.Verifications
                .Include(v => v.CreatedBy)
                .Include(v => v.Equipment)
                .Include(v => v.ModifiedBy);

            if (!string.IsNullOrEmpty(searchString))
            {
                verificationIQ = verificationIQ.Where(s => s.Equipment.Name.Contains(searchString) 
                                       || s.Equipment.InventoryNumber.Contains(searchString));
            }

            Verifications = await verificationIQ.OrderByDescending(v => v.Date).ToListAsync();
        }
    }
}
