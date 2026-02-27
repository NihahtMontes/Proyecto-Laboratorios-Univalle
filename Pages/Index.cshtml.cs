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

        // --- Core Stats ---
        public int TotalEquipment { get; set; }
        public int PendingRequests { get; set; }
        public int OngoingMaintenances { get; set; }
        public int RecentVerificationsCount { get; set; }

        // --- Quality Stats ---
        public int OperationalCount { get; set; }
        public int MaintenanceCount { get; set; }
        public int OutOfServiceCount { get; set; }
        public int OperationalPercent { get; set; }

        // --- Lists for Activity ---
        public List<EquipmentUnit> CriticalEquipment { get; set; } = new();
        public List<Request> LatestRequests { get; set; } = new();
        public List<Maintenance> ActiveMaintenances { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToPage("/Login");
            }

            // Calculations
            TotalEquipment = await _context.EquipmentUnits.CountAsync();
            PendingRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.Pending);
            OngoingMaintenances = await _context.Maintenances.CountAsync(m => m.Status == MaintenanceStatus.InProgress);
            
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            RecentVerificationsCount = await _context.Verifications.CountAsync(v => v.Date >= oneWeekAgo);

            // Detailed Equipment Stats
            OperationalCount = await _context.EquipmentUnits.CountAsync(u => u.CurrentStatus == EquipmentStatus.Operational);
            MaintenanceCount = await _context.EquipmentUnits.CountAsync(u => u.CurrentStatus == EquipmentStatus.UnderMaintenance);
            OutOfServiceCount = await _context.EquipmentUnits.CountAsync(u => u.CurrentStatus == EquipmentStatus.OutOfService);

            if (TotalEquipment > 0)
            {
                OperationalPercent = (OperationalCount * 100) / TotalEquipment;
            }

            // Critical Lists
            CriticalEquipment = await _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Include(u => u.Laboratory) // Location is now on the unit
                .Where(u => u.CurrentStatus == EquipmentStatus.OutOfService)
                .OrderByDescending(u => u.AcquisitionValue)
                .Take(5)
                .ToListAsync();

            LatestRequests = await _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.EquipmentUnit)
                .Include(r => r.RequestedBy)
                .OrderByDescending(r => r.CreatedDate)
                .Take(6)
                .ToListAsync();

            ActiveMaintenances = await _context.Maintenances
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu != null ? eu.Equipment : null)
                .Include(m => m.MaintenanceType)
                .Where(m => m.Status == MaintenanceStatus.InProgress || m.Status == MaintenanceStatus.Scheduled)
                .OrderBy(m => m.ScheduledDate)
                .Take(5)
                .ToListAsync();

            return Page();
        }
    }
}
