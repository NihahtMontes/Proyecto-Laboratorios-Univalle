using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Acquisitions
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

        [BindProperty(SupportsGet = true)]
        public int? FilterLaboratoryId { get; set; }

        public Microsoft.AspNetCore.Mvc.Rendering.SelectList LaboratoryList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var query = _context.Requests
                .Where(r => r.Type == RequestType.Purchasing)
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
                    (r.Equipment != null && r.Equipment.Name.ToLower().Contains(term)) ||
                    (r.InvestmentCode != null && r.InvestmentCode.ToLower().Contains(term)) ||
                    (r.RequestedBy != null && (r.RequestedBy.FirstName.ToLower().Contains(term) || r.RequestedBy.LastName.ToLower().Contains(term)))
                );
            }

            if (StatusFilter.HasValue) query = query.Where(r => r.Status == StatusFilter.Value);
            if (PriorityFilter.HasValue) query = query.Where(r => r.Priority == PriorityFilter.Value);
            if (FilterLaboratoryId.HasValue) query = query.Where(r => r.LaboratoryId == FilterLaboratoryId.Value);

            Requests = await query.OrderByDescending(r => r.CreatedDate).ToListAsync();

            var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
            LaboratoryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(labs, "Id", "Name", FilterLaboratoryId);
        }

        public async Task<IActionResult> OnGetDescargarReporteAsync(int id)
        {
            if (id <= 0) return RedirectToPage();
            try
            {
                var excelBytes = await _reportService.GenerateReport(id);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Adquisicion_{id}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al generar reporte: " + ex.Message;
                return RedirectToPage();
            }
        }
    }
}
