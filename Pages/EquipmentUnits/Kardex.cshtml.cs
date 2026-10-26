using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentUnits
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class KardexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public KardexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public EquipmentUnit Unit { get; set; } = default!;
        /// <summary>Línea de tiempo de estados: el tramo vigente primero y luego del más reciente al más antiguo.</summary>
        public List<EquipmentStateHistory> StateHistory { get; set; } = new();

        /// <summary>Tramo vigente (EndDate == null) del historial, si existe.</summary>
        public EquipmentStateHistory? CurrentSegment => StateHistory.FirstOrDefault(h => !h.EndDate.HasValue);
        public List<Maintenance> History { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public bool IsWizard { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLabId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ReturnStep { get; set; } = 5;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var unit = await _context.EquipmentUnits
                .AsNoTracking()
                .Include(u => u.Equipment)
                .Include(u => u.Laboratory)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null) return NotFound();
            Unit = unit;

            StateHistory = await _context.EquipmentStateHistories
                .AsNoTracking()
                .Include(h => h.CreatedBy)
                .Include(h => h.ModifiedBy)
                .Where(h => h.EquipmentUnitId == id)
                .OrderByDescending(h => h.EndDate == null)
                .ThenByDescending(h => h.StartDate)
                .ThenByDescending(h => h.Id)
                .ToListAsync();

            History = await _context.Maintenances
                .AsNoTracking()
                .Include(m => m.Management)
                .Include(m => m.Technician)
                .Include(m => m.CostDetails)
                .Include(m => m.Tasks)
                .Include(m => m.CreatedBy)
                .Where(m => m.EquipmentUnitId == id)
                .OrderByDescending(m => m.EndDate ?? m.StartDate ?? m.ScheduledDate ?? m.CreatedDate)
                .ThenByDescending(m => m.Id)
                .ToListAsync();

            return Page();
        }

        public static string GetEquipmentStatusBadge(Models.Enums.EquipmentStatus status) => status switch
        {
            Models.Enums.EquipmentStatus.Operational => "badge-success",
            Models.Enums.EquipmentStatus.UnderMaintenance or Models.Enums.EquipmentStatus.InRepair => "badge-warning",
            Models.Enums.EquipmentStatus.OutOfService or Models.Enums.EquipmentStatus.Broken => "badge-danger",
            Models.Enums.EquipmentStatus.OnLoan => "badge-info",
            // Datos históricos: "Por confirmar" se renderiza neutro (badge-light),
            // evitando confundirlo con "desconocido" (badge-secondary).
            Models.Enums.EquipmentStatus.PorConfirmar => "badge-light",
            _ => "badge-secondary"
        };

        /// <summary>Duración legible de un tramo del historial.</summary>
        public static string FormatDuration(EquipmentStateHistory state)
        {
            if (state.DurationDays is not int days || days < 0) return "—";
            return days == 0 ? "menos de 1 día" : days == 1 ? "1 día" : $"{days} días";
        }

        public static string GetMaintenanceStatusBadge(Models.Enums.MaintenanceStatus status) => status switch
        {
            Models.Enums.MaintenanceStatus.Completed => "badge-success",
            Models.Enums.MaintenanceStatus.InProgress => "badge-warning",
            Models.Enums.MaintenanceStatus.Scheduled => "badge-info",
            Models.Enums.MaintenanceStatus.Pending => "badge-secondary",
            Models.Enums.MaintenanceStatus.Cancelled => "badge-danger",
            _ => "badge-secondary"
        };

        public static string FormatManagement(Management? management)
        {
            if (management == null) return "Gestión sin detalle";
            if (!string.IsNullOrWhiteSpace(management.Code)) return management.Code;
            return management.Type == Models.Enums.ManagementType.Corrective
                ? $"CORR-{management.Year}-{management.Semester}"
                : $"{management.Year}-{management.Semester}";
        }
    }
}
