using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Management Management { get; set; } = default!;

        // Dashboard Metrics
        public int TotalPlans { get; set; }
        public int CompletedPlans { get; set; }
        public double GlobalProgress { get; set; }

        public Dictionary<string, int> TopEquipmentTypes { get; set; } = new();
        public Dictionary<string, int> TopGroups { get; set; } = new();
        public Dictionary<string, int> TopLaboratories { get; set; } = new();
        
        public IList<ManagementPlan> OverduePlans { get; set; } = new List<ManagementPlan>();

        // Plan Table L-48
        public IList<ManagementPlan> ManagementPlans { get; set; } = new List<ManagementPlan>();

        // Filters applied
        [BindProperty(SupportsGet = true)]
        public string? SearchResponsible { get; set; }

        [BindProperty(SupportsGet = true)]
        public ManagementPlanStatus? FilterStatus { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Managements == null)
            {
                return NotFound();
            }

            var m = await _context.Managements
                .Include(mg => mg.ManagementPlans)
                    .ThenInclude(p => p.EquipmentUnit)
                        .ThenInclude(eu => eu.Equipment)
                .Include(mg => mg.ManagementPlans)
                    .ThenInclude(p => p.EquipmentUnit)
                        .ThenInclude(eu => eu.Laboratory)
                .Include(mg => mg.ManagementPlans)
                    .ThenInclude(p => p.Maintenance)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (m == null)
            {
                return NotFound();
            }
            Management = m;

            // 1. Calculate Global Progress
            TotalPlans = Management.ManagementPlans.Count;
            CompletedPlans = Management.ManagementPlans.Count(p => p.PlanStatus == ManagementPlanStatus.Completed);
            GlobalProgress = TotalPlans > 0 ? Math.Round((double)CompletedPlans / TotalPlans * 100, 1) : 0;

            // 2. Metrics (Equipment Types - All to allow toggle)
            TopEquipmentTypes = Management.ManagementPlans
                .Where(p => p.EquipmentUnit?.Equipment != null)
                .GroupBy(p => p.EquipmentUnit.Equipment.Category.ToString())
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());

            // 3. Metrics (Groups / TypeClassifications - All)
            TopGroups = Management.ManagementPlans
                .Where(p => p.EquipmentUnit?.Equipment != null)
                .GroupBy(p => p.EquipmentUnit.Equipment.TypeClassification.ToString())
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());

            // 4. Metrics (Laboratories - All)
            TopLaboratories = Management.ManagementPlans
                .Where(p => p.EquipmentUnit?.Laboratory != null)
                .GroupBy(p => p.EquipmentUnit.Laboratory.Name)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());

            // 4. Overdue (Vencidos o próximos a vencer - mock logic relying on Planned Date in past)
            OverduePlans = Management.ManagementPlans
                .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < DateTime.Now.AddDays(7))
                .OrderBy(p => p.PlannedDate)
                .ToList();

            // 5. Build query for the details list with filters
            var query = _context.ManagementPlans
                .Include(p => p.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .Include(p => p.EquipmentUnit)
                    .ThenInclude(eu => eu.Laboratory)
                .Where(p => p.ManagementId == id);

            if (!string.IsNullOrEmpty(SearchResponsible))
            {
                query = query.Where(p => p.Responsible.Contains(SearchResponsible));
            }

            if (FilterStatus.HasValue)
            {
                query = query.Where(p => p.PlanStatus == FilterStatus.Value);
            }

            ManagementPlans = await query.ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSyncEquipmentsAsync(int id)
        {
            var management = await _context.Managements
                .Include(m => m.ManagementPlans)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (management == null || management.Status != ManagementStatus.Active)
            {
                TempData.Error("No se puede sincronizar una gestión que no está activa.");
                return RedirectToPage(new { id });
            }

            // Obtener todos los IDs de equipos ya en esta gestión
            var existingEquipmentIds = management.ManagementPlans
                .Select(p => p.EquipmentUnitId)
                .Where(id => id.HasValue)
                .Cast<int>()
                .ToList();

            // Obtener equipos activos no incluidos aún
            var newEquipments = await _context.EquipmentUnits
                .Where(eu => eu.CurrentStatus != EquipmentStatus.Deleted && !existingEquipmentIds.Contains(eu.Id))
                .ToListAsync();

            if (!newEquipments.Any())
            {
                TempData.Info("Todos los equipos activos ya están incluidos en esta ronda.");
                return RedirectToPage(new { id });
            }

            foreach (var eu in newEquipments)
            {
                var plan = new ManagementPlan
                {
                    ManagementId = id,
                    EquipmentUnitId = eu.Id,
                    CurrentPhase = WizardPhase.Verification,
                    CurrentState = WizardEquipmentState.PendingVerification,
                    PlanStatus = ManagementPlanStatus.Pending,
                    CreatedDate = DateTime.UtcNow,
                    PlannedDate = DateTime.Today.AddDays(7) // Valor por defecto
                };
                _context.ManagementPlans.Add(plan);
            }

            await _context.SaveChangesAsync();
            TempData.Success($"Se han sincronizado {newEquipments.Count} equipos nuevos a esta ronda.");

            return RedirectToPage(new { id });
        }
    }
}
