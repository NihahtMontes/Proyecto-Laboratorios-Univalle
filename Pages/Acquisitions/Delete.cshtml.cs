using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Acquisitions
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public DeleteModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Request AcquisitionRequest { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();
            
            AcquisitionRequest = request;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.Requests.FindAsync(id);
            if (request != null)
            {
                request.Status = RequestStatus.Cancelled;
                request.RejectionReason = string.IsNullOrWhiteSpace(request.RejectionReason)
                    ? "Solicitud marcada como eliminada lógicamente."
                    : request.RejectionReason;
                request.LastModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData.Success("Solicitud marcada como eliminada correctamente.");
            }

            return RedirectToPage("./Index");
        }
    }
}
