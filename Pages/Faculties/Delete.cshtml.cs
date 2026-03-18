using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Faculties
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
        public Faculty Faculty { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (faculty == null) return NotFound();
            
            Faculty = faculty;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound();

            // Perform Soft Delete (Logic Delete for Audit)
            faculty.Status = GeneralStatus.Eliminado;
            faculty.LastModifiedDate = DateTime.UtcNow;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                faculty.ModifiedById = currentUser.Id;
            }

            try
            {
                _context.Attach(faculty).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                TempData.Success($"La Facultad académica '{faculty.Name}' ha sido dada de baja del sistema institucional.");
            }
            catch (Exception ex)
            {
                TempData.Error("Hubo un error al intentar procesar la baja: " + ex.Message);
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("/Laboratories/Index", null, "faculties");
        }
    }
}
