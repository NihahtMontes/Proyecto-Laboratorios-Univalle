using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Equipment
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
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
        public Proyecto_Laboratorios_Univalle.Models.Equipment Equipment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipment = await _context.Equipments.FirstOrDefaultAsync(m => m.Id == id);

            if (equipment == null)
            {
                return NotFound();
            }
            else
            {
                Equipment = equipment;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment != null)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                int? userId = currentUser?.Id;

                equipment.CurrentStatus = EquipmentStatus.Deleted;
                equipment.LastModifiedDate = DateTime.Now;
                equipment.ModifiedById = userId;

                var newHistory = new EquipmentStateHistory
                {
                    EquipmentId = equipment.Id,
                    Status = equipment.CurrentStatus,
                    StartDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    Reason = "Equipment deleted",
                    CreatedById = userId
                };

                var lastHistory = await _context.EquipmentStateHistories
                  .Where(h => h.EquipmentId == equipment.Id && h.EndDate == null)
                  .OrderByDescending(h => h.StartDate)
                  .FirstOrDefaultAsync();

                if (lastHistory != null)
                    lastHistory.EndDate = DateTime.Now;

                _context.EquipmentStateHistories.Add(newHistory);

                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
