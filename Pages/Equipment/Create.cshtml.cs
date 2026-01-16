using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

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
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public int EquipmentTypeId { get; set; }
            [Required]
            public int LaboratoryId { get; set; }
            [Required]
            public int CityId { get; set; }
            [Required]
            public int CountryId { get; set; }
            [Required]
            public string Name { get; set; }
            [Required]
            public string InventoryNumber { get; set; }
            public string? Brand { get; set; }
            public string? Model { get; set; }
            public string? SerialNumber { get; set; }
            public int? UsefulLifeYears { get; set; }
            [DataType(DataType.Date)]
            public DateTime? AcquisitionDate { get; set; }
            public decimal? AcquisitionValue { get; set; }
            public string? Observations { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Uniqueness Check (Ignoring Deleted but filtering by status)
            bool invExists = await _context.Equipments
                .IgnoreQueryFilters()
                .AnyAsync(e => e.InventoryNumber == Input.InventoryNumber && e.CurrentStatus != EquipmentStatus.Deleted);

            if (invExists)
            {
                ModelState.AddModelError("Input.InventoryNumber", "El número de inventario ya está en uso por otro equipo activo.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["CityId"] = new SelectList(_context.Cities, "Id", "Name");
                ViewData["LaboratoryId"] = new SelectList(_context.Laboratories, "Id", "Name");
                ViewData["CountryId"] = new SelectList(_context.Countries, "Id", "Name");
                ViewData["EquipmentTypeId"] = new SelectList(_context.EquipmentTypes, "Id", "Name");
                return Page();
            }

            var equipment = new Models.Equipment
            {
                EquipmentTypeId = Input.EquipmentTypeId,
                LaboratoryId = Input.LaboratoryId,
                CityId = Input.CityId,
                CountryId = Input.CountryId,
                Name = Input.Name,
                InventoryNumber = Input.InventoryNumber,
                Brand = Input.Brand,
                Model = Input.Model,
                SerialNumber = Input.SerialNumber,
                UsefulLifeYears = Input.UsefulLifeYears,
                AcquisitionDate = Input.AcquisitionDate,
                AcquisitionValue = Input.AcquisitionValue,
                Observations = Input.Observations,
                CurrentStatus = EquipmentStatus.Operational,
                CreatedDate = DateTime.Now
            };

            _context.Equipments.Add(equipment);
            await _context.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            var initialHistory = new EquipmentStateHistory
            {
                EquipmentId = equipment.Id,
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
