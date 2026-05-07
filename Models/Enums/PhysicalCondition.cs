using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum PhysicalCondition
    {
        [Display(Name = "EXCELENTE (NUEVO)")]
        Excellent = 5,

        [Display(Name = "BUENO")]
        Good = 4,

        [Display(Name = "REGULAR")]
        Regular = 3,

        [Display(Name = "MALO")]
        Bad = 2,

        [Display(Name = "BAJA DEL EQUIPO")]
        Decommissioned = 1
    }
}
