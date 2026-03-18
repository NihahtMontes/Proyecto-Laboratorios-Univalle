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
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        private readonly IWebHostEnvironment _environment;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public IActionResult OnGet()
        {
            LoadLists();
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "La categoría es obligatoria")]
            [Display(Name = "Tipo de Recurso")]
            public EquipmentCategory Category { get; set; }

            [Display(Name = "Tipo de Material")]
            public UtensilType UtensilType { get; set; }

            //[Display(Name = "Tipo de Equipo")]
            //public int? EquipmentTypeId { get; set; }

            [Display(Name = "Imagen del Equipo")]
            public IFormFile? ImageUpload { get; set; }

            [Display(Name = "País de Origen")]
            public int? CountryId { get; set; }

            // City removed from UI requirement
            public int? CityId { get; set; }

            [Required(ErrorMessage = "El nombre del equipo es obligatorio")]
            [Display(Name = "Nombre del Equipo")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Marca")]
            public string? Brand { get; set; }

            [Display(Name = "Modelo")]
            public string? Model { get; set; }

            [Display(Name = "Vida Útil Estimada (Años)")]
            [Range(0, 50, ErrorMessage = "La vida útil debe estar entre 0 y 50 años")]
            public int? UsefulLifeYears { get; set; }

            [Display(Name = "Descripción / Especificaciones")]
            public string? Description { get; set; }

            [Required(ErrorMessage = "La clasificación técnica es obligatoria")]
            [Display(Name = "Clasificación de Tipo")]
            public EquipmentTypeClassification TypeClassification { get; set; } = EquipmentTypeClassification.Otro;
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
                LoadLists();
                return Page();
            }

            // Normalization
            Input.Name = Input.Name.Clean();
            Input.Brand = Input.Brand?.Clean();
            Input.Model = Input.Model?.Clean();
            Input.Description = Input.Description?.Clean();

            // Duplicate Check (Name + Model)
            var normalizedName = Input.Name.ToLower();
            var normalizedModel = Input.Model?.ToLower();

            var exists = await _context.Equipments
                .AnyAsync(e => e.Name.ToLower() == normalizedName &&
                               (string.IsNullOrEmpty(Input.Model) || e.Model.ToLower() == normalizedModel));

            if (exists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe un equipo registrado con este nombre y modelo.");
                LoadLists();
                return Page();
            }

            // Image Upload Handling
            string? uniqueFileName = null;
            if (Input.ImageUpload != null)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "equipment");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // Sanitizar nombre de archivo para evitar caracteres inválidos
                string safeFileName = Path.GetFileName(Input.ImageUpload.FileName);
                uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ImageUpload.CopyToAsync(fileStream);
                }
            }

            var equipment = new Models.Equipment
            {
                Category = Input.Category,
                UtensilType = Input.UtensilType, // Asignamos el nuevo Enum
                TypeClassification = Input.TypeClassification, // CORRECCIÓN: Se añade el mapeo de la clasificación dinámica

                //EquipmentTypeId = Input.EquipmentTypeId,
                ImageUrl = uniqueFileName,
                CountryId = Input.CountryId,
                CityId = Input.CityId,
                Name = Input.Name,
                Brand = Input.Brand,
                Model = Input.Model,
                UsefulLifeYears = Input.UsefulLifeYears,
                Description = Input.Description,
                CreatedDate = DateTime.UtcNow
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                equipment.CreatedById = currentUser.Id;
            }

            _context.Equipments.Add(equipment);
            await _context.SaveChangesAsync();

            TempData.Success($"Definición de '{equipment.Name}' registrada correctamente.");
            return RedirectToPage("./Details", new { id = equipment.Id });
        }

        private void LoadLists()
        {
            //ViewData["EquipmentTypeId"] = new SelectList(_context.EquipmentTypes.OrderBy(et => et.Name), "Id", "Name");

            var countries = _context.Countries
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Name)
                .ToList();
            ViewData["CountryId"] = new SelectList(countries, "Id", "Name");
            ViewData["CityId"] = new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text");
        }
    }
}