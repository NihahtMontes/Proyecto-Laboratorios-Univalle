using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Semana académica de una gestión (lunes a sábado). No se modifica una vez planificada.</summary>
    public class AcademicWeek
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ManagementId { get; set; }
        [ForeignKey("ManagementId")]
        public Management Management { get; set; } = null!;

        public int Number { get; set; }

        /// <summary>Lunes de la semana.</summary>
        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        /// <summary>Sábado de la semana.</summary>
        [Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        public ICollection<AcademicDay> Days { get; set; } = new List<AcademicDay>();
    }

    /// <summary>Día académico (lunes a sábado) habilitado o suspendido. Los domingos no existen.</summary>
    public class AcademicDay
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AcademicWeekId { get; set; }
        [ForeignKey("AcademicWeekId")]
        public AcademicWeek AcademicWeek { get; set; } = null!;

        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        public AcademicDayStatus Status { get; set; } = AcademicDayStatus.Enabled;

        [StringLength(200)]
        public string? Reason { get; set; }

        public int? DayRecoveryId { get; set; }
        [ForeignKey("DayRecoveryId")]
        public DayRecovery? DayRecovery { get; set; }
    }
}
