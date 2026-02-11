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
        private readonly UserManager<User> _userManager;

        public DeleteModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// User to be deleted/revoked. 
        /// Named 'AppUser' to avoid collision with 'PageModel.User' (ClaimsPrincipal).
        /// </summary>
        [BindProperty]
        public User AppUser { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Retrieve the user including audit details
            var user = await _context.Users
                .Include(u => u.CreatedBy)
                .Include(u => u.ModifiedBy)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }
            
            AppUser = user;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Find the user in the database
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if the user is already deleted
            if (user.Status == GeneralStatus.Eliminado)
            {
                TempData.Warning($"El usuario '{user.FullName}' ya se encuentra dado de baja.");
                return RedirectToPage("./Index");
            }

            // Perform Soft Delete (Logic Delete for Audit)
            user.Status = GeneralStatus.Eliminado;
            user.LastModifiedDate = DateTime.Now;

            // Get current user for audit tracking - Use base.User to refer to ClaimsPrincipal
            var currentUser = await _userManager.GetUserAsync(base.User);
            if (currentUser != null)
            {
                user.ModifiedById = currentUser.Id;
            }

            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData.Success($"El acceso para '{user.FullName}' ha sido revocado correctamente.");
            }
            catch (Exception ex)
            {
                TempData.Error($"Hubo un error al intentar procesar la baja: {ex.Message}");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}
