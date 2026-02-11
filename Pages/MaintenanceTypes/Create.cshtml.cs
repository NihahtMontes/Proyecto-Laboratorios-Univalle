using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.MaintenanceTypes
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
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
            [Display(Name = "Nombre")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Descripción")]
            [StringLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres")]
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var cleanName = Input.Name.Clean();

            // Duplicate validation
            if (_context.MaintenanceTypes.Any(mt => mt.Name.ToLower() == cleanName.ToLower()))
            {
                ModelState.AddModelError("Input.Name", "Ya existe una categoría técnica con este nombre.");
                return Page();
            }

            var maintenanceType = new MaintenanceType
            {
                Name = cleanName,
                Description = Input.Description?.Clean(),
                CreatedDate = DateTime.Now
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                maintenanceType.CreatedById = currentUser.Id;
            }

            _context.MaintenanceTypes.Add(maintenanceType);
            await _context.SaveChangesAsync();

            TempData.Success($"Categoría técnica '{maintenanceType.Name}' creada exitosamente.");
            return RedirectToPage("./Index");
        }
    }
}
