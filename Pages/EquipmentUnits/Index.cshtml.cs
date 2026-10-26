using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentUnits
{
    /// <summary>
    /// Listado plano de SOLO LECTURA de todas las unidades físicas vigentes.
    /// El DbContext ya excluye las unidades con CurrentStatus = Deleted (query filter global).
    /// </summary>
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private const int PageSize = 20;

        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<EquipmentUnit> Units { get; set; } = new PaginatedList<EquipmentUnit>(new List<EquipmentUnit>(), 0, 1, PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? LaboratoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public EquipmentStatus? Status { get; set; }

        public SelectList LaboratoriesList { get; set; } = default!;

        public async Task OnGetAsync(int? pageIndex)
        {
            // Laboratorios activos para el desplegable de filtros (mismo patrón que Equipment/Index).
            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Code)
                .ThenBy(l => l.Name)
                .ToListAsync();
            LaboratoriesList = new SelectList(LaboratoryDisplayHelper.ToSelectItems(labs), "Id", "DisplayName", LaboratoryId);

            var query = _context.EquipmentUnits
                .AsNoTracking()
                .Include(u => u.Equipment)
                .Include(u => u.Laboratory)
                .AsQueryable();

            // Búsqueda: n.º inventario, n.º serie, nombre o código de catálogo del equipo.
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.InventoryNumber.ToLower().Contains(term) ||
                    (u.SerialNumber != null && u.SerialNumber.ToLower().Contains(term)) ||
                    (u.Equipment != null && u.Equipment.Name.ToLower().Contains(term)) ||
                    (u.Equipment != null && u.Equipment.CatalogCode != null && u.Equipment.CatalogCode.ToLower().Contains(term)));
            }

            // Filtro por laboratorio asignado.
            if (LaboratoryId.HasValue)
            {
                query = query.Where(u => u.LaboratoryId == LaboratoryId.Value);
            }

            // Filtro por estado operativo.
            if (Status.HasValue)
            {
                query = query.Where(u => u.CurrentStatus == Status.Value);
            }

            Units = await PaginatedList<EquipmentUnit>.CreateAsync(
                query.OrderBy(u => u.Equipment!.Name).ThenBy(u => u.InventoryNumber),
                pageIndex ?? 1,
                PageSize);
        }
    }
}
