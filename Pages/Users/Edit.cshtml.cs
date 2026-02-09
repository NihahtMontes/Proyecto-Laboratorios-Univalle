using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

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

        public class UserInputModel
        {
            [Required(ErrorMessage = "Los nombres son obligatorios")]
            [Display(Name = "Nombres")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "El primer apellido es obligatorio")]
            [Display(Name = "Apellido Paterno")]
            public string LastName { get; set; } = string.Empty;

            [Display(Name = "Apellido Materno")]
            public string? SecondLastName { get; set; }

            [Required(ErrorMessage = "El documento de identidad es obligatorio")]
            [Display(Name = "C.I.")]
            public string IdentityCard { get; set; } = string.Empty;

            [Required(ErrorMessage = "El rol es obligatorio")]
            [Display(Name = "Rol")]
            public UserRole Role { get; set; }

            [Required]
            public GeneralStatus Status { get; set; }

            [Display(Name = "Cargo")]
            public string? Position { get; set; }

            [Display(Name = "Departamento")]
            public string? Department { get; set; }

            [Display(Name = "Fecha de Alta")]
            public DateTime? HireDate { get; set; }

            [Phone(ErrorMessage = "Formato de teléfono inválido")]
            [Display(Name = "Teléfono")]
            public string? PhoneNumber { get; set; }

            [Required(ErrorMessage = "El correo institucional es obligatorio")]
            [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Usuario")]
            public string? UserName { get; set; }

            [DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.", MinimumLength = 8)]
            [Display(Name = "Nueva Contraseña")]
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

            LoadRoles();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Id = id;

            if (string.IsNullOrEmpty(Input.NewPassword)) 
            {
                ModelState.Remove("Input.NewPassword");
            }

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            // Uniqueness Checks
            bool ciExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.IdentityCard == Input.IdentityCard && u.Id != id && u.Status != GeneralStatus.Eliminado);

            if (ciExists) ModelState.AddModelError("Input.IdentityCard", "Este C.I. ya está asignado a otro usuario.");

            bool emailExists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == Input.Email && u.Id != id && u.Status != GeneralStatus.Eliminado);

            if (emailExists) ModelState.AddModelError("Input.Email", "Este correo electrónico ya se encuentra registrado.");

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return Page();
            }

            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null) return NotFound();

            // Mapping Updates
            userToUpdate.FirstName = Input.FirstName.Clean();
            userToUpdate.LastName = Input.LastName.Clean();
            userToUpdate.SecondLastName = Input.SecondLastName?.Clean();
            userToUpdate.IdentityCard = Input.IdentityCard.Trim();
            userToUpdate.Role = Input.Role;
            userToUpdate.Status = Input.Status;
            userToUpdate.Position = Input.Position?.Clean();
            userToUpdate.Department = Input.Department?.Clean();
            userToUpdate.PhoneNumber = Input.PhoneNumber;

            // Handle Email Change
            if (userToUpdate.Email != Input.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(userToUpdate, Input.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors) ModelState.AddModelError("Input.Email", error.Description);
                    LoadRoles();
                    return Page();
                }
                userToUpdate.NormalizedEmail = Input.Email.ToUpper();
            }

            // Handle Password Reset
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(userToUpdate);
                var result = await _userManager.ResetPasswordAsync(userToUpdate, token, Input.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors) ModelState.AddModelError("Input.NewPassword", error.Description);
                    LoadRoles();
                    return Page();
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                userToUpdate.ModifiedById = currentUser.Id;
                userToUpdate.LastModifiedDate = DateTime.Now;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Datos de la cuenta '{userToUpdate.FullName}' actualizados correctamente.");
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

        private void LoadRoles()
        {
            ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>();
        }
    }
}
