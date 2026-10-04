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
        public List<SelectListItem> ArticleTypeOptions { get; set; } = new();
        public List<SelectListItem> EquipmentOptions { get; set; } = new();

        /// <summary>Snapshot de sólo lectura para la barra lateral (auditoría).</summary>
        public Article? ArticleSnapshot { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El código del artículo es obligatorio")]
            [Display(Name = "Código")]
            [StringLength(30, ErrorMessage = "El código no puede superar los 30 caracteres")]
            public string Code { get; set; } = string.Empty;

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

            [Required(ErrorMessage = "Seleccione el tipo de artículo")]
            [Display(Name = "Tipo")]
            public ArticleType? ArticleType { get; set; }

            [Display(Name = "Equipo de Catálogo")]
            public int? EquipmentId { get; set; }

            [Required]
            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
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
                Code = article.Code,
                Name = article.Name,
                ArticleCategoryId = article.ArticleCategoryId,
                MeasurementUnitId = article.MeasurementUnitId,
                ArticleType = article.ArticleType,
                EquipmentId = article.EquipmentId,
                Status = article.Status
            };

            LoadOptions();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadSnapshot(Input.Id);
                LoadOptions();
                return Page();
            }

            if (!Input.ArticleType.HasValue || !Enum.IsDefined(typeof(ArticleType), Input.ArticleType.Value))
            {
                ModelState.AddModelError("Input.ArticleType", "Seleccione un tipo de artículo válido.");
                await ReloadSnapshot(Input.Id);
                LoadOptions();
                return Page();
            }

            // Validación de FKs activas (categoría, unidad, equipo).
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

            if (Input.ArticleType == ArticleType.Equipment && Input.EquipmentId.HasValue)
            {
                var equipmentActive = await _context.Equipments
                    .AnyAsync(e => e.Id == Input.EquipmentId && e.Status == GeneralStatus.Activo);

                if (!equipmentActive)
                {
                    ModelState.AddModelError("Input.EquipmentId", "El equipo seleccionado no está disponible.");
                    await ReloadSnapshot(Input.Id);
                    LoadOptions();
                    return Page();
                }
            }
            else if (Input.ArticleType != ArticleType.Equipment && Input.EquipmentId.HasValue)
            {
                ModelState.AddModelError("Input.EquipmentId", "Sólo los artículos de tipo Equipo pueden vincularse a un catálogo de equipo.");
                await ReloadSnapshot(Input.Id);
                LoadOptions();
                return Page();
            }

            // Unicidad de Code (índice sin filtro: cualquier estado).
            var normalizedCode = Input.Code.Trim().ToLower();
            var codeExists = await _context.Articles
                .AnyAsync(a => a.Id != Input.Id
                               && a.Code != null
                               && a.Code.Trim().ToLower() == normalizedCode);

            if (codeExists)
            {
                ModelState.AddModelError("Input.Code", "Ya existe otro artículo con este código en el sistema.");
                await ReloadSnapshot(Input.Id);
                LoadOptions();
                return Page();
            }

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

            // AsTracking para preservar RowVersion del modelo.
            var articleToUpdate = await _context.Articles
                .AsTracking()
                .FirstOrDefaultAsync(a => a.Id == Input.Id);

            if (articleToUpdate == null) return NotFound();

            // Sincronizar columnas legadas desde las FKs activas.
            var categoryName = await _context.ArticleCategories
                .Where(c => c.Id == Input.ArticleCategoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? string.Empty;

            var unitCode = await _context.MeasurementUnits
                .Where(u => u.Id == Input.MeasurementUnitId)
                .Select(u => u.Code)
                .FirstOrDefaultAsync() ?? "UNIDAD";

            articleToUpdate.Code = Input.Code.Trim().ToUpper();
            articleToUpdate.Name = Input.Name.Clean();
            articleToUpdate.ArticleCategoryId = Input.ArticleCategoryId;
            articleToUpdate.MeasurementUnitId = Input.MeasurementUnitId;
            articleToUpdate.ArticleType = Input.ArticleType;
            articleToUpdate.EquipmentId = Input.ArticleType == ArticleType.Equipment ? Input.EquipmentId : null;
            articleToUpdate.Status = Input.Status;
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
                    "No se pudo guardar el artículo porque ya existe un registro con el mismo código o nombre.");
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

            ArticleTypeOptions = Enum.GetValues<ArticleType>()
                .Select(t => new SelectListItem
                {
                    Value = ((int)t).ToString(),
                    Text = EnumHelper.GetDisplayName(t)
                })
                .ToList();

            EquipmentOptions = _context.Equipments
                .AsNoTracking()
                .Where(e => e.Status == GeneralStatus.Activo)
                .OrderBy(e => e.CatalogCode)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{(string.IsNullOrWhiteSpace(e.CatalogCode) ? "S/C" : e.CatalogCode)} - {e.Name}"
                })
                .ToList();
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            return inner is Microsoft.Data.SqlClient.SqlException sqlEx
                   && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }
    }
}