using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Practices
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<Practice> Practices { get; set; } = new(new List<Practice>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CareerId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SubjectId { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Materias con estado Activo y, si hay carrera elegida, sólo de esa carrera.
            var subjectsQuery = _context.Subjects
                .AsNoTracking()
                .Include(s => s.Career)
                .Where(s => s.Status == GeneralStatus.Activo);

            if (CareerId.HasValue)
            {
                subjectsQuery = subjectsQuery.Where(s => s.CareerId == CareerId.Value);
            }

            ViewData["SubjectId"] = new SelectList(
                await subjectsQuery
                    .OrderBy(s => s.Career!.Name)
                    .ThenBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        Display = (s.Career != null ? s.Career.Name + " · " : "") + s.Name
                    })
                    .ToListAsync(),
                "Id", "Display", SubjectId);

            ViewData["CareerId"] = new SelectList(
                await _context.Careers
                    .AsNoTracking()
                    .Where(c => c.Status == GeneralStatus.Activo)
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync(),
                "Id", "Name", CareerId);

            // Prácticas activas (no eliminadas) con joins necesarios.
            var query = _context.Practices
                .AsNoTracking()
                .Include(p => p.Subject)
                .ThenInclude(s => s!.Career)
                .Where(p => p.Status != GeneralStatus.Eliminado);

            // Filtro por carrera (vía materia).
            if (CareerId.HasValue)
            {
                query = query.Where(p => p.Subject!.CareerId == CareerId.Value);
            }

            // Filtro directo por materia.
            if (SubjectId.HasValue)
            {
                query = query.Where(p => p.SubjectId == SubjectId.Value);
            }

            // Búsqueda por texto (código, número y descripción).
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    p.Code.ToLower().Contains(term) ||
                    (p.Number != null && p.Number.ToLower().Contains(term)) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            // Filtro por estado.
            if (StatusFilter.HasValue)
            {
                query = query.Where(p => p.Status == StatusFilter.Value);
            }

            Practices = await PaginatedList<Practice>.CreateAsync(
                query.OrderBy(p => p.Subject!.Name).ThenBy(p => p.Number).ThenBy(p => p.Code),
                PageIndex ?? 1,
                20);

            // Precalcular el conteo de requerimientos activos para cada práctica de la página actual.
            ViewData["ActiveRequirementsCount"] = await GetActiveRequirementsCountAsync(Practices);

            return Page();
        }

        private async Task<Dictionary<int, int>> GetActiveRequirementsCountAsync(IEnumerable<Practice> practices)
        {
            var ids = practices.Select(p => p.Id).ToList();
            if (ids.Count == 0) return new Dictionary<int, int>();

            return await _context.PracticeRequirements
                .AsNoTracking()
                .Where(r => ids.Contains(r.PracticeId))
                .GroupBy(r => r.PracticeId)
                .Select(g => new { PracticeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PracticeId, x => x.Count);
        }
    }
}