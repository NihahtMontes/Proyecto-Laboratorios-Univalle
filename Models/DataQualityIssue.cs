using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class DataQualityIssue
    {
        [Key]
        public long Id { get; set; }

        public int ImportBatchId { get; set; }

        [Required, StringLength(100)]
        public string IssueCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? EntityKey { get; set; }

        [StringLength(100)]
        public string? FieldName { get; set; }

        [StringLength(128)]
        public string? SourceSheet { get; set; }

        public int? SourceRowNumber { get; set; }

        [StringLength(200)]
        public string? SourceCode { get; set; }

        public string? CandidateKeys { get; set; }

        public string? OriginalValue { get; set; }
        public string? NormalizedValue { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DataQualityIssueSeverity Severity { get; set; } = DataQualityIssueSeverity.Warning;
        public DataQualityIssueStatus Status { get; set; } = DataQualityIssueStatus.Open;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedDate { get; set; }

        [StringLength(2000)]
        public string? ResolutionNotes { get; set; }

        public int? ResolvedByUserId { get; set; }

        [ForeignKey(nameof(ImportBatchId))]
        public virtual ImportBatch? ImportBatch { get; set; }

        [ForeignKey(nameof(ResolvedByUserId))]
        public virtual User? ResolvedByUser { get; set; }
    }
}
