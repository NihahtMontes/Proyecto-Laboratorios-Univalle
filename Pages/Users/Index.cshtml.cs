using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Users;

[Authorize(Roles = AuthorizationHelper.AdminRoles)]
public class IndexModel : PageModel
{
    private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

    public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
    {
        _context = context;
    }

    public PaginatedList<Person> PersonsList { get; set; } = default!;
    public PaginatedList<User> UserList { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public GeneralStatus? StatusFilter { get; set; }

    public async Task OnGetAsync(int? pageIndex)
    {
        // 1. Query base de Usuarios (Funciona porque User tiene FirstName y IdentityCard)
        var userQuery = _context.Users
            .Include(u => u.CreatedBy)
            .AsNoTracking();

        // 2. Query base de Directorio (Personas base)
        var personQuery = _context.People
            .Include(p => p.CreatedBy)
            .AsNoTracking();

        if (StatusFilter.HasValue)
        {
            userQuery = userQuery.Where(u => u.Status == StatusFilter.Value);
            personQuery = personQuery.Where(p => p.Status == StatusFilter.Value);
        }
        else
        {
            userQuery = userQuery.Where(u => u.Status != GeneralStatus.Eliminado);
            personQuery = personQuery.Where(p => p.Status != GeneralStatus.Eliminado);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();

            // Búsqueda en Usuarios (OK)
            userQuery = userQuery.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.UserName.ToLower().Contains(term) ||
                u.IdentityCard.Contains(term));

            // CORRECCIÓN: Búsqueda en Personas (Solo por campos existentes en Person.cs)
            personQuery = personQuery.Where(p =>
                (p.Email != null && p.Email.ToLower().Contains(term)) ||
                p.Id.ToString() == term);
        }

        UserList = await PaginatedList<User>.CreateAsync(
            userQuery.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
            pageIndex ?? 1, 10);

        PersonsList = await PaginatedList<Person>.CreateAsync(
            personQuery.OrderByDescending(p => p.Id),
            pageIndex ?? 1, 10);
    }
}