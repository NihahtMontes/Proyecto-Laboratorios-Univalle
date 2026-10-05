using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.ArticleCategories
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
            [Display(Name = "Nombre de la Categoría")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            // Code lo asigna el sistema (formato CAT-00001); no se captura en el InputModel.
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validación de unicidad por Nombre (entre no eliminados)
            var normalizedName = Input.Name.Trim().ToLower();
            var nameExists = await _context.ArticleCategories
                .AnyAsync(c => c.Name.Trim().ToLower() == normalizedName
                               && c.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe una categoría con este nombre en el sistema.");
                return Page();
            }

            // Code lo asigna el sistema mediante CatalogCodes; no se valida unicidad aquí.

            var category = new ArticleCategory
            {
                Name = Input.Name.Clean(),
                // Code vacío: CatalogCodes asignará el siguiente CAT-00001 al persistir.
                Code = string.Empty,
                Status = GeneralStatus.Activo
                // CreatedDate: el modelo tiene default = DateTime.UtcNow.
            };

            // ArticleCategory no implementa IAuditable; CreatedById no aplica.

            _context.ArticleCategories.Add(category);

            try
            {
                await _context.SaveWithCodeAsync(
                    _context.ArticleCategories.IgnoreQueryFilters().Select(x => (string?)x.Code),
                    CatalogCodes.ArticleCategory,
                    code => category.Code = code);
                TempData.Success($"Categoría '{category.Code}' registrada exitosamente.");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al crear la categoría {CategoryName}.", category.Name);
                ModelState.AddModelError(string.Empty,
                    "No se pudo registrar la categoría porque ya existe un registro con el mismo nombre.");
                return Page();
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            return inner is Microsoft.Data.SqlClient.SqlException sqlEx
                   && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }
    }
}