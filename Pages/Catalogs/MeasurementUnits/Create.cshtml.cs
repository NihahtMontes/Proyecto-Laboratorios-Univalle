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

            [Display(Name = "Código")]
            [StringLength(50, ErrorMessage = "El código no puede superar los 50 caracteres")]
            public string? Code { get; set; }
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

            // Unicidad por código
            if (!string.IsNullOrWhiteSpace(Input.Code))
            {
                var normalizedCode = Input.Code.Trim().ToLower();
                var codeExists = await _context.MeasurementUnits
                    .AnyAsync(u => u.Code != null && u.Code.Trim().ToLower() == normalizedCode);

                if (codeExists)
                {
                    ModelState.AddModelError("Input.Code", "Ya existe una unidad de medida con este código en el sistema.");
                    return Page();
                }
            }

            var unit = new MeasurementUnit
            {
                Name = Input.Name.Clean(),
                Code = Input.Code?.ToUpper().Trim() ?? string.Empty,
                Status = GeneralStatus.Activo
                // CreatedDate: el modelo tiene default = DateTime.UtcNow.
            };

            _context.MeasurementUnits.Add(unit);

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Unidad de medida '{unit.Name}' registrada exitosamente.");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al crear la unidad de medida {UnitName}.", unit.Name);
                ModelState.AddModelError(string.Empty,
                    "No se pudo registrar la unidad porque ya existe un registro con el mismo código o nombre.");
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