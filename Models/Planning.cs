using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Planificación: sesión concreta de práctica de laboratorio (antes L3Request). Código PRA-{Id}.
    /// </summary>
    public class Planning : IAuditable
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

        public int? AcademicWeekId { get; set; }
        [ForeignKey("AcademicWeekId")]
        public AcademicWeek? AcademicWeekRef { get; set; }

        /// <summary>
        /// Fuente de verdad del horario: fecha (ScheduledDate.Date) + StartPeriod + PeriodCount.
        /// ScheduledDate/ScheduledEndDate/StartTime/EndTime son derivados que solo escribe PlanningSlot.Apply.
        /// </summary>
        public int? StartPeriodId { get; set; }
        [ForeignKey("StartPeriodId")]
        public ClassPeriod? StartPeriod { get; set; }

        /// <summary>Cantidad de periodos consecutivos que ocupa la práctica desde StartPeriod.</summary>
        [Range(1, 12)]
        public int? PeriodCount { get; set; }

        public DateTime ScheduledDate { get; set; }

        public DateTime? ScheduledEndDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        [StringLength(1000)]
        public string? Observations { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = PlanningStatus.Planned;

        /// <summary>Estado que tenía antes de pasar a PendienteReposicion (el destino de la reposición lo hereda).</summary>
        [StringLength(50)]
        public string? StatusBeforeRecovery { get; set; }

        public ICollection<PlanningRequirement> Requirements { get; set; } = new List<PlanningRequirement>();
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
    /// <summary>Estados de la planificación (se guardan como texto, compatibles con RamaDani).</summary>
    public static class PlanningStatus
    {
        public const string Planned = "Planificada";
        public const string Approved = "Aprobada";
        public const string Finalized = "Finalizada";
        /// <summary>Legado: una planificación nunca se cancela; se conserva por datos previos.</summary>
        public const string Cancelled = "Cancelada";
        /// <summary>Origen de una reprogramación individual (visible atenuado, enlazado al destino).</summary>
        public const string Rescheduled = "Reprogramada";
        /// <summary>Planificación de un día suspendido que espera en la bandeja de reposición.</summary>
        public const string PendingRecovery = "PendienteReposicion";
        /// <summary>Origen ya reubicado dentro de una reposición de día.</summary>
        public const string Recovered = "Repuesta";
        public static readonly string[] All = [Planned, Approved, Finalized, Cancelled, Rescheduled, PendingRecovery, Recovered];
        /// <summary>Estados que NO ocupan ambiente ni docente ni demandan material.</summary>
        public static readonly string[] NonOccupying = [Cancelled, Rescheduled, PendingRecovery, Recovered];
        public static bool Occupies(string status) => !NonOccupying.Contains(status);
        /// <summary>Orígenes ya reubicados (o esperando reposición): su L-3 se sigue cerrando en ellos.</summary>
        public static readonly string[] Moved = [Rescheduled, PendingRecovery, Recovered];
        /// <summary>Se registran devoluciones del L-3: aprobada, o un origen movido con material entregado sin devolver.</summary>
        public static bool AllowsL3Return(string status, bool hasOpenDeparture) =>
            status == Approved || (hasOpenDeparture && Moved.Contains(status));
        /// <summary>Se registran incidentes L-5: aprobada, o un origen movido que tuvo salida de material.</summary>
        public static bool AllowsIncidents(string status, bool hasDeparture) =>
            status == Approved || (hasDeparture && Moved.Contains(status));
        public static string Label(string status) => status switch
        {
            PendingRecovery => "Pendiente de reposición",
            _ => status
        };
    }
}
