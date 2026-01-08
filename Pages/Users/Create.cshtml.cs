using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Users
{
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

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

            // Audit fields are handled automatically, no need for SelectLists
            return Page();
        }

        [BindProperty]
        public User User { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Users.Add(User);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
