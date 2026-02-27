using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Career
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
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

        public IActionResult OnGet()
        {
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

            var career = new Proyecto_Laboratorios_Univalle.Models.Career
            {
                Name = Input.Name.Clean(),
                FacultadId = Input.FacultadId,
                Status = Input.Status
            };

            _context.Careers.Add(career);
            await _context.SaveChangesAsync();

            TempData.Success("Carrera creada correctamente.");
            return RedirectToPage("./Index");
        }

        private void LoadLists()
        {
            ViewData["FacultadId"] = new SelectList(_context.Faculties.OrderBy(f => f.Name), "Id", "Name");
        }
    }
}
