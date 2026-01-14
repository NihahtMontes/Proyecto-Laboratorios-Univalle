using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // <-- Agregado para DisplayAttribute

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public UserInputModel Input { get; set; } = new();

        public int Id { get; set; }
        
        // Needed for filtering roles in View (or can be done in OnGet)
        public SelectList RoleOptions { get; set; }

        public class UserInputModel
        {
            [Required(ErrorMessage = "Los nombres son obligatorios")]
            [Display(Name = "Nombres")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El primer apellido es obligatorio")]
            [Display(Name = "Primer Apellido")]
            public string LastName { get; set; } = string.Empty;

            [Display(Name = "Segundo Apellido")]
            public string? SecondLastName { get; set; }

            [Required(ErrorMessage = "El CI es obligatorio")]
            [StringLength(10)]
            [Display(Name = "Cédula de Identidad")]
            public string IdentityCard { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Rol")]
            public UserRole Role { get; set; }

            [Required]
            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; }

            [Display(Name = "Cargo")]
            public string? Position { get; set; }

            [Display(Name = "Departamento")]
            public string? Department { get; set; }

            [Display(Name = "Fecha de Ingreso")]
            [DataType(DataType.Date)]
            public DateTime? HireDate { get; set; }

            [Display(Name = "Teléfono")]
            [Phone]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "El correo es obligatorio")]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            // Credentials
            [Display(Name = "Nombre de Usuario")]
            public string? UserName { get; set; }

            [Display(Name = "Nueva Contraseña (Opcional)")]
            [DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 6)]
            public string? NewPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();

            Id = user.Id;

            // Map Entity -> DTO
            Input = new UserInputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                SecondLastName = user.SecondLastName,
                IdentityCard = user.IdentityCard,
                Role = user.Role,
                Status = user.Status,
                Position = user.Position,
                Department = user.Department,
                HireDate = user.HireDate,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email ?? "",
                UserName = user.UserName
            };

            await CargarRoles();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Id = id;

            // Remove password validation if empty (it's optional in Edit)
            if (string.IsNullOrEmpty(Input.NewPassword))
            {
                ModelState.Remove("Input.NewPassword");
            }

            if (!ModelState.IsValid)
            {
                await CargarRoles();
                return Page();
            }

            // 1. Uniqueness Check: Is there any OTHER active user with this CI?
            bool ciExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.IdentityCard == Input.IdentityCard && 
                               u.Id != id && 
                               u.Status != GeneralStatus.Eliminado);

            if (ciExists)
            {
                ModelState.AddModelError("Input.IdentityCard", "El CI ya se encuentra registrado por otro usuario activo.");
            }

            // Check Email uniqueness
            bool emailExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == Input.Email && 
                               u.Id != id && 
                               u.Status != GeneralStatus.Eliminado);

            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "El correo electrónico ya está en uso por otro usuario activo.");
            }

            // Check UserName uniqueness
            if (!string.IsNullOrEmpty(Input.UserName))
            {
                bool userNameExists = await _context.Users
                    .IgnoreQueryFilters()
                    .AnyAsync(u => u.UserName == Input.UserName && 
                                   u.Id != id && 
                                   u.Status != GeneralStatus.Eliminado);

                if (userNameExists)
                {
                    ModelState.AddModelError("Input.UserName", "El nombre de usuario ya está en uso por otro usuario activo.");
                }
            }

            if (!ModelState.IsValid)
            {
                await CargarRoles();
                return Page();
            }

            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null) return NotFound();

            // 2. Update Fields
            userToUpdate.FirstName = Input.FirstName;
            userToUpdate.LastName = Input.LastName;
            userToUpdate.SecondLastName = Input.SecondLastName;
            userToUpdate.IdentityCard = Input.IdentityCard;
            userToUpdate.Role = Input.Role;
            userToUpdate.Status = Input.Status;
            userToUpdate.Position = Input.Position;
            userToUpdate.Department = Input.Department;
            userToUpdate.HireDate = Input.HireDate;
            userToUpdate.PhoneNumber = Input.PhoneNumber;

            // 3. Handle Email
            if (userToUpdate.Email != Input.Email)
            {
                userToUpdate.Email = Input.Email;
                userToUpdate.NormalizedEmail = Input.Email.ToUpper();
                userToUpdate.EmailConfirmed = true; 
            }

            // 4. Handle UserName
            if (!string.IsNullOrEmpty(Input.UserName) && Input.UserName != userToUpdate.UserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(userToUpdate, Input.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError("Input.UserName", error.Description);
                    }
                    await CargarRoles();
                    return Page();
                }
            }

            // 5. Handle Password
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(userToUpdate);
                var result = await _userManager.ResetPasswordAsync(userToUpdate, token, Input.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("Input.NewPassword", error.Description);
                    }
                    await CargarRoles();
                    return Page();
                }
            }

            // 6. Save (Auditing Preserved automatically by Context)
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        private async Task CargarRoles()
        {
            // Logic to filter roles if needed, similar to current code
             var excludedRoles = new[] { 
                ((int)UserRole.Ingeniero).ToString(), 
                ((int)UserRole.Tecnico).ToString(),
                ((int)UserRole.Director).ToString(),
                ((int)UserRole.SuperAdmin).ToString()
            };

            var rolesUsuario = EnumHelper.ToSelectList<UserRole>()
                .Where(e => !excludedRoles.Contains(e.Value));
            
            ViewData["UserRole"] = rolesUsuario;
            await Task.CompletedTask; 
        }
    }
}

