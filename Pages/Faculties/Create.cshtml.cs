using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Faculties
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string Name { get; set; }
            public string? Code { get; set; }
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 1. Normalización
            var normalizedName = Input.Name.NormalizeComparison();

            // 2. Validación de Duplicados
            var exists = await _context.Faculties
                .AnyAsync(f => f.Name.Trim().ToLower() == normalizedName && 
                               f.Status != GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("Input.Name", NotificationHelper.Faculties.FacultyNameDuplicate);
                return Page();
            }

            // 3. Validación de Formato
            if (!Input.Name.IsValidName())
            {
                ModelState.AddModelError("Input.Name", NotificationHelper.Countries.InvalidFormat);
                return Page();
            }

            // 4. Mapeo y Guardado
            var faculty = new Faculty
            {
                Name = Input.Name.Clean(),
                Code = Input.Code?.ToUpper().Trim(),
                Description = Input.Description?.Clean(),
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.UtcNow
            };

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            TempData.Success(NotificationHelper.Faculties.Created(faculty.Name));
            return RedirectToPage("./Index");
        }
    }
}
