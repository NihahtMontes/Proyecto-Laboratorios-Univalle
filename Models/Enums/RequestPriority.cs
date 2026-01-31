using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum RequestPriority
    {
        [Display(Name = "Baja")]
        Low = 0,

        [Display(Name = "Media")]
        Medium = 1,

        [Display(Name = "Alta")]
        High = 2,

        [Display(Name = "Crítica")]
        VeryHigh = 3
    }
}
