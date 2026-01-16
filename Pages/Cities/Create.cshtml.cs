using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            ViewData["CountryId"] = new SelectList(_context.Countries.Where(c => c.Status == GeneralStatus.Activo), "Id", "Name");
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public int CountryId { get; set; }
            [Required]
            public string Name { get; set; }
            public string? Region { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["CountryId"] = new SelectList(_context.Countries, "Id", "Name");
                return Page();
            }

            var city = new City
            {
                CountryId = Input.CountryId,
                Name = Input.Name,
                Region = Input.Region,
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.Now
            };

            _context.Cities.Add(city);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
