using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Equipment
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.Equipment Equipment { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipment = await _context.Equipments
                .Include(e => e.EquipmentType)
                .Include(e => e.City)
                .Include(e => e.Country)
                .Include(e => e.CreatedBy)
                .Include(e => e.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipment == null) return NotFound();

            Equipment = equipment;

            // Cargar unidades con filtros, búsqueda e inclusiones específicas de ubicación
            var unitsQuery = _context.EquipmentUnits
                .Include(u => u.Laboratory)
                    .ThenInclude(l => l.Faculty)
                .Where(u => u.EquipmentId == id);

            // Filtro por Estado (Soft Delete) - Mostrar activos por defecto
            unitsQuery = unitsQuery.Where(u => u.CurrentStatus != EquipmentStatus.Deleted);

            // Búsqueda inteligente
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.ToLower();
                unitsQuery = unitsQuery.Where(u => 
                    u.InventoryNumber.ToLower().Contains(term) || 
                    (u.SerialNumber != null && u.SerialNumber.ToLower().Contains(term)));
            }
            
            // Re-asignar las unidades cargadas al modelo para la vista
            Equipment.Units = await unitsQuery.OrderBy(u => u.InventoryNumber).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteUnitAsync(int id)
        {
            var unit = await _context.EquipmentUnits.FindAsync(id);

            if (unit != null)
            {
                unit.CurrentStatus = EquipmentStatus.Deleted;
                _context.Attach(unit).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                
                TempData.Success($"Unidad '{unit.InventoryNumber}' eliminada correctamente.");
            }

            return RedirectToPage(new { id = unit?.EquipmentId });
        }
    }
}