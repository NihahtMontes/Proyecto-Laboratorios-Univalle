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

            // AÑADIDO: Soporte para el Enum de material/utensilio
            [Display(Name = "Tipo de Material")]
            public UtensilType UtensilType { get; set; }

            [Required(ErrorMessage = "La clasificación técnica es obligatoria")]
            [Display(Name = "Clasificación de Tipo")]
            public EquipmentTypeClassification TypeClassification { get; set; } = EquipmentTypeClassification.Otro;

            // ELIMINADO: EquipmentTypeId ya no se utiliza

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

            [Display(Name = "Notas del Fabricante")]
            public List<string> Notes { get; set; } = new();
        }

        public Models.Equipment ExistingEquipmentDisplay { get; set; } = default!;
        public List<SelectListItem> UtensilTypeOptions { get; set; } = new();
        public List<SelectListItem> EquipmentTypeClassificationOptions { get; set; } = new();

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
                UtensilType = equipment.UtensilType,
                TypeClassification = equipment.TypeClassification,

                ExistingImageUrl = equipment.ImageUrl,
                CountryId = equipment.CountryId,
                CityId = equipment.CityId,
                Name = equipment.Name,
                Brand = equipment.Brand,
                Model = equipment.Model,
                UsefulLifeYears = equipment.UsefulLifeYears,
                Description = equipment.Description,
                Notes = await _context.EquipmentNotes
                    .Where(n => n.EquipmentId == equipment.Id)
                    .Select(n => n.Note)
                    .ToListAsync()
            };

            LoadLists();
            ViewData["ReturnUrl"] = Request.Query["returnUrl"].ToString();
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
                .AsTracking()
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

            // Update Fields (Manteniendo tu lógica de .Clean())
            equipment.Name = Input.Name.Clean();
            equipment.Category = Input.Category;
            if (Input.Category == EquipmentCategory.Equipment && !IsValidEquipmentClassificationForEdit(Input.TypeClassification, equipment.TypeClassification))
            {
                ModelState.AddModelError("Input.TypeClassification", "Seleccione una clasificacion tecnica valida para equipo.");
                await ReloadDisplayData(Input.Id);
                LoadLists();
                return Page();
            }

            if (Input.Category == EquipmentCategory.Utensil && !IsValidUtensilTypeForEdit(Input.UtensilType, equipment.UtensilType))
            {
                ModelState.AddModelError("Input.UtensilType", "Seleccione una subclasificacion de utensilio.");
                await ReloadDisplayData(Input.Id);
                LoadLists();
                return Page();
            }

            if (Input.Category == EquipmentCategory.Other)
            {
                Input.UtensilType = UtensilType.NoAplica;
                Input.TypeClassification = EquipmentTypeClassification.Otro;
            }
            else if (Input.Category == EquipmentCategory.Equipment)
            {
                Input.UtensilType = UtensilType.NoAplica;
            }
            else if (Input.Category == EquipmentCategory.Utensil)
            {
                Input.TypeClassification = EquipmentTypeClassification.Otro;
            }
            equipment.UtensilType = Input.UtensilType; // Actualizamos el Enum
            equipment.TypeClassification = Input.TypeClassification;

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

            try
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                await _context.SaveChangesAsync();

                var oldNotes = await _context.EquipmentNotes.Where(n => n.EquipmentId == equipment.Id).ToListAsync();
                _context.EquipmentNotes.RemoveRange(oldNotes);
                if (Input.Notes != null)
                {
                    foreach (var note in Input.Notes.Where(n => !string.IsNullOrWhiteSpace(n)))
                        _context.EquipmentNotes.Add(new EquipmentNote { EquipmentId = equipment.Id, Note = note.Trim() });
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                TempData.Error($"Error al guardar los cambios: {ex.Message}");
                await ReloadDisplayData(Input.Id);
                LoadLists();
                return Page();
            }

            TempData.Success($"Datos del equipo '{equipment.Name}' actualizados correctamente.");
            return RedirectToPage("./Details", new { id = equipment.Id });
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
            // ELIMINADA: La carga de EquipmentTypes (ya no es necesaria)

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

            UtensilTypeOptions = GetUtensilTypeOptions(Input.UtensilType);
            EquipmentTypeClassificationOptions = GetEquipmentTypeClassificationOptions(Input.TypeClassification);
        }

        private static List<SelectListItem> GetUtensilTypeOptions(UtensilType selected)
        {
            var options = new List<UtensilType>
            {
                UtensilType.NoAplica,
                UtensilType.MenajeCocina,
                UtensilType.BartendingBarismo,
                UtensilType.PanaderiaReposteriaPasteleria,
                UtensilType.Servicio,
                UtensilType.Manteleria,
                UtensilType.VajillaGeneral,
                UtensilType.Otros
            };

            if (selected is UtensilType.Vidrio or UtensilType.Plastico or UtensilType.Metal or UtensilType.Porcelana)
            {
                options.Add(selected);
            }

            return options
                .Distinct()
                .Select(value => new SelectListItem
                {
                    Value = ((int)value).ToString(),
                    Text = EnumHelper.GetDisplayName(value),
                    Selected = value == selected
                })
                .ToList();
        }

        private static List<SelectListItem> GetEquipmentTypeClassificationOptions(EquipmentTypeClassification selected)
        {
            var options = GetValidEquipmentClassifications().ToList();
            if (!options.Contains(selected))
            {
                options.Add(selected);
            }

            return options
                .Distinct()
                .Select(value => new SelectListItem
                {
                    Value = ((int)value).ToString(),
                    Text = EnumHelper.GetDisplayName(value),
                    Selected = value == selected
                })
                .ToList();
        }

        private static bool IsValidEquipmentClassificationForEdit(EquipmentTypeClassification posted, EquipmentTypeClassification original)
            => GetValidEquipmentClassifications().Contains(posted) || posted == original;

        private static bool IsValidUtensilTypeForEdit(UtensilType posted, UtensilType original)
            => IsValidNewUtensilType(posted) || (IsLegacyUtensilType(posted) && posted == original);

        private static bool IsValidNewUtensilType(UtensilType utensilType)
            => utensilType is UtensilType.MenajeCocina
                or UtensilType.BartendingBarismo
                or UtensilType.PanaderiaReposteriaPasteleria
                or UtensilType.Servicio
                or UtensilType.Manteleria
                or UtensilType.VajillaGeneral
                or UtensilType.Otros;

        private static bool IsLegacyUtensilType(UtensilType utensilType)
            => utensilType is UtensilType.Vidrio
                or UtensilType.Plastico
                or UtensilType.Metal
                or UtensilType.Porcelana;

        private static EquipmentTypeClassification[] GetValidEquipmentClassifications() => new[]
        {
            EquipmentTypeClassification.Calor,
            EquipmentTypeClassification.Frio,
            EquipmentTypeClassification.Congelacion,
            EquipmentTypeClassification.Ultracongelacion,
            EquipmentTypeClassification.MaquinasRotativas,
            EquipmentTypeClassification.Electronico,
            EquipmentTypeClassification.SeguridadIndustrial,
            EquipmentTypeClassification.Medicion,
            EquipmentTypeClassification.Audiovisuales,
            EquipmentTypeClassification.Electricos,
            EquipmentTypeClassification.Otro
        };
    }
}
