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

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
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
            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
            // Assuming RequestedBy is usually the current user or selectable from list. Original used all users.
            ViewData["RequestedById"] = new SelectList(_context.Users, "Id", "FullName");
            return Page();
        }

        [BindProperty]
        public Request MaintenanceRequest { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // Clear Validation
            ModelState.Remove("MaintenanceRequest.CreatedBy");
            ModelState.Remove("MaintenanceRequest.ModifiedBy");
            ModelState.Remove("MaintenanceRequest.Equipment");
            ModelState.Remove("MaintenanceRequest.RequestedBy");
            ModelState.Remove("MaintenanceRequest.ApprovedBy");

            if (!ModelState.IsValid)
            {
                ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
                ViewData["RequestedById"] = new SelectList(_context.Users, "Id", "FullName");
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // Typically default requested by to current user if not set? 
                if (MaintenanceRequest.RequestedById == null || MaintenanceRequest.RequestedById == 0)
                {
                     MaintenanceRequest.RequestedById = currentUser.Id;
                }
            }

            _context.Requests.Add(MaintenanceRequest);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
