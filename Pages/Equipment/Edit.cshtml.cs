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
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IWebHostEnvironment environment,
            ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
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
            public UtensilType? UtensilType { get; set; }

            [Display(Name = "Clasificación de Tipo")]
            public EquipmentTypeClassification? TypeClassification { get; set; }

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
            [StringLength(100)]
            public string? Brand { get; set; }

            [Display(Name = "Modelo")]
            [StringLength(100)]
            public string? Model { get; set; }

            [Display(Name = "Vida Útil Estimada (Años)")]
            [Range(0, 100)]
            public int? UsefulLifeYears { get; set; }

            [Display(Name = "Descripción / Especificaciones")]
            [StringLength(2000)]
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
            if (!Enum.IsDefined(typeof(EquipmentCategory), Input.Category))
                ModelState.AddModelError("Input.Category", "Seleccione una categoría válida.");

            var imageValidationError = await SafeImageUpload.ValidateAsync(
                Input.ImageUpload,
                HttpContext.RequestAborted);

            if (imageValidationError != null)
                ModelState.AddModelError("Input.ImageUpload", imageValidationError);

            ValidateNotes();

            if (!ModelState.IsValid)
            {
                await ReloadDisplayData(Input.Id);
                LoadLists();
                return Page();
            }

            await ValidateLocationAsync();
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

            Input.ExistingImageUrl = equipment.ImageUrl;

            // Update Fields (Manteniendo tu lógica de .Clean())
            equipment.Name = Input.Name.Clean();

            var normalizedName = Input.Name.Clean();
            var normalizedModel = Input.Model?.Clean();
            var duplicateExists = await _context.Equipments.AnyAsync(candidate =>
                candidate.Id != equipment.Id &&
                candidate.Name.ToLower() == normalizedName.ToLower() &&
                (string.IsNullOrEmpty(normalizedModel) ||
                    (candidate.Model != null && candidate.Model.ToLower() == normalizedModel.ToLower())));

            if (duplicateExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe otro equipo registrado con este nombre y modelo.");
                await ReloadDisplayData(Input.Id);
                LoadLists();
                return Page();
            }

            equipment.Name = normalizedName;
            // La clasificación se conserva: solo el servicio auditable puede modificarla.

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

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "equipment");
            var oldImageUrl = equipment.ImageUrl;
            string? newImageUrl = null;
            string? newImagePath = null;

            try
            {
                if (Input.ImageUpload is { Length: > 0 })
                {
                    newImageUrl = await SafeImageUpload.SaveAsync(
                        Input.ImageUpload,
                        uploadsFolder,
                        HttpContext.RequestAborted);
                    newImagePath = Path.Combine(uploadsFolder, newImageUrl);
                    equipment.ImageUrl = newImageUrl;
                }

                await using var tx = await _context.Database.BeginTransactionAsync();

                await _context.SaveChangesAsync();

                var oldNotes = await _context.EquipmentNotes
                    .AsTracking()
                    .Where(n => n.EquipmentId == equipment.Id)
                    .ToListAsync();
                _context.EquipmentNotes.RemoveRange(oldNotes);

                foreach (var note in Input.Notes.Where(note => !string.IsNullOrWhiteSpace(note)))
                {
                    _context.EquipmentNotes.Add(new EquipmentNote
                    {
                        EquipmentId = equipment.Id,
                        Note = note.Clean()
                    });
                }

                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                if (newImageUrl != null && !string.IsNullOrWhiteSpace(oldImageUrl))
                {
                    try
                    {
                        SafeImageUpload.DeleteStoredFile(uploadsFolder, oldImageUrl);
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        _logger.LogWarning(ex, "No se pudo retirar la imagen anterior del equipo {EquipmentId}.", equipment.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeImageUpload.DeleteIfExists(newImagePath);
                _logger.LogError(ex, "No se pudo actualizar el equipo {EquipmentId}.", equipment.Id);
                TempData.Error("No se pudieron guardar los cambios del equipo. Revise los datos e intente nuevamente.");
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
            Input.ExistingImageUrl = ExistingEquipmentDisplay.ImageUrl;
        }

        private async Task ValidateLocationAsync()
        {
            if (!Input.CountryId.HasValue)
            {
                if (Input.CityId.HasValue)
                    ModelState.AddModelError("Input.CityId", "Seleccione el país correspondiente a la ciudad.");
                return;
            }

            var countryExists = await _context.Countries.AnyAsync(country =>
                country.Id == Input.CountryId.Value && country.Status == GeneralStatus.Activo);

            if (!countryExists)
                ModelState.AddModelError("Input.CountryId", "El país seleccionado no está disponible.");

            if (Input.CityId.HasValue)
            {
                var cityMatchesCountry = await _context.Cities.AnyAsync(city =>
                    city.Id == Input.CityId.Value &&
                    city.CountryId == Input.CountryId.Value &&
                    city.Status == GeneralStatus.Activo);

                if (!cityMatchesCountry)
                    ModelState.AddModelError("Input.CityId", "La ciudad no pertenece al país seleccionado.");
            }
        }

        private void ValidateNotes()
        {
            Input.Notes ??= new List<string>();
            if (Input.Notes.Count > 50)
                ModelState.AddModelError("Input.Notes", "No se permiten más de 50 notas por equipo.");

            if (Input.Notes.Any(note => note?.Length > 500))
                ModelState.AddModelError("Input.Notes", "Cada nota puede tener como máximo 500 caracteres.");
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

        private static List<SelectListItem> GetUtensilTypeOptions(UtensilType? selected)
        {
            return EquipmentClassificationRules.UtensilSubclassifications
                .Select(value => new SelectListItem
                {
                    Value = ((int)value).ToString(),
                    Text = EnumHelper.GetDisplayName(value),
                    Selected = value == selected
                })
                .ToList();
        }

        private static List<SelectListItem> GetEquipmentTypeClassificationOptions(EquipmentTypeClassification? selected)
        {
            return GetValidEquipmentClassifications()
                .Select(value => new SelectListItem
                {
                    Value = ((int)value).ToString(),
                    Text = EnumHelper.GetDisplayName(value),
                    Selected = value == selected
                })
                .ToList();
        }

        private static bool IsValidEquipmentClassification(EquipmentTypeClassification classification)
            => EquipmentClassificationRules.IsValidEquipmentSubclassification(classification);

        private static bool IsValidUtensilType(UtensilType utensilType)
            => EquipmentClassificationRules.IsValidUtensilSubclassification(utensilType);

        private static EquipmentTypeClassification[] GetValidEquipmentClassifications() =>
            EquipmentClassificationRules.EquipmentSubclassifications.ToArray();
    }
}
