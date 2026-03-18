using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Laboratories
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
        public Laboratory Laboratory { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var laboratory = await _context.Laboratories
                .Include(l => l.Faculty)
                .Include(l => l.CreatedBy)
                .Include(l => l.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (laboratory == null) return NotFound();
            
            Laboratory = laboratory;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var laboratory = await _context.Laboratories.FindAsync(id);
            if (laboratory == null) return NotFound();

            // Perform Soft Delete (Logic Delete for Audit)
            laboratory.Status = GeneralStatus.Eliminado;
            laboratory.LastModifiedDate = DateTime.UtcNow;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                laboratory.ModifiedById = currentUser.Id;
            }

            try
            {
                _context.Attach(laboratory).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                TempData.Success($"El laboratorio '{laboratory.Name}' ha sido dado de baja del sistema institucional.");
            }
            catch (Exception ex)
            {
                TempData.Error("Hubo un error al intentar procesar la baja: " + ex.Message);
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}
