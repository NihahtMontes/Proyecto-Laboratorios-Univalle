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
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
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
        public Request MaintenanceRequest { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request =  await _context.Requests.FirstOrDefaultAsync(m => m.Id == id);
            if (request == null)
            {
                return NotFound();
            }
            MaintenanceRequest = request;

            ViewData["ApprovedById"] = new SelectList(_context.Users.Where(e=> e.Role == UserRole.Director), "Id", "FullName");
            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
            ViewData["RequestedById"] = new SelectList(_context.Users, "Id", "FullName");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            MaintenanceRequest.LastModifiedDate = DateTime.Now;
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                MaintenanceRequest.ModifiedById = currentUser.Id;
            }
            
            ModelState.Remove("MaintenanceRequest.CreatedBy");
            ModelState.Remove("MaintenanceRequest.ModifiedBy");
            ModelState.Remove("MaintenanceRequest.Equipment");
            ModelState.Remove("MaintenanceRequest.RequestedBy");
            ModelState.Remove("MaintenanceRequest.ApprovedBy");

            if (!ModelState.IsValid)
            {
                ViewData["ApprovedById"] = new SelectList(_context.Users.Where(e=> e.Role == UserRole.Director), "Id", "FullName");
                ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
                ViewData["RequestedById"] = new SelectList(_context.Users, "Id", "FullName");
                return Page();
            }

            _context.Attach(MaintenanceRequest).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RequestExists(MaintenanceRequest.Id))
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

        private bool RequestExists(int id)
        {
            return _context.Requests.Any(e => e.Id == id);
        }
    }
}
