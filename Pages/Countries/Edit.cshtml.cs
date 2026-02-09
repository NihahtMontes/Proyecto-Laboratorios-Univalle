using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Countries
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public Country Country { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var country = await _context.Countries
                .Include(c => c.CreatedBy)
                .Include(c => c.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (country == null) return NotFound();
            
            Country = country;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadCountry(Country.Id);
                return Page();
            }

            // Normalization
            var normalizedName = Country.Name.Trim().ToLower();

            // Duplicate Validation (Excluding current ID)
            var exists = await _context.Countries
                .AnyAsync(c => c.Id != Country.Id && 
                               c.Name.Trim().ToLower() == normalizedName && 
                               c.Status != GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("Country.Name", "Ya existe un país con este nombre en el sistema.");
                await ReloadCountry(Country.Id);
                return Page();
            }

            var countryToUpdate = await _context.Countries.FindAsync(Country.Id);
            if (countryToUpdate == null) return NotFound();

            // Mapping
            countryToUpdate.Name = Country.Name.Clean();
            countryToUpdate.Status = Country.Status;
            countryToUpdate.LastModifiedDate = DateTime.Now;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                countryToUpdate.ModifiedById = currentUser.Id;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"País '{countryToUpdate.Name}' actualizado correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CountryExists(Country.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private async Task ReloadCountry(int id)
        {
            Country = await _context.Countries
                .Include(c => c.CreatedBy)
                .Include(c => c.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id) ?? new Country();
        }

        private bool CountryExists(int id)
        {
            return _context.Countries.Any(e => e.Id == id);
        }
    }
}