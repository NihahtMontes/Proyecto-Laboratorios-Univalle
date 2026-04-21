using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Departures
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;

        public IndexModel(ApplicationDbContext context, IReportService reportService)
        {
            _context = context;
            _reportService = reportService;
        }

        public IList<Departure> Departures { get; set; } = new List<Departure>();

        [BindProperty]
        public ReportInputModel ReportInput { get; set; } = new();

        public class ReportInputModel
        {
            public int? LaboratoryId { get; set; }
            public int? ManagementId { get; set; }
            public int? ResponsibleId { get; set; }
        }

        public SelectList LaboratoryList { get; set; } = default!;
        public SelectList ManagementList { get; set; } = default!;
        public SelectList PersonnelList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
            LaboratoryList = new SelectList(labs, "Id", "Name");

            var managements = await _context.Managements.OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester).ToListAsync();
            ManagementList = new SelectList(managements, "Id", "Code");

            var people = await _context.People.ToListAsync();
            PersonnelList = new SelectList(people.Select(p => new {
                Id = p.Id,
                Name = p.FullName != "Ficha de Persona" ? p.FullName : (p.Email ?? $"Personal #{p.Id}")
            }), "Id", "Name");

            Departures = await _context.Departures
                .Include(d => d.EquipmentUnit).ThenInclude(u => u!.Equipment)
                .Include(d => d.Borrower)
                .OrderByDescending(d => d.DepartureDate)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostGenerateL3Async()
        {
            if (ReportInput.LaboratoryId == null || ReportInput.ManagementId == null || ReportInput.ResponsibleId == null)
            {
                TempData["Error"] = "Complete los parámetros para el reporte L-3.";
                return RedirectToPage();
            }

            try
            {
                // CONSULTA EN TIEMPO REAL CON TODAS LAS RELACIONES
                var listaSalidas = await _context.Departures
                    .Include(d => d.EquipmentUnit)
                        .ThenInclude(eu => eu!.Equipment) // ESTO TRAE EL NOMBRE "ESTUFA"
                    .Include(d => d.Borrower) // ESTO TRAE AL SOLICITANTE
                    .Where(d => d.EquipmentUnit!.LaboratoryId == ReportInput.LaboratoryId &&
                                d.ManagementId == ReportInput.ManagementId)
                    .OrderByDescending(d => d.DepartureDate)
                    .ToListAsync();

                if (!listaSalidas.Any())
                {
                    TempData["Error"] = "No hay datos registrados para esta combinación de Laboratorio y Gestión.";
                    return RedirectToPage();
                }

                var fileBytes = await _reportService.GenerateL3Report(
                    ReportInput.LaboratoryId.Value,
                    ReportInput.ManagementId.Value,
                    ReportInput.ResponsibleId.Value,
                    listaSalidas
                );

                var lab = await _context.Laboratories.FindAsync(ReportInput.LaboratoryId);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_L3_{lab?.Name ?? "General"}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error técnico: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}