using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum MaintenanceStatus
    {
        [Display(Name = "Pendiente")]
        Pending = 0,
        
        [Display(Name = "En Progreso")]
        InProgress = 1,
        
        [Display(Name = "Completado")]
        Completed = 2,
        
        [Display(Name = "Programado")]
        Scheduled = 3,
        
        [Display(Name = "Cancelado")]
        Cancelled = 99
    }
}
