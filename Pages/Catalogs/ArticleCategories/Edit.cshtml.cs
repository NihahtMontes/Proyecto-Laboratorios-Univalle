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
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        /// <summary>Snapshot de solo lectura para la barra lateral (auditoría visual).</summary>
        public ArticleCategory? CategorySnapshot { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
            [Display(Name = "Nombre de la Categoría")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Código")]
            [StringLength(50, ErrorMessage = "El código no puede superar los 50 caracteres")]
            public string? Code { get; set; }

            /// <summary>Estado administrativo. Sólo se permite Activo/Inactivo (Eliminado se rechaza en servidor).</summary>
            [Required(ErrorMessage = "El estado es obligatorio")]
            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.ArticleCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            CategorySnapshot = category;
            Input = new InputModel
            {
                Id = category.Id,
                Name = category.Name,
                Code = category.Code,
                Status = category.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadSnapshot(Input.Id);
                return Page();
            }

            // Rechazar Eliminado en servidor (sólo Activo/Inactivo permitidos).
            if (Input.Status == GeneralStatus.Eliminado)
            {
                ModelState.AddModelError("Input.Status",
                    "El estado Eliminado sólo se gestiona desde la opción Dar de Baja.");
                await ReloadSnapshot(Input.Id);
                return Page();
            }

            if (Input.Status != GeneralStatus.Activo && Input.Status != GeneralStatus.Inactivo)
            {
                ModelState.AddModelError("Input.Status", "Seleccione un estado válido (Activo o Inactivo).");
                await ReloadSnapshot(Input.Id);
                return Page();
            }

            var categoryToUpdate = await _context.ArticleCategories
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == Input.Id);

            if (categoryToUpdate == null) return NotFound();

            var normalizedName = Input.Name.Trim().ToLower();

            // Unicidad por nombre (entre no eliminados)
            var nameExists = await _context.ArticleCategories
                .AnyAsync(c => c.Id != Input.Id
                               && c.Name.Trim().ToLower() == normalizedName
                               && c.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe otra categoría con este nombre en el sistema.");
                await ReloadSnapshot(Input.Id);
                return Page();
            }

            // Unicidad por código (no se permite vacío en BD)
            var normalizedCode = (Input.Code ?? string.Empty).Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(normalizedCode))
            {
                var codeExists = await _context.ArticleCategories
                    .AnyAsync(c => c.Id != Input.Id
                                   && c.Code != null
                                   && c.Code.Trim().ToLower() == normalizedCode);

                if (codeExists)
                {
                    ModelState.AddModelError("Input.Code", "Ya existe otra categoría con este código en el sistema.");
                    await ReloadSnapshot(Input.Id);
                    return Page();
                }
            }

            // Copiar SOLO los campos permitidos (evita overposting).
            categoryToUpdate.Name = Input.Name.Clean();
            categoryToUpdate.Code = Input.Code?.ToUpper().Trim() ?? string.Empty;
            categoryToUpdate.Status = Input.Status;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Categoría '{categoryToUpdate.Name}' actualizada correctamente.");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al actualizar la categoría {CategoryId}.", Input.Id);
                ModelState.AddModelError(string.Empty,
                    "No se pudo guardar la categoría porque ya existe un registro con el mismo código o nombre.");
                await ReloadSnapshot(Input.Id);
                return Page();
            }
        }

        private async Task ReloadSnapshot(int id)
        {
            CategorySnapshot = await _context.ArticleCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            return inner is Microsoft.Data.SqlClient.SqlException sqlEx
                   && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }
    }
}