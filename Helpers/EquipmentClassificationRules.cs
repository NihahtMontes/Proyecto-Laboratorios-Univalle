using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Helpers
{
    public static class EquipmentClassificationRules
    {
        private static readonly EquipmentTypeClassification[] EquipmentSubclassificationValues =
            Enum.GetValues<EquipmentTypeClassification>();

        private static readonly UtensilType[] UtensilSubclassificationValues =
        {
            UtensilType.Vidrio,
            UtensilType.Plastico,
            UtensilType.Metal,
            UtensilType.Porcelana,
            UtensilType.MenajeCocina,
            UtensilType.BartendingBarismo,
            UtensilType.PanaderiaReposteriaPasteleria,
            UtensilType.Servicio,
            UtensilType.Manteleria,
            UtensilType.VajillaGeneral,
            UtensilType.Otros
        };

        public static IReadOnlyList<EquipmentTypeClassification> EquipmentSubclassifications =>
            EquipmentSubclassificationValues;

        public static IReadOnlyList<UtensilType> UtensilSubclassifications =>
            UtensilSubclassificationValues;

        public static bool IsValidEquipmentSubclassification(EquipmentTypeClassification value) =>
            EquipmentSubclassificationValues.Contains(value);

        public static bool IsValidUtensilSubclassification(UtensilType value) =>
            UtensilSubclassificationValues.Contains(value);

        public static EquipmentCategory ResolveCategory(
            EquipmentCategory category,
            EquipmentTypeClassification? equipmentClassification,
            UtensilType? utensilType)
        {
            return category switch
            {
                EquipmentCategory.Equipment when equipmentClassification.HasValue
                    && IsValidEquipmentSubclassification(equipmentClassification.Value)
                    => EquipmentCategory.Equipment,
                EquipmentCategory.Utensil when utensilType.HasValue
                    && IsValidUtensilSubclassification(utensilType.Value)
                    => EquipmentCategory.Utensil,
                _ => EquipmentCategory.Other
            };
        }

        public static bool RequiresOtherDetail(
            EquipmentCategory category,
            EquipmentTypeClassification? equipmentClassification,
            UtensilType? utensilType) =>
            category == EquipmentCategory.Other
            || equipmentClassification == EquipmentTypeClassification.Otro
            || utensilType == UtensilType.Otros;

        public static bool IsConfirmedCombinationValid(
            EquipmentCategory category,
            EquipmentTypeClassification? equipmentClassification,
            UtensilType? utensilType,
            string? otherDetail)
        {
            var hierarchyIsValid = category switch
            {
                EquipmentCategory.Equipment => equipmentClassification.HasValue
                    && IsValidEquipmentSubclassification(equipmentClassification.Value)
                    && !utensilType.HasValue,
                EquipmentCategory.Utensil => utensilType.HasValue
                    && IsValidUtensilSubclassification(utensilType.Value)
                    && !equipmentClassification.HasValue,
                EquipmentCategory.Other => !equipmentClassification.HasValue && !utensilType.HasValue,
                _ => false
            };

            return hierarchyIsValid
                && (!RequiresOtherDetail(category, equipmentClassification, utensilType)
                    || !string.IsNullOrWhiteSpace(otherDetail));
        }
    }
}
