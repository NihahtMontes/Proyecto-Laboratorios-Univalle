using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
// Evitar colisión con el namespace Pages.Career
using CareerEntity = Proyecto_Laboratorios_Univalle.Models.Career;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Subjects
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<Subject> Subjects { get; set; } = new PaginatedList<Subject>(new List<Subject>(), 0, 1, 20);

        public SelectList CareerOptions { get; set; } = new(Enumerable.Empty<CareerEntity>(), "Id", "Name");

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CareerFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SemesterFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            await LoadCareersAsync();

            // Subject no tiene query filter global: filtramos explícitamente los no eliminados.
            var query = _context.Subjects
                .AsNoTracking()
                .Include(s => s.Career)
                .Where(s => s.Status != GeneralStatus.Eliminado);

            // Filtro por texto (nombre o código)
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    (s.Code != null && s.Code.ToLower().Contains(term)));
            }

            // Filtro por carrera
            if (CareerFilter.HasValue)
            {
                query = query.Where(s => s.CareerId == CareerFilter.Value);
            }

            // Filtro por semestre
            if (SemesterFilter.HasValue)
            {
                query = query.Where(s => s.Semester == SemesterFilter.Value);
            }

            // Filtro por estado
            if (StatusFilter.HasValue)
            {
                query = query.Where(s => s.Status == StatusFilter.Value);
            }

            Subjects = await PaginatedList<Subject>.CreateAsync(
                query.OrderBy(s => s.Name).ThenBy(s => s.Code),
                pageIndex ?? 1, 20);
        }

        private async Task LoadCareersAsync()
        {
            var careers = await _context.Careers.AsNoTracking()
                .Where(c => c.Status == GeneralStatus.Activo)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            CareerOptions = new SelectList(careers, "Id", "Name", CareerFilter);
        }
    }
}