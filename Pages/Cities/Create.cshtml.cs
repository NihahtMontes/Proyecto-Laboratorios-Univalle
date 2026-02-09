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

namespace Proyecto_Laboratorios_Univalle.Pages.Cities
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
            LoadCountries();
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El país es obligatorio")]
            [Display(Name = "País")]
            public int CountryId { get; set; }

            [Required(ErrorMessage = "El nombre de la ciudad es obligatorio")]
            [Display(Name = "Nombre de la Ciudad")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Región/Departamento")]
            [StringLength(100, ErrorMessage = "La región no puede superar los 100 caracteres")]
            public string? Region { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadCountries();
                return Page();
            }

            // Normalization
            var normalizedName = Input.Name.Trim().ToLower();

            // Duplicate Validation: Same city IN THE SAME country
            var exists = await _context.Cities
                .AnyAsync(c => c.CountryId == Input.CountryId && 
                               c.Name.Trim().ToLower() == normalizedName && 
                               c.Status != GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("Input.Name", "Esta ciudad ya se encuentra registrada para el país seleccionado.");
                LoadCountries();
                return Page();
            }

            var country = await _context.Countries.FindAsync(Input.CountryId);
            if (country == null) 
            {
                LoadCountries();
                return Page();
            }

            var city = new City
            {
                CountryId = Input.CountryId,
                Name = Input.Name.Clean(),
                Region = Input.Region?.Clean(),
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.Now
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                city.CreatedById = currentUser.Id;
            }

            _context.Cities.Add(city);
            await _context.SaveChangesAsync();

            TempData.Success($"Ciudad '{city.Name}' registrada exitosamente en {country.Name}.");
            return RedirectToPage("./Index");
        }

        private void LoadCountries()
        {
            ViewData["CountryId"] = new SelectList(
                _context.Countries
                    .Where(c => c.Status == GeneralStatus.Activo)
                    .OrderBy(c => c.Name), 
                "Id", 
                "Name", 
                Input.CountryId);
        }
    }
}
