using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
            if (id == null) return NotFound();

            var equipment = await _context.Equipments
                // .Include(m => m.EquipmentType) // CORRECCIÓN: Se elimina esta línea porque ya no existe la relación física
                .Include(m => m.Units!)
                    .ThenInclude(u => u.Laboratory)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (equipment == null) return NotFound();

            Equipment = equipment;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var equipment = await _context.Equipments
                .Include(e => e.Units)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (equipment == null) return NotFound();

            if (equipment.Units != null && equipment.Units.Any(u => u.CurrentStatus != EquipmentStatus.Deleted))
            {
                TempData.Error($"No se puede eliminar '{equipment.Name}' porque tiene unidades activas asociadas. Elimine o dé de baja las unidades primero.");
                return RedirectToPage("./Details", new { id = equipment.Id });
            }

            _context.Equipments.Remove(equipment);
            await _context.SaveChangesAsync();

            TempData.Success($"La definición de equipo '{equipment.Name}' ha sido eliminada correctamente.");
            return RedirectToPage("./Index");
        }
    }
}