using Microsoft.AspNetCore.Authorization;
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

        public DeleteModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Country Country { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var country = await _context.Countries
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (country == null)
            {
                return NotFound();
            }
            else
            {
                Country = country;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // COUNTRIES
            try
            {
                var country = await _context.Countries.FindAsync(id);
                if (country != null)
                {
                    country.Status = GeneralStatus.Eliminado; // Soft delete
                    _context.Attach(country).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    
                    TempData.Success(NotificationHelper.Countries.Deleted(country.Name));
                }
            }
            catch (Exception ex)
            {
                TempData.Error("Hubo un error al intentar eliminar el país: " + ex.Message);
                return RedirectToPage("/Cities/Index");
            }

            return RedirectToPage("/Cities/Index");
        }
    }
}
