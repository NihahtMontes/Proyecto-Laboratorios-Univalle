using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum AcademicDayStatus
    {
        [Display(Name = "Habilitado")]
        Enabled = 0,

        [Display(Name = "Suspendido")]
        Suspended = 1
    }
}
