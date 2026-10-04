using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum DepartureReturnStatus
    {
        [Display(Name = "Parcial")]
        Partial = 0,

        [Display(Name = "Completa")]
        Complete = 1,

        [Display(Name = "Con faltantes")]
        WithShortages = 2,

        [Display(Name = "Por confirmar")]
        PorConfirmar = 9
    }
}
