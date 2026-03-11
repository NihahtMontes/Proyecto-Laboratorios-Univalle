
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
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Equipment
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        private readonly IWebHostEnvironment _environment;

        public EditModel(ApplicationDbContext context, UserManager<User> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [BindProperty]
        public EquipmentInputModel Input { get; set; } = new();

        public class EquipmentInputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "La categoría es obligatoria")]
            [Display(Name = "Tipo de Recurso")]
            public EquipmentCategory Category { get; set; }

            [Display(Name = "Tipo de Equipo")]
            public int? EquipmentTypeId { get; set; }



            [Display(Name = "Imagen del Equipo")]
            public IFormFile? ImageUpload { get; set; }
            public string? ExistingImageUrl { get; set; }

            [Display(Name = "País / Sede de Origen")]
            public int? CountryId { get; set; }

            public int? CityId { get; set; }

            [Required(ErrorMessage = "El nombre del equipo es obligatorio")]
            [Display(Name = "Nombre")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Marca")]
            public string? Brand { get; set; }

            [Display(Name = "Modelo")]
            public string? Model { get; set; }

            [Display(Name = "Vida Útil Estimada (Años)")]
            public int? UsefulLifeYears { get; set; }

            [Display(Name = "Descripción / Especificaciones")]
            public string? Description { get; set; }
        }

        public Models.Equipment ExistingEquipmentDisplay { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipment = await _context.Equipments
                .IgnoreQueryFilters()
                .Include(e => e.CreatedBy)
                .Include(e => e.ModifiedBy)
                .Include(e => e.Units) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipment == null) return NotFound();

            ExistingEquipmentDisplay = equipment;

            Input = new EquipmentInputModel
            {
                Id = equipment.Id,
                Category = equipment.Category,
                EquipmentTypeId = equipment.EquipmentTypeId,

                ExistingImageUrl = equipment.ImageUrl,
                CountryId = equipment.CountryId,
                CityId = equipment.CityId,
                Name = equipment.Name,
                Brand = equipment.Brand,
                Model = equipment.Model,
                UsefulLifeYears = equipment.UsefulLifeYears,
                Description = equipment.Description
            };

            LoadLists();
            return Page();
        }

        public async Task<JsonResult> OnGetCitiesByCountryAsync(int countryId)
        {
            var cities = await _context.Cities
                .Where(c => c.CountryId == countryId && c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Name)
                .Select(c => new { value = c.Id, text = c.Name })
                .ToListAsync();
            return new JsonResult(cities);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadDisplayData(Input.Id);
                LoadLists();
                return Page();
            }

            var equipment = await _context.Equipments
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == Input.Id); 

            if (equipment == null) return NotFound();

            // Image Upload Handling
            if (Input.ImageUpload != null)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "equipment");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.ImageUpload.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ImageUpload.CopyToAsync(fileStream);
                }
                
                equipment.ImageUrl = uniqueFileName;
            }

            // Update Fields
            equipment.Name = Input.Name.Clean();
            equipment.Category = Input.Category;
            equipment.EquipmentTypeId = Input.EquipmentTypeId;

            equipment.CountryId = Input.CountryId;
            equipment.CityId = Input.CityId;
            equipment.Brand = Input.Brand?.Clean();
            equipment.Model = Input.Model?.Clean();
            equipment.UsefulLifeYears = Input.UsefulLifeYears;
            equipment.Description = Input.Description?.Clean();
            equipment.LastModifiedDate = DateTime.UtcNow;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                equipment.ModifiedById = currentUser.Id;
            }

            await _context.SaveChangesAsync();

            TempData.Success($"Datos del equipo '{equipment.Name}' actualizados correctamente.");
            return RedirectToPage("./Index");
        }

        private async Task ReloadDisplayData(int id)
        {
            ExistingEquipmentDisplay = await _context.Equipments
                .IgnoreQueryFilters()
                .Include(e => e.CreatedBy)
                .Include(e => e.ModifiedBy)
                .Include(e => e.Units)
                .FirstOrDefaultAsync(e => e.Id == id) ?? new Models.Equipment();
        }

        private void LoadLists()
        {
            ViewData["EquipmentTypeId"] = new SelectList(_context.EquipmentTypes.OrderBy(et => et.Name), "Id", "Name", Input.EquipmentTypeId);
            
            var countries = _context.Countries
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Name)
                .ToList();
            ViewData["CountryId"] = new SelectList(countries, "Id", "Name", Input.CountryId);

            if (Input.CountryId.HasValue)
            {
                var cities = _context.Cities
                    .Where(c => c.CountryId == Input.CountryId.Value && c.Status == GeneralStatus.Activo)
                    .OrderBy(c => c.Name)
                    .ToList();
                ViewData["CityId"] = new SelectList(cities, "Id", "Name", Input.CityId);
            }
            else
            {
                ViewData["CityId"] = new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text");
            }
        }
    }
}