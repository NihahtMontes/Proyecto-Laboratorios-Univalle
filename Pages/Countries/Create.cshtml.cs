using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Countries
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
            [Required(ErrorMessage = "El nombre del país es obligatorio")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
            [Display(Name = "Nombre del País")]
            public string Name { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Normalization
            var normalizedInput = Input.Name.Trim().ToLower();

            // Duplicate Validation
            var exists = await _context.Countries
                .AnyAsync(c => c.Name.Trim().ToLower() == normalizedInput && 
                               c.Status != GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe un país con este nombre en el sistema.");
                return Page();
            }

            // Create Country
            var country = new Country
            {
                Name = Input.Name.Clean(),
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.Now
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                country.CreatedById = currentUser.Id;
            }

            _context.Countries.Add(country);
            await _context.SaveChangesAsync();

            TempData.Success($"País '{country.Name}' registrado exitosamente.");
            return RedirectToPage("./Index");
        }
    }
}