using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum RequestStatus
    {
        [Display(Name = "Pendiente")]
        Pending = 0,

        [Display(Name = "Programado")]
        Scheduled = 1,

        [Display(Name = "Programado")]
        InProgress = 2,

        [Display(Name = "Aprobado")]
        Approved = 3,

        [Display(Name = "Rechazado")]
        Rejected = 4,

        [Display(Name = "Completado")]
        Completed = 5,

        [Display(Name = "Cancelado")]
        Cancelled = 99
    }
}
