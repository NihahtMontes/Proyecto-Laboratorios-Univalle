using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Persons
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Person Person { get; set; } = default!;
        public PaginatedList<Maintenance> Maintenances { get; set; } = new(new List<Maintenance>(), 0, 1, 10);
        public List<Departure> Departures { get; set; } = new();
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
            if (id == null)
            {
                return NotFound();
            }

            var person = await _context.People
                .Include(p => p.CreatedBy)
                .Include(p => p.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (person == null)
            {
                return NotFound();
            }

            Person = person;

            await LoadManagementOptionsAsync();
            await ApplyDefaultManagementFilterAsync();
            Maintenances = await BuildMaintenanceHistoryQuery(id.Value);

            Departures = await _context.Departures
                .Include(l => l.EquipmentUnit)
                    .ThenInclude(u => u!.Equipment)
                .Where(l => l.BorrowerId == id)
                .OrderByDescending(l => l.DepartureDate)
                .Take(10)
                .ToListAsync();

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

        public int MaintenanceFirstItem => Maintenances.TotalCount == 0
            ? 0
            : ((Maintenances.PageIndex - 1) * Maintenances.PageSize) + 1;

        public int MaintenanceLastItem => Math.Min(Maintenances.PageIndex * Maintenances.PageSize, Maintenances.TotalCount);

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

        private async Task<PaginatedList<Maintenance>> BuildMaintenanceHistoryQuery(int personId)
        {
            var query = _context.Maintenances
                .AsNoTracking()
                .Include(m => m.Management)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(u => u!.Equipment)
                .Include(m => m.CostDetails)
                .Where(m => m.TechnicianId == personId);

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
                query = query.Where(m =>
                    (m.Description != null && m.Description.ToLower().Contains(term)) ||
                    (m.Observations != null && m.Observations.ToLower().Contains(term)) ||
                    (m.Recommendations != null && m.Recommendations.ToLower().Contains(term)) ||
                    (m.EquipmentUnit != null && m.EquipmentUnit.InventoryNumber.ToLower().Contains(term)) ||
                    (m.EquipmentUnit != null && m.EquipmentUnit.Equipment != null && m.EquipmentUnit.Equipment.Name.ToLower().Contains(term)));
            }

            query = query.OrderByDescending(m => m.EndDate ?? m.StartDate ?? m.ScheduledDate ?? m.CreatedDate)
                .ThenByDescending(m => m.Id);

            return await PaginatedList<Maintenance>.CreateAsync(query, Math.Max(1, MaintenancePageIndex), MaintenancePageSize);
        }
    }
}
