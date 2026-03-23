using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public ManagementInputModel Input { get; set; } = new();

        public class ManagementInputModel
        {
            [Required(ErrorMessage = "El código de gestión es obligatorio")]
            [Display(Name = "Código (Ej: L-48 2024)")]
            public string Code { get; set; } = string.Empty;

            [Required(ErrorMessage = "El nombre es obligatorio")]
            [Display(Name = "Nombre de la Gestión")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Descripción general")]
            public string? Description { get; set; }

            [Display(Name = "Fecha de Inicio")]
            [DataType(DataType.Date)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "Fecha Límite Planificada")]
            [DataType(DataType.Date)]
            public DateTime? PlannedEndDate { get; set; }

            [Required(ErrorMessage = "Debe asignar un Responsable")]
            [Display(Name = "Responsable de la Gestión L-48")]
            public string Responsible { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Verify if there's already an active Management
            var anyActive = _context.Managements.Any(m => m.Status == Models.Enums.ManagementStatus.Active);
            if (anyActive)
            {
                // We'll allow multiple active managements (maybe different scopes), 
                // but we might want to warn or just let it pass. For now, let it pass.
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Hay errores en el formulario, revise los datos ingreados.";
                // Fast-Fail logic: Redirigir a Modulo 7
                return RedirectToPage("/Requests/Index");
            }

            var management = new Management
            {
                Code = Input.Code,
                Name = Input.Name,
                Description = Input.Description,
                StartDate = Input.StartDate,
                PlannedEndDate = Input.PlannedEndDate,
                Responsible = Input.Responsible,
                Status = Models.Enums.ManagementStatus.Active,
                CreatedDate = DateTime.UtcNow,
                // Audit logic usually done in OnModelCreating/Overrides, but left mapped here
            };

            _context.Managements.Add(management);
            await _context.SaveChangesAsync();

            TempData["Success"] = "La Gestión L-48 ha sido creada correctamente. Ahora puede iniciar su tablero de planes.";
            return RedirectToPage("./Index");
        }
    }
}
