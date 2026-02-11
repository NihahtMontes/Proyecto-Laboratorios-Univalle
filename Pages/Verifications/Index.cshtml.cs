using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services.Reporting;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly IVerificationReportService _reportingService;

        public IndexModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            IVerificationReportService reportingService)
        {
            _context = context;
            _reportingService = reportingService;
        }

        public IList<Verification> Verifications { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        // Reporte L-6 Input
        [BindProperty]
        public ReportInputModel ReportInput { get; set; } = new();

        public class ReportInputModel
        {
            public int? LaboratoryId { get; set; }
            public string Term { get; set; } = "II/2025";
            public string Responsible { get; set; }
        }

        public Microsoft.AspNetCore.Mvc.Rendering.SelectList LaboratoryList { get; set; }

        public async Task OnGetAsync()
        {
            IQueryable<Verification> verificationIQ = _context.Verifications
                .Include(v => v.CreatedBy)
                .Include(v => v.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .Include(v => v.ModifiedBy);

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                verificationIQ = verificationIQ.Where(s => s.EquipmentUnit.Equipment.Name.ToLower().Contains(term) 
                                       || s.EquipmentUnit.InventoryNumber.ToLower().Contains(term));
            }

            Verifications = await verificationIQ.OrderByDescending(v => v.Date).ToListAsync();

            // Cargar lista de laboratorios para el reporte
            var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
            LaboratoryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(labs, "Id", "Name");
        }

        public async Task<IActionResult> OnPostGenerateReportAsync()
        {
            if (ReportInput.LaboratoryId == null)
            {
                return RedirectToPage();
            }

            try
            {
                var lab = await _context.Laboratories.FindAsync(ReportInput.LaboratoryId);
                if (lab == null) return NotFound();

                // 1. Get Equipment Units for the lab
                var units = await _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .IgnoreQueryFilters()
                    .Where(u => u.LaboratoryId == ReportInput.LaboratoryId && u.CurrentStatus != Models.Enums.EquipmentStatus.Deleted)
                    .OrderBy(u => u.InventoryNumber) 
                    .ToListAsync();

                // 2. Get LATEST Verification for each equipment unit
                var latestVerifications = await _context.Verifications
                    .Where(v => v.EquipmentUnit.LaboratoryId == ReportInput.LaboratoryId)
                    .GroupBy(v => v.EquipmentUnitId)
                    .Select(g => g.OrderByDescending(v => v.Date).First())
                    .ToDictionaryAsync(v => v.EquipmentUnitId, v => v);

                // 3. Map Verification Data to Equipment Unit
                foreach (var unit in units)
                {
                    if (latestVerifications.TryGetValue(unit.Id, out var ver))
                    {
                        unit.Notes = ver.Observations; // Using Notes field for report observations
                    }
                }

                var fileContent = _reportingService.GenerateLaboratoryReport(
                    lab.Name, 
                    units, 
                    ReportInput.Term ?? "II/2025", 
                    ReportInput.Responsible ?? "N/A", 
                    DateTime.Now
                );

                string fileName = $"Reporte_L6_{lab.Name}_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                TempData["Error"] = $"Error al generar el reporte L-6: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            try 
            {
                IQueryable<Verification> verificationIQ = _context.Verifications
                    .Include(v => v.CreatedBy)
                    .Include(v => v.EquipmentUnit)
                        .ThenInclude(eu => eu.Equipment)
                    .Include(v => v.ModifiedBy);

                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    var term = SearchTerm.Trim().ToLower();
                    verificationIQ = verificationIQ.Where(s => s.EquipmentUnit.Equipment.Name.ToLower().Contains(term) 
                                           || s.EquipmentUnit.InventoryNumber.ToLower().Contains(term));
                }

                var list = await verificationIQ.OrderByDescending(v => v.Date).ToListAsync();
                var excelBytes = _reportingService.GenerateVerificationsExcel(list);
                
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    $"Verificaciones_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar listado: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}
