using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum ServiceType
    {
        [Display(Name = "Interno")]
        Internal = 0,

        [Display(Name = "Externo")]
        External = 1,

        /// <summary>Dato histórico sin confirmar (solo importación; no se ofrece en formularios).</summary>
        [Display(Name = "Por confirmar")]
        PorConfirmar = 9
    }
}
