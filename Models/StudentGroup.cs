using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Grupo de estudiantes de la gestión: carrera + semestre + letra (A, B...). Ej.: "A1" = Grupo A de 1.er semestre.
    /// Sus materias son los <see cref="CourseGroup"/> (uno por materia, con su docente); todos comparten el nombre,
    /// los estudiantes y el factor del grupo, y su horario semanal es uno solo (horario por semestre).
    /// Como los grupos de materia, es dato de trabajo de la gestión: se purga al completarla.
    /// </summary>
    public class StudentGroup : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Código")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Gestión")]
        public int ManagementId { get; set; }
        [ForeignKey("ManagementId")]
        public virtual Management? Management { get; set; }

        [Display(Name = "Carrera")]
        public int CareerId { get; set; }
        [ForeignKey("CareerId")]
        public virtual Career? Career { get; set; }

        [Range(1, 12)]
        [Display(Name = "Semestre")]
        public int Semester { get; set; }

        /// <summary>Letra del paralelo (A, B, C...). Grupos históricos con otro formato conservan su nombre aquí.</summary>
        [Required, StringLength(18)]
        [Display(Name = "Letra")]
        public string Letter { get; set; } = string.Empty;

        /// <summary>Nombre visible = letra + semestre ("A1"). Se copia a cada <see cref="CourseGroup.Name"/> del grupo.</summary>
        [Required, StringLength(20)]
        [Display(Name = "Grupo")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 1000)]
        [Display(Name = "Estudiantes")]
        public int? StudentCount { get; set; }

        [Range(0, 1000)]
        [Display(Name = "Factor de escalamiento")]
        public int? ScalingFactor { get; set; }

        [Display(Name = "Laboratorio habitual")]
        public int? LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public virtual Laboratory? Laboratory { get; set; }

        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>Materias del grupo (una fila por materia, con su docente y horario).</summary>
        public virtual ICollection<CourseGroup> Subjects { get; set; } = new List<CourseGroup>();

        /// <summary>Nombre visible de un grupo: letra + semestre si la letra es solo A–Z ("A" + 1 = "A1"); si no, la letra tal cual.</summary>
        public static string ComposeName(string letter, int semester)
        {
            var clean = letter.Trim().ToUpperInvariant();
            return clean.Length > 0 && clean.All(c => c is >= 'A' and <= 'Z') ? $"{clean}{semester}" : clean;
        }
    }
}
