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
            // Cargar ciudades con su país concatenado: "Cali, Colombia"
            var cities = _context.Cities
                .Include(c => c.Country)
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Country.Name)
                .ThenBy(c => c.Name)
                .Select(c => new 
                { 
                    Id = c.Id, 
                    FullName = $"{c.Name}, {c.Country.Name}" 
                })
                .ToList();

            var laboratories = _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Name)
                .Select(l => new 
                { 
                    Id = l.Id, 
                    DisplayName = $"[{l.Code}] - {l.Name}" 
                })
                .ToList();

            ViewData["CityId"] = new SelectList(cities, "Id", "FullName");
            ViewData["LaboratoryId"] = new SelectList(laboratories, "Id", "DisplayName");
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
                // Reload lists on error
                var cities = _context.Cities
                .Include(c => c.Country)
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Country.Name)
                .ThenBy(c => c.Name)
                .Select(c => new 
                { 
                    Id = c.Id, 
                    FullName = $"{c.Name}, {c.Country.Name}" 
                })
                .ToList();

                var laboratories = _context.Laboratories
                    .Where(l => l.Status == GeneralStatus.Activo)
                    .OrderBy(l => l.Name)
                    .Select(l => new 
                    { 
                        Id = l.Id, 
                        DisplayName = $"[{l.Code}] - {l.Name}" 
                    })
                    .ToList();

                ViewData["CityId"] = new SelectList(cities, "Id", "FullName");
                ViewData["LaboratoryId"] = new SelectList(laboratories, "Id", "DisplayName");
                ViewData["EquipmentTypeId"] = new SelectList(_context.EquipmentTypes, "Id", "Name");
                return Page();
            }

            // Normalización
            Input.Name = Input.Name.Clean();
            Input.Brand = Input.Brand?.Clean();
            Input.Model = Input.Model?.Clean();
            
            // Deducir CountryId desde la Ciudad seleccionada
            var city = await _context.Cities.FindAsync(Input.CityId);
            if (city == null) return Page(); // Should not happen with valid modelstate

            var equipment = new Models.Equipment
            {
                EquipmentTypeId = Input.EquipmentTypeId,
                LaboratoryId = Input.LaboratoryId,
                CityId = Input.CityId,
                CountryId = city.CountryId, // Auto-asignado
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

            TempData["SuccessMessage"] = "Equipo registrado exitosamente.";
            return RedirectToPage("./Index");
        }
    }
}
