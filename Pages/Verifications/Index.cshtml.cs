using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
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

        public PaginatedList<SessionGroup> Sessions { get; set; } = default!;

        public int PageSize { get; set; } = 15;

        [BindProperty(SupportsGet = true)]
        public int? FilterLaboratoryId { get; set; }

        [BindProperty]
        public ReportInputModel ReportInput { get; set; } = new();

        public class ReportInputModel
        {
            public int? LaboratoryId { get; set; }
            public string Term { get; set; } = "II/2025";
            public string Responsible { get; set; } = string.Empty;
        }

        public class SessionGroup
        {
            public DateTime Date { get; set; }
            public int LaboratoryId { get; set; }
            public string LaboratoryName { get; set; } = string.Empty;
            public int TotalEquipments { get; set; }
            public int BadCount { get; set; }
            public string InspectorName { get; set; } = string.Empty;
        }

        public Microsoft.AspNetCore.Mvc.Rendering.SelectList LaboratoryList { get; set; } = default!;

        public async Task OnGetAsync(int? pageIndex)
        {
            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Name)
                .ToListAsync();
            LaboratoryList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(labs, "Id", "Name");

            if (!FilterLaboratoryId.HasValue)
            {
                Sessions = new PaginatedList<SessionGroup>(new List<SessionGroup>(), 0, 1, PageSize);
                return;
            }

            var query = _context.Verifications
                .Include(v => v.EquipmentUnit).ThenInclude(eu => eu!.Laboratory)
                .Include(v => v.CreatedBy)
                .Where(v => v.EquipmentUnit!.LaboratoryId == FilterLaboratoryId.Value)
                .GroupBy(v => v.Date.Date)
                .Select(g => new SessionGroup
                {
                    Date = g.Key,
                    LaboratoryId = FilterLaboratoryId.Value,
                    LaboratoryName = labs.FirstOrDefault(l => l.Id == FilterLaboratoryId.Value)!.Name,
                    TotalEquipments = g.Count(),
                    BadCount = g.Count(v => v.PhysicalCondition == PhysicalCondition.Bad),
                    InspectorName = g.First().CreatedBy != null
                        ? g.First().CreatedBy!.FirstName + " " + g.First().CreatedBy!.LastName
                        : "Sistema"
                })
                .OrderByDescending(s => s.Date);

            Sessions = await PaginatedList<SessionGroup>.CreateAsync(query, pageIndex ?? 1, PageSize);
        }

        public async Task<IActionResult> OnPostGenerateReportAsync()
        {
            if (ReportInput.LaboratoryId == null)
                return RedirectToPage();

            try
            {
                var lab = await _context.Laboratories.FindAsync(ReportInput.LaboratoryId);
                if (lab == null) return NotFound();

                var units = await _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .IgnoreQueryFilters()
                    .Where(u => u.LaboratoryId == ReportInput.LaboratoryId && u.CurrentStatus != EquipmentStatus.Deleted)
                    .OrderBy(u => u.InventoryNumber)
                    .ToListAsync();

                var latestVerifications = await _context.Verifications
                    .Where(v => v.EquipmentUnit!.LaboratoryId == ReportInput.LaboratoryId)
                    .GroupBy(v => v.EquipmentUnitId)
                    .Select(g => g.OrderByDescending(v => v.Date).First())
                    .ToDictionaryAsync(v => v.EquipmentUnitId, v => v);

                foreach (var unit in units)
                {
                    if (latestVerifications.TryGetValue(unit.Id, out var ver))
                        unit.Notes = ver.Observations;
                }

                var fileContent = _reportingService.GenerateLaboratoryReport(
                    lab.Name, units, ReportInput.Term ?? "II/2025",
                    ReportInput.Responsible ?? "N/A", DateTime.UtcNow);

                string fileName = $"Reporte_L6_{lab.Name}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                TempData.Error($"Error al generar el reporte L-6: {ex.Message}");
                return RedirectToPage();
            }
        }
    }
}
