using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Persons
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
        public Person Person { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Retrieve the person including audit details
            var person = await _context.People
                .Include(p => p.CreatedBy)
                .Include(p => p.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (person == null)
            {
                return NotFound();
            }

            Person = person;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Find the person in the database
            var person = await _context.People.FindAsync(id);
            if (person == null)
            {
                return NotFound();
            }

            // Check if already deleted
            if (person.Status == GeneralStatus.Eliminado)
            {
                TempData.Warning($"La identidad de '{person.FullName}' ya se encuentra dada de baja.");
                return RedirectToPage("./Index");
            }

            // Perform Soft Delete (Logic Delete for Audit)
            person.Status = GeneralStatus.Eliminado;
            person.LastModifiedDate = DateTime.UtcNow;

            // Get current user for audit tracking
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                person.ModifiedById = currentUser.Id;
            }

            try
            {
                _context.People.Update(person);
                await _context.SaveChangesAsync();
                TempData.Success($"La identidad de '{person.FullName}' ha sido dada de baja correctamente.");
            }
            catch (Exception ex)
            {
                TempData.Error($"Hubo un error al intentar procesar la baja: {ex.Message}");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("./Index");
        }
    }
}
