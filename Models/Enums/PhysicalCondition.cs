using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum PhysicalCondition
    {
        [Display(Name = "EXCELENTE")]
        Excelente = 5,
        
        [Display(Name = "BUENO")]
        Bueno = 4,
        
        [Display(Name = "REGULAR")]
        Regular = 3,
        
        [Display(Name = "MALO")]
        Malo = 2,
        
        [Display(Name = "BAJA")]
        Baja = 1 // Para dar de baja
    }
}
