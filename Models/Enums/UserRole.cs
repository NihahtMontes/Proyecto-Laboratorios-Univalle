using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum UserRole
    {
        [Display(Name = "Administrador")]
        Administrador = 1,

        [Display(Name = "Supervisor")]
        Supervisor = 2,

        [Display(Name = "Super Administrador")]
        SuperAdmin = 99
    }
}
