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
            [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
            [Display(Name = "Usuario")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El correo institucional es obligatorio")]
            [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
            [Display(Name = "Correo")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Los nombres son obligatorios")]
            [Display(Name = "Nombres")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El primer apellido es obligatorio")]
            [Display(Name = "Apellido")]
            public string LastName { get; set; } = string.Empty;

            [Display(Name = "Segundo Apellido")]
            public string? SecondLastName { get; set; }

            [Required(ErrorMessage = "El documento de identidad es obligatorio")]
            [Display(Name = "C.I.")]
            public string IdentityCard { get; set; } = string.Empty;

            [Required(ErrorMessage = "El rol es obligatorio")]
            [Display(Name = "Rol")]
            public UserRole Role { get; set; }

            [Display(Name = "Cargo")]
            public string? Position { get; set; }

            [Display(Name = "Departamento")]
            public string? Department { get; set; }

            [Display(Name = "Fecha de Contratación")]
            public DateTime? HireDate { get; set; }

            [Required(ErrorMessage = "El teléfono es obligatorio")]
            [Display(Name = "Teléfono")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "La contraseña es obligatoria")]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
            public string Password { get; set; } = string.Empty;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IActionResult OnGet()
        {
            LoadRoles();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            // Normalization
            var normalizedCI = Input.IdentityCard.Trim();
            var normalizedEmail = Input.Email.Trim().ToLower();
            var normalizedUserName = Input.UserName.Trim().ToLower();

            // Duplicate Validations (Ignoring soft-deleted)
            bool ciExists = await _context.Users
               .IgnoreQueryFilters()
               .AnyAsync(u => u.IdentityCard.Trim() == normalizedCI && u.Status != GeneralStatus.Eliminado);

            if (ciExists) ModelState.AddModelError("Input.IdentityCard", "Este documento de identidad ya está vinculado a otra cuenta.");

            bool emailExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email.Trim().ToLower() == normalizedEmail && u.Status != GeneralStatus.Eliminado);

            if (emailExists) ModelState.AddModelError("Input.Email", "Este correo electrónico ya se encuentra registrado.");

            bool userNameExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.UserName.Trim().ToLower() == normalizedUserName && u.Status != GeneralStatus.Eliminado);

            if (userNameExists) ModelState.AddModelError("Input.UserName", "El nombre de usuario ya está en uso.");

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            // User creation
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
                HireDate = Input.HireDate ?? DateTime.Now,
                PhoneNumber = Input.PhoneNumber?.Trim(),
                Status = GeneralStatus.Activo,
                EmailConfirmed = true,
                CreatedDate = DateTime.Now
            };

            // Get current auditor
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                user.CreatedById = currentUser.Id;
            }

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                TempData.Success($"Cuenta de usuario para '{user.FullName}' creada exitosamente.");
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            LoadRoles();
            return Page();
        }

        private void LoadRoles()
        {
            ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>();
        }
    }
}
