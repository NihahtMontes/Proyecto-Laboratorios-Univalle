using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum UserRole
    {
        [Display(Name = "Administrador")]
        Administrador = 1,

        [Display(Name = "Supervisor")]
        Supervisor = 2,

        /// <summary>Solo cierra prácticas del día: entrega/devolución L-3 e incidentes L-5.</summary>
        [Display(Name = "Encargado de cierre de prácticas")]
        PracticeClosure = 3,

        [Display(Name = "Super Administrador")]
        SuperAdmin = 99
    }
}
