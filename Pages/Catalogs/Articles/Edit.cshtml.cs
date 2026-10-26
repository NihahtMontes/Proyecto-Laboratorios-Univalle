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
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> UnitOptions { get; set; } = new();
        public List<SelectListItem> EquipmentOptions { get; set; } = new();

        /// <summary>Snapshot de sólo lectura para la barra lateral (auditoría).</summary>
        public Article? ArticleSnapshot { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            // Code no se captura: lo asigna el sistema (ART-00001) y se muestra desde ArticleSnapshot.

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

            [Required]
            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

            /// <summary>Vínculo opcional con el catálogo de equipos (artículos de tipo Equipo).</summary>
            [Display(Name = "Catálogo de equipo vinculado")]
            public int? EquipmentId { get; set; }

            /// <summary>
            /// Revisión del dato en la fuente. Sólo se ofrece el conjunto de valores que
            /// devuelve <see cref="EnumHelper.FormSelectList{T}"/> con el valor actual;
            /// un artículo ya Confirmado no puede regresar a Por confirmar.
            /// </summary>
            [Required]
            [Display(Name = "Revisión")]
            public DataReviewStatus ReviewStatus { get; set; } = DataReviewStatus.Confirmed;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // Sin query filter manual (Article tiene filter global, así que cargamos sólo no eliminados).
            var article = await _context.Articles
                .AsNoTracking()
                .Include(a => a.ArticleCategory)
                .Include(a => a.MeasurementUnit)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null) return NotFound();

            ArticleSnapshot = article;

            Input = new InputModel
            {
                Id = article.Id,
                Name = article.Name,
                ArticleCategoryId = article.ArticleCategoryId,
                MeasurementUnitId = article.MeasurementUnitId,
                EquipmentId = article.EquipmentId,
                Status = article.Status,
                ReviewStatus = article.ReviewStatus
            };

            LoadOptions();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // El estado Eliminado solo se asigna con la acción de baja (antes un POST manual lo aceptaba y el registro desaparecía).
            if (Input.Status == GeneralStatus.Eliminado)
            {
                ModelState.AddModelError("Input.Status", "El estado Eliminado solo se gestiona desde la opción Dar de Baja.");
            }

            if (!ModelState.IsValid)
            {
                await ReloadSnapshot(Input.Id);
                LoadOptions();
                return Page();
            }

            // Cargar la entidad tracked para conocer el ReviewStatus actual y validar la transición.
            var articleToUpdate = await _context.Articles
                .AsTracking()
                .FirstOrDefaultAsync(a => a.Id == Input.Id);

            if (articleToUpdate == null) return NotFound();

            // Validar transición de ReviewStatus contra los valores permitidos por FormSelectList(actual).
            var allowedReviewValues = EnumHelper
                .FormSelectList<DataReviewStatus>(articleToUpdate.ReviewStatus)
                .Select(s => (DataReviewStatus)int.Parse(s.Value))
                .ToHashSet();

            if (!allowedReviewValues.Contains(Input.ReviewStatus))
            {
                ModelState.AddModelError("Input.ReviewStatus",
                    "El estado de revisión enviado no es válido para el estado actual del artículo.");
                await ReloadSnapshot(Input.Id);
                LoadOptions();
                return Page();
            }

            // Validación de FKs activas (categoría, unidad).
            var categoryActive = await _context.ArticleCategories
                .AnyAsync(c => c.Id == Input.ArticleCategoryId && c.Status == GeneralStatus.Activo);

            if (!categoryActive)
            {
                ModelState.AddModelError("Input.ArticleCategoryId", "La categoría seleccionada no está disponible.");
                await ReloadSnapshot(Input.Id);
                LoadOptions();
                return Page();
            }

            var unitActive = await _context.MeasurementUnits
                .AnyAsync(u => u.Id == Input.MeasurementUnitId && u.Status == GeneralStatus.Activo);

            if (!unitActive)
            {
                ModelState.AddModelError("Input.MeasurementUnitId", "La unidad de medida seleccionada no está disponible.");
                await ReloadSnapshot(Input.Id);
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
                    await ReloadSnapshot(Input.Id);
                    LoadOptions();
                    return Page();
                }
            }

            // Code no se edita: lo asigna el sistema; no se valida unicidad aquí.

            // Unicidad de Name entre no eliminados.
            var normalizedName = Input.Name.Trim().ToLower();
            var nameExists = await _context.Articles
                .AnyAsync(a => a.Id != Input.Id
                               && a.Name.Trim().ToLower() == normalizedName
                               && a.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe otro artículo con este nombre en el sistema.");
                await ReloadSnapshot(Input.Id);
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

            // Code no se reasigna: lo conserva el sistema desde la creación.
            articleToUpdate.Name = Input.Name.Clean();
            articleToUpdate.ArticleCategoryId = Input.ArticleCategoryId;
            articleToUpdate.MeasurementUnitId = Input.MeasurementUnitId;
            articleToUpdate.EquipmentId = Input.EquipmentId;
            articleToUpdate.Status = Input.Status;
            articleToUpdate.ReviewStatus = Input.ReviewStatus;
            // Columnas legadas sincronizadas.
            articleToUpdate.Category = categoryName;
            articleToUpdate.UnitOfMeasure = string.IsNullOrWhiteSpace(unitCode) ? "UNIDAD" : unitCode;
            articleToUpdate.LastModifiedDate = DateTime.UtcNow;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                articleToUpdate.ModifiedById = currentUser.Id;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Artículo '{articleToUpdate.Name}' actualizado correctamente.");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ArticleExists(Input.Id)) return NotFound();
                throw;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al actualizar el artículo {ArticleId}.", Input.Id);
                ModelState.AddModelError(string.Empty,
                    "No se pudo guardar el artículo porque ya existe un registro con el mismo nombre.");
                await ReloadSnapshot(Input.Id);
                LoadOptions();
                return Page();
            }
        }

        private async Task ReloadSnapshot(int id)
        {
            ArticleSnapshot = await _context.Articles
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        private bool ArticleExists(int id)
        {
            return _context.Articles.Any(e => e.Id == id);
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