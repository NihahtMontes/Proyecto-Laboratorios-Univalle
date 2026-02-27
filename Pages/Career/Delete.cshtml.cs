using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Career
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Proyecto_Laboratorios_Univalle.Models.Career Career { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var career = await _context.Careers
                .Include(c => c.Facultad)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (career == null) return NotFound();
            
            Career = career;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var career = await _context.Careers.FindAsync(id);
            if (career != null)
            {
                // Soft delete
                career.Status = GeneralStatus.Eliminado;
                await _context.SaveChangesAsync();
                TempData.Success("Carrera eliminada correctamente.");
            }

            return RedirectToPage("./Index");
        }
    }
}
