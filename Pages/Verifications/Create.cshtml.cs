using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Verifications
{
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            ViewData["EquipmentId"] = new SelectList(_context.Equipments, "Id", "Name");
            return Page();
        }

        [BindProperty]
        public Verification Verification { get; set; } = default!;

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

            Verification.CreatedDate = DateTime.Now;
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                Verification.CreatedById = currentUser.Id;
            }

            _context.Verifications.Add(Verification);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
