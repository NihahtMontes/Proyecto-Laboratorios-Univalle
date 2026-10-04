using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    /// <summary>Tipo de artículo (TipoArticulo de la base estructural): equipo patrimonial o utensilio contable.</summary>
    public enum ArticleType
    {
        [Display(Name = "Equipo")]
        Equipment = 1,

        [Display(Name = "Utensilio")]
        Utensil = 2
    }
}
