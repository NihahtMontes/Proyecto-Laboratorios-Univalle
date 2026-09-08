using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Identifica de forma inmutable la fuente y ejecución de una importación histórica.
    /// No contiene credenciales ni rutas privadas de nube.
    /// </summary>
    public class ImportBatch
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string SourceType { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string ContractVersion { get; set; } = "legacy-v1";

        [Required, StringLength(260)]
        public string SourceName { get; set; } = string.Empty;

        [Required, StringLength(64, MinimumLength = 64)]
        public string SourceSha256 { get; set; } = string.Empty;

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Prepared;

        [StringLength(2000)]
        public string? Notes { get; set; }

        public virtual ICollection<DataQualityIssue> DataQualityIssues { get; set; } = [];
        public virtual ICollection<ImportSourceRow> SourceRows { get; set; } = [];
        public virtual ICollection<MaintenancePlan> MaintenancePlans { get; set; } = [];
        public virtual ICollection<EquipmentClassificationDecision> EquipmentClassificationDecisions { get; set; } = [];
    }
}
