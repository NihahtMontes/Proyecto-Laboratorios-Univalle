using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DetailsModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DetailsModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public new User User { get; set; } = default!;
        public string ProfilePictureVersion { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return RedirectToPage("/Error", new
                {
                    module = "Usuarios",
                    message = "No se recibio el identificador del usuario.",
                    returnUrl = Url.Page("./Index"),
                    listUrl = Url.Page("./Index")
                });
            }

            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.CreatedBy)
                .Include(u => u.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null)
            {
                return RedirectToPage("/Error", new
                {
                    module = "Usuarios",
                    entityId = id.ToString(),
                    message = "No se encontro el usuario solicitado.",
                    returnUrl = Url.Page("./Index"),
                    listUrl = Url.Page("./Index")
                });
            }
            
            User = user;
            ProfilePictureVersion = (user.LastModifiedDate ?? user.CreatedDate).Ticks.ToString();
            return Page();
        }
    }
}
