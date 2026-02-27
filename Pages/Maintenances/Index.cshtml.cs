using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Maintenance> Maintenances { get; set; } = default!;
        public IList<MaintenanceType> MaintenanceTypes { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public Models.Enums.MaintenanceStatus? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Load Maintenance Types (Tab 2)
            MaintenanceTypes = await _context.MaintenanceTypes
                .Include(mt => mt.CreatedBy)
                .OrderBy(mt => mt.Name)
                .ToListAsync();

            // 2. Base Query for Maintenances (Tab 1)
            var query = _context.Maintenances
                .Include(m => m.CreatedBy)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                        .ThenInclude(e => e!.EquipmentType)
                .Include(m => m.ModifiedBy)
                .Include(m => m.Technician)
                .Include(m => m.MaintenanceType)
                .AsQueryable();

            // 3. Apply Filters
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(m => 
                    m.EquipmentUnit!.Equipment!.Name.ToLower().Contains(term) ||
                    m.EquipmentUnit.InventoryNumber.ToLower().Contains(term) ||
                    (m.Technician != null && m.Technician.Id.ToString() == term)
                );
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(m => m.Status == StatusFilter.Value);
            }

            // 4. Execute Query
            Maintenances = await query
                .OrderByDescending(m => m.ScheduledDate)
                .ToListAsync();
        }
    }
}
