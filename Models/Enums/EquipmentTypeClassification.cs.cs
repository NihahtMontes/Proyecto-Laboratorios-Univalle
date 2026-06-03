using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum EquipmentTypeClassification
    {
        [Display(Name = "Electronico / Electrico")]
        Electronico = 0,

        [Display(Name = "Manual / Mecanico")]
        Manual = 1,

        [Display(Name = "Mobiliario")]
        Mobiliario = 2,

        [Display(Name = "Instrumental de Medicion")]
        Medicion = 3,

        [Display(Name = "Material de Vidrio")]
        Vidrio = 4,

        [Display(Name = "Reactivo / Quimico")]
        Reactivo = 5,

        [Display(Name = "Informatico / Software")]
        Informatico = 6,

        [Display(Name = "Otro")]
        Otro = 7,

        [Display(Name = "Equipos de calor")]
        Calor = 8,

        [Display(Name = "Equipos de frio")]
        Frio = 9,

        [Display(Name = "Equipos de congelacion")]
        Congelacion = 10,

        [Display(Name = "Equipos de ultracongelacion")]
        Ultracongelacion = 11,

        [Display(Name = "Maquinas rotativas")]
        MaquinasRotativas = 12,

        [Display(Name = "Equipos de seguridad industrial")]
        SeguridadIndustrial = 13,

        [Display(Name = "Equipos audiovisuales")]
        Audiovisuales = 14,

        [Display(Name = "Equipos electricos")]
        Electricos = 15
    }
}