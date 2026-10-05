using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.MeasurementUnits
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class CreateModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "El nombre de la unidad es obligatorio")]
            [Display(Name = "Nombre de la Unidad")]
            [StringLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres")]
            public string Name { get; set; } = string.Empty;

            // Code lo asigna el sistema (formato UNI-00001); no se captura en el InputModel.
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Unicidad por nombre (entre no eliminados)
            var normalizedName = Input.Name.Trim().ToLower();
            var nameExists = await _context.MeasurementUnits
                .AnyAsync(u => u.Name.Trim().ToLower() == normalizedName
                               && u.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe una unidad de medida con este nombre en el sistema.");
                return Page();
            }

            // Code lo asigna el sistema mediante CatalogCodes; no se valida unicidad aquí.

            var unit = new MeasurementUnit
            {
                Name = Input.Name.Clean(),
                // Code vacío: CatalogCodes asignará el siguiente UNI-00001 al persistir.
                Code = string.Empty,
                Status = GeneralStatus.Activo
                // CreatedDate: el modelo tiene default = DateTime.UtcNow.
            };

            _context.MeasurementUnits.Add(unit);

            try
            {
                await _context.SaveWithCodeAsync(
                    _context.MeasurementUnits.IgnoreQueryFilters().Select(x => (string?)x.Code),
                    CatalogCodes.MeasurementUnit,
                    code => unit.Code = code);
                TempData.Success($"Unidad de medida '{unit.Code}' registrada exitosamente.");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al crear la unidad de medida {UnitName}.", unit.Name);
                ModelState.AddModelError(string.Empty,
                    "No se pudo registrar la unidad porque ya existe un registro con el mismo nombre.");
                return Page();
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            return inner is Microsoft.Data.SqlClient.SqlException sqlEx
                   && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }
    }
}