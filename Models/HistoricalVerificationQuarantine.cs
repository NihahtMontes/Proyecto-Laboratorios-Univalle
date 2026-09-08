using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Preserva filas L-6 que no pueden enlazarse a una unidad fisica sin
    /// inventar una relacion. No participa en el flujo operativo del wizard.
    /// </summary>
    public class HistoricalVerificationQuarantine
    {
        [Key]
        public int Id { get; set; }

        public int? ImportBatchId { get; set; }

        [Required]
        [StringLength(200)]
        public string SourceKey { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string SourceSheet { get; set; } = string.Empty;

        public int SourceRow { get; set; }

        public string? InventoryRaw { get; set; }
        public string? DateRaw { get; set; }
        public string? PhysicalConditionRaw { get; set; }
        public string? FindingRaw { get; set; }
        public string? EquipmentNameRaw { get; set; }
        public string? Column6Raw { get; set; }
        public string? Column7Raw { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        public virtual ImportBatch? ImportBatch { get; set; }
    }
}
