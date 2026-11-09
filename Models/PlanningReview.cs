using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Acción registrada en el historial de una solicitud de práctica de un docente.</summary>
    public enum PlanningReviewAction
    {
        [Display(Name = "Enviada")] Submitted = 1,
        [Display(Name = "Reenviada con correcciones")] Resubmitted = 2,
        [Display(Name = "Observada")] Observed = 3,
        [Display(Name = "Aprobada")] Approved = 4,
        [Display(Name = "Aprobada con cambios")] ApprovedWithChanges = 5,
        [Display(Name = "Rechazada")] Rejected = 6,
        [Display(Name = "Retirada por el docente")] Withdrawn = 7
    }

    /// <summary>
    /// Historial inmutable de una solicitud docente (una fila por acción): quién, cuándo, de qué estado a cuál
    /// y con qué comentario. El estado vigente vive en <see cref="Planning.Status"/>; aquí no se sobrescribe nada.
    /// </summary>
    public class PlanningReview
    {
        [Key]
        public int Id { get; set; }

        public int PlanningId { get; set; }
        [ForeignKey(nameof(PlanningId))]
        public Planning? Planning { get; set; }

        public PlanningReviewAction Action { get; set; }

        [Required, StringLength(50)]
        public string FromStatus { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string ToStatus { get; set; } = string.Empty;

        /// <summary>Comentario del revisor (observación o motivo de rechazo) o nota del docente al reenviar.</summary>
        [StringLength(1000)]
        public string? Comment { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<PlanningReviewLine> Lines { get; set; } = new List<PlanningReviewLine>();
    }

    /// <summary>
    /// Cambio de cantidad que el revisor hizo a un artículo al aprobar con cambios ("pedido vs aprobado").
    /// PreviousQuantity null = artículo agregado por el revisor; NewQuantity null = artículo quitado.
    /// </summary>
    public class PlanningReviewLine
    {
        [Key]
        public int Id { get; set; }

        public int PlanningReviewId { get; set; }
        [ForeignKey(nameof(PlanningReviewId))]
        public PlanningReview? PlanningReview { get; set; }

        public int ArticleId { get; set; }
        [ForeignKey(nameof(ArticleId))]
        public Article? Article { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? PreviousQuantity { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? NewQuantity { get; set; }
    }
}
