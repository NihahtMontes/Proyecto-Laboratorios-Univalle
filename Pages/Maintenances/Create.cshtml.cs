using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            // Providing Equipment Name for selection
            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
            // Requests
            ViewData["RequestId"] = new SelectList(_context.Requests, "Id", "Id"); // Assuming Request has Id and maybe Description/Date, using Id for now
            // Technicians only
            ViewData["TechnicianId"] = new SelectList(_context.Users.Where(u => u.Role == UserRole.Tecnico), "Id", "FullName");
            // Maintenance Types
            ViewData["MaintenanceTypeId"] = new SelectList(_context.MaintenanceTypes, "Id", "Name");
            
            // Categories for Cost Details
            // ViewData["CategoryList"] = EnumHelper.ToSelectList<CostCategory>(); // Assuming EnumHelper exists, or we use Html.GetEnumSelectList in View.
            // Using a simple workaround just in case EnumHelper is specialized
            
            Maintenance = new Maintenance
            {
                CostDetails = new List<CostDetail>()
            };

            return Page();
        }

        [BindProperty]
        public Maintenance Maintenance { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove navigation properties from validation
            ModelState.Remove("Maintenance.CreatedBy");
            ModelState.Remove("Maintenance.ModifiedBy");
            ModelState.Remove("Maintenance.Equipment");
            ModelState.Remove("Maintenance.MaintenanceType");
            ModelState.Remove("Maintenance.Technician");
            ModelState.Remove("Maintenance.Request");
            ModelState.Remove("Maintenance.CostDetails"); // Validation of children separately? Or implicitly handled.

            // Validate that if status is Completed, it must have cost details?
            // The original logic checked: if Completed && Costs <= 0 -> Error.
            decimal totalCosts = Maintenance.CostDetails?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;
            
            if (Maintenance.Status == MaintenanceStatus.Completed && totalCosts <= 0)
            {
                 // Allow completion without costs? Usually specific logic. Keeping original rule.
                ModelState.AddModelError("Maintenance.Status", "Cannot complete maintenance without cost details.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
                ViewData["RequestId"] = new SelectList(_context.Requests, "Id", "Id");
                ViewData["TechnicianId"] = new SelectList(_context.Users.Where(u => u.Role == UserRole.Tecnico), "Id", "FullName");
                ViewData["MaintenanceTypeId"] = new SelectList(_context.MaintenanceTypes, "Id", "Name");
                return Page();
            }

            Maintenance.ActualCost = totalCosts;
            
            // Sync cost details FKs? handled by EF usually effectively.
            
            _context.Maintenances.Add(Maintenance);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
