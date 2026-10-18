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
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public LabInputModel Input { get; set; } = new();

        public Laboratory? ExistingLaboratory { get; set; }

        public class LabInputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "La facultad responsable es obligatoria")]
            [Display(Name = "Facultad")]
            public int FacultyId { get; set; }

            /// <summary>Código asignado por el sistema. Solo se conserva para mostrarlo en la UI.</summary>
            [Display(Name = "Código")]
            public string Code { get; set; } = string.Empty;

            [Required(ErrorMessage = "El nombre descriptivo es obligatorio")]
            [Display(Name = "Nombre")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Piso/Nivel")]
            [StringLength(50, ErrorMessage = "El piso o nivel no puede superar los 50 caracteres")]
            public string? Floor { get; set; }

            [Display(Name = "Descripción")]
            [StringLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres")]
            public string? Description { get; set; }

            [Required]
            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var laboratory = await _context.Laboratories
                .Include(l => l.CreatedBy)
                .Include(l => l.ModifiedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (laboratory == null) return NotFound();

            ExistingLaboratory = laboratory;

            // Map Entity to DTO
            Input = new LabInputModel
            {
                Id = laboratory.Id,
                FacultyId = laboratory.FacultyId,
                Code = laboratory.Code,
                Name = laboratory.Name,
                Floor = laboratory.Floor,
                Description = laboratory.Description,
                Status = laboratory.Status
            };

            LoadFaculties(laboratory.FacultyId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // El estado Eliminado solo se asigna con la baja lógica (antes un POST manual lo aceptaba).
            if (Input.Status == GeneralStatus.Eliminado)
            {
                ModelState.AddModelError("Input.Status", "El estado Eliminado solo se gestiona desde la opción Dar de Baja.");
            }

            if (!ModelState.IsValid)
            {
                await LoadLaboratoryData(Input.Id);
                // El código no se envía desde el formulario: repoblar desde BD para mantenerlo visible.
                if (ExistingLaboratory != null) Input.Code = ExistingLaboratory.Code;
                LoadFaculties(Input.FacultyId);
                return Page();
            }

            // Normalization
            var normalizedName = Input.Name.Clean().ToLower();

            // Validate Duplicate Name within same Faculty (excluding current record)
            bool nameExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Id != Input.Id &&
                               l.FacultyId == Input.FacultyId &&
                               l.Name.ToLower() == normalizedName &&
                               l.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe otro ambiente con este nombre en la facultad seleccionada.");
                await LoadLaboratoryData(Input.Id);
                LoadFaculties(Input.FacultyId);
                return Page();
            }

            var laboratory = await _context.Laboratories.FindAsync(Input.Id);
            if (laboratory == null) return NotFound();

            // La facultad debe existir y estar activa (o ser la que ya tenía el laboratorio).
            if (!await _context.Faculties.AnyAsync(f => f.Id == Input.FacultyId && (f.Status == GeneralStatus.Activo || f.Id == laboratory.FacultyId)))
            {
                ModelState.AddModelError("Input.FacultyId", "La facultad seleccionada no está disponible.");
                await LoadLaboratoryData(Input.Id);
                if (ExistingLaboratory != null) Input.Code = ExistingLaboratory.Code;
                LoadFaculties(laboratory.FacultyId);
                return Page();
            }

            // Entity update
            laboratory.FacultyId = Input.FacultyId;
            // Code no se modifica: lo asigna el sistema.
            laboratory.Name = Input.Name.Clean();
            laboratory.Floor = Input.Floor?.Clean();
            laboratory.Description = Input.Description?.Clean();
            laboratory.Status = Input.Status;
            laboratory.LastModifiedDate = DateTime.UtcNow;

            // Set current auditor
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                laboratory.ModifiedById = currentUser.Id;
            }

            try
            {
                // NoTracking global: FindAsync devuelve la entidad sin seguimiento; sin Attach el guardado no persistía.
                _context.Attach(laboratory).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                TempData.Success($"Datos del Laboratorio '{laboratory.Code}' ({laboratory.Name}) actualizados correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LaboratoryExists(laboratory.Id)) return NotFound();
                else throw;
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadLaboratoryData(int id)
        {
            ExistingLaboratory = await _context.Laboratories
                .Include(l => l.CreatedBy)
                .Include(l => l.ModifiedBy)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        private void LoadFaculties(int? selectedId = null)
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties
                .Where(f => f.Status == GeneralStatus.Activo || f.Id == selectedId)
                .OrderBy(f => f.Name), "Id", "Name", selectedId);
        }

        private bool LaboratoryExists(int id)
        {
            return _context.Laboratories.Any(e => e.Id == id);
        }
    }
}
