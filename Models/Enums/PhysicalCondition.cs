using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum PhysicalCondition
    {
        [Display(Name = "EXCELENTE (NUEVO)")]
        Excellent = 5,

        [Display(Name = "BUENO (no necesita mantenimiento)")]
        Good = 4,

        [Display(Name = "REGULAR (mantenimiento preventivo, aún operativo)")]
        Regular = 3,

        [Display(Name = "MALO (no funciona, requiere mantenimiento)")]
        Bad = 2,

        [Display(Name = "BAJA DEL EQUIPO")]
        Decommissioned = 1
    }
}
