using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
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
            [Required(ErrorMessage = "Usuario es obligatorio")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email es obligatorio")]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nombre es obligatorio")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Apellido es obligatorio")]
            public string LastName { get; set; } = string.Empty;

            public string? SecondLastName { get; set; }

            [Required(ErrorMessage = "C.I. es obligatorio")]
            public string IdentityCard { get; set; } = string.Empty;

            [Required(ErrorMessage = "Seleccione un rol")]
            public UserRole Role { get; set; }

            public string? Position { get; set; }
            public string? Department { get; set; }
            public DateTime? HireDate { get; set; }
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "Contraseña es obligatoria")]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 8)]
            public string Password { get; set; } = string.Empty;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public void OnGet() { LoadRoles(); }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) { LoadRoles(); return Page(); }

            // Validar duplicados (Email, Username, CI)
            var existing = await _context.Users.IgnoreQueryFilters()
                .AnyAsync(u => (u.UserName == Input.UserName || u.Email == Input.Email || u.IdentityCard == Input.IdentityCard)
                          && u.Status != GeneralStatus.Eliminado);

            if (existing)
            {
                ModelState.AddModelError(string.Empty, "El usuario, email o C.I. ya está registrado.");
                LoadRoles(); return Page();
            }

            var user = new User
            {
                UserName = Input.UserName.Trim().ToLower(),
                Email = Input.Email.Trim().ToLower(),
                FirstName = Input.FirstName.Trim(),
                LastName = Input.LastName.Trim(),
                SecondLastName = Input.SecondLastName?.Trim(),
                IdentityCard = Input.IdentityCard.Trim(),
                Role = Input.Role,
                Position = Input.Position,
                Department = Input.Department,
                HireDate = Input.HireDate ?? DateTime.Now,
                PhoneNumber = Input.PhoneNumber,
                Status = GeneralStatus.Activo,
                EmailConfirmed = true,
                CreatedDate = DateTime.Now,
                // CORRECCIÓN: Aseguramos un SecurityStamp desde el nacimiento del objeto
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                TempData["Success"] = "Usuario creado correctamente.";
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            LoadRoles();
            return Page();
        }

        private void LoadRoles() => ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>();
    }
}