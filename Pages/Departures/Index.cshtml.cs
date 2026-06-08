using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Departures
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<Departure> Departures { get; set; } = new PaginatedList<Departure>(new List<Departure>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public DepartureType? FilterType { get; set; }

        [BindProperty(SupportsGet = true)]
        public LoanStatus? FilterStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterLaboratoryId { get; set; }

        public SelectList LaboratoriesList { get; set; } = default!;

        public async Task OnGetAsync(int? pageIndex)
        {
            var query = _context.Departures
                .Include(d => d.EquipmentUnit)
                    .ThenInclude(u => u!.Equipment)
                .Include(d => d.EquipmentUnit)
                    .ThenInclude(u => u!.Laboratory)
                .Include(d => d.Borrower)
                .AsQueryable();

            // A) Cargar la lista de laboratorios para el desplegable
            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Code)
                .ThenBy(l => l.Name)
                .ToListAsync();
            LaboratoriesList = new SelectList(LaboratoryDisplayHelper.ToSelectItems(labs), "Id", "DisplayName");

            // Búsqueda rápida
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(d =>
                    d.EquipmentUnit!.Equipment!.Name.ToLower().Contains(term) ||
                    d.EquipmentUnit!.InventoryNumber.ToLower().Contains(term) ||
                    (d.Borrower != null && d.Borrower.FullName.ToLower().Contains(term)));
            }

            // Filtro por Tipo de Salida
            if (FilterType.HasValue)
            {
                query = query.Where(d => d.Type == FilterType.Value);
            }

            // Filtro por Estado
            if (FilterStatus.HasValue)
            {
                query = query.Where(d => d.Status == FilterStatus.Value);
            }

            // Filtro por Laboratorio (vía EquipmentUnit)
            if (FilterLaboratoryId.HasValue)
            {
                query = query.Where(d => d.EquipmentUnit != null && d.EquipmentUnit.LaboratoryId == FilterLaboratoryId.Value);
            }

            Departures = await PaginatedList<Departure>.CreateAsync(
                query.OrderByDescending(d => d.DepartureDate),
                pageIndex ?? 1, 20);
        }
    }
}
