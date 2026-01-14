using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Laboratorios_Univalle.Pages.Laboratories
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

        public IActionResult OnGet()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name");
            return Page();
        }

        [BindProperty]
        public Laboratory Laboratory { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Laboratory.CreatedBy");
            ModelState.Remove("Laboratory.ModifiedBy");
            ModelState.Remove("Laboratory.Faculty");

            // Validate Code uniqueness (including deleted ones but filtering by status)
            // "Is there any OTHER lab with the same Code that is NOT Deleted?"
            bool codeExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Code == Laboratory.Code && l.Status != GeneralStatus.Eliminado);

            if (codeExists)
            {
                ModelState.AddModelError("Laboratory.Code", "El código (Siglas) ya está en uso por otro laboratorio activo.");
            }

            // Validate Name uniqueness
            bool nameExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Name == Laboratory.Name && l.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                 ModelState.AddModelError("Laboratory.Name", "El nombre del laboratorio ya está en uso por otro laboratorio activo.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name");
                return Page();
            }

            _context.Laboratories.Add(Laboratory);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
