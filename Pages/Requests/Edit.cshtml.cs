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

namespace Proyecto_Laboratorios_Univalle.Pages.Requests
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
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
        public InputModel Input { get; set; } = new();

        public Request Request { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "La descripción es obligatoria")]
            [Display(Name = "Descripción del Problema")]
            public string Description { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Prioridad")]
            public RequestPriority Priority { get; set; }

            [Display(Name = "Estado de la Solicitud")]
            public RequestStatus Status { get; set; }

            [Display(Name = "Observaciones del Solicitante")]
            public string? Observations { get; set; }

            [Display(Name = "Aprobado Por")]
            public int? ApprovedById { get; set; }

            [Display(Name = "Motivo de Rechazo")]
            public string? RejectionReason { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Request = await _context.Requests
                .Include(r => r.Equipment)
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Request == null) return NotFound();

            Input = new InputModel
            {
                Id = Request.Id,
                Description = Request.Description,
                Priority = Request.Priority,
                Status = Request.Status,
                Observations = Request.Observations,
                ApprovedById = Request.ApprovedById,
                RejectionReason = Request.RejectionReason
            };

            await CargarListas();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Request = await _context.Requests
                    .Include(r => r.Equipment)
                    .Include(r => r.RequestedBy)
                    .FirstOrDefaultAsync(m => m.Id == Input.Id);
                await CargarListas();
                return Page();
            }

            var requestToUpdate = await _context.Requests.FindAsync(Input.Id);
            if (requestToUpdate == null) return NotFound();

            // Solo administradores pueden aprobar o rechazar
            var isUserAdmin = User.IsInRole("Administrator") || User.IsInRole("SuperAdmin");
            var currentUser = await _userManager.GetUserAsync(User);

            if (isUserAdmin)
            {
                // Si el estado cambió de Pendiente a Aprobado/Rechazado
                if (requestToUpdate.Status == RequestStatus.Pending && Input.Status != RequestStatus.Pending)
                {
                    requestToUpdate.ApprovalDate = DateTime.Now;
                    requestToUpdate.ApprovedById = currentUser?.Id;
                }
                
                requestToUpdate.Status = Input.Status;
                requestToUpdate.RejectionReason = Input.RejectionReason?.Clean();
                requestToUpdate.ApprovedById = Input.ApprovedById ?? requestToUpdate.ApprovedById;
            }

            requestToUpdate.Description = Input.Description.Clean();
            requestToUpdate.Priority = Input.Priority;
            requestToUpdate.Observations = Input.Observations?.Clean();
            requestToUpdate.LastModifiedDate = DateTime.Now;
            requestToUpdate.ModifiedById = currentUser?.Id;
            _context.Attach(requestToUpdate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success(NotificationHelper.Requests.Updated(requestToUpdate.Id));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RequestExists(requestToUpdate.Id))
                {
                    return NotFound();
                }
                else
                {
                    TempData.Error(NotificationHelper.Requests.SaveError("Error de concurrencia al guardar."));
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private async Task CargarListas()
        {
            var evaluadores = await _context.Users
                .Where(u => (u.Role == UserRole.Administrador || u.Role == UserRole.SuperAdmin) && u.Status == GeneralStatus.Activo)
                .OrderBy(u => u.FirstName)
                .Select(u => new { Id = u.Id, FullName = u.FullName })
                .ToListAsync();

            ViewData["ApprovedById"] = new SelectList(evaluadores, "Id", "FullName");
        }

        private bool RequestExists(int id)
        {
            return _context.Requests.Any(e => e.Id == id);
        }
    }
}
