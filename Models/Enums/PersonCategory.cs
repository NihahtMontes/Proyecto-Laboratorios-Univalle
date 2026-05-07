using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum PersonCategory
    {
        [Display(Name = "Técnico")]
        Tecnico = 1,

        [Display(Name = "Docente")]
        Docente = 2,

        [Display(Name = "Estudiante")]
        Estudiante = 3,

        [Display(Name = "Administrativo")]
        Administrativo = 4,

        [Display(Name = "Externo")]
        Externo = 5,

        [Display(Name = "Otro")]
        Otro = 99
    }
}
