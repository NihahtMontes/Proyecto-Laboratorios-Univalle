using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentUnits
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public EquipmentUnit EquipmentUnit { get; set; } = default!;
        public PaginatedList<Maintenance> MaintenanceHistory { get; set; } = new(new List<Maintenance>(), 0, 1, 10);
        public List<SelectListItem> ManagementOptions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? MaintenanceManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public ManagementType? MaintenanceManagementType { get; set; }

        [BindProperty(SupportsGet = true)]
        public MaintenanceSatisfaction? MaintenanceSatisfactionFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? MaintenanceSearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int MaintenancePageIndex { get; set; } = 1;

        public int MaintenancePageSize { get; } = 10;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipmentunit = await _context.EquipmentUnits
                .Include(e => e.Equipment)
                    .ThenInclude(e => e!.City)
                .Include(e => e.Laboratory)
                    .ThenInclude(l => l!.Faculty)
                .Include(e => e.Equipment)
                    .ThenInclude(e => e!.Notes)
                .Include(e => e.Career)
                .Include(e => e.Verifications!)
                .Include(e => e.StateHistory!)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipmentunit == null) return NotFound();
            
            EquipmentUnit = equipmentunit;
            await LoadManagementOptionsAsync();
            await ApplyDefaultManagementFilterAsync();
            MaintenanceHistory = await BuildMaintenanceHistoryQuery(equipmentunit.Id);
            return Page();
        }

        public string FormatManagement(Management? management)
        {
            if (management == null) return "Gestión sin detalle";
            if (!string.IsNullOrWhiteSpace(management.Code)) return management.Code;
            return management.Type == ManagementType.Corrective
                ? $"CORR-{management.Year}-{management.Semester}"
                : $"{management.Year}-{management.Semester}";
        }

        public int MaintenanceFirstItem => MaintenanceHistory.TotalCount == 0
            ? 0
            : ((MaintenanceHistory.PageIndex - 1) * MaintenanceHistory.PageSize) + 1;

        public int MaintenanceLastItem => Math.Min(MaintenanceHistory.PageIndex * MaintenanceHistory.PageSize, MaintenanceHistory.TotalCount);

        private async Task ApplyDefaultManagementFilterAsync()
        {
            if (Request.Query.ContainsKey(nameof(MaintenanceManagementId))) return;

            MaintenanceManagementId = await _context.Managements
                .AsNoTracking()
                .Where(m => m.Status == ManagementStatus.Active)
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Semester)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync();
        }

        private async Task LoadManagementOptionsAsync()
        {
            var managements = await _context.Managements
                .AsNoTracking()
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Semester)
                .ThenBy(m => m.Type)
                .ToListAsync();

            ManagementOptions = managements
                .Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = FormatManagement(m)
                })
                .ToList();
        }

        private async Task<PaginatedList<Maintenance>> BuildMaintenanceHistoryQuery(int equipmentUnitId)
        {
            var query = _context.Maintenances
                .AsNoTracking()
                .Include(m => m.Management)
                .Include(m => m.Technician)
                .Include(m => m.CostDetails)
                .Where(m => m.EquipmentUnitId == equipmentUnitId);

            if (MaintenanceManagementId.HasValue)
            {
                query = query.Where(m => m.ManagementId == MaintenanceManagementId.Value);
            }

            if (MaintenanceManagementType.HasValue)
            {
                query = query.Where(m => m.Management != null && m.Management.Type == MaintenanceManagementType.Value);
            }

            if (MaintenanceSatisfactionFilter.HasValue)
            {
                query = query.Where(m => m.SatisfactionLevel == MaintenanceSatisfactionFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(MaintenanceSearchTerm))
            {
                var term = MaintenanceSearchTerm.Trim().ToLower();
                var technicianIds = await GetMatchingPersonIdsAsync(term);

                query = query.Where(m =>
                    (m.Description != null && m.Description.ToLower().Contains(term)) ||
                    (m.Observations != null && m.Observations.ToLower().Contains(term)) ||
                    (m.Recommendations != null && m.Recommendations.ToLower().Contains(term)) ||
                    (technicianIds.Count > 0 && m.TechnicianId.HasValue && technicianIds.Contains(m.TechnicianId.Value)));
            }

            query = query.OrderByDescending(m => m.EndDate ?? m.StartDate ?? m.ScheduledDate ?? m.CreatedDate)
                .ThenByDescending(m => m.Id);

            return await PaginatedList<Maintenance>.CreateAsync(query, Math.Max(1, MaintenancePageIndex), MaintenancePageSize);
        }

        private async Task<List<int>> GetMatchingPersonIdsAsync(string term)
        {
            var internalIds = await _context.Interns
                .AsNoTracking()
                .Where(p => p.Name.ToLower().Contains(term))
                .Select(p => p.Id)
                .ToListAsync();

            var externalIds = await _context.Externs
                .AsNoTracking()
                .Where(p => p.Name.ToLower().Contains(term))
                .Select(p => p.Id)
                .ToListAsync();

            return internalIds.Concat(externalIds).Distinct().ToList();
        }
    }
}
