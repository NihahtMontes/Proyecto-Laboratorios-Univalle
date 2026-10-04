using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Practices
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El código de la práctica es obligatorio")]
            [StringLength(50, ErrorMessage = "El código no puede superar los 50 caracteres")]
            [Display(Name = "Código")]
            public string Code { get; set; } = string.Empty;

            [Required(ErrorMessage = "Debe seleccionar una materia")]
            [Display(Name = "Materia")]
            public int SubjectId { get; set; }

            [StringLength(20, ErrorMessage = "El número no puede superar los 20 caracteres")]
            [Display(Name = "N.º de práctica")]
            public string? Number { get; set; }

            [StringLength(2000)]
            [Display(Name = "Descripción")]
            public string? Description { get; set; }

            [StringLength(40)]
            [Display(Name = "Estado de cobertura")]
            public string? CoverageStatus { get; set; }

            [StringLength(60)]
            [Display(Name = "Método de cobertura")]
            public string? CoverageMethod { get; set; }

            [StringLength(500)]
            [Display(Name = "Fuente de referencia")]
            public string? SourceReference { get; set; }

            [StringLength(2000)]
            [Display(Name = "Observaciones")]
            public string? Observations { get; set; }

            [Required(ErrorMessage = "Debe seleccionar un estado")]
            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var practice = await _context.Practices
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (practice == null) return NotFound();

            Input = new InputModel
            {
                Id = practice.Id,
                Code = practice.Code,
                SubjectId = practice.SubjectId,
                Number = practice.Number,
                Description = practice.Description,
                CoverageStatus = practice.CoverageStatus,
                CoverageMethod = practice.CoverageMethod,
                SourceReference = practice.SourceReference,
                Observations = practice.Observations,
                Status = practice.Status
            };

            await LoadSubjectsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSubjectsAsync();
                return Page();
            }

            var normalizedCode = Input.Code.Trim().ToLower();

            // Unicidad contra otros registros (no contra sí mismo). Índice único global en Code.
            var duplicate = await _context.Practices
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Id != Input.Id && p.Code.Trim().ToLower() == normalizedCode);

            if (duplicate)
            {
                ModelState.AddModelError("Input.Code", "Ya existe otra práctica con este código en el sistema (incluyendo registros eliminados).");
                await LoadSubjectsAsync();
                return Page();
            }

            // Validar que la materia esté activa.
            var subjectActive = await _context.Subjects
                .AsNoTracking()
                .AnyAsync(s => s.Id == Input.SubjectId && s.Status == GeneralStatus.Activo);

            if (!subjectActive)
            {
                ModelState.AddModelError("Input.SubjectId", "La materia seleccionada no existe o no está activa.");
                await LoadSubjectsAsync();
                return Page();
            }

            var practiceToUpdate = await _context.Practices
                .AsTracking()
                .FirstOrDefaultAsync(p => p.Id == Input.Id);

            if (practiceToUpdate == null) return NotFound();

            practiceToUpdate.Code = Input.Code.Clean().ToUpperInvariant();
            practiceToUpdate.SubjectId = Input.SubjectId;
            practiceToUpdate.Number = Input.Number?.Clean().ToUpperInvariant();
            practiceToUpdate.Description = Input.Description?.Clean();
            practiceToUpdate.CoverageStatus = Input.CoverageStatus?.Clean();
            practiceToUpdate.CoverageMethod = Input.CoverageMethod?.Clean();
            practiceToUpdate.SourceReference = Input.SourceReference?.Clean();
            practiceToUpdate.Observations = Input.Observations?.Clean();
            practiceToUpdate.Status = Input.Status;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                _logger.LogWarning(ex, "Concurrencia al actualizar práctica {Id}", Input.Id);
                ModelState.AddModelError("Input.Code", "Ya existe otra práctica con este código en el sistema.");
                await LoadSubjectsAsync();
                return Page();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PracticeExists(Input.Id)) return NotFound();
                throw;
            }

            TempData.Success($"Datos de la Práctica '{practiceToUpdate.Code}' actualizados correctamente.");
            return RedirectToPage("./Index");
        }

        private bool PracticeExists(int id)
        {
            return _context.Practices.IgnoreQueryFilters().Any(e => e.Id == id);
        }

        private async Task LoadSubjectsAsync()
        {
            var subjects = await _context.Subjects
                .AsNoTracking()
                .Include(s => s.Career)
                .Where(s => s.Status == GeneralStatus.Activo)
                .OrderBy(s => s.Career!.Name)
                .ThenBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    Display = (s.Career != null ? s.Career.Name + " · " : "") + s.Name
                })
                .ToListAsync();

            ViewData["SubjectId"] = new SelectList(subjects, "Id", "Display", Input.SubjectId);
        }
    }
}