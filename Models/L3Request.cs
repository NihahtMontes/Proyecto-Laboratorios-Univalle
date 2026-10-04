using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Solicitud L-3 (= SolicitudesL3): pedido de artículos de una práctica programada o extraordinaria.
    /// </summary>
    public class L3Request : IAuditable
    {
        [Key]
        public int Id { get; set; }

        /// <summary>SolicitudL3Codigo (PRA-00001 en la app).</summary>
        [StringLength(50)]
        public string? Code { get; set; }

        public int ManagementId { get; set; }
        [ForeignKey("ManagementId")]
        public Management? Management { get; set; }

        /// <summary>Programación de la práctica (null = solicitud extraordinaria).</summary>
        public int? PracticeScheduleId { get; set; }
        [ForeignKey("PracticeScheduleId")]
        public PracticeSchedule? PracticeSchedule { get; set; }

        public DateTime RequestDate { get; set; }

        public int? CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus? Campus { get; set; }

        public int? CareerId { get; set; }
        [ForeignKey("CareerId")]
        public Career? Career { get; set; }

        public int? SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }

        /// <summary>Legado (grupos A–D). Reemplazado por CourseGroupId + GroupName.</summary>
        public int? GroupId { get; set; }
        [ForeignKey("GroupId")]
        public Group? Group { get; set; }

        public int? CourseGroupId { get; set; }
        [ForeignKey("CourseGroupId")]
        public CourseGroup? CourseGroup { get; set; }

        [StringLength(20)]
        public string? GroupName { get; set; }

        // Legado de RamaDani: docente como usuario del sistema. Se conserva opcional.
        public int? TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }

        // Docente como actor (People, categoría Docente).
        public int TeacherPersonId { get; set; }
        [ForeignKey("TeacherPersonId")]
        public Person? TeacherPerson { get; set; }

        /// <summary>Solicitud para todos los laboratorios (TodosLosLaboratorios); si no, LaboratoryId es obligatorio.</summary>
        public bool AllLaboratories { get; set; }

        public int? LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public Laboratory? Laboratory { get; set; }

        [Required]
        [StringLength(255)]
        public string PracticeName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PracticeNumber { get; set; }

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

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [StringLength(1000)]
        public string? Observations { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = L3RequestStatus.Planned;

        public ICollection<L3RequestDetail> Details { get; set; } = new List<L3RequestDetail>();
        public ICollection<L5Incident> Incidents { get; set; } = new List<L5Incident>();
        public ICollection<Departure> Departures { get; set; } = new List<Departure>();

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
