using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.EquipmentTypes
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
            [Display(Name = "Nombre de la Categoría")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Descripción")]
            [StringLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres")]
            public string? Description { get; set; }

            [Display(Name = "¿Requiere Calibración?")]
            public bool RequiresCalibration { get; set; }

            [Display(Name = "Frecuencia de Mantenimiento (Meses)")]
            [Range(0, 60, ErrorMessage = "La frecuencia debe estar entre 0 y 60 meses")]
            public int? MaintenanceFrequencyMonths { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Normalization
            var normalizedName = Input.Name.Trim().ToLower();

            // Duplicate Check
            var exists = await _context.EquipmentTypes
                .AnyAsync(et => et.Name.Trim().ToLower() == normalizedName);

            if (exists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe una categoría con este nombre.");
                return Page();
            }

            var equipmentType = new EquipmentType
            {
                Name = Input.Name.Clean(),
                Description = Input.Description?.Clean(),
                RequiresCalibration = Input.RequiresCalibration,
                MaintenanceFrequencyMonths = Input.MaintenanceFrequencyMonths,
                CreatedDate = DateTime.UtcNow
            };

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                equipmentType.CreatedById = currentUser.Id;
            }

            _context.EquipmentTypes.Add(equipmentType);
            await _context.SaveChangesAsync();

            TempData.Success($"Categoría '{equipmentType.Name}' creada exitosamente.");
            return RedirectToPage("./Index");
        }
    }
}
