using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum EquipmentCategory
    {
        [Display(Name = "Equipo")]
        Equipment = 0,

        [Display(Name = "Utensilio")]
        Utensil = 1
    }
    public enum UtensilType
    {
        NoAplica = 0,
        Vidrio = 1,
        Plastico = 2,
        Metal = 3
    }
}
