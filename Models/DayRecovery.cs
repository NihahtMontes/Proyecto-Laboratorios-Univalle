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

        /// <summary>
        /// Categoría del motivo (una de <see cref="DayRecoveryReasonCategories.All"/>): sirve para filtrar las reposiciones.
        /// Nula en las reposiciones anteriores a esta columna.
        /// </summary>
        [StringLength(40)]
        public string? ReasonCategory { get; set; }

        public DayRecoveryStatus Status { get; set; } = DayRecoveryStatus.InProgress;

        public ICollection<Rescheduling> Reschedulings { get; set; } = new List<Rescheduling>();

        // IAuditable implementation
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedById { get; set; }
        public int? ModifiedById { get; set; }
    }

    /// <summary>Categorías del motivo de una suspensión de día con reposición (texto guardado tal cual).</summary>
    public static class DayRecoveryReasonCategories
    {
        public const string Other = "Otro";

        public static readonly IReadOnlyList<string> All =
        [
            "Feriado", "Paro o movilización", "Corte de servicios", "Evento institucional", "Clima o emergencia", Other
        ];

        public static bool IsValid(string? value) => value != null && All.Contains(value);
    }
}
