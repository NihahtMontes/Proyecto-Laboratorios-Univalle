using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
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
        public Request MaintenanceRequest { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.RequestedBy)
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();
            
            MaintenanceRequest = request;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.Maintenance)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            // Referential Integrity Validation: Cannot delete if maintenance already exists linked to this request
            if (request.Maintenance != null)
            {
                TempData.Error("No se puede eliminar la solicitud porque ya tiene un registro de mantenimiento asociado.");
                return RedirectToPage("./Index");
            }

            MaintenanceRequest = request;
            _context.Requests.Remove(MaintenanceRequest);
            await _context.SaveChangesAsync();
            
            TempData.Success($"La solicitud #{request.Id} ha sido eliminada correctamente.");
            return RedirectToPage("./Index");
        }
    }
}
