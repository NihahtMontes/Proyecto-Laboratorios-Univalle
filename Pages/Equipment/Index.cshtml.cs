using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public int? TypeId { get; set; }

        public async Task OnGetAsync()
        {
            var equipmentQuery = _context.Equipments
                .Include(e => e.EquipmentType)
                .Include(e => e.City)
                .Include(e => e.Country)
                .Include(e => e.Units!)
                    .ThenInclude(u => u.Laboratory!)
                        .ThenInclude(l => l.Faculty)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                equipmentQuery = equipmentQuery.Where(e => e.Name.ToLower().Contains(term) || 
                                                         e.Units!.Any(u => u.InventoryNumber.Contains(term)) || 
                                                         (e.Brand != null && e.Brand.ToLower().Contains(term)));
            }

            if (TypeId.HasValue)
            {
                equipmentQuery = equipmentQuery.Where(e => e.EquipmentTypeId == TypeId.Value);
            }

            Equipment = await equipmentQuery.OrderBy(e => e.Name).ToListAsync();

            ViewData["TypeId"] = new SelectList(await _context.EquipmentTypes.OrderBy(t => t.Name).ToListAsync(), "Id", "Name", TypeId);

            EquipmentTypes = await _context.EquipmentTypes
                .Include(Et => Et.CreatedBy)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }
}
