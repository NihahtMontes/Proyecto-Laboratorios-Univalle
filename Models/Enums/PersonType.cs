using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum PersonType
    {
        [Display(Name = "Interno")]
        Internal = 1,

        [Display(Name = "Externo")]
        External = 2
    }
}
