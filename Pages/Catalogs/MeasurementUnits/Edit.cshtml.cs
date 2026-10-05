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
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context,
            ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        /// <summary>Snapshot de solo lectura para la barra lateral (auditoría visual).</summary>
        public MeasurementUnit? UnitSnapshot { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "El nombre de la unidad es obligatorio")]
            [Display(Name = "Nombre de la Unidad")]
            [StringLength(60, ErrorMessage = "El nombre no puede superar los 60 caracteres")]
            public string Name { get; set; } = string.Empty;

            // Code no se captura: lo asigna el sistema (UNI-00001) y se muestra desde UnitSnapshot.

            /// <summary>Estado administrativo. Sólo se permite Activo/Inactivo (Eliminado se rechaza en servidor).</summary>
            [Required(ErrorMessage = "El estado es obligatorio")]
            [Display(Name = "Estado")]
            public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var unit = await _context.MeasurementUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null) return NotFound();

            UnitSnapshot = unit;
            Input = new InputModel
            {
                Id = unit.Id,
                Name = unit.Name,
                Status = unit.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await ReloadSnapshot(Input.Id);
                return Page();
            }

            // Rechazar Eliminado en servidor (sólo Activo/Inactivo permitidos).
            if (Input.Status == GeneralStatus.Eliminado)
            {
                ModelState.AddModelError("Input.Status",
                    "El estado Eliminado sólo se gestiona desde la opción Dar de Baja.");
                await ReloadSnapshot(Input.Id);
                return Page();
            }

            if (Input.Status != GeneralStatus.Activo && Input.Status != GeneralStatus.Inactivo)
            {
                ModelState.AddModelError("Input.Status", "Seleccione un estado válido (Activo o Inactivo).");
                await ReloadSnapshot(Input.Id);
                return Page();
            }

            var unitToUpdate = await _context.MeasurementUnits
                .AsTracking()
                .FirstOrDefaultAsync(u => u.Id == Input.Id);

            if (unitToUpdate == null) return NotFound();

            var normalizedName = Input.Name.Trim().ToLower();

            // Unicidad por nombre (entre no eliminados)
            var nameExists = await _context.MeasurementUnits
                .AnyAsync(u => u.Id != Input.Id
                               && u.Name.Trim().ToLower() == normalizedName
                               && u.Status != GeneralStatus.Eliminado);

            if (nameExists)
            {
                ModelState.AddModelError("Input.Name", "Ya existe otra unidad de medida con este nombre en el sistema.");
                await ReloadSnapshot(Input.Id);
                return Page();
            }

            // Code no se edita: lo asigna el sistema; no se valida unicidad aquí.

            // Copiar SOLO los campos permitidos (evita overposting).
            unitToUpdate.Name = Input.Name.Clean();
            // Code no se reasigna: lo conserva el sistema desde la creación.
            unitToUpdate.Status = Input.Status;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success($"Unidad de medida '{unitToUpdate.Name}' actualizada correctamente.");
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Conflicto de unicidad al actualizar la unidad de medida {UnitId}.", Input.Id);
                ModelState.AddModelError(string.Empty,
                    "No se pudo guardar la unidad porque ya existe un registro con el mismo nombre.");
                await ReloadSnapshot(Input.Id);
                return Page();
            }
        }

        private async Task ReloadSnapshot(int id)
        {
            UnitSnapshot = await _context.MeasurementUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            return inner is Microsoft.Data.SqlClient.SqlException sqlEx
                   && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
        }
    }
}