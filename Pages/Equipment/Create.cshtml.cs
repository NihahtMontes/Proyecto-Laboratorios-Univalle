using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Services;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Equipment
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CreateModel> _logger;
        private readonly IEquipmentClassificationDecisionService _classificationService;

        public CreateModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            UserManager<User> userManager,
            IWebHostEnvironment environment,
            ILogger<CreateModel> logger,
            IEquipmentClassificationDecisionService classificationService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
            _classificationService = classificationService;
        }

        public IActionResult OnGet()
        {
            LoadLists();
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> UtensilTypeOptions { get; set; } = new();
        public List<SelectListItem> EquipmentTypeClassificationOptions { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "La categoría es obligatoria")]
            [Display(Name = "Tipo de Recurso")]
            public EquipmentCategory? Category { get; set; }

            [Display(Name = "Tipo de Material")]
            public UtensilType? UtensilType { get; set; }

            [Required(ErrorMessage = "El código de catálogo es obligatorio")]
            [StringLength(30)]
            [RegularExpression(@"^CAT-\d{4,}$", ErrorMessage = "Use el formato CAT-0001.")]
            [Display(Name = "Código de catálogo")]
            public string CatalogCode { get; set; } = string.Empty;

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
            [StringLength(100)]
            public string? Brand { get; set; }

            [Display(Name = "Modelo")]
            [StringLength(100)]
            public string? Model { get; set; }

            [Display(Name = "Vida Útil Estimada (Años)")]
            [Range(0, 50, ErrorMessage = "La vida útil debe estar entre 0 y 50 años")]
            public int? UsefulLifeYears { get; set; }

            [Display(Name = "Descripción / Especificaciones")]
            [StringLength(2000)]
            public string? Description { get; set; }

            [Display(Name = "Notas del Fabricante")]
            public List<string> Notes { get; set; } = new();

            [Display(Name = "Clasificación de Tipo")]
            public EquipmentTypeClassification? TypeClassification { get; set; }

            [StringLength(1000)]
            [Display(Name = "Detalle de Otro")]
            public string? OtherClassificationDetail { get; set; }

            [Required(ErrorMessage = "La evidencia de clasificación es obligatoria")]
            [StringLength(1000)]
            [Display(Name = "Evidencia de clasificación")]
            public string EvidenceReference { get; set; } = string.Empty;
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
            if (!Input.Category.HasValue || !Enum.IsDefined(typeof(EquipmentCategory), Input.Category.Value))
                ModelState.AddModelError("Input.Category", "Seleccione una categoría válida.");

            var imageValidationError = await SafeImageUpload.ValidateAsync(
                Input.ImageUpload,
                HttpContext.RequestAborted);

            if (imageValidationError != null)
                ModelState.AddModelError("Input.ImageUpload", imageValidationError);

            ValidateNotes();

            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

            await ValidateLocationAsync();
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
            Input.CatalogCode = Input.CatalogCode.Trim().ToUpperInvariant();
            Input.OtherClassificationDetail = Input.OtherClassificationDetail?.Clean();
            Input.EvidenceReference = Input.EvidenceReference.Clean();

            if (!Input.Category.HasValue || !EquipmentClassificationRules.IsConfirmedCombinationValid(
                    Input.Category.Value,
                    Input.TypeClassification,
                    Input.UtensilType,
                    Input.OtherClassificationDetail))
            {
                ModelState.AddModelError(string.Empty,
                    "La combinación de categoría, subtipo y detalle de Otro no es válida.");
                LoadLists();
                return Page();
            }

            if (Input.Category == EquipmentCategory.Other)
            {
                Input.UtensilType = null;
                Input.TypeClassification = null;
            }
            else if (Input.Category == EquipmentCategory.Equipment)
            {
                Input.UtensilType = null;
            }
            else if (Input.Category == EquipmentCategory.Utensil)
            {
                Input.TypeClassification = null;
            }

            // Duplicate Check (Name + Model)
            var normalizedName = Input.Name.ToLower();
            var normalizedModel = Input.Model?.ToLower();

            var exists = await _context.Equipments
                .AnyAsync(e => e.Name.ToLower() == normalizedName &&
                               (string.IsNullOrEmpty(Input.Model) || (e.Model ?? string.Empty).ToLower() == normalizedModel));

            var catalogCodeExists = await _context.Equipments
                .IgnoreQueryFilters()
                .AnyAsync(e => e.CatalogCode == Input.CatalogCode);

            if (exists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe un equipo registrado con este nombre y modelo.");
                LoadLists();
                return Page();
            }

            if (catalogCodeExists)
            {
                ModelState.AddModelError("Input.CatalogCode", "El código de catálogo ya existe.");
                LoadLists();
                return Page();
            }

            var equipment = new Models.Equipment
            {
                CatalogCode = Input.CatalogCode,
                Category = Input.Category.Value,
                UtensilType = Input.UtensilType, // Asignamos el nuevo Enum
                TypeClassification = Input.TypeClassification, // CORRECCIÓN: Se añade el mapeo de la clasificación dinámica
                ClassificationReviewStatus = EquipmentClassificationReviewStatus.LegacyInferred,
                OtherClassificationDetail = Input.OtherClassificationDetail,

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

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "equipment");
            string? uploadedFilePath = null;

            try
            {
                if (Input.ImageUpload is { Length: > 0 })
                {
                    var storedFileName = await SafeImageUpload.SaveAsync(
                        Input.ImageUpload,
                        uploadsFolder,
                        HttpContext.RequestAborted);
                    equipment.ImageUrl = storedFileName;
                    uploadedFilePath = Path.Combine(uploadsFolder, storedFileName);
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                _context.Equipments.Add(equipment);
                await _context.SaveChangesAsync();

                foreach (var note in Input.Notes.Where(note => !string.IsNullOrWhiteSpace(note)))
                {
                    _context.EquipmentNotes.Add(new EquipmentNote
                    {
                        EquipmentId = equipment.Id,
                        Note = note.Clean()
                    });
                }

                await _classificationService.ApplyAsync(new EquipmentClassificationDecisionInput(
                    DecisionKey: $"manual-create:{equipment.CatalogCode}",
                    CatalogCode: equipment.CatalogCode!,
                    ReviewStatus: EquipmentClassificationReviewStatus.Confirmed,
                    Category: Input.Category,
                    TypeClassification: Input.TypeClassification,
                    UtensilType: Input.UtensilType,
                    GeneralStatus: GeneralStatus.Activo,
                    OtherDetail: Input.OtherClassificationDetail,
                    EvidenceReference: Input.EvidenceReference,
                    ResponsiblePersonId: null,
                    ResponsibleSnapshot: currentUser?.FullName ?? User.Identity?.Name ?? "Administrador",
                    DecisionDate: DateTime.UtcNow,
                    EffectiveFrom: null,
                    ImportBatchId: null,
                    ImportSourceRowId: null,
                    RecordedByUserId: currentUser?.Id), HttpContext.RequestAborted);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                SafeImageUpload.DeleteIfExists(uploadedFilePath);
                _logger.LogError(ex, "No se pudo crear la definición de equipo {EquipmentName}.", Input.Name);
                ModelState.AddModelError(string.Empty,
                    "No se pudo guardar el equipo. Revise los datos e intente nuevamente.");
                LoadLists();
                return Page();
            }

            TempData.Success($"Definición de '{equipment.Name}' registrada correctamente.");
            return RedirectToPage("./Details", new { id = equipment.Id });
        }

        private async Task ValidateLocationAsync()
        {
            if (!Input.CountryId.HasValue)
            {
                if (Input.CityId.HasValue)
                    ModelState.AddModelError("Input.CityId", "Seleccione el país correspondiente a la ciudad.");
                return;
            }

            var countryExists = await _context.Countries
                .AnyAsync(country => country.Id == Input.CountryId.Value && country.Status == GeneralStatus.Activo);

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
            var countries = _context.Countries
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Name)
                .ToList();
            ViewData["CountryId"] = new SelectList(countries, "Id", "Name");
            ViewData["CityId"] = new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text");
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
            => GetValidEquipmentClassifications().Contains(classification);

        private static bool IsValidNewUtensilType(UtensilType utensilType)
            => EquipmentClassificationRules.IsValidUtensilSubclassification(utensilType);

        private static EquipmentTypeClassification[] GetValidEquipmentClassifications() =>
            EquipmentClassificationRules.EquipmentSubclassifications.ToArray();
    }
}
