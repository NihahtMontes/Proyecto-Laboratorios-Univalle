using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Articles
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
        public Article Article { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // Article tiene query filter global: el Eliminado se filtra automáticamente.
            var article = await _context.Articles
                .AsNoTracking()
                .Include(a => a.ArticleCategory)
                .Include(a => a.MeasurementUnit)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null) return NotFound();

            Article = article;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var article = await _context.Articles
                .AsTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null) return NotFound();

            // Soft delete: baja lógica preservando auditoría.
            article.Status = GeneralStatus.Eliminado;
            article.LastModifiedDate = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"El artículo '{article.Name}' ha sido dado de baja del catálogo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo dar de baja el artículo {ArticleId}.", id);
                TempData.Error("No se pudo dar de baja el artículo. Intente nuevamente.");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}