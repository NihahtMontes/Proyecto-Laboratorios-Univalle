using Microsoft.AspNetCore.Authorization;
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
                ViewData["CountryId"] = new SelectList(_context.Countries.Where(c => c.Status == GeneralStatus.Activo), "Id", "Name");
                return Page();
            }

            // 1. Normalizar para validación (quitar espacios, minúsculas)
            var normalizedName = Input.Name.NormalizeComparison();

            // 2. Validar Duplicados: Misma ciudad EN EL MISMO país
            var exists = await _context.Cities
                .AnyAsync(c => c.CountryId == Input.CountryId && 
                               c.Name.Trim().ToLower() == normalizedName && 
                               c.Status != GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("Input.Name", NotificationHelper.Cities.CityNameDuplicate);
                // Recargar el select list
                ViewData["CountryId"] = new SelectList(_context.Countries.Where(c => c.Status == GeneralStatus.Activo), "Id", "Name");
                return Page();
            }

            // 3. Validar Formato (Sin caracteres especiales)
            if (!Input.Name.IsValidName())
            {
                ModelState.AddModelError("Input.Name", NotificationHelper.Countries.InvalidFormat);
                ViewData["CountryId"] = new SelectList(_context.Countries.Where(c => c.Status == GeneralStatus.Activo), "Id", "Name");
                return Page();
            }

            // 4. Obtener nombre del país para el mensaje de éxito
            var country = await _context.Countries.FindAsync(Input.CountryId);

            var city = new City
            {
                CountryId = Input.CountryId,
                Name = Input.Name.Clean(),
                Region = Input.Region?.Clean(),
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.UtcNow // Estándar de auditoría
            };

            _context.Cities.Add(city);
            await _context.SaveChangesAsync();

            // 4. Notificación Estandarizada
            TempData.Success(NotificationHelper.Cities.Created(city.Name, country?.Name ?? "N/A"));

            return RedirectToPage("./Index");
        }
    }
}
