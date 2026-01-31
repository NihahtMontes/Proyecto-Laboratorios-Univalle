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
            // 1. Cargar Tipos de Mantenimiento (Tab 2)
            MaintenanceTypes = await _context.MaintenanceTypes
                .Include(mt => mt.CreatedBy)
                .OrderBy(mt => mt.Name)
                .ToListAsync();

            // 2. Consulta Base de Mantenimientos (Tab 1)
            var query = _context.Maintenances
                .Include(m => m.CreatedBy)
                .Include(m => m.Equipment)
                    .ThenInclude(e => e.EquipmentType)
                .Include(m => m.ModifiedBy)
                .Include(m => m.Technician)
                .Include(m => m.MaintenanceType)
                .AsQueryable();

            // 3. Aplicar Filtros
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(m => 
                    m.Equipment.Name.ToLower().Contains(term) ||
                    m.Equipment.InventoryNumber.ToLower().Contains(term) ||
                    (m.Technician != null && m.Technician.FirstName.ToLower().Contains(term)) ||
                    (m.Technician != null && m.Technician.LastName.ToLower().Contains(term))
                );
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(m => m.Status == StatusFilter.Value);
            }

            // 4. Ejecutar Consulta
            Maintenances = await query
                .OrderByDescending(m => m.ScheduledDate)
                .ToListAsync();
        }
    }
}
