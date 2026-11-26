using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Estado de la cuenta de un estudiante que se registró solo (la aprueba un administrador).</summary>
    public enum StudentAccountStatus
    {
        [Display(Name = "Pendiente de aprobación")] Pending = 0,
        [Display(Name = "Aprobada")] Approved = 1,
        [Display(Name = "Rechazada")] Rejected = 2,
        [Display(Name = "Dada de baja")] Deactivated = 3
    }

    public enum FreePracticeSlotStatus
    {
        [Display(Name = "Abierto")] Open = 0,
        [Display(Name = "Cancelado")] Cancelled = 1
    }

    public enum SlotReservationStatus
    {
        [Display(Name = "Pendiente")] Pending = 0,
        [Display(Name = "Aprobada")] Approved = 1,
        [Display(Name = "Rechazada")] Rejected = 2,
        [Display(Name = "Cancelada por el estudiante")] Cancelled = 3,
        [Display(Name = "Asistió")] Attended = 4,
        [Display(Name = "No se presentó")] NoShow = 5,
        [Display(Name = "Mal uso")] Misuse = 6
    }

    public enum StudentIncidentType
    {
        [Display(Name = "No se presentó")] NoShow = 0,
        [Display(Name = "Mal uso del laboratorio o del material")] Misuse = 1,
        [Display(Name = "Reserva falsa")] FalseReservation = 2,
        [Display(Name = "Otra")] Other = 3
    }

    /// <summary>
    /// Datos de estudiante de un usuario con rol Estudiante (registro propio con correo @est.univalle.edu).
    /// La suspensión y la baja son manuales (decisión 2026-11-23).
    /// </summary>
    public class StudentProfile
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual User? User { get; set; }

        /// <summary>Código de carrera (LGS, LCG, LND, LTH).</summary>
        [Required, StringLength(10)]
        public string Career { get; set; } = string.Empty;

        [Range(1, 10)]
        public int Semester { get; set; }

        public StudentAccountStatus AccountStatus { get; set; } = StudentAccountStatus.Pending;

        public int? ReviewedById { get; set; }
        public DateTime? ReviewedDate { get; set; }

        [StringLength(500)]
        public string? ReviewNote { get; set; }

        /// <summary>Hasta qué día (inclusive) no puede reservar; null = no está suspendido.</summary>
        public DateTime? SuspendedUntil { get; set; }

        [StringLength(500)]
        public string? SuspensionReason { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<SlotReservation> Reservations { get; set; } = new List<SlotReservation>();
        public virtual ICollection<StudentIncident> Incidents { get; set; } = new List<StudentIncident>();

        [NotMapped]
        public bool IsSuspended => SuspendedUntil is DateTime until && until.Date >= DateTime.Today;
    }

    /// <summary>Cupo de práctica libre que abre la Inge: laboratorio, fecha, periodos y cuántos estudiantes.</summary>
    public class FreePracticeSlot
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Code { get; set; } = string.Empty;

        public int LaboratoryId { get; set; }
        public virtual Laboratory? Laboratory { get; set; }

        public DateTime Date { get; set; }

        public int StartPeriodId { get; set; }
        public virtual ClassPeriod? StartPeriod { get; set; }

        [Range(1, 12)]
        public int PeriodCount { get; set; }

        [Range(1, 30)]
        public int Capacity { get; set; } = 6;

        [StringLength(500)]
        public string? Notes { get; set; }

        public FreePracticeSlotStatus Status { get; set; } = FreePracticeSlotStatus.Open;

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<SlotReservation> Reservations { get; set; } = new List<SlotReservation>();
    }

    /// <summary>Lugar que pide un estudiante en un cupo; la Inge lo aprueba o rechaza y luego se marca la asistencia.</summary>
    public class SlotReservation
    {
        public int Id { get; set; }

        public int FreePracticeSlotId { get; set; }
        public virtual FreePracticeSlot? Slot { get; set; }

        public int StudentProfileId { get; set; }
        public virtual StudentProfile? Student { get; set; }

        /// <summary>Qué va a practicar (menú, técnica).</summary>
        [Required, StringLength(500)]
        public string Purpose { get; set; } = string.Empty;

        public SlotReservationStatus Status { get; set; } = SlotReservationStatus.Pending;

        public int? ReviewedById { get; set; }
        public DateTime? ReviewedDate { get; set; }

        [StringLength(500)]
        public string? ReviewNote { get; set; }

        public DateTime? CancelledDate { get; set; }

        public int? AttendanceById { get; set; }
        public DateTime? AttendanceDate { get; set; }

        [StringLength(500)]
        public string? AttendanceNote { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<SlotReservationItem> Items { get; set; } = new List<SlotReservationItem>();
    }

    /// <summary>Material que pide el estudiante (como un L-3 simple), revisado por la Inge junto con la reserva.</summary>
    public class SlotReservationItem
    {
        public int Id { get; set; }

        public int SlotReservationId { get; set; }
        public virtual SlotReservation? Reservation { get; set; }

        public int ArticleId { get; set; }
        public virtual Article? Article { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }
    }

    /// <summary>Falta registrada en la ficha del estudiante (historial visible para administradores).</summary>
    public class StudentIncident
    {
        public int Id { get; set; }

        public int StudentProfileId { get; set; }
        public virtual StudentProfile? Student { get; set; }

        public int? SlotReservationId { get; set; }
        public virtual SlotReservation? Reservation { get; set; }

        public StudentIncidentType Type { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
