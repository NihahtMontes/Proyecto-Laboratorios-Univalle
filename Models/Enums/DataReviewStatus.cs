using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    /// <summary>
    /// Confirmación del dato en la fuente del cliente. No es un estado de ciclo de vida:
    /// un artículo puede estar Activo y "Por confirmar" a la vez.
    /// </summary>
    public enum DataReviewStatus
    {
        [Display(Name = "Confirmado")]
        Confirmed = 0,

        /// <summary>Valor literal "Por confirmar" de la base de la ingeniera (no se ofrece en formularios de alta).</summary>
        [Display(Name = "Por confirmar")]
        PorConfirmar = 9
    }
}
