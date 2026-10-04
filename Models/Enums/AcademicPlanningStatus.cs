using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    /// <summary>Estado de una planificación académica. Solo puede existir UNA activa (como la gestión).</summary>
    public enum AcademicPlanningStatus
    {
        [Display(Name = "Activa")]
        Active = 0,

        [Display(Name = "Inactiva")]
        Inactive = 1,

        [Display(Name = "Cerrada")]
        Completed = 2
    }
}
