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

            [Required(ErrorMessage = "El código institucional es obligatorio")]
            [Display(Name = "Código")]
            [StringLength(20, ErrorMessage = "El código no puede superar los 20 caracteres")]
            public string Code { get; set; } = string.Empty;

            [Required(ErrorMessage = "El nombre descriptivo es obligatorio")]
            [Display(Name = "Nombre")]
            [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
            public string Name { get; set; } = string.Empty;

            [Display(Name = "Tipo/Especialidad")]
            public string? Type { get; set; }

            [Display(Name = "Edificio")]
            public string? Building { get; set; }

            [Display(Name = "Piso/Nivel")]
            public string? Floor { get; set; }

            [Display(Name = "Descripción")]
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
                Type = laboratory.Type,
                Building = laboratory.Building,
                Floor = laboratory.Floor,
                Description = laboratory.Description,
                Status = laboratory.Status
            };

            LoadFaculties(laboratory.FacultyId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadLaboratoryData(Input.Id);
                LoadFaculties(Input.FacultyId);
                return Page();
            }

            // Normalization
            var normalizedName = Input.Name.Trim().ToLower();
            var normalizedCode = Input.Code.Trim().ToUpper();

            // Validate Duplicate Code (excluding current record)
            bool codeExists = await _context.Laboratories
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Id != Input.Id && 
                               l.Code.ToUpper() == normalizedCode && 
                               l.Status != GeneralStatus.Eliminado);

            if (codeExists)
            {
                ModelState.AddModelError("Input.Code", $"El código institucional '{normalizedCode}' ya pertenece a otro laboratorio.");
                await LoadLaboratoryData(Input.Id);
                LoadFaculties(Input.FacultyId);
                return Page();
            }

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

            // Entity update
            laboratory.FacultyId = Input.FacultyId;
            laboratory.Code = normalizedCode;
            laboratory.Name = Input.Name.Clean();
            laboratory.Type = Input.Type?.Clean();
            laboratory.Building = Input.Building?.Clean();
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
                await _context.SaveChangesAsync();
                TempData.Success($"Datos del Laboratorio '{laboratory.Name}' actualizados correctamente.");
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
