using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

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
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public int FacultyId { get; set; }
            [Required]
            public string Code { get; set; }
            [Required]
            public string Name { get; set; }
            public string? Type { get; set; }
            public string? Building { get; set; }
            public string? Floor { get; set; }
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validate Code uniqueness (including deleted ones but filtering by status)
            bool codeExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Code == Input.Code && l.Status != GeneralStatus.Eliminado);

            if (codeExists)
            {
                ModelState.AddModelError("Input.Code", "El código (Siglas) ya está en uso por otro laboratorio activo.");
            }

            // Validate Name uniqueness
            bool nameExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Name == Input.Name && l.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "El nombre del laboratorio ya está en uso por otro laboratorio activo.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name");
                return Page();
            }

            var laboratory = new Laboratory
            {
                FacultyId = Input.FacultyId,
                Code = Input.Code,
                Name = Input.Name,
                Type = Input.Type,
                Building = Input.Building,
                Floor = Input.Floor,
                Description = Input.Description,
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.Now
            };

            _context.Laboratories.Add(laboratory);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
