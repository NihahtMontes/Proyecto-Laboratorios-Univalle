using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Request> Requests { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Requests = await _context.Requests
                .Include(s => s.ApprovedBy)
                .Include(s => s.CreatedBy)
                .Include(s => s.Equipment)
                .Include(s => s.ModifiedBy)
                .Include(s => s.RequestedBy) // Added RequestedBy
                .ToListAsync();
        }
    }
}
