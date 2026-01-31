using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum RequestStatus
    {
        [Display(Name = "Pendiente")]
        Pending = 0,

        [Display(Name = "Programado")]
        Scheduled = 1,

        [Display(Name = "Aprobado")]
        Approved = 2,

        [Display(Name = "Rechazado")]
        Rejected = 3,

        [Display(Name = "Completado")]
        Completed = 4,

        [Display(Name = "Cancelado")]
        Cancelled = 99
    }
}
