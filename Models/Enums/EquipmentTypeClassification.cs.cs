using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum EquipmentTypeClassification
    {
        [Display(Name = "Electrónico / Eléctrico")]
        Electronico = 0,

        [Display(Name = "Manual / Mecánico")]
        Manual = 1,

        [Display(Name = "Mobiliario")]
        Mobiliario = 2,

        [Display(Name = "Instrumental de Medición")]
        Medicion = 3,

        [Display(Name = "Material de Vidrio")]
        Vidrio = 4,

        [Display(Name = "Reactivo / Químico")]
        Reactivo = 5,

        [Display(Name = "Informático / Software")]
        Informatico = 6,

        [Display(Name = "Otro")]
        Otro = 7
    }
}
