using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum UserRole
    {
        [Display(Name = "Técnico")]
        Tecnico = 1,
        [Display(Name = "Supervisor")]
        Supervisor = 2,
        [Display(Name = "Director")]
        Director = 3,
        [Display(Name = "Ingeniero")]
        Ingeniero = 4,
        [Display(Name = "Administrador")]
        Administrador = 5,
        [Display(Name = "Super administrador")]
        SuperAdmin = 99
    }
}
