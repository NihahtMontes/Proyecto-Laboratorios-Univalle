using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

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

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLaboratoryId { get; set; }

        public SelectList LaboratoriesList { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public EquipmentCategory? SelectedCategory { get; set; }

        public async Task OnGetAsync()
        {
            var equipmentQuery = _context.Equipments
                .Include(e => e.City)
                .Include(e => e.Country)
                .Include(e => e.Units!)
                    .ThenInclude(u => u.Laboratory!)
                        .ThenInclude(l => l.Faculty)
                .AsQueryable();

            // A) CARGAR LA LISTA DE LABORATORIOS PARA EL DESPLEGABLE
            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Name)
                .ToListAsync();
            LaboratoriesList = new SelectList(labs, "Id", "Name");

            // Lógica de Búsqueda 
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                equipmentQuery = equipmentQuery.Where(e => e.Name.ToLower().Contains(term) ||
                                                           e.Units!.Any(u => u.InventoryNumber.Contains(term) || (u.Career != null && u.Career.Name.ToLower().Contains(term))) ||
                                                           (e.Brand != null && e.Brand.ToLower().Contains(term)));
            }

            // Filtro por Categoría (Enum)
            if (SelectedCategory.HasValue)
            {
                equipmentQuery = equipmentQuery.Where(e => e.Category == SelectedCategory.Value);
            }

            // B) APLICAR EL FILTRO POR AMBIENTE (LABORATORIO)
            if (SelectedLaboratoryId.HasValue)
            {
                equipmentQuery = equipmentQuery.Where(e => e.Units!.Any(u => u.LaboratoryId == SelectedLaboratoryId.Value));
            }

            // C) ORDENAR ALFABÉTICAMENTE POR NOMBRE
            Equipment = await equipmentQuery.OrderBy(e => e.Name).ToListAsync();
        }
    }
}