using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum MaintenanceSatisfaction
    {
        [Display(Name = "Muy Satisfecho")]
        VerySatisfied = 5,

        [Display(Name = "Satisfecho")]
        Satisfied = 4,

        [Display(Name = "Ok")]
        Ok = 3,

        [Display(Name = "Insatisfecho")]
        Dissatisfied = 2,

        [Display(Name = "Inaceptable")]
        Unacceptable = 1
    }
}
