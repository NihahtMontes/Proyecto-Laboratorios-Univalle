using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Solicitud L-3 histórica del Excel que no tiene fecha de práctica y por eso
    /// no puede convertirse en Planning. Registro de solo lectura con sus líneas de material.
    /// </summary>
    public class UndatedL3Request : IAuditable
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Solicitudl3codigo del Excel de origen.</summary>
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public int? ManagementId { get; set; }
        [ForeignKey("ManagementId")]
        public Management? Management { get; set; }

        /// <summary>Código de sede como texto del Excel (sin FK a Campus).</summary>
        [StringLength(50)]
        public string? CampusCode { get; set; }

        /// <summary>Código de carrera como texto del Excel (sin FK a Career).</summary>
        [StringLength(50)]
        public string? CareerCode { get; set; }

        public int? LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public Laboratory? Laboratory { get; set; }

        /// <summary>Solicitud para todos los laboratorios (TodosLosLaboratorios); igual que Planning.</summary>
        public bool AllLaboratories { get; set; }

        /// <summary>Docente como actor (People), mismo tipo base que Planning.TeacherPerson.</summary>
        public int? TeacherPersonId { get; set; }
        [ForeignKey("TeacherPersonId")]
        public Person? Person { get; set; }

        public int? SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        [StringLength(50)]
        public string? GroupName { get; set; }

        public DateTime? RequestDate { get; set; }

        /// <summary>Hora de inicio como texto del Excel (sin normalizar a periodos).</summary>
        [StringLength(20)]
        public string? StartTimeText { get; set; }

        [StringLength(20)]
        public string? EndTimeText { get; set; }

        [StringLength(50)]
        public string? PracticeNumberText { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(2000)]
        public string? Observations { get; set; }

        public int? ImportBatchId { get; set; }

        public ICollection<UndatedL3RequestLine> Lines { get; set; } = new List<UndatedL3RequestLine>();

        // IAuditable implementation
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedById { get; set; }
        public int? ModifiedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
