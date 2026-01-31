using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    public class CreateModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public CreateModel(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public class InputModel
        {
            [Required]
            public string UserName { get; set; }
            [Required]
            public string Email { get; set; }
            [Required]
            public string FirstName { get; set; }
            [Required]
            public string LastName { get; set; }
            public string? SecondLastName { get; set; }
            [Required]
            public string IdentityCard { get; set; }
            [Required]
            public UserRole Role { get; set; }

            public string? Position { get; set; }
            public string? Department { get; set; }
            public DateTime? HireDate { get; set; }

            [Required]
            public string? PhoneNumber { get; set; }
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IActionResult OnGet()
        {
            ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Normalizar Entradas
            var normalizedCI = Input.IdentityCard.Normalize();
            var normalizedEmail = Input.Email.Normalize();
            var normalizedUserName = Input.UserName.Normalize();

            // 2. Check CI (Ignorando eliminados)
            bool ciExists = await _context.Users
               .IgnoreQueryFilters()
               .AnyAsync(u => u.IdentityCard.Trim() == normalizedCI && u.Status != GeneralStatus.Eliminado);

            if (ciExists) ModelState.AddModelError("Input.IdentityCard", NotificationHelper.Users.UserCIDuplicate);

            // 3. Check Email
            bool emailExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email.Trim().ToLower() == normalizedEmail && u.Status != GeneralStatus.Eliminado);

            if (emailExists) ModelState.AddModelError("Input.Email", NotificationHelper.Users.UserEmailDuplicate);

            // 4. Check UserName
            bool userNameExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.UserName.Trim().ToLower() == normalizedUserName && u.Status != GeneralStatus.Eliminado);

            if (userNameExists) ModelState.AddModelError("Input.UserName", NotificationHelper.Users.UserNameDuplicate);

            if (!ModelState.IsValid)
            {
                ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>();
                ViewData["PersonCategory"] = EnumHelper.ToSelectList<PersonCategory>();
                return Page();
            }

            // 5. Crear usuario
            var user = new User
            {
                UserName = normalizedUserName,
                Email = normalizedEmail,
                FirstName = Input.FirstName.Clean(),
                LastName = Input.LastName.Clean(),
                SecondLastName = Input.SecondLastName?.Clean(),
                IdentityCard = normalizedCI,
                Role = Input.Role,
                Position = Input.Position?.Clean(),
                Department = Input.Department?.Clean(),
                HireDate = Input.HireDate,
                PhoneNumber = Input.PhoneNumber?.Trim(),
                Status = GeneralStatus.Activo,
                EmailConfirmed = true 
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                TempData.Success(NotificationHelper.Users.Created(user.FullName));
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>();
            ViewData["PersonCategory"] = EnumHelper.ToSelectList<PersonCategory>();
            return Page();
        }
    }
}
