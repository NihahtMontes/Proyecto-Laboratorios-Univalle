using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum PhysicalCondition
    {
        [Display(Name = "NUEVO")]
        New = 5,

        [Display(Name = "MUY BUENO")]
        VeryGood = 4,

        [Display(Name = "BUENO")]
        Good = 3,

        [Display(Name = "MALO")]
        Bad = 2,

        [Display(Name = "ROTO")]
        Broken = 1
    }
}
