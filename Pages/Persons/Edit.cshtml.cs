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
        public Person Person { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Uniqueness Check (Ignoring self)
            bool ciExists = await _context.People
                .AnyAsync(p => p.IdentityCard == Person.IdentityCard.Trim().ToUpper() && 
                               p.Id != Person.Id && 
                               p.Status != GeneralStatus.Eliminado);

            if (ciExists)
            {
                ModelState.AddModelError("Person.IdentityCard", "Este documento de identidad ya pertenece a otro registro.");
                return Page();
            }

            // Normalization
            Person.FirstName = Person.FirstName.Clean();
            Person.LastName = Person.LastName.Clean();
            Person.IdentityCard = Person.IdentityCard.Trim().ToUpper();
            Person.Email = Person.Email?.Trim().ToLower();

            // Status Preservation/Update
            // Auditing
            Person.LastModifiedDate = DateTime.Now;
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                Person.ModifiedById = currentUser.Id;
            }

            _context.Attach(Person).State = EntityState.Modified;

            // These fields should not be changed by the Edit form
            _context.Entry(Person).Property(x => x.CreatedById).IsModified = false;
            _context.Entry(Person).Property(x => x.CreatedDate).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Datos de '{Person.FullName}' actualizados correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PersonExists(Person.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData.Error($"Error al actualizar la información: {ex.Message}");
                return Page();
            }

            return RedirectToPage("./Index");
        }

        private bool PersonExists(int id)
        {
            return _context.People.Any(e => e.Id == id);
        }
    }
}
