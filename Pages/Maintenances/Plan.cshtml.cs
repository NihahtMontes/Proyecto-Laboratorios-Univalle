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
    /// <summary>
    /// Listado de SOLO LECTURA del Plan de Mantenimiento (L-48): datos históricos
    /// importados desde Excel en la tabla MaintenancePlans. Sin Create/Edit/Delete.
    /// </summary>
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class PlanModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public PlanModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<MaintenancePlan> Plans { get; set; } = default!;

        public int PageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public MaintenanceType? MaintenanceType { get; set; }

        public SelectList ManagementList { get; set; } = default!;

        public async Task OnGetAsync(int? pageIndex)
        {
            // Gestiones para el filtro (el query filter global excluye las eliminadas).
            var managements = await _context.Managements
                .AsNoTracking()
                .OrderBy(m => m.Code)
                .Select(m => new { m.Id, m.Code })
                .ToListAsync();
            ManagementList = new SelectList(managements, "Id", "Code", ManagementId);

            var query = _context.MaintenancePlans
                .AsNoTracking()
                .Include(p => p.Management)
                .Include(p => p.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(p => p.Laboratory)
                .Include(p => p.ResponsiblePerson)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    (p.PlanCode != null && p.PlanCode.ToLower().Contains(term)) ||
                    (p.EquipmentUnit != null && p.EquipmentUnit.InventoryNumber.ToLower().Contains(term)) ||
                    (p.EquipmentUnit != null && p.EquipmentUnit.Equipment != null && p.EquipmentUnit.Equipment.Name.ToLower().Contains(term)));
            }

            if (ManagementId.HasValue)
            {
                query = query.Where(p => p.ManagementId == ManagementId.Value);
            }

            if (MaintenanceType.HasValue)
            {
                query = query.Where(p => p.MaintenanceType == MaintenanceType.Value);
            }

            Plans = await PaginatedList<MaintenancePlan>.CreateAsync(
                query
                    .OrderBy(p => p.Management!.Code)
                    .ThenBy(p => p.PlannedDate)
                    .ThenBy(p => p.PlanCode)
                    .ThenBy(p => p.Id),
                pageIndex ?? 1, PageSize);
        }
    }
}
