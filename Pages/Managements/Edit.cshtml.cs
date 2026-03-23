using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Managements
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
        public EditManagementInputModel Input { get; set; } = new();

        public class EditManagementInputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El código es obligatorio")]
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

            [Required(ErrorMessage = "Responsable de la gestión es requerido")]
            [Display(Name = "Responsable (Líder / Creador)")]
            public string Responsible { get; set; } = string.Empty;
            
            public Models.Enums.ManagementStatus Status { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Managements == null)
            {
                return NotFound();
            }

            var management = await _context.Managements.FirstOrDefaultAsync(m => m.Id == id);
            
            if (management == null)
            {
                return NotFound();
            }
            
            // Check if Immutable
            if (management.Status == Models.Enums.ManagementStatus.Closed)
            {
                TempData["Error"] = "Esta gestión está CERRADA y no puede modificarse.";
                return RedirectToPage("./Index");
            }

            Input = new EditManagementInputModel
            {
                Id = management.Id,
                Code = management.Code,
                Name = management.Name,
                Description = management.Description,
                StartDate = management.StartDate,
                PlannedEndDate = management.PlannedEndDate,
                Responsible = management.Responsible,
                Status = management.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var management = await _context.Managements.FindAsync(Input.Id);
            if (management == null)
            {
                return NotFound();
            }

            if (management.Status == Models.Enums.ManagementStatus.Closed)
            {
                TempData["Error"] = "Error de Seguridad: Intento de modificación sobre Gestión CERRADA.";
                return RedirectToPage("./Index");
            }

            management.Code = Input.Code;
            management.Name = Input.Name;
            management.Description = Input.Description;
            management.StartDate = Input.StartDate;
            management.PlannedEndDate = Input.PlannedEndDate;
            management.Responsible = Input.Responsible;
            // Status remains active or isn't changed here explicitly (use the grid lock for closing)

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ManagementExists(management.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            
            TempData["Success"] = "La información de la Gestión ha sido actualizada con éxito.";
            return RedirectToPage("./Index");
        }

        private bool ManagementExists(int id)
        {
            return (_context.Managements?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
