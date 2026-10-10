using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.CourseGroups
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly ICourseGroupPurgeService _purge;
        private readonly IGroupScheduleService _schedules;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            ICourseGroupPurgeService purge,
            IGroupScheduleService schedules,
            ILogger<DeleteModel> logger)
        {
            _context = context;
            _purge = purge;
            _schedules = schedules;
            _logger = logger;
        }

        [BindProperty]
        public CourseGroup Group { get; set; } = default!;

        public string SubjectName { get; set; } = "—";
        public string ManagementCode { get; set; } = "—";
        public string TeacherName { get; set; } = "—";
        public string LaboratoryName { get; set; } = "—";

        /// <summary>Horario por periodos ("Lunes periodos 1–3 · ..."); null si el grupo no tiene periodos.</summary>
        public string? ScheduleSummary { get; set; }

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

            var slots = await _schedules.GetGroupSlotsAsync(group.Id);
            ScheduleSummary = slots.Count > 0 ? _schedules.Summarize(slots) : null;

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

            // Solo lectura para validar existencia y conservar la gestión del grupo en el redirect;
            // el borrado físico lo hace el servicio (Ignora query filters: también limpia Eliminados antiguos).
            var group = await _context.CourseGroups
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            try
            {
                // Borrado físico aprobado por el usuario: los grupos son datos de trabajo desechables.
                // Las planificaciones y programaciones conservan la materia y el nombre del grupo.
                await _purge.DeleteGroupAsync(id.Value);
                TempData.Success("Grupo eliminado. Las planificaciones conservan la materia y el nombre del grupo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo eliminar el grupo {GroupId}.", id);
                TempData.Error("No se pudo eliminar el grupo. Intente nuevamente.");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index", new { ManagementFilter = group.ManagementId });
        }
    }
}