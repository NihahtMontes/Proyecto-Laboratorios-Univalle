using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public User UserEntity { get; set; } = default!; // Renaming property to avoid conflict with implicit User (ClaimsPrincipal) or be explicit

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            UserEntity = user;
            CargarEstados();
            CargarRoles();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("UserEntity.CreatedBy");
            ModelState.Remove("UserEntity.ModifiedBy");
            ModelState.Remove("UserEntity.SecurityStamp");
            ModelState.Remove("UserEntity.ConcurrencyStamp");
            ModelState.Remove("UserEntity.PasswordHash");
            ModelState.Remove("UserEntity.NormalizedUserName");
            ModelState.Remove("UserEntity.NormalizedEmail");
            
            ModelState.Remove("UserEntity.Id");

            if (!ModelState.IsValid)
            {
                CargarEstados();
                CargarRoles();
                return Page();
            }

            var userBD = await _context.Users.FindAsync(UserEntity.Id);

            if (userBD == null)
            {
                return NotFound();
            }

            // Update fields
            userBD.FirstName = UserEntity.FirstName;
            userBD.LastName = UserEntity.LastName;
            userBD.SecondLastName = UserEntity.SecondLastName;
            userBD.IdentityCard = UserEntity.IdentityCard;
            userBD.Role = UserEntity.Role;
            userBD.Status = UserEntity.Status;
            userBD.Position = UserEntity.Position;
            userBD.Department = UserEntity.Department;
            userBD.HireDate = UserEntity.HireDate;
            
            if (userBD.UserName != UserEntity.UserName)
            {
                await _userManager.SetUserNameAsync(userBD, UserEntity.UserName);
            }
            if (userBD.Email != UserEntity.Email)
            {
               await _userManager.SetEmailAsync(userBD, UserEntity.Email);
            }
            if (userBD.PhoneNumber != UserEntity.PhoneNumber)
            {
               await _userManager.SetPhoneNumberAsync(userBD, UserEntity.PhoneNumber);
            }

            userBD.LastModifiedDate = DateTime.Now;
            
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                userBD.ModifiedById = currentUser.Id;
            }

            try
            {
                await _userManager.UpdateAsync(userBD); 
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(UserEntity.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        private void CargarEstados()
        {
            var estados = Enum.GetValues(typeof(GeneralStatus))
                .Cast<GeneralStatus>()
                .Where(e => e != GeneralStatus.Deleted)
                .Select(e => new { Value = (int)e, Text = e.ToString() })
                .ToList();

            ViewData["ListaEstados"] = new SelectList(estados, "Value", "Text");
        }

        private void CargarRoles()
        {
            var roles = Enum.GetValues<UserRole>()
                .Select(r => new SelectListItem
                {
                    Value = ((int)r).ToString(),
                    Text = r.ToString()
                })
                .ToList();

            ViewData["Roles"] = roles;
        }
    }
}