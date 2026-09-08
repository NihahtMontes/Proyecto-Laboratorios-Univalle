using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Helpers;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Pages.Equipment
{
    [Authorize(Roles = AuthorizationHelper.AdminRoles)]
    public class IndexModel : PageModel
    {
        private readonly Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext _context;

        public IndexModel(Proyecto_Laboratorios_Univalle.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<Proyecto_Laboratorios_Univalle.Models.Equipment> Equipment { get; set; } = new PaginatedList<Proyecto_Laboratorios_Univalle.Models.Equipment>(new List<Proyecto_Laboratorios_Univalle.Models.Equipment>(), 0, 1, 20);

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLaboratoryId { get; set; }

        public SelectList LaboratoriesList { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public EquipmentCategory? SelectedCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public EquipmentTypeClassification? SelectedEquipmentTypeClassification { get; set; }

        [BindProperty(SupportsGet = true)]
        public UtensilType? SelectedUtensilType { get; set; }

        [BindProperty(SupportsGet = true)]
        public EquipmentClassificationReviewStatus? SelectedReviewStatus { get; set; }

        public List<SelectListItem> EquipmentTypeClassificationOptions { get; set; } = new();

        public List<SelectListItem> UtensilTypeOptions { get; set; } = new();

        public async Task OnGetAsync(int? pageIndex)
        {
            var validEquipmentClassifications = EquipmentClassificationRules.EquipmentSubclassifications.ToArray();
            var validUtensilTypes = EquipmentClassificationRules.UtensilSubclassifications.ToArray();

            var equipmentQuery = _context.Equipments
                .Include(e => e.City)
                .Include(e => e.Country)
                .Include(e => e.Units!)
                    .ThenInclude(u => u.Laboratory!)
                        .ThenInclude(l => l.Faculty)
                .AsQueryable();

            // A) CARGAR LA LISTA DE LABORATORIOS PARA EL DESPLEGABLE
            var labs = await _context.Laboratories
                .Where(l => l.Status == GeneralStatus.Activo)
                .OrderBy(l => l.Code)
                .ThenBy(l => l.Name)
                .ToListAsync();
            LaboratoriesList = new SelectList(LaboratoryDisplayHelper.ToSelectItems(labs), "Id", "DisplayName");

            await LoadSubclassificationOptionsAsync();

            NormalizeHierarchicalFilters();

            // Lógica de Búsqueda 
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                equipmentQuery = equipmentQuery.Where(e =>
                    e.Name.ToLower().Contains(term) ||
                    (e.Brand != null && e.Brand.ToLower().Contains(term)) ||
                    (e.Model != null && e.Model.ToLower().Contains(term)) ||
                    e.Units!.Any(u =>
                        u.CurrentStatus != EquipmentStatus.Deleted &&
                        (u.InventoryNumber.ToLower().Contains(term) ||
                         (u.SerialNumber != null && u.SerialNumber.ToLower().Contains(term)) ||
                         (u.Career != null && u.Career.Name.ToLower().Contains(term)))));
            }

            // Filtro jerárquico: tipo de recurso y subclasificación aplicable.
            if (SelectedCategory == EquipmentCategory.Equipment)
            {
                equipmentQuery = equipmentQuery.Where(e =>
                    e.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                    e.Category == EquipmentCategory.Equipment &&
                    e.TypeClassification.HasValue &&
                    validEquipmentClassifications.Contains(e.TypeClassification.Value));

                if (SelectedEquipmentTypeClassification.HasValue)
                {
                    equipmentQuery = equipmentQuery.Where(e =>
                        e.TypeClassification == SelectedEquipmentTypeClassification.Value);
                }
            }
            else if (SelectedCategory == EquipmentCategory.Utensil)
            {
                equipmentQuery = equipmentQuery.Where(e =>
                    e.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                    e.Category == EquipmentCategory.Utensil &&
                    e.UtensilType.HasValue &&
                    validUtensilTypes.Contains(e.UtensilType.Value));

                if (SelectedUtensilType.HasValue)
                {
                    equipmentQuery = equipmentQuery.Where(e =>
                        e.UtensilType == SelectedUtensilType.Value);
                }
            }
            else if (SelectedCategory == EquipmentCategory.Other)
            {
                equipmentQuery = equipmentQuery.Where(e =>
                    e.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed &&
                    e.Category == EquipmentCategory.Other);
            }

            if (SelectedReviewStatus.HasValue)
            {
                equipmentQuery = equipmentQuery.Where(e =>
                    e.ClassificationReviewStatus == SelectedReviewStatus.Value);
            }

            // B) APLICAR EL FILTRO POR AMBIENTE (LABORATORIO)
            if (SelectedLaboratoryId.HasValue)
            {
                equipmentQuery = equipmentQuery.Where(e => e.Units!.Any(u =>
                    u.CurrentStatus != EquipmentStatus.Deleted &&
                    u.LaboratoryId == SelectedLaboratoryId.Value));
            }

            // C) ORDENAR ALFABÉTICAMENTE POR NOMBRE
            Equipment = await PaginatedList<Proyecto_Laboratorios_Univalle.Models.Equipment>.CreateAsync(equipmentQuery.OrderBy(e => e.Name), pageIndex ?? 1, 20);
        }

        private async Task LoadSubclassificationOptionsAsync()
        {
            var equipmentClassifications = await _context.Equipments
                .Where(e => e.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed
                    && e.Category == EquipmentCategory.Equipment
                    && e.TypeClassification.HasValue)
                .Select(e => e.TypeClassification)
                .Distinct()
                .ToListAsync();

            EquipmentTypeClassificationOptions = equipmentClassifications
                .Where(value => value.HasValue && EquipmentClassificationRules.IsValidEquipmentSubclassification(value.Value))
                .Select(value => new SelectListItem
                {
                    Value = ((int)value!.Value).ToString(),
                    Text = EnumHelper.GetDisplayName(value.Value)
                })
                .OrderBy(option => option.Text)
                .ToList();

            var utensilTypes = await _context.Equipments
                .Where(e => e.ClassificationReviewStatus == EquipmentClassificationReviewStatus.Confirmed
                    && e.Category == EquipmentCategory.Utensil
                    && e.UtensilType.HasValue)
                .Select(e => e.UtensilType)
                .Distinct()
                .ToListAsync();

            UtensilTypeOptions = utensilTypes
                .Where(value => value.HasValue && EquipmentClassificationRules.IsValidUtensilSubclassification(value.Value))
                .Select(value => new SelectListItem
                {
                    Value = ((int)value!.Value).ToString(),
                    Text = EnumHelper.GetDisplayName(value.Value)
                })
                .OrderBy(option => option.Text)
                .ToList();
        }

        private void NormalizeHierarchicalFilters()
        {
            if (SelectedCategory.HasValue &&
                !Enum.IsDefined(typeof(EquipmentCategory), SelectedCategory.Value))
            {
                SelectedCategory = null;
            }

            if (SelectedCategory == EquipmentCategory.Equipment)
            {
                SelectedUtensilType = null;

                if (SelectedEquipmentTypeClassification.HasValue &&
                    !EquipmentClassificationRules.IsValidEquipmentSubclassification(SelectedEquipmentTypeClassification.Value))
                {
                    SelectedEquipmentTypeClassification = null;
                }

                return;
            }

            if (SelectedCategory == EquipmentCategory.Utensil)
            {
                SelectedEquipmentTypeClassification = null;

                if (SelectedUtensilType.HasValue &&
                    !EquipmentClassificationRules.IsValidUtensilSubclassification(SelectedUtensilType.Value))
                {
                    SelectedUtensilType = null;
                }

                return;
            }

            SelectedEquipmentTypeClassification = null;
            SelectedUtensilType = null;
        }
    }
}
