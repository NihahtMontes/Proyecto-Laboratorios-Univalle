using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.CourseGroups
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<CourseGroup> CourseGroups { get; set; } = new PaginatedList<CourseGroup>(new List<CourseGroup>(), 0, 1, 20);

        public SelectList ManagementOptions { get; set; } = new(Enumerable.Empty<Management>(), "Id", "Code");
        public SelectList SubjectOptions { get; set; } = new(Enumerable.Empty<Subject>(), "Id", "Name");
        public SelectList TeacherOptions { get; set; } = new(Enumerable.Empty<Intern>(), "Id", "Name");

        // Diccionario en memoria con los nombres de los docentes referenciados (Person.FullName es NotMapped).
        private Dictionary<int, string> TeacherNames { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ManagementFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SubjectFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? TeacherFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public string GetTeacherName(int teacherPersonId)
            => TeacherNames.TryGetValue(teacherPersonId, out var name) ? name : $"Docente #{teacherPersonId}";

        public async Task OnGetAsync(int? pageIndex)
        {
            await LoadFiltersAsync();

            // CourseGroup tiene query filter global que excluye Eliminados.
            // Se declara explícitamente IQueryable<CourseGroup> para poder reasignar tras Include/Where.
            IQueryable<CourseGroup> query = _context.CourseGroups
                .AsNoTracking()
                .Include(g => g.Subject)
                .Include(g => g.Management)
                .Include(g => g.Laboratory);

            // Filtro por texto (Code o Name)
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(g =>
                    g.Code.ToLower().Contains(term) ||
                    g.Name.ToLower().Contains(term));
            }

            // Filtro por gestión
            if (ManagementFilter.HasValue)
            {
                query = query.Where(g => g.ManagementId == ManagementFilter.Value);
            }

            // Filtro por materia
            if (SubjectFilter.HasValue)
            {
                query = query.Where(g => g.SubjectId == SubjectFilter.Value);
            }

            // Filtro por docente
            if (TeacherFilter.HasValue)
            {
                query = query.Where(g => g.TeacherPersonId == TeacherFilter.Value);
            }

            // Filtro por estado
            if (StatusFilter.HasValue)
            {
                query = query.Where(g => g.Status == StatusFilter.Value);
            }

            CourseGroups = await PaginatedList<CourseGroup>.CreateAsync(
                query.OrderBy(g => g.Subject!.Name).ThenBy(g => g.Name),
                pageIndex ?? 1, 20);

            // Docentes: lookup por Id desde Interns (Person.FullName es NotMapped).
            var teacherIds = CourseGroups.Select(g => g.TeacherPersonId).Distinct().ToList();
            TeacherNames = teacherIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Interns.AsNoTracking()
                    .Where(i => teacherIds.Contains(i.Id))
                    .Select(i => new { i.Id, i.Name })
                    .ToDictionaryAsync(i => i.Id, i => i.Name);
        }

        private async Task LoadFiltersAsync()
        {
            var managements = await _context.Managements.AsNoTracking()
                .Where(m => m.Status != ManagementStatus.Deleted && m.Semester > 0)
                .OrderByDescending(m => m.Year).ThenByDescending(m => m.Semester)
                .Select(m => new { m.Id, m.Code })
                .ToListAsync();

            var subjects = await _context.Subjects.AsNoTracking()
                .Where(s => s.Status == GeneralStatus.Activo)
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            var teachers = await _context.Interns.AsNoTracking()
                .Where(i => i.Category == PersonCategory.Docente
                            && i.Status == GeneralStatus.Activo
                            && i.InternStatus == GeneralStatus.Activo)
                .OrderBy(i => i.Name)
                .Select(i => new { i.Id, i.Name })
                .ToListAsync();

            ManagementOptions = new SelectList(managements, "Id", "Code", ManagementFilter);
            SubjectOptions = new SelectList(subjects, "Id", "Name", SubjectFilter);
            TeacherOptions = new SelectList(teachers, "Id", "Name", TeacherFilter);
        }
    }
}