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
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public DeleteModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
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
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (country == null) return NotFound();
            
            Country = country;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var country = await _context.Countries.FindAsync(id);
            if (country == null) return NotFound();

            // Decisión del usuario (2026-10-18): un país con ciudades activas no se da de baja (antes sus ciudades se ocultaban en cascada).
            var activeCities = await _context.Cities.CountAsync(c => c.CountryId == country.Id && c.Status != GeneralStatus.Eliminado);
            if (activeCities > 0)
            {
                TempData.Warning($"No se puede dar de baja el país '{country.Name}': tiene {activeCities} ciudad(es) activa(s). Dé de baja o reasigne primero sus ciudades.");
                return RedirectToPage("./Index");
            }

            // Perform Soft Delete (Logic Delete)
            country.Status = GeneralStatus.Eliminado;
            country.LastModifiedDate = DateTime.UtcNow;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                country.ModifiedById = currentUser.Id;
            }

            _context.Attach(country).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            TempData.Success($"El país '{country.Name}' ha sido dado de baja correctamente.");
            
            return RedirectToPage("./Index");
        }
    }
}
