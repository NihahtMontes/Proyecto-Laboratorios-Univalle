using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Schedules
{
    /// <summary>
    /// Listado de SOLO LECTURA de la programación de prácticas (PracticeSchedules)
    /// importada del Excel histórico. Sin Create/Edit/Delete ni POST.
    /// </summary>
    [Authorize]
    public class PracticesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PracticesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<PracticeSchedule> Schedules { get; set; } = default!;

        public int PageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementId { get; set; }

        [BindProperty(SupportsGet = true)]
        public PracticeScheduleStatus? Status { get; set; }

        public SelectList ManagementOptions { get; set; } = default!;

        // Nombres de docentes por Id (Person.FullName es [NotMapped]; mismos lookup que CourseGroups).
        private Dictionary<int, string> TeacherNames { get; set; } = new();

        public string GetTeacherName(int teacherPersonId)
            => TeacherNames.TryGetValue(teacherPersonId, out var name) ? name : $"Docente #{teacherPersonId}";

        public async Task OnGetAsync(int? pageIndex)
        {
            // Gestiones para el filtro (el query filter global ya excluye Deleted).
            var managements = await _context.Managements.AsNoTracking()
                .OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester)
                .Select(m => new { m.Id, m.Code })
                .ToListAsync();
            ManagementOptions = new SelectList(managements, "Id", "Code", ManagementId);

            // PracticeSchedule tiene query filter global IsDeleted = false.
            IQueryable<PracticeSchedule> query = _context.PracticeSchedules
                .AsNoTracking()
                .Include(s => s.Management)
                .Include(s => s.Subject)
                .Include(s => s.Practice)
                .Include(s => s.Laboratory);

            // Búsqueda: código, materia, práctica, grupo y docente.
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();

                // El nombre del docente vive en Intern (Person.FullName es NotMapped).
                var teacherIds = await _context.Interns.AsNoTracking()
                    .Where(i => i.Name.ToLower().Contains(term))
                    .Select(i => i.Id)
                    .ToListAsync();

                query = query.Where(s =>
                    s.Code.ToLower().Contains(term) ||
                    s.Subject!.Code.ToLower().Contains(term) ||
                    s.Subject!.Name.ToLower().Contains(term) ||
                    s.Practice!.Name.ToLower().Contains(term) ||
                    (s.Practice!.Number != null && s.Practice.Number.ToLower().Contains(term)) ||
                    (s.GroupName != null && s.GroupName.ToLower().Contains(term)) ||
                    teacherIds.Contains(s.TeacherPersonId));
            }

            if (ManagementId.HasValue)
            {
                query = query.Where(s => s.ManagementId == ManagementId.Value);
            }

            if (Status.HasValue)
            {
                query = query.Where(s => s.Status == Status.Value);
            }

            Schedules = await PaginatedList<PracticeSchedule>.CreateAsync(
                query.OrderByDescending(s => s.ManagementId)
                    .ThenBy(s => s.ExecutionDate)
                    .ThenBy(s => s.Id),
                pageIndex ?? 1, PageSize);

            // Nombres de docentes solo de la página actual: una sola consulta.
            var pageTeacherIds = Schedules.Select(s => s.TeacherPersonId).Distinct().ToList();
            TeacherNames = pageTeacherIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Interns.AsNoTracking()
                    .Where(i => pageTeacherIds.Contains(i.Id))
                    .Select(i => new { i.Id, i.Name })
                    .ToDictionaryAsync(i => i.Id, i => i.Name);
        }
    }
}
