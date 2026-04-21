using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly IReportService _reportService;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, IReportService reportService)
        {
            _context = context;
            _reportService = reportService;
        }

        public IList<Maintenance> Maintenances { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public Models.Enums.MaintenanceStatus? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLaboratoryId { get; set; }

        // ========================================
        // PROPIEDADES PÚBLICAS PARA EL REPORTE L-8
        // ========================================
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
            // Cargar lista de laboratorios
            var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
            LaboratoryList = new SelectList(labs, "Id", "Name");

            // Cargar gestiones para el reporte
            var managements = await _context.Managements.OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester).ToListAsync();
            ManagementList = new SelectList(managements, "Id", "Code");

            // Cargar personal de forma segura (usando FullName del modelo Person)
            var people = await _context.People.ToListAsync();
            PersonnelList = new SelectList(people.Select(p => new {
                Id = p.Id,
                Name = p.FullName != "Ficha de Persona" ? p.FullName : (p.Email ?? $"Personal #{p.Id}")
            }), "Id", "Name");

            var query = _context.Maintenances
                .Include(m => m.CreatedBy)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Laboratory)
                .Include(m => m.ModifiedBy)
                .Include(m => m.Technician)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(m =>
                    m.EquipmentUnit!.Equipment!.Name.ToLower().Contains(term) ||
                    m.EquipmentUnit.InventoryNumber.ToLower().Contains(term) ||
                    (m.Technician != null && m.Technician.FullName.ToLower().Contains(term))
                );
            }

            if (StatusFilter.HasValue)
            {
                query = query.Where(m => m.Status == StatusFilter.Value);
            }

            if (SelectedLaboratoryId.HasValue)
            {
                query = query.Where(m => m.EquipmentUnit!.LaboratoryId == SelectedLaboratoryId.Value);
            }

            Maintenances = await query
                .OrderBy(m => m.EquipmentUnit!.Equipment!.Name)
                .ToListAsync();
        }

        // ========================================
        // ACCIÓN PARA GENERAR REPORTE L-8 (KARDEX)
        // ========================================
        public async Task<IActionResult> OnPostGenerateL8Async()
        {
            if (ReportInput.LaboratoryId == null || ReportInput.ManagementId == null || ReportInput.ResponsibleId == null)
            {
                TempData["Error"] = "Complete los parámetros (Laboratorio, Gestión y Responsable) para generar el Kardex L-8.";
                return RedirectToPage();
            }

            try
            {
                // Obtenemos el historial real filtrado por laboratorio para el Excel
                var query = _context.Maintenances
                    .Include(m => m.EquipmentUnit)
                        .ThenInclude(eu => eu!.Equipment)
                    .Include(m => m.Technician)
                    .Where(m => m.EquipmentUnit!.LaboratoryId == ReportInput.LaboratoryId)
                    .AsQueryable();

                // CORRECCIÓN: Cambiado 'Date' por 'ScheduledDate' o 'CreatedDate' según disponibilidad
                // Usamos una expresión que el proveedor de base de datos pueda traducir
                var historial = await query
                    .OrderByDescending(m => m.ScheduledDate ?? m.CreatedDate)
                    .ToListAsync();

                if (!historial.Any())
                {
                    TempData["Error"] = "No se encontraron mantenimientos para el laboratorio seleccionado.";
                    return RedirectToPage();
                }

                var fileBytes = await _reportService.GenerateL8Report(
                    ReportInput.LaboratoryId.Value,
                    ReportInput.ManagementId.Value,
                    ReportInput.ResponsibleId.Value,
                    historial
                );

                var lab = await _context.Laboratories.FindAsync(ReportInput.LaboratoryId);
                string fileName = $"Kardex_L8_{lab?.Name ?? "General"}_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al generar el Kardex L-8: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}