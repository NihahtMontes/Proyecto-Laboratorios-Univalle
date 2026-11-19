using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    /// <summary>Tipo de artículo (TipoArticulo de la base estructural): equipo patrimonial o utensilio contable.</summary>
    public enum ArticleType
    {
        [Display(Name = "Equipo")]
        Equipment = 1,

        [Display(Name = "Utensilio")]
        Utensil = 2,

        /// <summary>Insumo que se consume en la práctica: descuenta existencia al entregar y no se devuelve.</summary>
        [Display(Name = "Consumible")]
        Consumable = 3
    }
}
