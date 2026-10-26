using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Línea de material de una solicitud L-3 histórica sin fecha (= Solicitudl3detalle del Excel).</summary>
    public class UndatedL3RequestLine : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UndatedL3RequestId { get; set; }
        [ForeignKey("UndatedL3RequestId")]
        public UndatedL3Request UndatedL3Request { get; set; } = null!;

        /// <summary>Solicitudl3detallecodigo del Excel de origen.</summary>
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public int? ArticleId { get; set; }
        [ForeignKey("ArticleId")]
        public Article? Article { get; set; }

        /// <summary>Unidad física concreta (NumeroInventario), opcional.</summary>
        public int? EquipmentUnitId { get; set; }
        [ForeignKey("EquipmentUnitId")]
        public EquipmentUnit? EquipmentUnit { get; set; }

        public decimal? RequestedQuantity { get; set; }

        [StringLength(2000)]
        public string? Observations { get; set; }

        // IAuditable implementation
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedById { get; set; }
        public int? ModifiedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
