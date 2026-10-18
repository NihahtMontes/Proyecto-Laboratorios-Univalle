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
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Proyecto_Laboratorios_Univalle.Models.Practice Practice { get; set; } = default!;
        public List<PracticeRequirementView> Requirements { get; set; } = new();

        [BindProperty]
        public RequirementInputModel RequirementInput { get; set; } = new();

        [BindProperty]
        public RequirementUpdateModel RequirementUpdate { get; set; } = new();

        public class PracticeRequirementView
        {
            public int Id { get; set; }
            public int ArticleId { get; set; }
            public string ArticleCode { get; set; } = string.Empty;
            public string ArticleName { get; set; } = string.Empty;
            public string UnitOfMeasure { get; set; } = string.Empty;
            public decimal? Quantity { get; set; }
            public bool IsScalable { get; set; }
            public string? ValidationStatus { get; set; }
            public string? Observations { get; set; }
        }

        public class RequirementInputModel
        {
            [Required(ErrorMessage = "Debe seleccionar un artículo")]
            [Display(Name = "Artículo")]
            public int ArticleId { get; set; }

            [Required(ErrorMessage = "La cantidad es obligatoria")]
            [Range(0.01, 999999, ErrorMessage = "La cantidad debe ser mayor a 0")]
            [Display(Name = "Cantidad")]
            public decimal Quantity { get; set; } = 1m;

            [Display(Name = "Escalable")]
            public bool IsScalable { get; set; }

            [StringLength(40)]
            [Display(Name = "Estado de validación")]
            public string? ValidationStatus { get; set; }

            [StringLength(1000)]
            [Display(Name = "Observaciones")]
            public string? Observations { get; set; }
        }

        public class RequirementUpdateModel
        {
            [Required]
            public int Id { get; set; }

            [Required(ErrorMessage = "La cantidad es obligatoria")]
            [Range(0.01, 999999, ErrorMessage = "La cantidad debe ser mayor a 0")]
            [Display(Name = "Cantidad")]
            public decimal Quantity { get; set; }

            [Display(Name = "Escalable")]
            public bool IsScalable { get; set; }

            [StringLength(1000)]
            [Display(Name = "Observaciones")]
            public string? Observations { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var practice = await LoadPracticeAsync(id.Value);
            if (practice == null) return NotFound();

            Practice = practice;
            Requirements = await LoadRequirementsAsync(practice.Id);
            await LoadArticlesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddRequirementAsync(int? id)
        {
            if (id == null) return NotFound();

            var practice = await LoadPracticeAsync(id.Value);
            if (practice == null) return NotFound();

            Practice = practice;

            // La página tiene dos [BindProperty]: este formulario solo envía RequirementInput.*; el RequirementUpdate vacío
            // (Quantity = 0) fallaba su [Range] (con la clave "Quantity", sin prefijo) y el alta de líneas de receta
            // terminaba en silencio sin guardar nada.
            KeepModelStateFor(nameof(RequirementInput));

            if (!ModelState.IsValid)
            {
                Requirements = await LoadRequirementsAsync(practice.Id);
                await LoadArticlesAsync();
                return Page();
            }

            // Validar que el artículo exista y esté activo.
            var articleActive = await _context.Articles
                .AsNoTracking()
                .AnyAsync(a => a.Id == RequirementInput.ArticleId && a.Status == GeneralStatus.Activo);

            if (!articleActive)
            {
                ModelState.AddModelError("RequirementInput.ArticleId", "El artículo seleccionado no existe o no está activo.");
                Requirements = await LoadRequirementsAsync(practice.Id);
                await LoadArticlesAsync();
                return Page();
            }

            // Validar que no exista ya una línea activa para este artículo (índice único filtrado IsRemoved = 0).
            var duplicate = await _context.PracticeRequirements
                .AsNoTracking()
                .AnyAsync(r => r.PracticeId == practice.Id
                            && r.ArticleId == RequirementInput.ArticleId
                            && !r.IsRemoved);

            if (duplicate)
            {
                ModelState.AddModelError("RequirementInput.ArticleId", "El artículo ya está en la receta de esta práctica.");
                Requirements = await LoadRequirementsAsync(practice.Id);
                await LoadArticlesAsync();
                return Page();
            }

            var requirement = new Proyecto_Laboratorios_Univalle.Models.PracticeRequirement
            {
                PracticeId = practice.Id,
                ArticleId = RequirementInput.ArticleId,
                Quantity = RequirementInput.Quantity,
                IsScalable = RequirementInput.IsScalable,
                ValidationStatus = RequirementInput.ValidationStatus?.Clean(),
                Observations = RequirementInput.Observations?.Clean(),
                IsRemoved = false
            };

            _context.PracticeRequirements.Add(requirement);

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success("Artículo agregado a la receta de la práctica.");
            }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al agregar artículo {ArticleId} a la práctica {PracticeId}", RequirementInput.ArticleId, practice.Id);
                ModelState.AddModelError("RequirementInput.ArticleId", "El artículo ya está en la receta de esta práctica.");
                Requirements = await LoadRequirementsAsync(practice.Id);
                await LoadArticlesAsync();
                return Page();
            }

            RequirementInput = new RequirementInputModel();
            Requirements = await LoadRequirementsAsync(practice.Id);
            await LoadArticlesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateRequirementAsync(int? id)
        {
            if (id == null) return NotFound();

            var practice = await LoadPracticeAsync(id.Value);
            if (practice == null) return NotFound();

            Practice = practice;

            // Este formulario solo envía RequirementUpdate.*; se ignora la validación del otro [BindProperty].
            KeepModelStateFor(nameof(RequirementUpdate));

            if (!ModelState.IsValid)
            {
                Requirements = await LoadRequirementsAsync(practice.Id);
                await LoadArticlesAsync();
                return Page();
            }

            // IDOR: cargar la línea y verificar que pertenezca a la práctica de la ruta.
            var requirement = await _context.PracticeRequirements
                .AsTracking()
                .FirstOrDefaultAsync(r => r.Id == RequirementUpdate.Id);

            if (requirement == null || requirement.PracticeId != practice.Id || requirement.IsRemoved)
            {
                TempData.Error("La línea de receta seleccionada no pertenece a esta práctica o ya fue retirada.");
                return RedirectToPage(new { id = practice.Id });
            }

            requirement.Quantity = RequirementUpdate.Quantity;
            requirement.IsScalable = RequirementUpdate.IsScalable;
            requirement.Observations = RequirementUpdate.Observations?.Clean();

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success("Línea de receta actualizada correctamente.");
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData.Error("La línea de receta fue modificada por otro usuario. Recargue la página.");
                return RedirectToPage(new { id = practice.Id });
            }

            RequirementUpdate = new RequirementUpdateModel();
            Requirements = await LoadRequirementsAsync(practice.Id);
            await LoadArticlesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveRequirementAsync(int? id, int requirementId)
        {
            if (id == null) return NotFound();

            var practice = await LoadPracticeAsync(id.Value);
            if (practice == null) return NotFound();

            // IDOR: la línea debe pertenecer a esta práctica y no estar ya retirada.
            var requirement = await _context.PracticeRequirements
                .AsTracking()
                .FirstOrDefaultAsync(r => r.Id == requirementId);

            if (requirement == null || requirement.PracticeId != practice.Id)
            {
                TempData.Error("La línea de receta seleccionada no pertenece a esta práctica.");
                return RedirectToPage(new { id = practice.Id });
            }

            if (requirement.IsRemoved)
            {
                TempData.Warning("La línea de receta ya estaba retirada.");
                return RedirectToPage(new { id = practice.Id });
            }

            // Soft-delete: preserva historial sin afectar unicidad (índice filtrado IsRemoved = 0).
            requirement.IsRemoved = true;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success("Línea de receta retirada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo retirar la línea de receta {RequirementId}", requirementId);
                TempData.Error("No se pudo retirar la línea de receta. Intente nuevamente.");
            }

            return RedirectToPage(new { id = practice.Id });
        }

        /// <summary>Conserva solo los errores del modelo indicado; el resto de [BindProperty] de la página no participa en este handler.</summary>
        private void KeepModelStateFor(string prefix)
        {
            foreach (var key in ModelState.Keys.Where(k => k != prefix && !k.StartsWith(prefix + ".", StringComparison.Ordinal)).ToList())
            {
                ModelState.Remove(key);
            }
        }

        private async Task<Proyecto_Laboratorios_Univalle.Models.Practice?> LoadPracticeAsync(int id)
        {
            return await _context.Practices
                .AsNoTracking()
                .Include(p => p.Subject)
                .ThenInclude(s => s!.Career)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status != GeneralStatus.Eliminado);
        }

        private async Task<List<PracticeRequirementView>> LoadRequirementsAsync(int practiceId)
        {
            return await _context.PracticeRequirements
                .AsNoTracking()
                .Include(r => r.Article)
                .ThenInclude(a => a!.MeasurementUnit)
                .Where(r => r.PracticeId == practiceId && !r.IsRemoved)
                .OrderBy(r => r.Article!.Code)
                .Select(r => new PracticeRequirementView
                {
                    Id = r.Id,
                    ArticleId = r.ArticleId,
                    ArticleCode = r.Article!.Code,
                    ArticleName = r.Article!.Name,
                    UnitOfMeasure = r.Article!.MeasurementUnit != null ? r.Article!.MeasurementUnit.Name : r.Article!.UnitOfMeasure,
                    Quantity = r.Quantity,
                    IsScalable = r.IsScalable,
                    ValidationStatus = r.ValidationStatus,
                    Observations = r.Observations
                })
                .ToListAsync();
        }

        private async Task LoadArticlesAsync()
        {
            // Artículos activos seleccionables para la receta. Los marcados con
            // ReviewStatus = PorConfirmar siguen siendo elegibles, pero se etiquetan
            // visualmente con el sufijo "· Por confirmar" para que el docente los
            // identifique al componer la receta.
            var articles = await _context.Articles
                .AsNoTracking()
                .Where(a => a.Status == GeneralStatus.Activo)
                .OrderBy(a => a.Code)
                .Select(a => new
                {
                    a.Id,
                    Display = a.Code + " — " + a.Name
                        + (a.ReviewStatus == Models.Enums.DataReviewStatus.PorConfirmar
                            ? " · Por confirmar"
                            : string.Empty)
                })
                .ToListAsync();

            ViewData["ArticleId"] = new SelectList(articles, "Id", "Display");
        }
    }
}