using Microsoft.AspNetCore.Authorization;
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

        public CreateModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public string Name { get; set; }
            public string? Description { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Input.Name = Input.Name.Clean();
            Input.Description = Input.Description.Clean();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validar duplicados básicos
            if (_context.MaintenanceTypes.Any(mt => mt.Name.ToLower() == Input.Name.ToLower()))
            {
                ModelState.AddModelError("Input.Name", "Ya existe un tipo de mantenimiento con este nombre.");
                return Page();
            }

            var maintenanceType = new MaintenanceType
            {
                Name = Input.Name,
                Description = Input.Description,
                CreatedDate = DateTime.Now
            };

            _context.MaintenanceTypes.Add(maintenanceType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Tipo de mantenimiento '{maintenanceType.Name}' creado correctamente.";
            return RedirectToPage("/Maintenances/Index", null, "tipos"); // Regresa a la pestaña de tipos
        }
    }
}
