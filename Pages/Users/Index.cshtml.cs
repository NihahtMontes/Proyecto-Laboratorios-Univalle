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

    public IList<Person> PersonsList { get; set; } = default!;
    public IList<User> UserList { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public GeneralStatus? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        // 1. Load users for "Cuentas de Acceso" tab
        var userQuery = _context.Users
            .Include(u => u.CreatedBy)
            .Include(u => u.ModifiedBy)
            .AsNoTracking();

        // 2. Load persons for "Directorio de Personal" tab
        var personQuery = _context.People
            .Include(p => p.CreatedBy)
            .Include(p => p.ModifiedBy)
            .AsNoTracking();

        // Apply filters
        if (StatusFilter.HasValue)
        {
            userQuery = userQuery.Where(u => u.Status == StatusFilter.Value);
            personQuery = personQuery.Where(p => p.Status == StatusFilter.Value);
        }
        else
        {
            // By default exclude deleted
            userQuery = userQuery.Where(u => u.Status != GeneralStatus.Eliminado);
            personQuery = personQuery.Where(p => p.Status != GeneralStatus.Eliminado);
        }

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();
            userQuery = userQuery.Where(u => u.FirstName.ToLower().Contains(term) || 
                                           u.LastName.ToLower().Contains(term) || 
                                           u.UserName!.ToLower().Contains(term) ||
                                           (u.IdentityCard != null && u.IdentityCard.Contains(term)));

            personQuery = personQuery.Where(p => p.Email.Contains(term) || 
                                              p.Id.ToString() == term);
        }

        UserList = await userQuery.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
        PersonsList = await personQuery.OrderByDescending(p => p.Id).ToListAsync();
    }
}
