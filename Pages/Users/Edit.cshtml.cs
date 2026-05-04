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
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? SecondLastName { get; set; }
            public string IdentityCard { get; set; } = string.Empty;
            public UserRole Role { get; set; }
            public GeneralStatus Status { get; set; }
            public string? Position { get; set; }
            public string? Department { get; set; }
            public DateTime? HireDate { get; set; }
            public string? PhoneNumber { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? UserName { get; set; }
            public string? NewPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();

            Id = user.Id;
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
            var userToUpdate = await _userManager.FindByIdAsync(id.ToString());
            if (userToUpdate == null) return NotFound();

            // Actualizar datos básicos
            userToUpdate.FirstName = Input.FirstName;
            userToUpdate.LastName = Input.LastName;
            userToUpdate.SecondLastName = Input.SecondLastName;
            userToUpdate.IdentityCard = Input.IdentityCard;
            userToUpdate.Role = Input.Role;
            userToUpdate.Status = Input.Status;
            userToUpdate.Position = Input.Position;
            userToUpdate.Department = Input.Department;
            userToUpdate.PhoneNumber = Input.PhoneNumber;
            userToUpdate.Email = Input.Email;

            // Cambio de Password opcional
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(userToUpdate);
                await _userManager.ResetPasswordAsync(userToUpdate, token, Input.NewPassword);
            }

            // CORRECCIÓN: Si el SecurityStamp es nulo, lo generamos para evitar el error de Identity
            if (string.IsNullOrEmpty(userToUpdate.SecurityStamp))
            {
                await _userManager.UpdateSecurityStampAsync(userToUpdate);
            }

            var result = await _userManager.UpdateAsync(userToUpdate);
            if (result.Succeeded)
            {
                TempData["Success"] = "Usuario actualizado.";
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            LoadRoles();
            return Page();
        }

        private void LoadRoles() => ViewData["UserRole"] = EnumHelper.ToSelectList<UserRole>();
    }
}