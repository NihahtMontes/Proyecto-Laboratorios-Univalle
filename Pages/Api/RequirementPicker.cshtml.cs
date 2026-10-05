using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Laboratorios_Univalle.Services;

namespace Proyecto_Laboratorios_Univalle.Pages.Api
{
    /// <summary>
    /// Endpoints JSON del buscador de requerimientos (partial _RequirementPicker): búsquedas y fichas de detalle.
    /// Solo lectura; las altas se hacen en Requerimientos y en el kit de la materia.
    /// </summary>
    [Authorize]
    public class RequirementPickerModel : PageModel
    {
        private readonly RequirementPickerService _picker;

        public RequirementPickerModel(RequirementPickerService picker) => _picker = picker;

        public IActionResult OnGet() => NotFound();

        public async Task<JsonResult> OnGetArticlesAsync(int? categoryId, string? q, bool confirmedOnly = false) =>
            new(await _picker.SearchArticlesAsync(categoryId, q, confirmedOnly));

        /// <summary>Unidades físicas en la sede/facultad de la práctica <paramref name="planId"/>.</summary>
        public async Task<JsonResult> OnGetUnitsAsync(int planId, int? laboratoryId, string? q, bool operationalOnly = false, bool withSerialOnly = false)
        {
            var scope = await _picker.PracticeScopeAsync(planId);
            if (scope == null)
                return new(new RequirementPickerService.SearchPage<RequirementPickerService.EquipmentGroup>([], false, 0));
            return new(await _picker.SearchUnitsAsync(scope.Value.FacultyId, scope.Value.CampusId, laboratoryId, q, operationalOnly, withSerialOnly));
        }

        public async Task<JsonResult> OnGetEquipmentAsync(string? q, bool withUnitsOnly = false) =>
            new(await _picker.SearchEquipmentCatalogAsync(q, withUnitsOnly));

        public async Task<IActionResult> OnGetArticleDetailAsync(int id) =>
            await _picker.ArticleDetailAsync(id) is { } detail ? new JsonResult(detail) : NotFound();

        public async Task<IActionResult> OnGetUnitDetailAsync(int id) =>
            await _picker.UnitDetailAsync(id) is { } detail ? new JsonResult(detail) : NotFound();

        public async Task<IActionResult> OnGetEquipmentDetailAsync(int id) =>
            await _picker.EquipmentDetailAsync(id) is { } detail ? new JsonResult(detail) : NotFound();
    }
}
