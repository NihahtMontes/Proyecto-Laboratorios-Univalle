using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

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

        public PaginatedList<Maintenance> Maintenances { get; set; } = default!;

        public int PageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public Models.Enums.MaintenanceStatus? StatusFilter { get; set; }


        [BindProperty(SupportsGet = true)]
        public int? SelectedLaboratoryId { get; set; }

        public SelectList LaboratoryList { get; set; } = default!;

        public async Task OnGetAsync(int? pageIndex)
        {

            var labsQuery = _context.Laboratories.AsQueryable();
            var labs = await labsQuery.OrderBy(l => l.Code).ThenBy(l => l.Name).ToListAsync();
            LaboratoryList = new SelectList(LaboratoryDisplayHelper.ToSelectItems(labs), "Id", "DisplayName", SelectedLaboratoryId);



                var query = _context.Maintenances
                .Include(m => m.CreatedBy)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Laboratory)
                .Include(m => m.ModifiedBy)
                .Include(m => m.Technician)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                var technicianIds = await _context.People
                    .OfType<Intern>()
                    .Where(p => p.Name.ToLower().Contains(term))
                    .Select(p => p.Id)
                    .Concat(_context.People
                        .OfType<Extern>()
                        .Where(p => p.Name.ToLower().Contains(term))
                        .Select(p => p.Id))
                    .ToListAsync();

                query = query.Where(m =>
                    m.EquipmentUnit!.Equipment!.Name.ToLower().Contains(term) ||
                    m.EquipmentUnit.InventoryNumber.ToLower().Contains(term) ||
                    (m.TechnicianId.HasValue && technicianIds.Contains(m.TechnicianId.Value))
                );
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(m => m.Status == StatusFilter.Value);
            }

            if (SelectedLaboratoryId.HasValue)
            {
                query = query.Where(m => m.EquipmentUnit!.LaboratoryId == SelectedLaboratoryId.Value);
            }

            Maintenances = await PaginatedList<Maintenance>.CreateAsync(
                query.OrderBy(m => m.EquipmentUnit!.Equipment!.Name),
                pageIndex ?? 1, PageSize);
        }
    }
}
