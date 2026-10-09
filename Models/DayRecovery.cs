using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Reposición de un día entero suspendido (en la UI "Reposición de día";
    /// no confundir con la reposición de material del L-5).
    /// La auditoría la llena ApplicationDbContext, no asignar a mano.
    /// </summary>
    public class DayRecovery : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ManagementId { get; set; }
        [ForeignKey("ManagementId")]
        public Management Management { get; set; } = null!;

        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public DayRecoveryStatus Status { get; set; } = DayRecoveryStatus.InProgress;

        public ICollection<Rescheduling> Reschedulings { get; set; } = new List<Rescheduling>();

        // IAuditable implementation
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedById { get; set; }
        public int? ModifiedById { get; set; }
    }
}
