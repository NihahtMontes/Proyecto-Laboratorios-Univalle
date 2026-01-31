using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;

namespace Proyecto_Laboratorios_Univalle.Pages.Faculties
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
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

            var normalizedName = Faculty.Name.NormalizeComparison();

            var exists = await _context.Faculties
                .AnyAsync(f => f.Id != Faculty.Id && 
                               f.Name.Trim().ToLower() == normalizedName && 
                               f.Status != Proyecto_Laboratorios_Univalle.Models.Enums.GeneralStatus.Eliminado);

            if (exists)
            {
                ModelState.AddModelError("Faculty.Name", NotificationHelper.Faculties.FacultyNameDuplicate);
                await ReloadFaculty(Faculty.Id);
                return Page();
            }

            if (!Faculty.Name.IsValidName())
            {
                ModelState.AddModelError("Faculty.Name", NotificationHelper.Countries.InvalidFormat);
                await ReloadFaculty(Faculty.Id);
                return Page();
            }

            var facultyToUpdate = await _context.Faculties.FindAsync(Faculty.Id);
            if (facultyToUpdate == null) return NotFound();

            facultyToUpdate.Name = Faculty.Name.Clean();
            facultyToUpdate.Code = Faculty.Code?.Clean().ToUpper();
            facultyToUpdate.Description = Faculty.Description?.Clean();
            facultyToUpdate.Status = Faculty.Status;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success(NotificationHelper.Faculties.Updated(facultyToUpdate.Name));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FacultyExists(Faculty.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("/Laboratories/Index");
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
