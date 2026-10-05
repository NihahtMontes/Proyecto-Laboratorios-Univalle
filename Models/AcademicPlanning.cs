using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Planificación académica de prácticas de laboratorio: una por gestión y carrera.
    /// Define el rango de fechas del que se generan las semanas académicas (lunes a viernes).
    /// </summary>
    public class AcademicPlanning : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ManagementId { get; set; }
        [ForeignKey("ManagementId")]
        public Management Management { get; set; } = null!;

        /// <summary>Sede donde se ejecutan las prácticas; limita los laboratorios disponibles. Nullable solo por datos previos.</summary>
        public int? CampusId { get; set; }
        [ForeignKey("CampusId")]
        public Campus? Campus { get; set; }

        [Required]
        public int FacultyId { get; set; }
        [ForeignKey("FacultyId")]
        public Faculty Faculty { get; set; } = null!;

        [Required]
        public int CareerId { get; set; }
        [ForeignKey("CareerId")]
        public Career Career { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        /// <summary>Solo una planificación activa en todo el sistema (índice único filtrado).</summary>
        public Enums.AcademicPlanningStatus Status { get; set; } = Enums.AcademicPlanningStatus.Inactive;

        public ICollection<AcademicWeek> Weeks { get; set; } = new List<AcademicWeek>();
        public ICollection<L3Request> Practices { get; set; } = new List<L3Request>();

        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedById { get; set; }
        public int? ModifiedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
    }

    /// <summary>Semana académica de una planificación (lunes a viernes), lectiva o no.</summary>
    public class AcademicWeek
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AcademicPlanningId { get; set; }
        [ForeignKey("AcademicPlanningId")]
        public AcademicPlanning AcademicPlanning { get; set; } = null!;

        public int Number { get; set; }

        /// <summary>Lunes de la semana.</summary>
        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        /// <summary>Viernes de la semana.</summary>
        [Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        public bool IsLective { get; set; } = true;

        [StringLength(100)]
        public string? Note { get; set; }
    }
}
