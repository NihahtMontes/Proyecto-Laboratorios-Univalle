using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Detalles de costos para Mantenimientos o Items de Solicitudes de Adquisición
    /// </summary>
    public class CostDetail : IAuditable
    {
        [Key]
        public int Id { get; set; }

        // ========================================
        // FOREIGN KEYS (Polymorphic-ish or specific)
        // ========================================
        
        [Display(Name = "Solicitud")]
        public int? RequestId { get; set; }

        [Display(Name = "Mantenimiento")]
        public int? MaintenanceId { get; set; }

        // ========================================
        // COST INFORMATION
        // ========================================
        [Required(ErrorMessage = "El concepto/nombre es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Descripción del Ítem")]
        public string Concept { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Detalles Adicionales")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Cantidad")]
        [Range(0.01, 99999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Quantity { get; set; } = 1;

        [StringLength(50)]
        [Display(Name = "Unidad")]
        public string? UnitOfMeasure { get; set; } = "Unidad";

        [Required]
        [Precision(18, 2)]
        [Display(Name = "Precio Unitario (Estimado)")]
        [Range(0, 999999999, ErrorMessage = "El precio debe ser positivo")]
        public decimal UnitPrice { get; set; }

        // ========================================
        // CATEGORIZATION
        // ========================================
        [Required]
        [Display(Name = "Categoría")]
        public CostCategory Category { get; set; } = CostCategory.SparePart;

        // ========================================
        // PROVIDER (Optional)
        // ========================================
        [StringLength(200)]
        [Display(Name = "Proveedor Sugerido")]
        public string? Provider { get; set; }

        [Display(Name = "Proveedor Normalizado")]
        public int? ProviderPersonId { get; set; }

        [Display(Name = "Fecha del Costo")]
        [DataType(DataType.Date)]
        public DateTime? CostDate { get; set; }

        [StringLength(200)]
        public string? HistoricalSourceKey { get; set; }

        [StringLength(100)]
        [Display(Name = "N° Factura/Recibo")]
        public string? InvoiceNumber { get; set; }

        // ========================================
        // AUDIT
        // ========================================
        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        public int? ImportBatchId { get; set; }

        // ========================================
        // NAVIGATION
        // ========================================
        [ForeignKey("RequestId")]
        public virtual Request? Request { get; set; }

        [ForeignKey("MaintenanceId")]
        public virtual Maintenance? Maintenance { get; set; }

        [ForeignKey(nameof(ProviderPersonId))]
        public virtual Person? ProviderPerson { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        [ForeignKey(nameof(ImportBatchId))]
        public virtual ImportBatch? ImportBatch { get; set; }

        // ========================================
        // CALCULATIONS
        // ========================================
        [NotMapped]
        [Display(Name = "Subtotal")]
        public decimal Subtotal => Quantity * UnitPrice;
    }
}
