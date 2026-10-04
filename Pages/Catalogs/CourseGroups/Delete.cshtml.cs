using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.CourseGroups
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
        public CourseGroup Group { get; set; } = default!;

        public string SubjectName { get; set; } = "—";
        public string ManagementCode { get; set; } = "—";
        public string TeacherName { get; set; } = "—";
        public string LaboratoryName { get; set; } = "—";

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // CourseGroup tiene query filter global: usar IgnoreQueryFilters para mostrar cualquier estado.
            var group = await _context.CourseGroups
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(g => g.Subject)
                .Include(g => g.Management)
                .Include(g => g.Laboratory)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            Group = group;

            // Docente: nombre desde Interns (Person.FullName es NotMapped).
            TeacherName = await _context.Interns.AsNoTracking()
                .Where(i => i.Id == group.TeacherPersonId)
                .Select(i => i.Name)
                .FirstOrDefaultAsync() ?? $"Docente #{group.TeacherPersonId}";

            SubjectName = group.Subject?.Name ?? "Sin materia";
            ManagementCode = group.Management?.Code ?? "Sin gestión";
            LaboratoryName = group.Laboratory != null
                ? $"{group.Laboratory.Code} · {group.Laboratory.Name}"
                : "Sin laboratorio";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            // CourseGroup tiene query filter global: usar IgnoreQueryFilters para modificar incluso si ya fue dado de baja.
            var group = await _context.CourseGroups
                .IgnoreQueryFilters()
                .AsTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            // Soft Delete: baja lógica para auditoría
            group.Status = GeneralStatus.Eliminado;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"El grupo '{group.Name}' ha sido dado de baja del catálogo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo dar de baja el grupo {GroupId}.", id);
                TempData.Error("No se pudo dar de baja el grupo. Intente nuevamente.");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}