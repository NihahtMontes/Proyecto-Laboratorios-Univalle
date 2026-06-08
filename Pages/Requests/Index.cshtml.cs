using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly IReportService _reportService;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, IReportService reportService)
        {
            _context = context;
            _reportService = reportService;
        }

        public PaginatedList<Request> Requests { get; set; } = default!;

        public int PageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public RequestStatus? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public RequestPriority? PriorityFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterLaboratoryId { get; set; }

        public Microsoft.AspNetCore.Mvc.Rendering.SelectList LaboratoryList { get; set; } = default!;

        public async Task OnGetAsync(int? pageIndex)
        {
            var query = _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.EquipmentUnit)
                    .ThenInclude(eu => eu != null ? eu.Laboratory : null)
                .Include(r => r.RequestedBy)
                .Include(r => r.ApprovedBy)
                .Include(r => r.CreatedBy)
                .Include(r => r.ModifiedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(r =>
                    r.Description.ToLower().Contains(term) ||
                    r.Equipment!.Name.ToLower().Contains(term) ||
                    (r.EquipmentUnit != null && r.EquipmentUnit.InventoryNumber.ToLower().Contains(term)) ||
                    (r.RequestedBy != null && (r.RequestedBy.FirstName.ToLower().Contains(term) || r.RequestedBy.LastName.ToLower().Contains(term)))
                );
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(r => r.Status == StatusFilter.Value);
            }

            if (PriorityFilter.HasValue)
            {
                query = query.Where(r => r.Priority == PriorityFilter.Value);
            }

            // Apply laboratory filter
            if (FilterLaboratoryId.HasValue)
            {
                query = query.Where(r => r.EquipmentUnit != null && r.EquipmentUnit.LaboratoryId == FilterLaboratoryId.Value);
            }

            Requests = await PaginatedList<Request>.CreateAsync(
                query.OrderByDescending(r => r.CreatedDate),
                pageIndex ?? 1, PageSize);

            // Load labs for the dropdown
            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Code)
                .ThenBy(l => l.Name)
                .ToListAsync();
            LaboratoryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(LaboratoryDisplayHelper.ToSelectItems(labs), "Id", "DisplayName", FilterLaboratoryId);
        }

        public async Task<IActionResult> OnGetDescargarReporteAsync(int id)
        {
            if (id <= 0)
            {
                TempData.Error(NotificationHelper.Requests.InvalidId);
                return RedirectToPage();
            }

            try
            {
                var request = await _context.Requests
                    .AsNoTracking()
                    .Include(r => r.Laboratory)
                    .Include(r => r.Equipment)
                        .ThenInclude(e => e!.City)
                    .Include(r => r.Equipment)
                        .ThenInclude(e => e!.Country)
                    .Include(r => r.RequestedBy)
                    .Include(r => r.EquipmentUnit)
                        .ThenInclude(u => u!.Laboratory)
                            .ThenInclude(l => l!.Faculty)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    TempData.Error("La solicitud no existe.");
                    return RedirectToPage();
                }

                var excelBytes = await _reportService.GenerateSolicitudMantenimientoExcel(request);
                var fileName = $"Solicitud_Mantenimiento_{id}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                TempData.Error(NotificationHelper.Requests.SaveError($"Error técnico: {ex.Message}"));
                return RedirectToPage();
            }
        }
    }
}
