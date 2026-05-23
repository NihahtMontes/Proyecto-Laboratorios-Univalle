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

                EquipmentStatuses = plans
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando el Centro de Reportes para la gestion {ManagementId} y lab {LabId}", ManagementId, SelectedLabId);
                LaboratoriesList ??= new SelectList(new List<Laboratory>(), "Id", "Name");
            }
        }
    }
}
