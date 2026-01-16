using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Detailed cost breakdown for maintenance (spare parts, labor, etc.)
    /// </summary>
    public class CostDetail : IAuditable
    {
        // ========================================
        // PRIMARY KEY
        // ========================================
        [Key]
        public int Id { get; set; }

        // ========================================
        // FOREIGN KEY
        // ========================================
        [Required]
        [Display(Name = "Mantenimiento")]
        public int MaintenanceId { get; set; }

        // ========================================
        // COST INFORMATION
        // ========================================
        [Required(ErrorMessage = "El concepto es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Concepto")]
        public string Concept { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descripción Detallada")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Cantidad")]
        [Range(0.01, 99999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Quantity { get; set; } = 1;

        [StringLength(50)]
        [Display(Name = "Unidad de Medida")]
        public string? UnitOfMeasure { get; set; } = "Unidad";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio Unitario")]
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
        [Display(Name = "Proveedor")]
        public string? Provider { get; set; }

        [StringLength(100)]
        [Display(Name = "N° Factura/Recibo")]
        public string? InvoiceNumber { get; set; }

        // ========================================
        // AUDIT (IAuditable Implementation)
        // ========================================
        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        // ========================================
        // NAVIGATION PROPERTIES
        // ========================================
        [ForeignKey("MaintenanceId")]
        public virtual Maintenance? Maintenance { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        // ========================================
        // CALCULATED PROPERTIES
        // ========================================

        /// <summary>
        /// Item subtotal (Quantity * UnitPrice)
        /// </summary>
        [NotMapped]
        [Display(Name = "Subtotal")]
        public decimal Subtotal => Quantity * UnitPrice;
    }
}
