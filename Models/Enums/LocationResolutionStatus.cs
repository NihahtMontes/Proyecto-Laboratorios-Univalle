using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum LocationResolutionStatus
    {
        [Display(Name = "Pendiente de confirmar")]
        Pending = 0,

        [Display(Name = "Confirmada")]
        Confirmed = 1
    }
}
