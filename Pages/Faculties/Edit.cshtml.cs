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
        public Faculty Faculty { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var faculty = await _context.Faculties
                .Include(f => f.CreatedBy)
                .Include(f => f.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (faculty == null) return NotFound();
            
            Faculty = faculty;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadFaculty(Faculty.Id);
                return Page();
            }

            // Normalization
            var normalizedName = Faculty.Name.Trim().ToLower();

            // Duplicate Validation (excluding current ID)
            var exists = await _context.Faculties
                .AnyAsync(f => f.Id != Faculty.Id && 
                               f.Name.Trim().ToLower() == normalizedName && 
                               f.Status != GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("Faculty.Name", "Ya existe otra facultad con este nombre en el sistema institucional.");
                await ReloadFaculty(Faculty.Id);
                return Page();
            }

            var facultyToUpdate = await _context.Faculties.FindAsync(Faculty.Id);
            if (facultyToUpdate == null) return NotFound();

            // Entity Mapping
            facultyToUpdate.Name = Faculty.Name.Clean();
            facultyToUpdate.Code = Faculty.Code?.Clean().ToUpper();
            facultyToUpdate.Description = Faculty.Description?.Clean();
            facultyToUpdate.Status = Faculty.Status;
            facultyToUpdate.LastModifiedDate = DateTime.UtcNow;

            // Set current auditor
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                facultyToUpdate.ModifiedById = currentUser.Id;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Datos de la Facultad '{facultyToUpdate.Name}' actualizados correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FacultyExists(Faculty.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("/Laboratories/Index", null, "faculties");
        }

        private async Task ReloadFaculty(int id)
        {
            Faculty = await _context.Faculties
                .Include(f => f.CreatedBy)
                .Include(f => f.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id) ?? new Faculty();
        }

        private bool FacultyExists(int id)
        {
            return _context.Faculties.Any(e => e.Id == id);
        }
    }
}
