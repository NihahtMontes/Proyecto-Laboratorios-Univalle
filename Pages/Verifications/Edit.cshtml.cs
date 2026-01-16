using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
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
        public Verification Verification { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var verification = await _context.Verifications.FirstOrDefaultAsync(m => m.Id == id);
            if (verification == null)
            {
                return NotFound();
            }
            Verification = verification;
            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Verification.CreatedBy");
            ModelState.Remove("Verification.ModifiedBy");
            ModelState.Remove("Verification.Equipment");

            if (!ModelState.IsValid)
            {
                ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
                return Page();
            }

            _context.Attach(Verification).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VerificationExists(Verification.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool VerificationExists(int id)
        {
            return _context.Verifications.Any(e => e.Id == id);
        }
    }
}
