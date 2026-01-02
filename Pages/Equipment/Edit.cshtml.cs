using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Equipment
{
    [Authorize(Roles = AuthorizationHelper.ManagementRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<User> userManager)
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

            var equipment = await _context.Equipments
                .Include(e => e.Country)
                .Include(e => e.City)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipment == null)
            {
                return NotFound();
            }

            Equipment = equipment;
            CargarViewData();
            CargarEstadosEquipo();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var city = await _context.Cities.FindAsync(Equipment.CityId);
            if (city != null)
            {
                Equipment.CountryId = city.CountryId;
            }

            ModelState.Remove("Equipment.CountryId");
            ModelState.Remove("Equipment.Country");
            ModelState.Remove("Equipment.City");
            ModelState.Remove("Equipment.EquipmentType");
            ModelState.Remove("Equipment.Laboratory");
            ModelState.Remove("Equipment.CreatedBy");
            ModelState.Remove("Equipment.ModifiedBy");
            ModelState.Remove("Equipment.EquipmentStateHistories");

            if (!ModelState.IsValid)
            {
                CargarViewData();
                CargarEstadosEquipo();
                return Page();
            }

            var equipmentBD = await _context.Equipments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == Equipment.Id);

            if (equipmentBD == null)
            {
                return NotFound();
            }

            bool statusChanged = equipmentBD.CurrentStatus != Equipment.CurrentStatus;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    Equipment.LastModifiedDate = DateTime.Now;
                    
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        Equipment.ModifiedById = currentUser.Id;
                    }

                    _context.Attach(Equipment).State = EntityState.Modified;

                    _context.Entry(Equipment).Property(e => e.CreatedById).IsModified = false;
                    _context.Entry(Equipment).Property(e => e.CreatedDate).IsModified = false;

                    await _context.SaveChangesAsync();

                    if (statusChanged)
                    {
                        var lastHistory = await _context.EquipmentStateHistories
                            .Where(h => h.EquipmentId == Equipment.Id && h.EndDate == null)
                            .OrderByDescending(h => h.StartDate)
                            .FirstOrDefaultAsync();

                        if (lastHistory != null)
                        {
                            lastHistory.EndDate = DateTime.Now;
                            _context.EquipmentStateHistories.Update(lastHistory);
                        }

                        var newHistory = new EquipmentStateHistory
                        {
                            EquipmentId = Equipment.Id,
                            Status = Equipment.CurrentStatus,
                            StartDate = DateTime.Now,
                            RegisteredDate = DateTime.Now,
                            RegisteredById = currentUser?.Id,
                            Reason = "Status/Info updated" 
                        };
                        _context.EquipmentStateHistories.Add(newHistory);

                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool EquipmentExists(int id)
        {
            return _context.Equipments.Any(e => e.Id == id);
        }

        private void CargarViewData()
        {
            ViewData["CityId"] = new SelectList(
                _context.Cities
                    .Include(c => c.Country)
                    .Where(c => c.Status == GeneralStatus.Active),
                "Id",
                "Name"
            );

            ViewData["LaboratoryId"] = new SelectList(
                _context.Laboratories.Where(l => l.Status == GeneralStatus.Active),
                "Id",
                "Name"
            );

            ViewData["EquipmentTypeId"] = new SelectList(
                _context.EquipmentTypes,
                "Id",
                "Name"
            );
        }

        private void CargarEstadosEquipo()
        {
            var estados = Enum.GetValues(typeof(EquipmentStatus))
                .Cast<EquipmentStatus>()
                .Where(e => e != EquipmentStatus.Deleted)
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                });

            ViewData["ListaEstadosEquipo"] = new SelectList(estados, "Value", "Text", (int)Equipment.CurrentStatus);
        }
    }
}