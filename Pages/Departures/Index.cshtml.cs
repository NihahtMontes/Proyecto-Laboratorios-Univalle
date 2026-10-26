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
                .Include(d => d.Planning)
                    .ThenInclude(p => p!.Laboratory)
                .Include(d => d.OriginLaboratory)
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
                // FullName es una propiedad calculada que EF no traduce a SQL (la búsqueda daba error 500): se busca en el nombre de la persona.
                var internIds = _context.Set<Intern>().Where(i => i.Name.ToLower().Contains(term)).Select(i => i.Id);
                var externIds = _context.Set<Extern>().Where(e => e.Name.ToLower().Contains(term)).Select(e => e.Id);
                query = query.Where(d =>
                    d.EquipmentUnit!.Equipment!.Name.ToLower().Contains(term) ||
                    d.EquipmentUnit!.InventoryNumber.ToLower().Contains(term) ||
                    (d.Code != null && d.Code.ToLower().Contains(term)) ||
                    (d.Planning != null && d.Planning.Code != null && d.Planning.Code.ToLower().Contains(term)) ||
                    (d.BorrowerId != null && (internIds.Contains(d.BorrowerId.Value) || externIds.Contains(d.BorrowerId.Value))));
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

            // Filtro por Laboratorio: ambiente de la unidad de equipo o, en salidas de
            // práctica (L-3), el ambiente de la planificación que origina la salida.
            if (FilterLaboratoryId.HasValue)
            {
                query = query.Where(d =>
                    (d.EquipmentUnit != null && d.EquipmentUnit.LaboratoryId == FilterLaboratoryId.Value) ||
                    (d.PlanningId != null && d.Planning!.LaboratoryId == FilterLaboratoryId.Value));
            }

            Departures = await PaginatedList<Departure>.CreateAsync(
                query.OrderByDescending(d => d.DepartureDate),
                pageIndex ?? 1, 20);
        }
    }
}
