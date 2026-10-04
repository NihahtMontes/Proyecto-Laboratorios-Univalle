using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    /// <summary>Estado de una programación de práctica (ProgramacionPracticas.Estado).</summary>
    public enum PracticeScheduleStatus
    {
        [Display(Name = "Programada")]
        Scheduled = 0,

        [Display(Name = "Ejecutada")]
        Executed = 1,

        [Display(Name = "Reprogramada")]
        Rescheduled = 2,

        [Display(Name = "Cancelada")]
        Cancelled = 3,

        /// <summary>Texto de la fuente no reconocido o sin confirmar (solo importación).</summary>
        [Display(Name = "Por confirmar")]
        PorConfirmar = 9
    }
}
