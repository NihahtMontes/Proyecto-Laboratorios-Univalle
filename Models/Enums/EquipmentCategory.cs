using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum EquipmentCategory
    {
        [Display(Name = "Equipo")]
        Equipment = 0,

        [Display(Name = "Utensilio")]
        Utensil = 1,

        [Display(Name = "Otro")]
        Other = 2
    }

    // Este enum se usa para el filtrado cuando elijan "Utensilio".
    public enum UtensilType
    {
        [Display(Name = "No Aplica")]
        NoAplica = 0,

        [Display(Name = "Material de Vidrio")]
        Vidrio = 1,

        [Display(Name = "Material de Plastico")]
        Plastico = 2,

        [Display(Name = "Material de Metal")]
        Metal = 3,

        [Display(Name = "Porcelana")]
        Porcelana = 4,

        [Display(Name = "Menaje de cocina")]
        MenajeCocina = 5,

        [Display(Name = "Bartending y Barismo")]
        BartendingBarismo = 6,

        [Display(Name = "Panaderia, Reposteria y Pasteleria")]
        PanaderiaReposteriaPasteleria = 7,

        [Display(Name = "Servicio")]
        Servicio = 8,

        [Display(Name = "Manteleria")]
        Manteleria = 9,

        [Display(Name = "Vajilla en general")]
        VajillaGeneral = 10,

        [Display(Name = "Otros")]
        Otros = 11
    }
}
