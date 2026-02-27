using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Career
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public int CareerId { get; set; }

        public class InputModel
        {
            [Key]
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre de la carrera es obligatorio")]
            [StringLength(200)]
            [Display(Name = "Nombre de la Carrera")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Facultad")]
            public int? FacultadId { get; set; }

            [Required]
            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var career = await _context.Careers.FirstOrDefaultAsync(m => m.Id == id);
            if (career == null) return NotFound();

            CareerId = career.Id;
            Input = new InputModel
            {
                Id = career.Id,
                Name = career.Name,
                FacultadId = career.FacultadId,
                Status = career.Status
            };

            LoadLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadLists();
                return Page();
            }

            var career = await _context.Careers.FindAsync(Input.Id);
            if (career == null) return NotFound();

            career.Name = Input.Name.Clean();
            career.FacultadId = Input.FacultadId;
            career.Status = Input.Status;

            _context.Attach(career).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CareerExists(Input.Id)) return NotFound();
                else throw;
            }

            TempData.Success("Carrera actualizada correctamente.");
            return RedirectToPage("./Index");
        }

        private bool CareerExists(int id)
        {
            return _context.Careers.Any(e => e.Id == id);
        }

        private void LoadLists()
        {
            ViewData["FacultadId"] = new SelectList(_context.Faculties.OrderBy(f => f.Name), "Id", "Name", Input.FacultadId);
        }
    }
}
