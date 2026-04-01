using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public EditManagementInputModel Input { get; set; } = new();

        public class EditManagementInputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El año es obligatorio")]
            [Range(2000, 2100, ErrorMessage = "Año fuera de rango")]
            [Display(Name = "Año")]
            public int Year { get; set; }

            [Required(ErrorMessage = "El semestre es obligatorio")]
            [Range(1, 2, ErrorMessage = "Semestre inválido (1 o 2)")]
            [Display(Name = "Semestre")]
            public int Semester { get; set; }

            [Display(Name = "Descripción general")]
            public string? Description { get; set; }

            [Display(Name = "Fecha de Inicio")]
            [DataType(DataType.Date)]
            public DateTime? StartDate { get; set; }

            [Display(Name = "Fecha Límite Planificada")]
            [DataType(DataType.Date)]
            public DateTime? PlannedEndDate { get; set; }

            [Required]
            [Display(Name = "Estado")]
            public ManagementStatus Status { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var management = await _context.Managements.FirstOrDefaultAsync(m => m.Id == id);
            
            if (management == null) return NotFound();
            
            // Si está eliminada, no se debería editar por esta vía
            if (management.Status == ManagementStatus.Deleted) return NotFound();

            Input = new EditManagementInputModel
            {
                Id = management.Id,
                Year = management.Year,
                Semester = management.Semester,
                Description = management.Description,
                StartDate = management.StartDate,
                PlannedEndDate = management.PlannedEndDate,
                Status = management.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var management = await _context.Managements.FindAsync(Input.Id);
            if (management == null) return NotFound();

            // Bloqueo si ya estaba terminada? El usuario dijo que se puede editar el estado.
            // Pero validamos la regla de "Única Activa" si cambia a Activo
            if (Input.Status == ManagementStatus.Active && management.Status != ManagementStatus.Active)
            {
                var anyActive = await _context.Managements.AnyAsync(m => m.Status == ManagementStatus.Active && m.Id != management.Id);
                if (anyActive)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe otra gestión activa. Debe desactivarla antes de activar esta.");
                    return Page();
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);

            management.Year = Input.Year;
            management.Semester = Input.Semester;
            management.Code = $"{Input.Year}-{Input.Semester}";
            management.Description = Input.Description;
            management.StartDate = Input.StartDate;
            management.PlannedEndDate = Input.PlannedEndDate;
            management.Status = Input.Status;
            
            management.ModifiedById = currentUser?.Id;
            management.LastModifiedDate = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ManagementExists(management.Id)) return NotFound();
                else throw;
            }
            
            TempData["Success"] = "Gestión actualizada correctamente.";
            return RedirectToPage("./Index");
        }

        private bool ManagementExists(int id)
        {
            return _context.Managements.Any(e => e.Id == id);
        }
    }
}
