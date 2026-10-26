using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Faculties
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class DeleteModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<DeleteModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public Faculty Faculty { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties
                .Include(m => m.CreatedBy)
                .Include(m => m.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (faculty == null) return NotFound();
            
            Faculty = faculty;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound();

            // Decisión del usuario (2026-10-18): una facultad con laboratorios activos no se da de baja (antes se ocultaban en cascada).
            var activeLabs = await _context.Laboratories.CountAsync(l => l.FacultyId == faculty.Id && l.Status != GeneralStatus.Eliminado);
            if (activeLabs > 0)
            {
                TempData.Warning($"No se puede dar de baja la facultad '{faculty.Name}': tiene {activeLabs} laboratorio(s) activo(s). Dé de baja o reasigne primero sus laboratorios.");
                return RedirectToPage("/Laboratories/Index", null, "faculties");
            }

            // Perform Soft Delete (Logic Delete for Audit)
            faculty.Status = GeneralStatus.Eliminado;
            faculty.LastModifiedDate = DateTime.UtcNow;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                faculty.ModifiedById = currentUser.Id;
            }

            try
            {
                _context.Attach(faculty).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                TempData.Success($"La Facultad académica '{faculty.Name}' ha sido dada de baja del sistema institucional.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo dar de baja la facultad {FacultyId}.", id);
                TempData.Error("No se pudo dar de baja la facultad. Intente nuevamente.");
                return RedirectToPage("./Delete", new { id });
            }

            return RedirectToPage("/Laboratories/Index", null, "faculties");
        }
    }
}
