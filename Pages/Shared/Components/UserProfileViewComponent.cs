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

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

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
