using Microsoft.AspNetCore.Authorization;
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

        public DeleteModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
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

            var city = await _context.Cities
                .Include(c => c.Country)
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (city == null)
            {
                return NotFound();
            }
            else
            {
                City = city;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            // CITIES
            try
            {
                var city = await _context.Cities.FindAsync(id);
                if (city != null)
                {
                    city.Status = GeneralStatus.Eliminado; // Soft delete
                    _context.Attach(city).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    
                    TempData.Success(NotificationHelper.Cities.Deleted(city.Name));
                }
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
