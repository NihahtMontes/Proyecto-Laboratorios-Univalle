using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum EquipmentClassificationReviewStatus
    {
        [Display(Name = "Inferido legado")]
        LegacyInferred = 0,

        [Display(Name = "Pendiente de cliente")]
        PendingClient = 1,

        [Display(Name = "Confirmado")]
        Confirmed = 2
    }
}
