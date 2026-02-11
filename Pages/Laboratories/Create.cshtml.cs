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

            [Required(ErrorMessage = "El código identificador es obligatorio")]
            [Display(Name = "Código")]
            [StringLength(20, ErrorMessage = "El código no puede superar los 20 caracteres")]
            public string Code { get; set; } = string.Empty;

            [Required(ErrorMessage = "El nombre del laboratorio es obligatorio")]
            [Display(Name = "Nombre")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Tipo/Especialidad")]
            public string? Type { get; set; }

            [Display(Name = "Edificio")]
            public string? Building { get; set; }

            [Display(Name = "Piso")]
            public string? Floor { get; set; }

            [Display(Name = "Descripción")]
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                LoadFaculties();
                return Page();
            }

            // Normalization
            var normalizedName = Input.Name.Trim().ToLower();
            var normalizedCode = Input.Code.Trim().ToUpper();

            // Validate Duplicate Code (Global check, ignoring soft-deleted)
            bool codeExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Code.ToUpper() == normalizedCode && l.Status != GeneralStatus.Eliminado);

            if (codeExists)
            {
                ModelState.AddModelError("Input.Code", $"El código institucional '{normalizedCode}' ya se encuentra registrado.");
                LoadFaculties();
                return Page();
            }

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
                Code = normalizedCode,
                Name = Input.Name.Clean(),
                Type = Input.Type?.Clean(),
                Building = Input.Building?.Clean(),
                Floor = Input.Floor?.Clean(),
                Description = Input.Description?.Clean(),
                Status = GeneralStatus.Activo,
                CreatedDate = DateTime.Now
            };

            // Set current auditor
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                laboratory.CreatedById = currentUser.Id;
            }

            _context.Laboratories.Add(laboratory);
            await _context.SaveChangesAsync();

            TempData.Success($"Laboratorio '{laboratory.Name}' registrado exitosamente en el catálogo.");
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
