using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Maintenance Maintenance { get; set; } = default!;

        public string FormatManagement(Management? management)
        {
            if (management == null) return "Gestión sin detalle";
            if (!string.IsNullOrWhiteSpace(management.Code)) return management.Code;
            return management.Type == Models.Enums.ManagementType.Corrective
                ? $"CORR-{management.Year}-{management.Semester}"
                : $"{management.Year}-{management.Semester}";
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance = await _context.Maintenances
                .Include(m => m.Management)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Equipment)
                .Include(m => m.EquipmentUnit)
                    .ThenInclude(eu => eu!.Laboratory)
                .Include(m => m.Technician)
                .Include(m => m.Request)
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .Include(m => m.CostDetails)
                .Include(m => m.Tasks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maintenance == null)
            {
                return NotFound();
            }
            else
            {
                Maintenance = maintenance;
            }
            ViewData["ManagementId"] = Maintenance.ManagementId;
            ViewData["ManagementType"] = Maintenance.Management?.Type.ToString();
            return Page();
        }
    }
}
