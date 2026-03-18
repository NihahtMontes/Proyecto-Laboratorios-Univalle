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

    // Este es el Enum que usaremos para el filtrado cuando elijan "Utensilio"
    public enum UtensilType
    {
        [Display(Name = "No Aplica")]
        NoAplica = 0,
        [Display(Name = "Material de Vidrio")]
        Vidrio = 1,
        [Display(Name = "Material de Plástico")]
        Plastico = 2,
        [Display(Name = "Material de Metal")]
        Metal = 3,
        [Display(Name = "Porcelana")]
        Porcelana = 4
    }
}