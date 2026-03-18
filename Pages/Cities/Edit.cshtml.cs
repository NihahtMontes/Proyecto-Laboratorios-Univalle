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
        public City City { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var city = await _context.Cities
                .Include(c => c.Country)
                .Include(c => c.CreatedBy)
                .Include(c => c.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (city == null) return NotFound();
            
            City = city;
            LoadCountries();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadCity(City.Id);
                LoadCountries();
                return Page();
            }

            // Normalization
            var normalizedName = City.Name.Trim().ToLower();

            // Duplicate Validation (Same country, different ID)
            var exists = await _context.Cities
                .AnyAsync(c => c.Id != City.Id && 
                               c.CountryId == City.CountryId && 
                               c.Name.Trim().ToLower() == normalizedName && 
                               c.Status != GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("City.Name", "Esta ciudad ya se encuentra registrada para el país seleccionado.");
                await ReloadCity(City.Id);
                LoadCountries();
                return Page();
            }

            var cityToUpdate = await _context.Cities.FindAsync(City.Id);
            if (cityToUpdate == null) return NotFound();

            // Mapping
            cityToUpdate.Name = City.Name.Clean();
            cityToUpdate.CountryId = City.CountryId;
            cityToUpdate.Region = City.Region?.Clean();
            cityToUpdate.Status = City.Status;
            cityToUpdate.LastModifiedDate = DateTime.UtcNow;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                cityToUpdate.ModifiedById = currentUser.Id;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Localidad '{cityToUpdate.Name}' actualizada exitosamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CityExists(City.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private async Task ReloadCity(int id)
        {
            City = await _context.Cities
                .Include(c => c.Country)
                .Include(c => c.CreatedBy)
                .Include(c => c.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id) ?? new City();
        }

        private void LoadCountries()
        {
            ViewData["CountryId"] = new SelectList(
                _context.Countries
                    .Where(c => c.Status == GeneralStatus.Activo || c.Id == City.CountryId)
                    .OrderBy(c => c.Name), 
                "Id", 
                "Name", 
                City.CountryId);
        }

        private bool CityExists(int id)
        {
            return _context.Cities.Any(e => e.Id == id);
        }
    }
}
