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

        /// <summary>Solicita prácticas de laboratorio y consulta solo las suyas (vinculado a una persona docente).</summary>
        [Display(Name = "Docente")]
        Docente = 4,

        [Display(Name = "Super Administrador")]
        SuperAdmin = 99
    }
}
