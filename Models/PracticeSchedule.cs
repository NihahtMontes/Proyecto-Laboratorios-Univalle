using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Programación de una práctica (ProgramacionPracticas de la base estructural V4):
    /// qué práctica hace qué grupo, con qué docente, dónde y cuándo.
    /// </summary>
    public class PracticeSchedule : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Código")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Gestión")]
        public int ManagementId { get; set; }
        [ForeignKey("ManagementId")]
        public virtual Management? Management { get; set; }

        [Display(Name = "Materia")]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [Display(Name = "Práctica")]
        public int PracticeId { get; set; }
        [ForeignKey("PracticeId")]
        public virtual Practice? Practice { get; set; }

        /// <summary>Grupo derivado (gestión + materia + grupo).</summary>
        public int? CourseGroupId { get; set; }
        [ForeignKey("CourseGroupId")]
        public virtual CourseGroup? CourseGroup { get; set; }

        [StringLength(20)]
        [Display(Name = "Grupo")]
        public string? GroupName { get; set; }

        [Display(Name = "Docente")]
        public int TeacherPersonId { get; set; }
        [ForeignKey("TeacherPersonId")]
        public virtual Person? TeacherPerson { get; set; }

        [Display(Name = "Laboratorio")]
        public int? LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public virtual Laboratory? Laboratory { get; set; }

        /// <summary>Semana académica declarada por la fuente ("no deducir de la fecha").</summary>
        [Range(1, 30)]
        [Display(Name = "Semana")]
        public int? WeekNumber { get; set; }

        /// <summary>Semana de la planificación académica, si se resolvió.</summary>
        public int? AcademicWeekId { get; set; }
        [ForeignKey("AcademicWeekId")]
        public virtual AcademicWeek? AcademicWeek { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Fecha de ejecución")]
        public DateTime? ExecutionDate { get; set; }

        [Display(Name = "Hora de inicio")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "Hora de fin")]
        public TimeSpan? EndTime { get; set; }

        [Range(0, 1000)]
        [Display(Name = "Estudiantes")]
        public int? StudentCount { get; set; }

        [Display(Name = "Demostrativa")]
        public bool? IsDemonstrative { get; set; }

        [Range(0, 1000)]
        [Display(Name = "Factor")]
        public int? ScalingFactor { get; set; }

        [Display(Name = "Estado")]
        public Enums.PracticeScheduleStatus Status { get; set; } = Enums.PracticeScheduleStatus.Scheduled;

        public int? ImportBatchId { get; set; }

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
