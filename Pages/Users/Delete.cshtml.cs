using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DeleteModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        // Esta es la propiedad que le falta a tu código para que el HTML funcione
        [BindProperty]
        public User AppUser { get; set; } = default!;

        // Método OnGet necesario para cargar los datos cuando abres la página
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            AppUser = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);

            if (AppUser == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                // Soft Delete (Baja lógica para auditoría)
                user.Status = GeneralStatus.Eliminado;

                // Si el usuario tenía una cuenta de Identity, esto bloquea su acceso
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;

                await _context.SaveChangesAsync();
                TempData["Success"] = "El acceso del usuario ha sido revocado correctamente.";
            }

            return RedirectToPage("./Index");
        }
    }
}