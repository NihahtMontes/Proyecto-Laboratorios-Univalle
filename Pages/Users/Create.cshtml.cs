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
            public string? Department { get; set; } // <-- Agrega esta propiedad
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
            // Roles de Usuario CREATE solo personas que necesitan tener una credencial.
            var excludedRoles = new[] {
                ((int)UserRole.Ingeniero).ToString(),

                ((int)UserRole.Tecnico).ToString(),
                ((int)UserRole.Director).ToString(),
                ((int)UserRole.SuperAdmin).ToString()
            };

            var rolesUsuario = EnumHelper.ToSelectList<UserRole>()
                .Where(e => !excludedRoles.Contains(e.Value));

            ViewData["UserRole"] = rolesUsuario;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // ... existing reload logic ...
            }

            // Check if IdentityCard already exists (Ignorando eliminados)
            bool ciExists = await _context.Users
               .IgnoreQueryFilters()
               .AnyAsync(u => u.IdentityCard == Input.IdentityCard && u.Status != GeneralStatus.Eliminado);

            if (ciExists)
            {
                ModelState.AddModelError("Input.IdentityCard", "El CI ya se encuentra registrado por otro usuario activo.");
            }

            // Check Email uniqueness (Ignoring Deleted)
            bool emailExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == Input.Email && u.Status != GeneralStatus.Eliminado);

            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "El correo electrónico ya está en uso por otro usuario activo.");
            }

            // Check UserName uniqueness (Ignoring Deleted)
            bool userNameExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.UserName == Input.UserName && u.Status != GeneralStatus.Eliminado);

            if (userNameExists)
            {
                ModelState.AddModelError("Input.UserName", "El nombre de usuario ya está en uso por otro usuario activo.");
            }

            if (!ModelState.IsValid)
            {
                // Reload roles
                var excludedRoles = new[] {
                    ((int)UserRole.Ingeniero).ToString(),
                    ((int)UserRole.Tecnico).ToString(),
                    ((int)UserRole.Director).ToString(),
                    ((int)UserRole.SuperAdmin).ToString()
                };
                ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>().Where(e => !excludedRoles.Contains(e.Value));
                return Page();
            }

            var user = new User
            {
                UserName = Input.UserName,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                SecondLastName = Input.SecondLastName,
                IdentityCard = Input.IdentityCard,
                Role = Input.Role,
                Position = Input.Position,
                Department = Input.Department,
                HireDate = Input.HireDate,
                PhoneNumber = Input.PhoneNumber,
                Status = GeneralStatus.Activo, // Default status
                EmailConfirmed = true // Auto-confirm for internal creation
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // Note: CreatedBy/Date will be handled by DbContext Interceptor/Override
                // But UserManager uses its own store. 
                // However, ApplicationDbContext handles the saving. 
                // So IAuditable should work IF UserManager calls SaveChanges on the same context or IF we manually save.
                // UserManager calls CreateAsync -> UserStore -> Context.Add + SaveChanges.
                // So our SaveChanges override in AppDbContext SHOULD fire.
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Reload roles if failed
            var excludedRolesReuse = new[] {
                ((int)UserRole.Ingeniero).ToString(),
                ((int)UserRole.Tecnico).ToString(),
                ((int)UserRole.Director).ToString(),
                ((int)UserRole.SuperAdmin).ToString()
            };
            ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>().Where(e => !excludedRolesReuse.Contains(e.Value));
            return Page();
        }
    }
}
