using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Reprogramación individual: mueve una planificación (origen) a una nueva fecha, periodo,
    /// ambiente y docente creando la planificación destino. Snapshot del bloque original.
    /// La auditoría la llena ApplicationDbContext (CreatedById = quien la realizó), no asignar a mano.
    /// </summary>
    public class Rescheduling : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OriginPlanningId { get; set; }
        [ForeignKey("OriginPlanningId")]
        public Planning OriginPlanning { get; set; } = null!;

        [Required]
        public int DestinationPlanningId { get; set; }
        [ForeignKey("DestinationPlanningId")]
        public Planning DestinationPlanning { get; set; } = null!;

        // Snapshot del bloque original antes de mover.
        [Column(TypeName = "date")]
        public DateTime OriginDate { get; set; }

        public int? OriginStartPeriodId { get; set; }
        [ForeignKey("OriginStartPeriodId")]
        public ClassPeriod? OriginStartPeriod { get; set; }

        public int? OriginPeriodCount { get; set; }

        public int? OriginLaboratoryId { get; set; }
        [ForeignKey("OriginLaboratoryId")]
        public Laboratory? OriginLaboratory { get; set; }

        public int OriginTeacherPersonId { get; set; }
        [ForeignKey("OriginTeacherPersonId")]
        public Person? OriginTeacherPerson { get; set; }

        [Required]
        [StringLength(50)]
        public string OriginStatus { get; set; } = string.Empty;

        // Bloque destino ya aplicado.
        [Column(TypeName = "date")]
        public DateTime DestinationDate { get; set; }

        public int DestinationStartPeriodId { get; set; }
        [ForeignKey("DestinationStartPeriodId")]
        public ClassPeriod? DestinationStartPeriod { get; set; }

        public int DestinationPeriodCount { get; set; }

        public int DestinationLaboratoryId { get; set; }
        [ForeignKey("DestinationLaboratoryId")]
        public Laboratory? DestinationLaboratory { get; set; }

        public int DestinationTeacherPersonId { get; set; }
        [ForeignKey("DestinationTeacherPersonId")]
        public Person? DestinationTeacherPerson { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public int? DayRecoveryId { get; set; }
        [ForeignKey("DayRecoveryId")]
        public DayRecovery? DayRecovery { get; set; }

        /// <summary>Advertencias aceptadas por el usuario al confirmar (JSON, nvarchar(max)).</summary>
        public string? AcceptedWarnings { get; set; }

        // IAuditable implementation
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedById { get; set; }
        public int? ModifiedById { get; set; }
    }
}
