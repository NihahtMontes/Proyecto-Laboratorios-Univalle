using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum VerificationStatus
    {
        [Display(Name = "Borrador")]
        Draft = 0,

        [Display(Name = "Completado / Satisfactorio")]
        Completed = 1,

        [Display(Name = "Con Observaciones")]
        WithObservations = 2,

        [Display(Name = "Revisado")]
        Reviewed = 3,

        [Display(Name = "Anulada")]
        Annulled = 99
    }
}
