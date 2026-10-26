using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Subjects
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
        public Subject Subject { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // Subject no tiene query filter global: cargar incluyendo cualquier estado para mostrar
            var subject = await _context.Subjects
                .AsNoTracking()
                .Include(s => s.Career)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            Subject = subject;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            // Subject no tiene query filter global: cargar con AsTracking para modificar
            var subject = await _context.Subjects
                .AsTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null) return NotFound();

            // QA 2026-10-18: integridad referencial — una materia con prácticas activas no se da de baja (las dejaba huérfanas).
            var activePractices = await _context.Practices.CountAsync(p => p.SubjectId == subject.Id && p.Status != GeneralStatus.Eliminado);
            if (activePractices > 0)
            {
                TempData.Warning($"No se puede dar de baja la materia '{subject.Name}': tiene {activePractices} práctica(s) activa(s). Dé de baja primero sus prácticas.");
                return RedirectToPage("./Index");
            }

            // Soft Delete: baja lógica para auditoría
            subject.Status = GeneralStatus.Eliminado;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"La materia '{subject.Name}' ha sido dada de baja del catálogo académico.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo dar de baja la materia {SubjectId}.", id);
                TempData.Error("No se pudo dar de baja la materia. Intente nuevamente.");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}