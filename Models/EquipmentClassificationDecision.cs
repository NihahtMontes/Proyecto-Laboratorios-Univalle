using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class EquipmentClassificationDecision : IAuditable
    {
        [Key]
        public long Id { get; set; }

        [Required, StringLength(200)]
        public string DecisionKey { get; set; } = string.Empty;

        public int EquipmentId { get; set; }
        public EquipmentClassificationReviewStatus ReviewStatus { get; set; }

        public EquipmentCategory? Category { get; set; }
        public EquipmentTypeClassification? TypeClassification { get; set; }
        public UtensilType? UtensilType { get; set; }
        public GeneralStatus? GeneralStatus { get; set; }

        [StringLength(1000)]
        public string? OtherDetail { get; set; }

        [StringLength(1000)]
        public string? EvidenceReference { get; set; }

        public int? ResponsiblePersonId { get; set; }

        [StringLength(200)]
        public string? ResponsibleSnapshot { get; set; }

        public DateTime? DecisionDate { get; set; }

        /// <summary>
        /// NULL significa vigente desde el origen conocido del catálogo.
        /// </summary>
        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public int? ImportBatchId { get; set; }
        public long? ImportSourceRowId { get; set; }
        public int? RecordedByUserId { get; set; }

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        [ForeignKey(nameof(EquipmentId))]
        public virtual Equipment Equipment { get; set; } = null!;

        [ForeignKey(nameof(ResponsiblePersonId))]
        public virtual Person? ResponsiblePerson { get; set; }

        [ForeignKey(nameof(ImportBatchId))]
        public virtual ImportBatch? ImportBatch { get; set; }

        [ForeignKey(nameof(ImportSourceRowId))]
        public virtual ImportSourceRow? ImportSourceRow { get; set; }

        [ForeignKey(nameof(RecordedByUserId))]
        public virtual User? RecordedByUser { get; set; }
    }
}
