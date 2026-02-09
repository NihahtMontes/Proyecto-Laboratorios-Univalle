using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Cities
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
        public City City { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var city = await _context.Cities
                .Include(c => c.Country)
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (city == null) return NotFound();
            
            City = city;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var city = await _context.Cities.FindAsync(id);
            if (city == null) return NotFound();

            // Perform Soft Delete (Logic Delete)
            city.Status = GeneralStatus.Eliminado;
            city.LastModifiedDate = DateTime.Now;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                city.ModifiedById = currentUser.Id;
            }

            try
            {
                _context.Attach(city).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                TempData.Success($"La localidad '{city.Name}' ha sido dada de baja del sistema.");
            }
            catch (Exception ex)
            {
                TempData.Error("Hubo un error al intentar eliminar la ciudad: " + ex.Message);
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}
