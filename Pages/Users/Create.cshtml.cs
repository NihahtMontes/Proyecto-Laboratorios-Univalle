using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // que pasa con los pibes
        public IActionResult OnGet()
        {
           

            CargarRoles();
            return Page();
        }

        [BindProperty]
        public User SystemUser { get; set; } = default!;

        [BindProperty]
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(16, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("SystemUser.Id");
            ModelState.Remove("SystemUser.SecurityStamp");
            ModelState.Remove("SystemUser.ConcurrencyStamp");
            ModelState.Remove("SystemUser.CreatedBy");
            ModelState.Remove("SystemUser.ModifiedBy");
            ModelState.Remove("SystemUser.NormalizedUserName");
            ModelState.Remove("SystemUser.NormalizedEmail");

            if (!ModelState.IsValid)
            {
                CargarRoles();
                return Page();
            }

            SystemUser.CreatedDate = DateTime.Now;
            
            var currentUser = await _userManager.GetUserAsync(User); // Uses PageModel.User (ClaimsPrincipal)
            if (currentUser != null)
            {
                SystemUser.CreatedById = currentUser.Id;
            }
            
            var result = await _userManager.CreateAsync(SystemUser, Password);

            if (result.Succeeded)
            {
                // Assign Identity Role if compatible (Assuming Identity Roles are synced or used as standard claims)
                // await _userManager.AddToRoleAsync(User, User.Role.ToString());
                return RedirectToPage("./Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            CargarRoles();
            return Page();
        }

        private void CargarRoles()
        {
            var roles = Helpers.EnumHelper.ToSelectList<UserRole>()
                                  .Where(e => e.Value != ((int)UserRole.SuperAdmin).ToString());
            ViewData["Roles"] = roles;
        }
    }
}
