using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Laboratorios_Univalle.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- Active Management ---
        public Management? ActiveManagement { get; set; }
        
        // --- Core Stats ---
        public int TotalEquipment { get; set; }
        public int PendingRequests { get; set; }
        public int OngoingMaintenances { get; set; }
        public int RecentVerificationsCount { get; set; }

        // --- Active Management Metrics ---
        public Dictionary<string, int> LabProgress { get; set; } = new();
        public Dictionary<string, int> TypeProgress { get; set; } = new();
        public Dictionary<string, int> GroupProgress { get; set; } = new();
        public List<ManagementPlan> OverduePlans { get; set; } = new();
        public List<ManagementPlan> UpcomingPlans { get; set; } = new();
        public List<ManagementPlan> RecentActivity { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToPage("/Login");
            }

            // 1. Core General Stats (Module 1/4/9)
            TotalEquipment = await _context.EquipmentUnits.CountAsync();
            PendingRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.Pending);
            OngoingMaintenances = await _context.Maintenances.CountAsync(m => m.Status == MaintenanceStatus.InProgress);
            
            var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
            RecentVerificationsCount = await _context.Verifications.CountAsync(v => v.Date >= oneWeekAgo);

            // 2. Locate Active Management (Module 2 - L-48)
            ActiveManagement = await _context.Managements
                .Include(m => m.ManagementPlans)
                    .ThenInclude(p => p.EquipmentUnit)
                        .ThenInclude(eu => eu.Equipment)
                .Include(m => m.ManagementPlans)
                    .ThenInclude(p => p.EquipmentUnit)
                        .ThenInclude(eu => eu.Laboratory)
                .FirstOrDefaultAsync(m => m.Status == ManagementStatus.Activo);

            if (ActiveManagement != null)
            {
                // Calculate Metrics for Dashboard display
                LabProgress = ActiveManagement.ManagementPlans
                    .Where(p => p.EquipmentUnit?.Laboratory != null)
                    .GroupBy(p => p.EquipmentUnit.Laboratory.Name)
                    .ToDictionary(g => g.Key, g => (int)Math.Round((double)g.Count(p => p.PlanStatus == ManagementPlanStatus.Completed) / g.Count() * 100));

                TypeProgress = ActiveManagement.ManagementPlans
                    .Where(p => p.EquipmentUnit?.Equipment != null)
                    .GroupBy(p => p.EquipmentUnit.Equipment.Category.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                GroupProgress = ActiveManagement.ManagementPlans
                    .Where(p => p.EquipmentUnit?.Equipment != null)
                    .GroupBy(p => p.EquipmentUnit.Equipment.TypeClassification.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                OverduePlans = ActiveManagement.ManagementPlans
                    .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value < DateTime.Now)
                    .OrderBy(p => p.PlannedDate)
                    .Take(3)
                    .ToList();

                UpcomingPlans = ActiveManagement.ManagementPlans
                    .Where(p => p.PlanStatus != ManagementPlanStatus.Completed && p.PlannedDate.HasValue && p.PlannedDate.Value >= DateTime.Now)
                    .OrderBy(p => p.PlannedDate)
                    .Take(3)
                    .ToList();

                RecentActivity = ActiveManagement.ManagementPlans
                    .OrderByDescending(p => p.Id) // Mock for real logs if they exist
                    .Take(5)
                    .ToList();
            }

            return Page();
        }
    }
}