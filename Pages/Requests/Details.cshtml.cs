using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
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

        public Request Request { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.ApprovedBy)
                .Include(r => r.CreatedBy)
                .Include(r => r.Equipment)
                    .ThenInclude(e => e!.EquipmentType)
                .Include(r => r.ModifiedBy)
                .Include(r => r.RequestedBy)
                .Include(r => r.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();
            
            Request = request;
            return Page();
        }

        public async Task<IActionResult> OnGetDescargarReporteAsync(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "Identificador de solicitud no válido.";
                return RedirectToPage(new { id = id });
            }

            try
            {
                // Determinar tipo para el nombre del archivo
                var requestType = await _context.Requests
                    .Where(r => r.Id == id)
                    .Select(r => r.Type)
                    .FirstOrDefaultAsync();

                var prefix = requestType == RequestType.Purchasing ? "Solicitud_Adquisicion" : "Solicitud_Mantenimiento";
                
                var excelBytes = await _reportService.GenerateReport(id);
                var fileName = $"{prefix}_{id}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                TempData["Error"] = $"No se pudo generar el Excel: {ex.Message}";
                return RedirectToPage(new { id = id });
            }
        }
    }
}
