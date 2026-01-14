using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            if (id == null)
            {
                return NotFound();
            }

            var laboratory = await _context.Laboratories
                .Include(l => l.Faculty)
                .Include(l => l.CreatedBy)
                .Include(l => l.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (laboratory == null)
            {
                return NotFound();
            }
            else
            {
                Laboratory = laboratory;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var laboratory = await _context.Laboratories.FindAsync(id);
            if (laboratory != null)
            {
                laboratory.Status = GeneralStatus.Eliminado;
                laboratory.LastModifiedDate = DateTime.Now;
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    laboratory.ModifiedById = currentUser.Id;
                }
                
                _context.Laboratories.Update(laboratory);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
