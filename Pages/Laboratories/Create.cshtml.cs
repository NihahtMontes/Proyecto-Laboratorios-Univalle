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
            // 1. Validaciones Preliminares
            if (!ModelState.IsValid)
            {
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name");
                return Page();
            }

            // 2. Normalización para validación interna
            var normalizedName = Input.Name.NormalizeComparison();
            var normalizedCode = Input.Code.NormalizeComparison().ToUpper();

            // 3. Validar Duplicados de Código (Global)
            bool codeExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Code.ToUpper() == normalizedCode && l.Status != GeneralStatus.Eliminado);

            if (codeExists)
            {
                ModelState.AddModelError("Input.Code", NotificationHelper.Laboratories.LabCodeDuplicate);
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name");
                return Page();
            }

            // 4. Validar Duplicados de Nombre (En la misma Facultad)
            bool nameExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.FacultyId == Input.FacultyId && 
                               l.Name.ToLower() == normalizedName && 
                               l.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", NotificationHelper.Laboratories.LabNameDuplicate);
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name");
                return Page();
            }

            // 5. Validar Formato de Nombre
            if (!Input.Name.IsValidName())
            {
                ModelState.AddModelError("Input.Name", NotificationHelper.Countries.InvalidFormat);
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name");
                return Page();
            }

            // 6. Guardado Estandarizado
            var laboratory = new Laboratory
            {
                FacultyId = Input.FacultyId,
                Code = Input.Code.Clean().ToUpper(),
                Name = Input.Name.Clean(),
                Type = Input.Type?.Clean(),
                Building = Input.Building?.Clean(),
                Floor = Input.Floor?.Clean(),
                Description = Input.Description?.Clean(),
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.UtcNow
            };

            _context.Laboratories.Add(laboratory);
            await _context.SaveChangesAsync();

            TempData.Success(NotificationHelper.Laboratories.Created(laboratory.Name));
            return RedirectToPage("./Index");
        }
    }
}
