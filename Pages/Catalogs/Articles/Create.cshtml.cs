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

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Articles
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> UnitOptions { get; set; } = new();
        public List<SelectListItem> EquipmentOptions { get; set; } = new();

        public IActionResult OnGet()
        {
            LoadOptions();
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            // Code lo asigna el sistema (formato ART-00001); no se captura en el InputModel.

            [Required(ErrorMessage = "El nombre del artículo es obligatorio")]
            [Display(Name = "Nombre")]
            [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Seleccione la categoría")]
            [Display(Name = "Categoría")]
            public int? ArticleCategoryId { get; set; }

            [Required(ErrorMessage = "Seleccione la unidad de medida")]
            [Display(Name = "Unidad de Medida")]
            public int? MeasurementUnitId { get; set; }

            /// <summary>Vínculo opcional con el catálogo de equipos (artículos de tipo Equipo).</summary>
            [Display(Name = "Catálogo de equipo vinculado")]
            public int? EquipmentId { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadOptions();
                return Page();
            }

            // Validación: FKs activos
            var categoryActive = await _context.ArticleCategories
                .AnyAsync(c => c.Id == Input.ArticleCategoryId && c.Status == GeneralStatus.Activo);

            if (!categoryActive)
            {
                ModelState.AddModelError("Input.ArticleCategoryId", "La categoría seleccionada no está disponible.");
                LoadOptions();
                return Page();
            }

            var unitActive = await _context.MeasurementUnits
                .AnyAsync(u => u.Id == Input.MeasurementUnitId && u.Status == GeneralStatus.Activo);

            if (!unitActive)
            {
                ModelState.AddModelError("Input.MeasurementUnitId", "La unidad de medida seleccionada no está disponible.");
                LoadOptions();
                return Page();
            }

            // Validación: EquipmentId (si viene informado) debe existir y estar activo.
            // El query filter global ya excluye Equipment con Status == Eliminado; el filtro
            // explícito por Status == Activo mantiene el mismo criterio de "disponible" usado
            // en la lista EquipmentOptions y bloquea FKs de catálogos dados de baja.
            if (Input.EquipmentId.HasValue)
            {
                var equipmentActive = await _context.Equipments
                    .AnyAsync(e => e.Id == Input.EquipmentId.Value && e.Status == GeneralStatus.Activo);

                if (!equipmentActive)
                {
                    ModelState.AddModelError("Input.EquipmentId", "El catálogo de equipo no existe.");
                    LoadOptions();
                    return Page();
                }
            }

            // Code lo asigna el sistema mediante CatalogCodes; no se valida unicidad aquí.

            // Unicidad de Name entre no eliminados (índice con filtro [Status] <> 2).
            var normalizedName = Input.Name.Trim().ToLower();
            var nameExists = await _context.Articles
                .AnyAsync(a => a.Name.Trim().ToLower() == normalizedName
                               && a.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe un artículo con este nombre en el sistema.");
                LoadOptions();
                return Page();
            }

            // Sincronizar columnas legadas desde las FKs activas.
            var categoryName = await _context.ArticleCategories
                .Where(c => c.Id == Input.ArticleCategoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            var unitCode = await _context.MeasurementUnits
                .Where(u => u.Id == Input.MeasurementUnitId)
                .Select(u => u.Code)
                .FirstOrDefaultAsync() ?? "UNIDAD";

            var article = new Article
            {
                // Code vacío: CatalogCodes asignará el siguiente ART-00001 al persistir.
                Code = string.Empty,
                Name = Input.Name.Clean(),
                ArticleCategoryId = Input.ArticleCategoryId,
                MeasurementUnitId = Input.MeasurementUnitId,
                EquipmentId = Input.EquipmentId,
                Status = GeneralStatus.Activo,
                ReviewStatus = DataReviewStatus.Confirmed,
                // Columnas legadas sincronizadas.
                Category = categoryName,
                UnitOfMeasure = string.IsNullOrWhiteSpace(unitCode) ? "UNIDAD" : unitCode,
                CreatedDate = DateTime.UtcNow
            };

            var currentUser = await _userManager.GetUserAsync(User);
            // ArticleAuditIds y Settings los centraliza ApplicationDbContext (IAuditable).

            _context.Articles.Add(article);

            try
            {
                await _context.SaveWithCodeAsync(
                    _context.Articles.IgnoreQueryFilters().Select(x => (string?)x.Code),
                    CatalogCodes.Article,
                    code => article.Code = code);
                TempData.Success($"Artículo '{article.Code}' registrado exitosamente.");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al crear el artículo {ArticleCode}.", article.Code);
                ModelState.AddModelError(string.Empty,
                    "No se pudo registrar el artículo porque ya existe un registro con el mismo nombre.");
                LoadOptions();
                return Page();
            }
        }

        private void LoadOptions()
        {
            CategoryOptions = _context.ArticleCategories
                .AsNoTracking()
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            UnitOptions = _context.MeasurementUnits
                .AsNoTracking()
                .Where(u => u.Status == GeneralStatus.Activo)
                .OrderBy(u => u.Name)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"{u.Code} - {u.Name}"
                })
                .ToList();

            EquipmentOptions = _context.Equipments
                .AsNoTracking()
                .Where(e => e.Status == GeneralStatus.Activo)
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Brand,
                    e.Model,
                    UnitCount = _context.EquipmentUnits
                        .Count(u => u.EquipmentId == e.Id && !ArticleRetiredStatuses.Contains(u.CurrentStatus))
                })
                .ToList()
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = BuildEquipmentLabel(e.Name, e.Brand, e.Model, e.UnitCount)
                })
                .ToList();
        }

        /// <summary>
        /// Estados de unidad que ya no están operativas (retiradas/dadas de baja).
        /// Reutilizado por EquipmentOptions; el patrón proviene de
        /// <see cref="Proyecto_Laboratorios_Univalle.Services.RequirementPickerService"/>
        /// y <see cref="Proyecto_Laboratorios_Univalle.Services.ArticleAvailabilityService"/>.
        /// </summary>
        private static readonly EquipmentStatus[] ArticleRetiredStatuses =
        {
            EquipmentStatus.Decommissioned, EquipmentStatus.Dismantled, EquipmentStatus.Deleted
        };

        private static string BuildEquipmentLabel(string name, string? brand, string? model, int unitCount)
        {
            var hasBrand = !string.IsNullOrWhiteSpace(brand);
            var hasModel = !string.IsNullOrWhiteSpace(model);
            string middle;
            if (hasBrand && hasModel) middle = $" · {brand!.Trim()} {model!.Trim()}";
            else if (hasBrand) middle = $" · {brand!.Trim()}";
            else if (hasModel) middle = $" · {model!.Trim()}";
            else middle = string.Empty;
            return $"{name}{middle} ({unitCount} unid.)";
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            return inner is Microsoft.Data.SqlClient.SqlException sqlEx
                   && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }
    }
}