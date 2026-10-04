using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class L3Request : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; } = null!;

        [Required]
        public int GroupId { get; set; }
        [ForeignKey("GroupId")]
        public Group Group { get; set; } = null!;

        // Legado de RamaDani: docente como usuario del sistema. Se conserva opcional.
        public int? TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }

        // Docente como actor (People, categoría Docente).
        public int? TeacherPersonId { get; set; }
        [ForeignKey("TeacherPersonId")]
        public Person? TeacherPerson { get; set; }

        [Required]
        public int LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public Laboratory Laboratory { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string PracticeName { get; set; } = string.Empty;

        /// <summary>Número de semana académica (se mantiene sincronizado con AcademicWeekRef).</summary>
        public int AcademicWeek { get; set; }

        /// <summary>Planificación a la que pertenece la práctica (null en prácticas legadas).</summary>
        public int? AcademicPlanningId { get; set; }
        [ForeignKey("AcademicPlanningId")]
        public AcademicPlanning? AcademicPlanning { get; set; }

        public int? AcademicWeekId { get; set; }
        [ForeignKey("AcademicWeekId")]
        public AcademicWeek? AcademicWeekRef { get; set; }

        public DateTime ScheduledDate { get; set; }

        public DateTime? ScheduledEndDate { get; set; }

        [StringLength(1000)]
        public string? Observations { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = L3RequestStatus.Planned;

        public ICollection<L3RequestDetail> Details { get; set; } = new List<L3RequestDetail>();
        public ICollection<L5Incident> Incidents { get; set; } = new List<L5Incident>();

        // IAuditable implementation
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedById { get; set; }
        public int? ModifiedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
    /// <summary>Estados de la planificación L-3 (se guardan como texto, compatibles con RamaDani).</summary>
    public static class L3RequestStatus
    {
        public const string Planned = "Planificado";
        public const string Approved = "Aprobado";
        public const string Finalized = "Finalizado";
        public const string Cancelled = "Cancelado";
    }
}
