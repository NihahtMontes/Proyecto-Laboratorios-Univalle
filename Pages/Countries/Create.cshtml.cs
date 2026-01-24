using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
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
        private readonly Data.ApplicationDbContext _context;

        public CreateModel(Data.ApplicationDbContext context)
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
            [Required(ErrorMessage = "El nombre del país es obligatorio")]
            [StringLength(100, MinimumLength = 2,
                ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
            [Display(Name = "Nombre del País")]
            public string Name { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ✅ Normalizar ANTES de la consulta
            var normalizedInput = Input.Name.Normalize();

            // ✅ Usar solo métodos que EF puede traducir a SQL
            var exists = await _context.Countries
                .AnyAsync(c => c.Name.Trim().ToLower() == normalizedInput);

            if (exists)
            {
                ModelState.AddModelError("Input.Name",
                    NotificationHelper.Countries.CountryNameDuplicate);
                return Page();
            }

            var country = new Country
            {
                Name = Input.Name.Clean(),
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.UtcNow
            };

            _context.Countries.Add(country);
            await _context.SaveChangesAsync();

            TempData.Success(NotificationHelper.Countries.Created(country.Name));
            return RedirectToPage("./Index");
        }
    }
}