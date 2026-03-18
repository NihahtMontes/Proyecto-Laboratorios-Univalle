using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum RequestType
    {
        [Display(Name = "Solicitud Técnica (Mantenimiento/Calibración)")]
        Technical = 1,

        [Display(Name = "Solicitud Administrativa (Adquisición de Bienes/Servicios)")]
        Purchasing = 2
    }
}