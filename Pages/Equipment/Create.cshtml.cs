using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Equipment
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]

    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ojalá que detecte
        public IActionResult OnGet()
        {
            ViewData["CityId"] = new SelectList(_context.Cities, "Id", "Name");
            ViewData["LaboratoryId"] = new SelectList(_context.Laboratories, "Id", "Name");
            ViewData["CountryId"] = new SelectList(_context.Countries, "Id", "Name");
            ViewData["EquipmentTypeId"] = new SelectList(_context.EquipmentTypes, "Id", "Name");
            return Page();
        }

        [BindProperty]
        public Proyecto_Laboratorios_Univalle.Models.Equipment Equipment { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Equipment.CreatedBy");
            ModelState.Remove("Equipment.ModifiedBy");
            ModelState.Remove("Equipment.Laboratory");
            ModelState.Remove("Equipment.EquipmentType");
            ModelState.Remove("Equipment.Country");
            ModelState.Remove("Equipment.City");
            ModelState.Remove("Equipment.EquipmentStateHistories");

            // Uniqueness Check (Ignoring Deleted but filtering by status)
            bool invExists = await _context.Equipments
                .IgnoreQueryFilters()
                .AnyAsync(e => e.InventoryNumber == Equipment.InventoryNumber && e.CurrentStatus != EquipmentStatus.Deleted);

            if (invExists)
            {
                ModelState.AddModelError("Equipment.InventoryNumber", "El número de inventario ya está en uso por otro equipo activo.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["CityId"] = new SelectList(_context.Cities, "Id", "Name");
                ViewData["LaboratoryId"] = new SelectList(_context.Laboratories, "Id", "Name");
                ViewData["CountryId"] = new SelectList(_context.Countries, "Id", "Name");
                ViewData["EquipmentTypeId"] = new SelectList(_context.EquipmentTypes, "Id", "Name");
                return Page();
            }

            Equipment.CurrentStatus = EquipmentStatus.Operational; // Default status

            _context.Equipments.Add(Equipment);
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var initialHistory = new EquipmentStateHistory
            {
                EquipmentId = Equipment.Id,
                Status = EquipmentStatus.Operational,
                StartDate = DateTime.Now,
                CreatedDate = DateTime.Now,
                CreatedById = currentUser?.Id,
                Reason = "Initial equipment registration"
            };

            _context.EquipmentStateHistories.Add(initialHistory);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
