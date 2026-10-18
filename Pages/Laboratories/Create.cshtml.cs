using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Laboratories
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            LoadFaculties();
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "La facultad responsable es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            [Required(ErrorMessage = "El nombre del laboratorio es obligatorio")]
            [Display(Name = "Nombre")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Piso")]
            [StringLength(50, ErrorMessage = "El piso o nivel no puede superar los 50 caracteres")]
            public string? Floor { get; set; }

            [Display(Name = "Descripción")]
            [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres")]
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadFaculties();
                return Page();
            }

            // La facultad debe existir y estar activa (antes una facultad inexistente provocaba un error 500 por la FK).
            if (!await _context.Faculties.AnyAsync(f => f.Id == Input.FacultyId && f.Status == GeneralStatus.Activo))
            {
                ModelState.AddModelError("Input.FacultyId", "La facultad seleccionada no está disponible.");
                LoadFaculties();
                return Page();
            }

            // Normalization
            var normalizedName = Input.Name.Clean().ToLower();

            // Validate Duplicate Name within the same Faculty
            bool nameExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.FacultyId == Input.FacultyId &&
                               l.Name.ToLower() == normalizedName &&
                               l.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe un ambiente con este nombre en la facultad seleccionada.");
                LoadFaculties();
                return Page();
            }

            // Entity Mapping
            var laboratory = new Laboratory
            {
                FacultyId = Input.FacultyId,
                Code = string.Empty,
                Name = Input.Name.Clean(),
                Floor = Input.Floor?.Clean(),
                Description = Input.Description?.Clean(),
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.UtcNow
            };

            // Set current auditor
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                laboratory.CreatedById = currentUser.Id;
            }

            _context.Laboratories.Add(laboratory);
            await _context.SaveWithCodeAsync(
                _context.Laboratories.IgnoreQueryFilters().Select(x => (string?)x.Code),
                CatalogCodes.Laboratory,
                code => laboratory.Code = code);

            TempData.Success($"Laboratorio '{laboratory.Code}' ({laboratory.Name}) registrado exitosamente en el catálogo.");
            return RedirectToPage("./Index");
        }

        private void LoadFaculties()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo)
                .OrderBy(f => f.Name), "Id", "Name");
        }
    }
}
