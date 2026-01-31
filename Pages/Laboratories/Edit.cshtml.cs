using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Laboratories
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
        public LabInputModel Input { get; set; } = new();

        public int Id { get; set; }
        public Laboratory ExistingLaboratory { get; set; } = default!;

        public class LabInputModel
        {
            public int FacultyId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Type { get; set; }
            public string? Building { get; set; }
            public string? Floor { get; set; }
            public string? Description { get; set; }
            public GeneralStatus Status { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            ExistingLaboratory = await _context.Laboratories
                .Include(l => l.CreatedBy)
                .Include(l => l.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ExistingLaboratory == null) return NotFound();

            Id = ExistingLaboratory.Id;

            Input = new LabInputModel
            {
                FacultyId = ExistingLaboratory.FacultyId,
                Code = ExistingLaboratory.Code,
                Name = ExistingLaboratory.Name,
                Type = ExistingLaboratory.Type,
                Building = ExistingLaboratory.Building,
                Floor = ExistingLaboratory.Floor,
                Description = ExistingLaboratory.Description,
                Status = ExistingLaboratory.Status
            };

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name", ExistingLaboratory.FacultyId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Id = id;

            if (!ModelState.IsValid)
            {
                await LoadLaboratoryData(id);
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name", Input.FacultyId);
                return Page();
            }

            var normalizedName = Input.Name.NormalizeComparison();
            var normalizedCode = Input.Code.NormalizeComparison().ToUpper();

            bool codeExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Id != id && 
                               l.Code.ToUpper() == normalizedCode && 
                               l.Status != GeneralStatus.Eliminado);

            if (codeExists)
            {
                ModelState.AddModelError("Input.Code", NotificationHelper.Laboratories.LabCodeDuplicate);
                await LoadLaboratoryData(id);
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name", Input.FacultyId);
                return Page();
            }

            bool nameExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Id != id && 
                               l.FacultyId == Input.FacultyId && 
                               l.Name.ToLower() == normalizedName && 
                               l.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", NotificationHelper.Laboratories.LabNameDuplicate);
                await LoadLaboratoryData(id);
                ViewData["FacultyId"] = new SelectList(_context.Faculties, "Id", "Name", Input.FacultyId);
                return Page();
            }

            var laboratory = await _context.Laboratories.FindAsync(id);
            if (laboratory == null) return NotFound();

            laboratory.FacultyId = Input.FacultyId;
            laboratory.Code = Input.Code.Clean().ToUpper();
            laboratory.Name = Input.Name.Clean();
            laboratory.Type = Input.Type?.Clean();
            laboratory.Building = Input.Building?.Clean();
            laboratory.Floor = Input.Floor?.Clean();
            laboratory.Description = Input.Description?.Clean();
            laboratory.Status = Input.Status;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success(NotificationHelper.Laboratories.Updated(laboratory.Name));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LaboratoryExists(laboratory.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadLaboratoryData(int id)
        {
            ExistingLaboratory = await _context.Laboratories
                .Include(l => l.CreatedBy)
                .Include(l => l.ModifiedBy)
                .FirstOrDefaultAsync(l => l.Id == id) ?? new Laboratory();
        }

        private bool LaboratoryExists(int id)
        {
            return _context.Laboratories.Any(e => e.Id == id);
        }
    }
}
