using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Trazabilidad idempotente de una fila de origen hacia una entidad destino.
    /// Una fila puede producir varias entidades, cada una con su propio registro.
    /// </summary>
    public class ImportSourceRow : IAuditable
    {
        [Key]
        public long Id { get; set; }

        public int ImportBatchId { get; set; }

        [Required, StringLength(128)]
        public string SourceSheet { get; set; } = string.Empty;

        public int SourceRowNumber { get; set; }

        [Required, StringLength(200)]
        public string SourceRowKey { get; set; } = string.Empty;

        [StringLength(200)]
        public string? OriginalIdentifier { get; set; }

        [Required, StringLength(100)]
        public string TargetEntityName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? TargetEntityKey { get; set; }

        public ImportSourceRowStatus MigrationStatus { get; set; } = ImportSourceRowStatus.Pending;
        public DataReconciliationStatus ReconciliationStatus { get; set; } = DataReconciliationStatus.Pending;

        public string? OriginalDataJson { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public DateTime? LastReconciledDate { get; set; }
        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        [ForeignKey(nameof(ImportBatchId))]
        public virtual ImportBatch? ImportBatch { get; set; }
        public virtual ICollection<EquipmentClassificationDecision> EquipmentClassificationDecisions { get; set; } = [];
    }
}
