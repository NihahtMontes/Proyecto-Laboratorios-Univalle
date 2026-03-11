using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre de la facultad es obligatorio")]
            [Display(Name = "Nombre de la Facultad")]
            [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Sigla Oficial")]
            [StringLength(10, ErrorMessage = "La sigla oficial no puede superar los 10 caracteres")]
            public string? Code { get; set; }

            [Display(Name = "Descripción")]
            [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres")]
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Normalization
            var normalizedName = Input.Name.Trim().ToLower();

            // Duplicate Validation
            var exists = await _context.Faculties
                .AnyAsync(f => f.Name.Trim().ToLower() == normalizedName && 
                               f.Status != GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe una facultad con este nombre en el sistema institucional.");
                return Page();
            }

            // Entity Mapping
            var faculty = new Faculty
            {
                Name = Input.Name.Clean(),
                Code = Input.Code?.ToUpper().Trim(),
                Description = Input.Description?.Clean(),
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.UtcNow
            };

            // Set current auditor
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                faculty.CreatedById = currentUser.Id;
            }

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            TempData.Success($"Unidad Académica '{faculty.Name}' registrada exitosamente.");
            return RedirectToPage("/Laboratories/Index", null, "faculties");
        }
    }
}
