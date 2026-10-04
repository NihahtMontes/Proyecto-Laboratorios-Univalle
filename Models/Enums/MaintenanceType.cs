using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum MaintenanceType
    {
        [Display(Name = "Preventivo")]
        Preventivo = 1,

        [Display(Name = "Correctivo")]
        Correctivo = 2,

        [Display(Name = "Calibración")]
        Calibracion = 3,

        [Display(Name = "Limpieza")]
        Limpieza = 4,

        [Display(Name = "Eléctrico")]
        Electrico = 5,

        /// <summary>Tipo sin confirmar en la base del cliente (solo importación; no es "Otros").</summary>
        [Display(Name = "Por confirmar")]
        PorConfirmar = 9,

        [Display(Name = "Otros")]
        Otros = 99
    }
}
