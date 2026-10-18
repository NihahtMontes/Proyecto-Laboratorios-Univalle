using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Persons
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<Person> Person { get; set; } = new PaginatedList<Person>(new List<Person>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public GeneralStatus? StatusFilter { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            var query = _context.People
                .Include(p => p.CreatedBy)
                .Include(p => p.ModifiedBy)
                .Where(p => p.Status != GeneralStatus.Eliminado);

            // Búsqueda por nombre / razón social, correo o id (el placeholder prometía nombre pero solo buscaba correo o id).
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                var internIds = _context.Set<Intern>().Where(i => i.Name.ToLower().Contains(term)).Select(i => i.Id);
                var externIds = _context.Set<Extern>().Where(e => e.Name.ToLower().Contains(term)).Select(e => e.Id);
                query = query.Where(p => (p.Email ?? string.Empty).Contains(term)
                                         || p.Id.ToString() == term
                                         || internIds.Contains(p.Id)
                                         || externIds.Contains(p.Id));
            }

            // Status Filter
            if (StatusFilter.HasValue)
            {
                query = query.Where(p => p.Status == StatusFilter.Value);
            }

            Person = await PaginatedList<Person>.CreateAsync(query.OrderByDescending(p => p.Id), pageIndex ?? 1, 20);
        }
    }
}
