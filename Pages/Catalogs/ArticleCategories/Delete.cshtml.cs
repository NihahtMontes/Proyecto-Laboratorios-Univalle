using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.ArticleCategories
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public ArticleCategory Category { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.ArticleCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            Category = category;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            // NoTracking global: usar AsTracking explícito para modificar.
            // Sin query filter para ArticleCategory: usar IgnoreQueryFilters y validar manualmente.
            var category = await _context.ArticleCategories
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            // Validación referencial: no permitir baja si hay artículos activos que la usen.
            var inUseByArticles = await _context.Articles
                .AnyAsync(a => a.ArticleCategoryId == id && a.Status != GeneralStatus.Eliminado);

            if (inUseByArticles)
            {
                TempData.Error($"No se puede retirar la categoría '{category.Name}' porque tiene artículos activos asociados.");
                return RedirectToPage("./Delete", new { id });
            }

            // Idempotente: una segunda baja ya no vuelve a renombrar el código (quedaba CAT-0000N-DEL-…-DEL-… y luego no se podía dar de baja).
            if (category.Status == GeneralStatus.Eliminado)
            {
                TempData.Warning($"La categoría '{category.Name}' ya se encuentra dada de baja.");
                return RedirectToPage("./Index");
            }

            // Soft delete
            category.Status = GeneralStatus.Eliminado;
            category.Code = string.IsNullOrWhiteSpace(category.Code)
                ? category.Code
                : $"{category.Code}-DEL-{DateTime.UtcNow:yyyyMMddHHmmss}";

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"La categoría '{category.Name}' ha sido dada de baja del sistema.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo dar de baja la categoría {CategoryId}.", id);
                TempData.Error("No se pudo dar de baja la categoría. Intente nuevamente.");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}