using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class Intern : Person
    {
        [Required(ErrorMessage = "El nombre del Laboratorio Especializado es obligatorio")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]*$", ErrorMessage = "Formato de nombre inválido (use letras, números y guiones)")]
        [Display(Name = "Laboratorio Especializado")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Estado Interno")]
        public GeneralStatus InternStatus { get; set; } = GeneralStatus.Activo;

        public override string FullName => Name;
    }
}
