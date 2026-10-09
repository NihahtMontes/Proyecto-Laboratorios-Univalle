using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum DayRecoveryStatus
    {
        [Display(Name = "En reposición")]
        InProgress = 0,

        [Display(Name = "Repuesta")]
        Completed = 1
    }
}
