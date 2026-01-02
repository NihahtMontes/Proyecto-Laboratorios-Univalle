using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum VerificationResult
    {
        [Display(Name = "Sin Verificar")]
        NotChecked = 0,

        [Display(Name = "Bueno")]
        Good = 1,

        [Display(Name = "Malo")]
        Bad = 2,

        [Display(Name = "No Aplica")]
        NotApplicable = 3
    }
}
