using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // --- NUEVAS PROPIEDADES PARA FILTRADO POR AMBIENTE ---
        [BindProperty(SupportsGet = true)]
        public string? SelectedBlock { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLaboratoryId { get; set; }

        public List<string> Blocks { get; set; } = new();
        public SelectList LaboratoryList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // 1. Cargar lista de edificios (bloques) únicos para el primer desplegable
            Blocks = await _context.Laboratories
                .Where(l => !string.IsNullOrEmpty(l.Building))
                .Select(l => l.Building!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();

            // 2. Preparar lista de laboratorios (Filtrada por bloque si se seleccionó uno)
            var labsQuery = _context.Laboratories.AsQueryable();
            if (!string.IsNullOrEmpty(SelectedBlock))
            {
                labsQuery = labsQuery.Where(l => l.Building == SelectedBlock);
            }
            var labs = await labsQuery.OrderBy(l => l.Name).ToListAsync();
            LaboratoryList = new SelectList(labs, "Id", "Name");

            // 3. Load Maintenance Types (Por compatibilidad técnica)
            MaintenanceTypes = await _context.MaintenanceTypes
                .Include(mt => mt.CreatedBy)
                .OrderBy(mt => mt.Name)
                .ToListAsync();

            // 4. Base Query for Maintenances
            var query = _context.Maintenances
                .Include(m => m.CreatedBy)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu.Laboratory)
                .Include(m => m.ModifiedBy)
                .Include(m => m.Technician)
                .Include(m => m.MaintenanceType)
                .AsQueryable();

            // 5. Apply Filters
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(m =>
                    m.EquipmentUnit!.Equipment!.Name.ToLower().Contains(term) ||
                    m.EquipmentUnit.InventoryNumber.ToLower().Contains(term) ||
                    (m.Technician != null && m.Technician.FullName.ToLower().Contains(term))
                );
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(m => m.Status == StatusFilter.Value);
            }

            // Filtro por Laboratorio Específico
            if (SelectedLaboratoryId.HasValue)
            {
                query = query.Where(m => m.EquipmentUnit!.LaboratoryId == SelectedLaboratoryId.Value);
            }
            // O Filtro por Bloque/Edificio completo
            else if (!string.IsNullOrEmpty(SelectedBlock))
            {
                query = query.Where(m => m.EquipmentUnit!.Laboratory!.Building == SelectedBlock);
            }

            // 6. Execute Query - Ordenado alfabéticamente por nombre de equipo
            Maintenances = await query
                .OrderBy(m => m.EquipmentUnit!.Equipment!.Name)
                .ToListAsync();
        }
    }
}