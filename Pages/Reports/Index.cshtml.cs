using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Reports
{
    public class EquipmentUnitReportStatus
    {
        public int PlanId { get; set; }
        public EquipmentUnit Unit { get; set; } = null!;
        public int? RequestId { get; set; }
        public int? MaintenanceId { get; set; }
        public int? DepartureId { get; set; }
        public int? AcquisitionRequestId { get; set; }
        public bool HasL6 { get; set; }
        public bool HasL7 => RequestId.HasValue;
        public bool HasL8 => MaintenanceId.HasValue;
        public bool HasL3 => DepartureId.HasValue;
        public bool HasAdquisicion => AcquisitionRequestId.HasValue;
    }

    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IManagementContextService _managementContext;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            ICurrentUserService currentUser,
            IManagementContextService managementContext,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _managementContext = managementContext;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReportFilter { get; set; }

        public SelectList LaboratoriesList { get; set; } = null!;
        public List<EquipmentUnitReportStatus> EquipmentStatuses { get; set; } = new();
        public Laboratory? SelectedLaboratory { get; set; }
        public Management? SelectedManagement { get; set; }
        public bool IsCorrective => SelectedManagement?.Type == ManagementType.Corrective;
        public string CurrentUserFullName { get; set; } = "";
        public string PrintDate { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        public async Task OnGetAsync()
        {
            try
            {
                SelectedManagement = ManagementId.HasValue
                    ? await _context.Managements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == ManagementId.Value)
                    : await _managementContext.GetCurrentManagementAsync(ManagementType.Preventive);

                ManagementId = SelectedManagement?.Id;
                ViewData["ManagementId"] = SelectedManagement?.Id;
                ViewData["ManagementType"] = SelectedManagement?.Type.ToString();

                if (_currentUser.UserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(_currentUser.UserId.Value);
                    CurrentUserFullName = user != null
                        ? $"{user.FirstName} {user.LastName}".Trim()
                        : "Sistema";
                }

                var labsQuery = _context.Laboratories.AsNoTracking().OrderBy(l => l.Name);
                var labs = await labsQuery.ToListAsync();
                LaboratoriesList = new SelectList(labs, "Id", "Name", SelectedLabId);

                if (!SelectedLabId.HasValue || SelectedManagement == null) return;

                SelectedLaboratory = await _context.Laboratories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == SelectedLabId);

                var plans = await _context.ManagementPlans
                    .AsNoTracking()
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Equipment)
                    .Include(p => p.EquipmentUnit).ThenInclude(u => u!.Laboratory)
                    .Where(p => p.ManagementId == SelectedManagement.Id
                             && p.EquipmentUnit != null
                             && p.EquipmentUnit.LaboratoryId == SelectedLabId)
                    .OrderBy(p => p.EquipmentUnit!.InventoryNumber)
                    .ThenBy(p => p.Id)
                    .ToListAsync();

                var statuses = plans
                    .Where(p => p.EquipmentUnit != null)
                    .Select(p => new EquipmentUnitReportStatus
                    {
                        PlanId = p.Id,
                        Unit = p.EquipmentUnit!,
                        RequestId = p.RequestId,
                        MaintenanceId = p.MaintenanceId,
                        DepartureId = p.DepartureId,
                        AcquisitionRequestId = p.AcquisitionRequestId,
                        HasL6 = !IsCorrective && p.VerificationId.HasValue
                    })
                    .ToList();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.Trim();
                    statuses = statuses
                        .Where(s =>
                            Contains(s.Unit.Equipment?.Name, term) ||
                            Contains(s.Unit.InventoryNumber, term) ||
                            Contains(s.Unit.SerialNumber, term) ||
                            Contains(s.Unit.Equipment?.Brand, term) ||
                            Contains(s.Unit.Equipment?.Model, term))
                        .ToList();
                }

                statuses = (ReportFilter ?? "all") switch
                {
                    "available" => statuses.Where(HasAnyReport).ToList(),
                    "missing" => statuses.Where(s => !HasAnyReport(s)).ToList(),
                    "l6" => statuses.Where(s => s.HasL6).ToList(),
                    "l7" => statuses.Where(s => s.HasL7).ToList(),
                    "l8" => statuses.Where(s => s.HasL8).ToList(),
                    "l3" => statuses.Where(s => s.HasL3).ToList(),
                    "l12" => statuses.Where(s => s.HasAdquisicion).ToList(),
                    _ => statuses
                };

                EquipmentStatuses = statuses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando el Centro de Reportes para la gestion {ManagementId} y lab {LabId}", ManagementId, SelectedLabId);
                LaboratoriesList ??= new SelectList(new List<Laboratory>(), "Id", "Name");
            }
        }

        private static bool Contains(string? value, string term)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasAnyReport(EquipmentUnitReportStatus status)
        {
            return status.HasL6
                || status.HasL7
                || status.HasL8
                || status.HasL3
                || status.HasAdquisicion;
        }
    }
}
