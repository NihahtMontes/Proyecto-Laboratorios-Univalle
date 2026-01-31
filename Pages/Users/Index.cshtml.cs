using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Users;

    //[Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Person> PersonsList { get; set; } = default!;
        public IList<User> UserList { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public Proyecto_Laboratorios_Univalle.Models.Enums.GeneralStatus? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            var userQuery = _context.Users
                .Include(u => u.CreatedBy)
                .Include(u => u.ModifiedBy)
                .Where(u => u.Status != Proyecto_Laboratorios_Univalle.Models.Enums.GeneralStatus.Eliminado);

            // Búsqueda por Nombre, Username o CI
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                userQuery = userQuery.Where(u => u.FirstName.ToLower().Contains(term) || 
                                               u.LastName.ToLower().Contains(term) || 
                                               u.UserName.ToLower().Contains(term) ||
                                               u.IdentityCard.Contains(term));
            }

            // Filtro por Estado
            if (StatusFilter.HasValue)
            {
                userQuery = userQuery.Where(u => u.Status == StatusFilter.Value);
            }

            UserList = await userQuery.ToListAsync();

            // Para People, mantenemos una consulta similar si se requiere búsqueda en pestañas (opcional por ahora)
            PersonsList = await _context.People.ToListAsync();
        }
    }

