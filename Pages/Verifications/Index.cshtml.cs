using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;
using Proyecto_Laboratorios_Univalle.Services.Reporting;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly IReportService _reportService;
        private readonly IVerificationReportService _reportingService;

        public IndexModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            IReportService reportService,
            IVerificationReportService reportingService)
        {
            _context = context;
            _reportService = reportService;
            _reportingService = reportingService;
        }

        public IList<Verification> Verifications { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterLaboratoryId { get; set; }

        [BindProperty]
        public ReportInputModel ReportInput { get; set; } = new();

        public class ReportInputModel
        {
            public int? LaboratoryId { get; set; }
            public int? ManagementId { get; set; }
            public int? ResponsibleId { get; set; }
            public string Term { get; set; } = "2026";
            public string Responsible { get; set; } = string.Empty;
        }

        public SelectList LaboratoryList { get; set; } = default!;
        public SelectList ManagementList { get; set; } = default!;
        public SelectList PersonnelList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            IQueryable<Verification> verificationIQ = _context.Verifications
                .Include(v => v.CreatedBy)
                .Include(v => v.EquipmentUnit)
                    .ThenInclude(eu => eu != null ? eu.Equipment : null)
                .Include(v => v.ModifiedBy);

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                verificationIQ = verificationIQ.Where(s => s.EquipmentUnit.Equipment.Name.ToLower().Contains(term)
                                       || s.EquipmentUnit.InventoryNumber.ToLower().Contains(term));
            }

            if (FilterLaboratoryId.HasValue)
            {
                verificationIQ = verificationIQ.Where(v => v.EquipmentUnit.LaboratoryId == FilterLaboratoryId.Value);
            }

            Verifications = await verificationIQ.OrderByDescending(v => v.Date).ToListAsync();

            var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
            LaboratoryList = new SelectList(labs, "Id", "Name");

            var man = await _context.Managements.OrderByDescending(m => m.StartDate).ToListAsync();
            // CORRECCIÓN: Cambiamos "Name" por "Code" para la lista de Gestiones
            ManagementList = new SelectList(man, "Id", "Code");

            var people = await _context.People.ToListAsync();
            var interns = await _context.Interns.ToListAsync();
            var externs = await _context.Externs.ToListAsync();

            var personnelData = people.Select(p => {
                var intern = interns.FirstOrDefault(i => i.Id == p.Id);
                var @extern = externs.FirstOrDefault(e => e.Id == p.Id);
                return new
                {
                    Id = p.Id,
                    Name = intern?.Name ?? @extern?.Name ?? p.Email ?? $"Personal #{p.Id}"
                };
            }).OrderBy(x => x.Name).ToList();

            PersonnelList = new SelectList(personnelData, "Id", "Name");
        }

        public async Task<IActionResult> OnPostGenerateReportAsync()
        {
            if (ReportInput.LaboratoryId == null || ReportInput.ManagementId == null || ReportInput.ResponsibleId == null)
            {
                TempData["Error"] = "Debe seleccionar Laboratorio, Gestión y Responsable.";
                return RedirectToPage();
            }

            try
            {
                var fileContent = await _reportService.GenerateL6Report(
                    ReportInput.LaboratoryId.Value,
                    ReportInput.ManagementId.Value,
                    ReportInput.ResponsibleId.Value
                );

                var lab = await _context.Laboratories.FindAsync(ReportInput.LaboratoryId);
                string fileName = $"Formulario_L6_{lab?.Name ?? "Reporte"}_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
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
                        .ThenInclude(eu => eu != null ? eu.Equipment : null)
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
                    $"Verificaciones_{DateTime.UtcNow:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar listado: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}