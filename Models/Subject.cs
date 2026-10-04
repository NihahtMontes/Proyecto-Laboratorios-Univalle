using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Materia del plan de estudios de una carrera (Materias de la base estructural del cliente).</summary>
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Materia")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Código")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Carrera a la que pertenece (nullable solo por materias creadas antes de esta versión).</summary>
        [Display(Name = "Carrera")]
        public int? CareerId { get; set; }
        [ForeignKey("CareerId")]
        public virtual Career? Career { get; set; }

        [Range(1, 12)]
        [Display(Name = "Semestre")]
        public int? Semester { get; set; }

        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
    }
}
