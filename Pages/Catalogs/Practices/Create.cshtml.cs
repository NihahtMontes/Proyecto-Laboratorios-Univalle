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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
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
        }

        public async Task<IActionResult> OnGetAsync()
        {
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

            // Validación de unicidad (índice único global en Code).
            var duplicate = await _context.Practices
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Code.Trim().ToLower() == normalizedCode);

            if (duplicate)
            {
                ModelState.AddModelError("Input.Code", "Ya existe una práctica con este código en el sistema (incluyendo registros eliminados).");
                await LoadSubjectsAsync();
                return Page();
            }

            // Validar que la materia exista y esté activa.
            var subjectActive = await _context.Subjects
                .AsNoTracking()
                .AnyAsync(s => s.Id == Input.SubjectId && s.Status == GeneralStatus.Activo);

            if (!subjectActive)
            {
                ModelState.AddModelError("Input.SubjectId", "La materia seleccionada no existe o no está activa.");
                await LoadSubjectsAsync();
                return Page();
            }

            var practice = new Proyecto_Laboratorios_Univalle.Models.Practice
            {
                Code = Input.Code.Clean().ToUpperInvariant(),
                SubjectId = Input.SubjectId,
                Number = Input.Number?.Clean().ToUpperInvariant(),
                Description = Input.Description?.Clean(),
                CoverageStatus = Input.CoverageStatus?.Clean(),
                CoverageMethod = Input.CoverageMethod?.Clean(),
                SourceReference = Input.SourceReference?.Clean(),
                Observations = Input.Observations?.Clean(),
                Status = GeneralStatus.Activo
            };

            _context.Practices.Add(practice);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                _logger.LogWarning(ex, "Concurrencia al registrar práctica {Code}", practice.Code);
                ModelState.AddModelError("Input.Code", "Ya existe una práctica con este código en el sistema.");
                await LoadSubjectsAsync();
                return Page();
            }

            TempData.Success($"Práctica '{practice.Code}' registrada exitosamente.");
            return RedirectToPage("./Index");
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

            ViewData["SubjectId"] = new SelectList(subjects, "Id", "Display");
        }
    }
}