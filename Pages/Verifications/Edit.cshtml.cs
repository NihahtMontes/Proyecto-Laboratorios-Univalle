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

            var verification = await _context.Verifications
                .Include(v => v.EquipmentUnit)
                    .ThenInclude(eu => eu.Equipment)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (verification == null)
            {
                return NotFound();
            }
            Verification = verification;

            var units = _context.EquipmentUnits
                .Include(u => u.Equipment)
                .Select(u => new { 
                    Id = u.Id, 
                    DisplayName = $"{u.Equipment.Name} (INV: {u.InventoryNumber})" 
                })
                .ToList();

            ViewData["EquipmentUnitId"] = new SelectList(units, "Id", "DisplayName");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove navigational properties from validation to avoid circular reference or required errors
            ModelState.Remove("Verification.CreatedBy");
            ModelState.Remove("Verification.ModifiedBy");
            ModelState.Remove("Verification.EquipmentUnit");

            if (!ModelState.IsValid)
            {
                var units = _context.EquipmentUnits
                    .Include(u => u.Equipment)
                    .Select(u => new { 
                        Id = u.Id, 
                        DisplayName = $"{u.Equipment.Name} (INV: {u.InventoryNumber})" 
                    })
                    .ToList();

                ViewData["EquipmentUnitId"] = new SelectList(units, "Id", "DisplayName");
                return Page();
            }

            // Set audit tracking for modification
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                Verification.ModifiedById = user.Id;
            }
            Verification.LastModifiedDate = DateTime.Now;

            _context.Attach(Verification).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success(NotificationHelper.Verifications.Updated(Verification.Id));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VerificationExists(Verification.Id))
                {
                    return NotFound();
                }
                else
                {
                    TempData.Error(NotificationHelper.Requests.SaveError("Concurrency conflict."));
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
