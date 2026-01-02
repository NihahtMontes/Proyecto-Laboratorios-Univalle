using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum CostCategory
    {
        [Display(Name = "Repuesto/Material")]
        SparePart = 1,

        [Display(Name = "Mano de Obra")]
        Labor = 2,

        [Display(Name = "Herramienta/Equipo")]
        Tool = 3,

        [Display(Name = "Transporte")]
        Transport = 4,

        [Display(Name = "Servicio Externo")]
        ExternalService = 5,

        [Display(Name = "Calibración")]
        Calibration = 6,

        [Display(Name = "Consumibles")]
        Consumables = 7,

        [Display(Name = "Viáticos")]
        TravelExpenses = 8,

        [Display(Name = "Personal")]
        Staff = 9,

        [Display(Name = "Otros")]
        Others = 99
    }
}
