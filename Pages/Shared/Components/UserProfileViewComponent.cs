using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Shared.Components
{
    public class UserProfileViewComponent : ViewComponent
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ApplicationDbContext _context;

        public UserProfileViewComponent(ICurrentUserService currentUserService, ApplicationDbContext context)
        {
            _currentUserService = currentUserService;
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = _currentUserService.UserId;
            if (userId == null)
            {
                return View(new UserProfileViewModel());
            }

            Models.User? user;
            try
            {
                user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId.Value);
            }
            catch (Exception ex) when (ex is System.Data.Common.DbException || ex.InnerException is System.Data.Common.DbException
                || ex is InvalidOperationException || ex is TimeoutException)
            {
                // Defecto D-02 (Práctica 3): sin base de datos, este componente del layout hacía fallar también la página
                // de error y el usuario recibía una respuesta vacía. Se muestra el nombre de la sesión y la página sigue.
                var name = UserClaimsPrincipal.Identity?.Name ?? string.Empty;
                return View(new UserProfileViewModel
                {
                    UserId = userId.Value,
                    FullName = name,
                    Initials = name.Length > 0 ? name[..1].ToUpperInvariant() : "?"
                });
            }

            if (user == null)
            {
                return View(new UserProfileViewModel());
            }

            var model = new UserProfileViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Initials = user.Initials,
                ProfilePictureVersion = (user.LastModifiedDate ?? user.CreatedDate).Ticks.ToString()
            };

            return View(model);
        }
    }

    public class UserProfileViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string ProfilePictureVersion { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
    }
}
