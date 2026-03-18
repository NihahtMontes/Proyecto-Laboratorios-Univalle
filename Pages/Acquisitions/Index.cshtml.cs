using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Acquisitions
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
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
            // Solo traemos las solicitudes de adquisición (Purchasing) para optimizar
            Requests = await _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.EquipmentUnit)
                .Include(r => r.RequestedBy)
                .Where(r => r.Type == Models.Enums.RequestType.Purchasing)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }
    }
}