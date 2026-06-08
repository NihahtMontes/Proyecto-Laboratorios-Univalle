using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Acquisitions
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly IReportService _reportService;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, IReportService reportService)
        {
            _context = context;
            _reportService = reportService;
        }

        public Request AcquisitionRequest { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.ApprovedBy)
                .Include(r => r.CreatedBy)
                .Include(r => r.Equipment)
                .Include(r => r.ModifiedBy)
                .Include(r => r.RequestedBy)
                .Include(r => r.Laboratory)
                .Include(r => r.EquipmentUnit)
                .Include(r => r.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();

            AcquisitionRequest = request;
            return Page();
        }

        public async Task<IActionResult> OnGetDescargarReporteAsync(int id)
        {
            if (id <= 0) return RedirectToPage(new { id = id });
            try
            {
                var excelBytes = await _reportService.GenerateReport(id);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_Adquisicion_{id}.xlsx");
            }
            catch (Exception ex)
            {
                TempData.Error("No se pudo generar el Excel: " + ex.Message);
                return RedirectToPage(new { id = id });
            }
        }
    }
}
