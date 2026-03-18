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

        public IList<Request> Requests { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public RequestStatus? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public RequestPriority? PriorityFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Requests
                .Include(r => r.Equipment)
                // .ThenInclude(e => e!.EquipmentType) // CORRECCIÓN: Se elimina esta línea porque la relación ya no existe
                .Include(r => r.EquipmentUnit)
                .Include(r => r.RequestedBy)
                .Include(r => r.ApprovedBy)
                .Include(r => r.CreatedBy)
                .Include(r => r.ModifiedBy)
                .AsQueryable();

            // Apply search filter (Case-insensitive)
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

            // Apply status filter
            if (StatusFilter.HasValue)
            {
                query = query.Where(r => r.Status == StatusFilter.Value);
            }

            // Apply priority filter
            if (PriorityFilter.HasValue)
            {
                query = query.Where(r => r.Priority == PriorityFilter.Value);
            }

            Requests = await query
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
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
                var excelBytes = await _reportService.GenerateSolicitudMantenimientoExcel(id);
                var fileName = $"Solicitud_Mantenimiento_{id}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear(); // Evitar problemas de estado si hubo error en BD
                TempData.Error(NotificationHelper.Requests.SaveError($"Error técnico: {ex.Message}"));
                return RedirectToPage();
            }
        }
    }
}