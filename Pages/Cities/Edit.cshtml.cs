using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Cities
{

    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public City City { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var city = await _context.Cities.FirstOrDefaultAsync(m => m.Id == id);
            if (city == null)
            {
                return NotFound();
            }
            City = city;
            ViewData["CountryId"] = new SelectList(_context.Countries, "Id", "Name");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("City.Country");

            if (!ModelState.IsValid)
            {
                ViewData["CountryId"] = new SelectList(_context.Countries, "Id", "Name");
                return Page();
            }

            // 1. Normalizar
            var normalizedName = City.Name.NormalizeComparison();

            // 2. Validar Duplicados (Mismo país, diferente ID)
            var exists = await _context.Cities
                .AnyAsync(c => c.Id != City.Id && 
                               c.CountryId == City.CountryId && 
                               c.Name.Trim().ToLower() == normalizedName && 
                               c.Status != Proyecto_Laboratorios_Univalle.Models.Enums.GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("City.Name", NotificationHelper.Cities.CityNameDuplicate);
                ViewData["CountryId"] = new SelectList(_context.Countries, "Id", "Name");
                return Page();
            }

            // 3. Validar Formato
            if (!City.Name.IsValidName())
            {
                ModelState.AddModelError("City.Name", NotificationHelper.Countries.InvalidFormat);
                ViewData["CountryId"] = new SelectList(_context.Countries, "Id", "Name");
                return Page();
            }

            _context.Attach(City).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CityExists(City.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool CityExists(int id)
        {
            return _context.Cities.Any(e => e.Id == id);
        }
    }
}
