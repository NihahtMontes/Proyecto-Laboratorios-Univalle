using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

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
        public IList<EquipmentType> EquipmentTypes { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? TypeFilter { get; set; }

        public async Task OnGetAsync()
        {
            var equipmentQuery = _context.Equipments
                .Include(e => e.City)
                .Include(e => e.CreatedBy)
                .Include(e => e.Laboratory)
                .Include(e => e.ModifiedBy)
                .Include(e => e.Country)
                .Include(e => e.EquipmentType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                equipmentQuery = equipmentQuery.Where(e => e.Name.ToLower().Contains(term) || 
                                                        e.InventoryNumber.ToLower().Contains(term) || 
                                                        e.Brand.ToLower().Contains(term));
            }

            if (TypeFilter.HasValue)
            {
                equipmentQuery = equipmentQuery.Where(e => e.EquipmentTypeId == TypeFilter.Value);
            }

            Equipment = await equipmentQuery.ToListAsync();

            // Para la pestaña de Tipos de Equipamiento
            EquipmentTypes = await _context.EquipmentTypes
                .Include(Et => Et.CreatedBy)
                .ToListAsync();
        }
    }
}
