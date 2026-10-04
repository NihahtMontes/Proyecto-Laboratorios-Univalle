using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Practices
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(ApplicationDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Proyecto_Laboratorios_Univalle.Models.Practice Practice { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var practice = await _context.Practices
                .AsNoTracking()
                .Include(p => p.Subject)
                .ThenInclude(s => s!.Career)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (practice == null) return NotFound();

            Practice = practice;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var practice = await _context.Practices.FindAsync(id);
            if (practice == null) return NotFound();

            // Soft delete: marcado como baja lógica preservando integridad histórica.
            practice.Status = GeneralStatus.Eliminado;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"La Práctica '{practice.Code}' ha sido dada de baja del catálogo institucional.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo dar de baja la práctica {PracticeId}", id);
                TempData.Error("No se pudo dar de baja la práctica. Intente nuevamente.");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}