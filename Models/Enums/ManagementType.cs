using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum ManagementType
    {
        [Display(Name = "Preventiva")]
        Preventive = 0,

        [Display(Name = "Correctiva")]
        Corrective = 1,

        /// <summary>Gestión histórica con tipo sin confirmar en la base del cliente (solo importación).</summary>
        [Display(Name = "Por confirmar")]
        PorConfirmar = 9
    }
}
