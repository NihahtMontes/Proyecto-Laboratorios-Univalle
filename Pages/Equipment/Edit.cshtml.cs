using System;
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
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public EquipmentInputModel Input { get; set; } = new();

        public int Id { get; set; }

        public class EquipmentInputModel
        {
            public int EquipmentTypeId { get; set; }
            public int LaboratoryId { get; set; }
            public int CityId { get; set; }
            public int CountryId { get; set; } // agregado para bind del select País
            public string Name { get; set; } = string.Empty;
            public string InventoryNumber { get; set; } = string.Empty;
            public string? Brand { get; set; }
            public string? Model { get; set; }
            public string? SerialNumber { get; set; }
            public int? UsefulLifeYears { get; set; }
            public DateTime AcquisitionDate { get; set; }
            public decimal? AcquisitionValue { get; set; }
            public EquipmentStatus CurrentStatus { get; set; }
            public string? Observations { get; set; }
            // CountryId es ahora parte del Input para enlazar el select en la vista.
        }

        // We use this property to display ReadOnly info in the View (like Country/City names, dates)
        public Proyecto_Laboratorios_Univalle.Models.Equipment ExistingEquipmentDisplay { get; set; } = default!;


        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipment = await _context.Equipments
                .IgnoreQueryFilters() // Load Includes even if they are soft-deleted
                .Include(e => e.Country)
                .Include(e => e.City)
                .FirstOrDefaultAsync(m => m.Id == id && m.CurrentStatus != EquipmentStatus.Deleted);

            if (equipment == null) return NotFound();

            Id = equipment.Id;
            ExistingEquipmentDisplay = equipment; // For display purposes

            // Map Entity -> DTO
            Input = new EquipmentInputModel
            {
                EquipmentTypeId = equipment.EquipmentTypeId,
                LaboratoryId = equipment.LaboratoryId,
                CityId = equipment.CityId,
                CountryId = equipment.City?.CountryId ?? equipment.CountryId, // Prioritize derived Country from City
                Name = equipment.Name,
                InventoryNumber = equipment.InventoryNumber,
                Brand = equipment.Brand,
                Model = equipment.Model,
                SerialNumber = equipment.SerialNumber,
                UsefulLifeYears = equipment.UsefulLifeYears,
                AcquisitionDate = equipment.AcquisitionDate ?? DateTime.MinValue,
                AcquisitionValue = equipment.AcquisitionValue,
                CurrentStatus = equipment.CurrentStatus,
                Observations = equipment.Observations
            };

            CargarViewData();
            CargarEstadosEquipo();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Id = id;
            if (!ModelState.IsValid)
            {
                // Reload display data if validation fails
                var eq = await _context.Equipments
                    .IgnoreQueryFilters()
                    .Include(e=>e.Country)
                    .Include(e=>e.City)
                    .FirstOrDefaultAsync(e=>e.Id == id);
                if (eq != null) ExistingEquipmentDisplay = eq;

                CargarViewData();
                CargarEstadosEquipo();
                return Page();
            }

            // 1. Uniqueness Check (Ignoring Deleted)
            bool invExists = await _context.Equipments
                .IgnoreQueryFilters()
                .AnyAsync(e => e.InventoryNumber == Input.InventoryNumber && 
                               e.Id != id && 
                               e.CurrentStatus != EquipmentStatus.Deleted);

            if (invExists)
            {
                ModelState.AddModelError("Input.InventoryNumber", "El número de inventario ya está en uso por otro equipo.");
                // Reload display data
                var eq = await _context.Equipments
                    .IgnoreQueryFilters()
                    .Include(e=>e.Country)
                    .Include(e=>e.City)
                    .FirstOrDefaultAsync(e=>e.Id == id);
                if (eq != null) ExistingEquipmentDisplay = eq;

                CargarViewData();
                CargarEstadosEquipo();
                return Page();
            }

            var equipmentBD = await _context.Equipments
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id); // Also ignore filters to fetch invalid-state items for update

            if (equipmentBD == null) return NotFound();

            // Derive Country from City
            var city = await _context.Cities.FindAsync(Input.CityId);
            if (city != null)
            {
                equipmentBD.CountryId = city.CountryId;
            }

            // Capture status change for history
            bool statusChanged = equipmentBD.CurrentStatus != Input.CurrentStatus;

            // Update Fields
            equipmentBD.EquipmentTypeId = Input.EquipmentTypeId;
            equipmentBD.LaboratoryId = Input.LaboratoryId;
            equipmentBD.CityId = Input.CityId;
            equipmentBD.Name = Input.Name;
            equipmentBD.InventoryNumber = Input.InventoryNumber;
            equipmentBD.Brand = Input.Brand;
            equipmentBD.Model = Input.Model;
            equipmentBD.SerialNumber = Input.SerialNumber;
            equipmentBD.UsefulLifeYears = Input.UsefulLifeYears;
            equipmentBD.AcquisitionDate = Input.AcquisitionDate;
            equipmentBD.AcquisitionValue = Input.AcquisitionValue;
            equipmentBD.CurrentStatus = Input.CurrentStatus;
            equipmentBD.Observations = Input.Observations;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    await _context.SaveChangesAsync();

                    if (statusChanged)
                    {
                        var currentUser = await _userManager.GetUserAsync(User);
                        int? userId = currentUser?.Id;

                        var lastHistory = await _context.EquipmentStateHistories
                            .Where(h => h.EquipmentId == id && h.EndDate == null)
                            .OrderByDescending(h => h.StartDate)
                            .FirstOrDefaultAsync();

                        if (lastHistory != null)
                        {
                            lastHistory.EndDate = DateTime.Now;
                            _context.EquipmentStateHistories.Update(lastHistory);
                        }

                        var newHistory = new EquipmentStateHistory
                        {
                            EquipmentId = id,
                            Status = Input.CurrentStatus,
                            StartDate = DateTime.Now,
                            CreatedDate = DateTime.Now,
                            CreatedById = userId,
                            Reason = "Status/Info updated via Edit" 
                        };
                        _context.EquipmentStateHistories.Add(newHistory);

                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        // Endpoint para obtener ciudades por país (llamado desde JS)
        public async Task<JsonResult> OnGetCitiesAsync(int countryId)
        {
            var cities = await _context.Cities
                .Where(c => (c.CountryId == countryId && c.Status == GeneralStatus.Activo))
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return new JsonResult(cities);
        }

        private void CargarViewData()
        {
            // Poblamos países - aseguro incluir el country actual aunque no sea activo
            ViewData["CountryId"] = new SelectList(
                _context.Countries
                    .Where(c => c.Status == GeneralStatus.Activo || (Input != null && c.Id == Input.CountryId))
                    .OrderBy(c => c.Name),
                "Id",
                "Name",
                Input?.CountryId
            );

            // Si tenemos CountryId, limitar ciudades al país seleccionado; si no, mostrar todas activas
            if (Input != null && Input.CountryId > 0)
            {
                ViewData["CityId"] = new SelectList(
                    _context.Cities
                        .Where(c => (c.Status == GeneralStatus.Activo && c.CountryId == Input.CountryId) || c.Id == Input.CityId)
                        .OrderBy(c => c.Name),
                    "Id",
                    "Name",
                    Input.CityId
                );
            }
            else
            {
                ViewData["CityId"] = new SelectList(
                    _context.Cities
                        .Where(c => c.Status == GeneralStatus.Activo || (Input != null && c.Id == Input.CityId))
                        .OrderBy(c => c.Name),
                    "Id",
                    "Name",
                    Input?.CityId
                );
            }

            ViewData["LaboratoryId"] = new SelectList(
                _context.Laboratories.Where(l => l.Status == GeneralStatus.Activo || (Input != null && l.Id == Input.LaboratoryId)),
                "Id",
                "Name",
                Input?.LaboratoryId
            );

            ViewData["EquipmentTypeId"] = new SelectList(
                _context.EquipmentTypes,
                "Id",
                "Name",
                Input?.EquipmentTypeId
            );
        }

        private void CargarEstadosEquipo()
        {
            var estados = EnumHelper.GetStatusSelectList<EquipmentStatus>();
            ViewData["ListaEstadosEquipo"] = new SelectList(
                estados, 
                "Value", 
                "Text", 
                (int)Input.CurrentStatus
            );
        }
    }
}