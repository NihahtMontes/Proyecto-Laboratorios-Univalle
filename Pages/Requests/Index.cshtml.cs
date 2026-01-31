using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
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

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public RequestStatus? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public RequestPriority? PriorityFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Requests
                .Include(r => r.Equipment)
                    .ThenInclude(e => e.EquipmentType)
                .Include(r => r.RequestedBy)
                .Include(r => r.ApprovedBy)
                .Include(r => r.CreatedBy)
                .Include(r => r.ModifiedBy)
                .AsQueryable();

            // Apply search
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(r => 
                    r.Description.ToLower().Contains(term) ||
                    r.Equipment.Name.ToLower().Contains(term) ||
                    r.Equipment.InventoryNumber.ToLower().Contains(term) ||
                    r.RequestedBy.FirstName.ToLower().Contains(term) ||
                    r.RequestedBy.LastName.ToLower().Contains(term)
                );
            }

            // Apply status filter
            if (StatusFilter.HasValue)
            {
                query = query.Where(r => r.Status == StatusFilter.Value);
            }

            // Apply priority filter
            if (PriorityFilter.HasValue)
            {
                query = query.Where(r => r.Priority == PriorityFilter.Value);
            }

            Requests = await query
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }
    }
}
