using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Maintenances
{
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Maintenance Maintenance { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var maintenance =  await _context.Maintenances
                .Include(m => m.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (maintenance == null)
            {
                return NotFound();
            }
            Maintenance = maintenance;

            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
            ViewData["RequestId"] = new SelectList(_context.Requests, "Id", "Id"); // Assuming description is not available yet, using Id
            ViewData["TechnicianId"] = new SelectList(_context.Users.Where(u => u.Role == UserRole.Technician), "Id", "FullName");
            ViewData["MaintenanceTypeId"] = new SelectList(_context.MaintenanceTypes, "Id", "Name");
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
             // Remove navigation properties from validation
            ModelState.Remove("Maintenance.CreatedBy");
            ModelState.Remove("Maintenance.ModifiedBy");
            ModelState.Remove("Maintenance.Equipment");
            ModelState.Remove("Maintenance.MaintenanceType");
            ModelState.Remove("Maintenance.Technician");
            ModelState.Remove("Maintenance.Request");
            // Note: CostDetails might need manual handling if they are bound from client

            decimal totalCosts = Maintenance.CostDetails?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;

            if (Maintenance.Status == MaintenanceStatus.Completed && totalCosts <= 0)
            {
               // Warn if completing without costs, logic from original code
               // Or maybe it is allowed? Original code said "No se puede completar..."
                 ModelState.AddModelError("Maintenance.Status", "Cannot complete maintenance without cost details.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
                ViewData["RequestId"] = new SelectList(_context.Requests, "Id", "Id");
                ViewData["TechnicianId"] = new SelectList(_context.Users.Where(u => u.Role == UserRole.Technician), "Id", "FullName");
                ViewData["MaintenanceTypeId"] = new SelectList(_context.MaintenanceTypes, "Id", "Name");
                return Page();
            }

            Maintenance.ActualCost = totalCosts;
            Maintenance.LastModifiedDate = DateTime.Now;
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                Maintenance.ModifiedById = currentUser.Id;
            }

            var maintenanceDB = await _context.Maintenances
                .Include(m => m.CostDetails)
                .FirstOrDefaultAsync(m => m.Id == Maintenance.Id);

            if (maintenanceDB == null) return NotFound();

            _context.Entry(maintenanceDB).CurrentValues.SetValues(Maintenance);

            // Sync Cost Details
            // 1. Delete removed
            foreach (var existingDetail in maintenanceDB.CostDetails.ToList())
            {
                if (!Maintenance.CostDetails.Any(d => d.Id == existingDetail.Id))
                {
                    _context.Remove(existingDetail);
                }
            }

            // 2. Add or Update
            foreach (var detailForm in Maintenance.CostDetails)
            {
                detailForm.MaintenanceId = Maintenance.Id; // Ensure FK
                
                var existingDetail = maintenanceDB.CostDetails.FirstOrDefault(d => d.Id == detailForm.Id && d.Id != 0);
                if (existingDetail != null)
                {
                    _context.Entry(existingDetail).CurrentValues.SetValues(detailForm);
                }
                else
                {
                    maintenanceDB.CostDetails.Add(detailForm);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MaintenanceExists(Maintenance.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool MaintenanceExists(int id)
        {
            return _context.Maintenances.Any(e => e.Id == id);
        }
    }
}
