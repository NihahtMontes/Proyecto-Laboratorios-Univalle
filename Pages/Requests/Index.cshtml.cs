using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        [BindProperty(SupportsGet = true)]
        public int? FilterLaboratoryId { get; set; }

        // ========================================
        // NUEVO: PROPIEDADES PARA REPORTE L-7
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
                // CORRECCIÓN: Búsqueda segura sin usar FirstName/LastName directamente en SQL si da problemas
                query = query.Where(r =>
                    r.Description.ToLower().Contains(term) ||
                    (r.Equipment != null && r.Equipment.Name.ToLower().Contains(term)) ||
                    (r.EquipmentUnit != null && r.EquipmentUnit.InventoryNumber.ToLower().Contains(term))
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

            if (FilterLaboratoryId.HasValue)
            {
                query = query.Where(r => r.EquipmentUnit != null && r.EquipmentUnit.LaboratoryId == FilterLaboratoryId.Value);
            }

            Requests = await query
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            // CARGA DE LISTAS PARA LOS FILTROS Y EL REPORTE
            var labs = await _context.Laboratories.OrderBy(l => l.Name).ToListAsync();
            LaboratoryList = new SelectList(labs, "Id", "Name", FilterLaboratoryId);

            var managements = await _context.Managements.OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester).ToListAsync();
            ManagementList = new SelectList(managements, "Id", "Code");

            // Carga segura de personal (Igual que en Verificaciones)
            var people = await _context.People.ToListAsync();
            PersonnelList = new SelectList(people.Select(p => new {
                Id = p.Id,
                Name = p.FullName != "Ficha de Persona" ? p.FullName : (p.Email ?? $"Personal #{p.Id}")
            }), "Id", "Name");
        }

        // ========================================
        // NUEVO: ACCIÓN PARA GENERAR REPORTE L-7
        // ========================================
        public async Task<IActionResult> OnPostGenerateL7Async()
        {
            // 1. Replicamos la lógica de filtrado de OnGet para obtener la MISMA lista de la UI
            var query = _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.EquipmentUnit)
                .AsQueryable();

            if (FilterLaboratoryId.HasValue)
                query = query.Where(r => r.EquipmentUnit.LaboratoryId == FilterLaboratoryId.Value);

            // Filtramos solo las de tipo Técnico (L-7)
            query = query.Where(r => r.Type == RequestType.Technical);

            var listaFiltrada = await query.ToListAsync();

            // 2. Generamos el reporte con la lista real
            var fileBytes = await _reportService.GenerateL7Report(
                ReportInput.LaboratoryId ?? 0,
                ReportInput.ManagementId ?? 0,
                ReportInput.ResponsibleId ?? 0,
                listaFiltrada
            );

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte_L7_RealTime.xlsx");
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
                _context.ChangeTracker.Clear();
                TempData.Error(NotificationHelper.Requests.SaveError($"Error técnico: {ex.Message}"));
                return RedirectToPage();
            }
        }
    }
}