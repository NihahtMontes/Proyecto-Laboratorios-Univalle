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

        /// <summary>Código actual en BD (no bindeado, sólo lectura en la vista).</summary>
        public string CurrentCode { get; set; } = string.Empty;

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre de la práctica es obligatorio")]
            [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres")]
            [Display(Name = "Nombre de la práctica")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Debe seleccionar una materia")]
            [Display(Name = "Materia")]
            public int SubjectId { get; set; }

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

            CurrentCode = practice.Code;

            Input = new InputModel
            {
                Id = practice.Id,
                Name = practice.Name,
                SubjectId = practice.SubjectId,
                Description = practice.Description,
                CoverageStatus = practice.CoverageStatus,
                CoverageMethod = practice.CoverageMethod,
                SourceReference = practice.SourceReference,
                Observations = practice.Observations,
                Status = practice.Status
            };

            await LoadSubjectsAsync();
            await LoadCoverageStatusesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Cargar CurrentCode desde BD antes de cualquier posible retorno a la página.
            var currentForCode = await _context.Practices
                .AsNoTracking()
                .Where(p => p.Id == Input.Id)
                .Select(p => p.Code)
                .FirstOrDefaultAsync();
            CurrentCode = currentForCode ?? string.Empty;

            if (!ModelState.IsValid)
            {
                await LoadSubjectsAsync();
                await LoadCoverageStatusesAsync();
                return Page();
            }

            // Validar el valor de CoverageStatus contra el catálogo permitido.
            if (!string.IsNullOrWhiteSpace(Input.CoverageStatus)
                && Array.IndexOf(PracticeCoverageStatus.All, Input.CoverageStatus) < 0)
            {
                ModelState.AddModelError("Input.CoverageStatus", "El estado de cobertura seleccionado no es válido.");
                await LoadSubjectsAsync();
                await LoadCoverageStatusesAsync();
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
                await LoadCoverageStatusesAsync();
                return Page();
            }

            var practiceToUpdate = await _context.Practices
                .AsTracking()
                .FirstOrDefaultAsync(p => p.Id == Input.Id);

            if (practiceToUpdate == null) return NotFound();

            // No se modifica Code (lo asigna el sistema). No se modifica Number (campo histórico del Excel).
            practiceToUpdate.Name = Input.Name.Clean();
            practiceToUpdate.SubjectId = Input.SubjectId;
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
            catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
            {
                _logger.LogWarning(ex, "No se pudo actualizar la práctica {Id}", Input.Id);
                ModelState.AddModelError(string.Empty, "Ya existe un registro con los mismos datos o no se pudo guardar la práctica. Intente nuevamente.");
                await LoadSubjectsAsync();
                await LoadCoverageStatusesAsync();
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

        private async Task LoadCoverageStatusesAsync()
        {
            // Construye la lista base con los estados permitidos.
            var statuses = Proyecto_Laboratorios_Univalle.Models.PracticeCoverageStatus.All.ToList();

            // Si el valor actual cargado en Input no está en la lista, lo añadimos como opción extra
            // para que el usuario pueda conservarlo sin perderlo visualmente.
            var currentStatus = Input.CoverageStatus;
            if (!string.IsNullOrWhiteSpace(currentStatus) && !statuses.Contains(currentStatus))
            {
                statuses.Add(currentStatus);
            }

            ViewData["CoverageStatuses"] = new SelectList(statuses);
            await Task.CompletedTask;
        }
    }
}