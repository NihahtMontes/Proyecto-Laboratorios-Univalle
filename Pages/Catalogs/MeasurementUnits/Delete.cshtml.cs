using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.MeasurementUnits
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
        public MeasurementUnit Unit { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var unit = await _context.MeasurementUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null) return NotFound();

            Unit = unit;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            // NoTracking global: usar AsTracking explícito para modificar.
            var unit = await _context.MeasurementUnits
                .AsTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null) return NotFound();

            // Validación referencial: no permitir baja si hay artículos activos que la usen.
            var inUseByArticles = await _context.Articles
                .AnyAsync(a => a.MeasurementUnitId == id && a.Status != GeneralStatus.Eliminado);

            if (inUseByArticles)
            {
                TempData.Error($"No se puede retirar la unidad '{unit.Name}' porque tiene artículos activos asociados.");
                return RedirectToPage("./Delete", new { id });
            }

            // Soft delete. Se renombra el código para liberar el índice único.
            unit.Status = GeneralStatus.Eliminado;
            unit.Code = string.IsNullOrWhiteSpace(unit.Code)
                ? unit.Code
                : $"{unit.Code}-DEL-{DateTime.UtcNow:yyyyMMddHHmmss}";

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"La unidad de medida '{unit.Name}' ha sido dada de baja del sistema.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo dar de baja la unidad de medida {UnitId}.", id);
                TempData.Error("No se pudo dar de baja la unidad de medida. Intente nuevamente.");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}