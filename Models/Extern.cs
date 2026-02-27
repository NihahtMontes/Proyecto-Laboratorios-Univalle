using System.ComponentModel.DataAnnotations;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class Extern : Person
    {
        [Required]
        [Display(Name = "¿Es una Entidad?")]
        public bool IsEntity { get; set; } = false;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200)]
        [RegularExpression(@"^[a-zA-Z0-9\s.\-]*$", ErrorMessage = "Formato de nombre inválido")]
        [Display(Name = "Nombre de Persona o Entidad")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [StringLength(500)]
        [Display(Name = "Dirección")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Estado Externo")]
        public GeneralStatus ExternStatus { get; set; } = GeneralStatus.Activo;

        public override string FullName => Name;
    }
}
