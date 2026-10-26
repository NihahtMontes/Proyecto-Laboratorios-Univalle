using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Account
{
    /// <summary>
    /// Perfil propio en modo lectura (nombre, usuario, correo, teléfono y rol).
    /// Sin cambio de contraseña: la edición la hace el administrador en Usuarios.
    /// </summary>
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly Data.ApplicationDbContext _context;

        public ProfileModel(UserManager<User> userManager, Data.ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string Initials { get; set; } = "US";
        public string ProfilePictureVersion { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
            {
                return RedirectToPage("/Error", new { module = "Perfil", message = "No se encontró el usuario de la sesión actual." });
            }

            // Igual que Pages/Users/Details: lectura sin tracking y sin filtros de auditoría (NoTracking global).
            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return RedirectToPage("/Error", new { module = "Perfil", entityId = userId, message = "No se encontró el usuario solicitado." });
            }

            FullName = user.FullName;
            UserName = user.UserName ?? string.Empty;
            Email = user.Email ?? string.Empty;
            PhoneNumber = user.PhoneNumber ?? string.Empty;
            RoleName = user.Role switch
            {
                UserRole.Administrador => "Administrador",
                UserRole.Supervisor => "Supervisor",
                UserRole.PracticeClosure => "Encargado de cierre de prácticas",
                UserRole.SuperAdmin => "Super Administrador",
                _ => user.Role.ToString()
            };
            ProfilePictureUrl = user.ProfilePictureUrl;
            Initials = string.IsNullOrWhiteSpace(user.Initials) ? "US" : user.Initials;
            ProfilePictureVersion = (user.LastModifiedDate ?? user.CreatedDate).Ticks.ToString();
            return Page();
        }
    }
}
