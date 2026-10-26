using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Pages.Catalogs.Stock
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class EditModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public EditModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        /// <summary>
        /// Pantalla de origen (p. ej. los requerimientos de una planificación): Guardar y Cancelar vuelven ahí.
        /// Solo se aceptan rutas locales.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        /// <summary>ReturnUrl validado (local) o null si no se indicó o no es seguro.</summary>
        public string? SafeReturnUrl => !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

        /// <summary>Vuelve a la pantalla de origen o, sin origen, al listado de existencias con sus filtros.</summary>
        private IActionResult BackToOrigin(int? laboratoryFilter, string? searchTerm) =>
            SafeReturnUrl is string url
                ? LocalRedirect(url)
                : RedirectToPage("./Index", new { laboratoryFilter, searchTerm });

        public Laboratory? Laboratory { get; set; }
        public Article? Article { get; set; }

        /// <summary>Indica si la fila de stock ya existía (edición real) o si se va a crear.</summary>
        public bool RowExists { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            public int LaboratoryId { get; set; }

            public int ArticleId { get; set; }

            // decimal? + Required: un campo vacío o ausente ya no se guarda en silencio como 0.
            // Range con decimal real: antes los límites enteros dejaban pasar -0.01 (error 500 por el CHECK de la BD) y 999999.01.
            [Required(ErrorMessage = "La cantidad disponible es obligatoria.")]
            [Range(typeof(decimal), "0", "999999", ErrorMessage = "La cantidad debe estar entre 0 y 999999.")]
            [Display(Name = "Cantidad disponible")]
            public decimal? AvailableQuantity { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Fecha de actualización")]
            public DateTime? UpdatedOn { get; set; }

            /// <summary>Token de concurrencia (RowVersion en base64).</summary>
            public string? RowVersionBase64 { get; set; }

            // Estado de los filtros para regresar al Index.
            public int? LaboratoryFilter { get; set; }
            public string? SearchTerm { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? laboratoryId, int? articleId, int? laboratoryFilter, string? searchTerm)
        {
            if (laboratoryId == null || articleId == null)
            {
                return NotFound();
            }

            var lab = await _context.Laboratories
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == laboratoryId.Value && l.Status == GeneralStatus.Activo);

            if (lab == null)
            {
                TempData.Error("El laboratorio seleccionado no existe o no está activo.");
                return BackToOrigin(laboratoryFilter, searchTerm);
            }

            var article = await _context.Articles
                .AsNoTracking()
                .Include(a => a.MeasurementUnit)
                .FirstOrDefaultAsync(a => a.Id == articleId.Value && a.Status == GeneralStatus.Activo);

            if (article == null)
            {
                TempData.Error("El artículo seleccionado no existe o no está activo.");
                return BackToOrigin(laboratoryFilter, searchTerm);
            }

            Laboratory = lab;
            Article = article;

            var stockRow = await _context.ArticleStocks
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.LaboratoryId == laboratoryId.Value && s.ArticleId == articleId.Value);

            if (stockRow != null)
            {
                RowExists = true;
                Input = new InputModel
                {
                    Id = stockRow.Id,
                    LaboratoryId = stockRow.LaboratoryId,
                    ArticleId = stockRow.ArticleId,
                    AvailableQuantity = stockRow.AvailableQuantity,
                    UpdatedOn = stockRow.UpdatedOn,
                    RowVersionBase64 = Convert.ToBase64String(stockRow.RowVersion ?? Array.Empty<byte>()),
                    LaboratoryFilter = laboratoryFilter,
                    SearchTerm = searchTerm
                };
            }
            else
            {
                RowExists = false;
                Input = new InputModel
                {
                    Id = 0,
                    LaboratoryId = laboratoryId.Value,
                    ArticleId = articleId.Value,
                    AvailableQuantity = 0,
                    UpdatedOn = DateTime.UtcNow.Date,
                    LaboratoryFilter = laboratoryFilter,
                    SearchTerm = searchTerm
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validar FK: laboratorio y artículo deben existir y estar activos.
            var lab = await _context.Laboratories
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == Input.LaboratoryId && l.Status == GeneralStatus.Activo);

            if (lab == null)
            {
                ModelState.AddModelError(string.Empty, "El laboratorio seleccionado no existe o no está activo.");
                await ReloadDisplayAsync(Input.LaboratoryId, Input.ArticleId);
                return Page();
            }

            var article = await _context.Articles
                .AsNoTracking()
                .Include(a => a.MeasurementUnit)
                .FirstOrDefaultAsync(a => a.Id == Input.ArticleId && a.Status == GeneralStatus.Activo);

            if (article == null)
            {
                ModelState.AddModelError(string.Empty, "El artículo seleccionado no existe o no está activo.");
                await ReloadDisplayAsync(Input.LaboratoryId, Input.ArticleId);
                return Page();
            }

            Laboratory = lab;
            Article = article;

            // La BD guarda 2 decimales: se rechaza el exceso en lugar de redondearlo en silencio.
            if (Input.AvailableQuantity.HasValue && decimal.Round(Input.AvailableQuantity.Value, 2) != Input.AvailableQuantity.Value)
            {
                ModelState.AddModelError("Input.AvailableQuantity", "La cantidad admite hasta 2 decimales.");
            }

            if (!ModelState.IsValid)
            {
                RowExists = Input.Id > 0;
                return Page();
            }

            ArticleStock? stockToUpdate;
            bool creatingNew = false;

            if (Input.Id > 0)
            {
                stockToUpdate = await _context.ArticleStocks
                    .AsTracking()
                    .FirstOrDefaultAsync(s => s.Id == Input.Id);

                if (stockToUpdate == null)
                {
                    TempData.Error("La existencia que intenta modificar ya no existe.");
                    return BackToOrigin(Input.LaboratoryFilter, Input.SearchTerm);
                }

                RowExists = true;

                // La fila indicada debe ser la del par laboratorio-artículo del formulario (antes se modificaba la fila de otro par).
                if (stockToUpdate.LaboratoryId != Input.LaboratoryId || stockToUpdate.ArticleId != Input.ArticleId)
                {
                    TempData.Error("La existencia indicada no corresponde al laboratorio y artículo seleccionados.");
                    return BackToOrigin(Input.LaboratoryFilter, Input.SearchTerm);
                }

                // Concurrencia optimista: la RowVersion capturada en GET es obligatoria y debe ser válida
                // (antes un token vacío o malformado se ignoraba y la edición sobrescribía sin control).
                byte[]? originalRowVersion = null;
                if (!string.IsNullOrEmpty(Input.RowVersionBase64))
                {
                    try
                    {
                        originalRowVersion = Convert.FromBase64String(Input.RowVersionBase64);
                    }
                    catch (FormatException)
                    {
                        originalRowVersion = null;
                    }
                }

                if (originalRowVersion == null || originalRowVersion.Length == 0)
                {
                    TempData.Warning("No se pudo verificar la versión de esta existencia. Se recargaron los valores actuales; revise y vuelva a guardar.");
                    return RedirectToPage(new { laboratoryId = Input.LaboratoryId, articleId = Input.ArticleId, laboratoryFilter = Input.LaboratoryFilter, searchTerm = Input.SearchTerm, returnUrl = SafeReturnUrl });
                }

                _context.Entry(stockToUpdate).Property(s => s.RowVersion).OriginalValue = originalRowVersion;
            }
            else
            {
                // Verificación previa: si alguien creó la fila mientras tanto, evitar duplicado.
                var existingRow = await _context.ArticleStocks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.LaboratoryId == Input.LaboratoryId && s.ArticleId == Input.ArticleId);

                if (existingRow != null)
                {
                    ModelState.AddModelError(string.Empty, "Ya existe una existencia para este artículo en este laboratorio. Recargue la página para editarla.");
                    RowExists = true;
                    return Page();
                }

                stockToUpdate = new ArticleStock
                {
                    LaboratoryId = Input.LaboratoryId,
                    ArticleId = Input.ArticleId,
                    AvailableQuantity = Input.AvailableQuantity!.Value,
                    UpdatedOn = Input.UpdatedOn?.Date
                };
                _context.ArticleStocks.Add(stockToUpdate);
                creatingNew = true;
                RowExists = false;
            }

            stockToUpdate.AvailableQuantity = Input.AvailableQuantity!.Value;
            stockToUpdate.UpdatedOn = Input.UpdatedOn?.Date;

            try
            {
                await _context.SaveChangesAsync();
                TempData.Success(creatingNew
                    ? $"Existencia registrada para «{article.Name}» en «{LaboratoryDisplayHelper.Format(lab)}»."
                    : $"Existencia actualizada para «{article.Name}» en «{LaboratoryDisplayHelper.Format(lab)}».");

                return BackToOrigin(Input.LaboratoryFilter, Input.SearchTerm);
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData.Warning("Otra persona modificó esta existencia; se recargaron los valores actuales.");
                return RedirectToPage("./Edit", new
                {
                    laboratoryId = Input.LaboratoryId,
                    articleId = Input.ArticleId,
                    laboratoryFilter = Input.LaboratoryFilter,
                    searchTerm = Input.SearchTerm,
                    returnUrl = SafeReturnUrl
                });
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                ModelState.AddModelError(string.Empty, "Ya existe una existencia registrada para este artículo en este laboratorio.");
                RowExists = true;
                return Page();
            }
        }

        private async Task ReloadDisplayAsync(int laboratoryId, int articleId)
        {
            Laboratory = await _context.Laboratories.AsNoTracking().FirstOrDefaultAsync(l => l.Id == laboratoryId);
            Article = await _context.Articles.AsNoTracking().Include(a => a.MeasurementUnit).FirstOrDefaultAsync(a => a.Id == articleId);
            RowExists = Input.Id > 0;
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            var inner = ex.InnerException;
            while (inner != null)
            {
                if (inner is Microsoft.Data.SqlClient.SqlException sqlEx
                    && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    return true;
                }
                inner = inner.InnerException;
            }
            return false;
        }
    }
}