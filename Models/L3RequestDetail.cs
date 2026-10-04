using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Línea de la solicitud L-3 (= SolicitudL3Detalles): artículo pedido y, opcionalmente, la unidad física.</summary>
    public class L3RequestDetail
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string? Code { get; set; }

        [Required]
        public int L3RequestId { get; set; }
        [ForeignKey("L3RequestId")]
        public L3Request L3Request { get; set; } = null!;

        public int ArticleId { get; set; }
        [ForeignKey("ArticleId")]
        public Article? Article { get; set; }

        /// <summary>Unidad física concreta (NumeroInventario), opcional.</summary>
        public int? EquipmentUnitId { get; set; }
        [ForeignKey("EquipmentUnitId")]
        public EquipmentUnit? EquipmentUnit { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? RequestedQuantity { get; set; }

        [StringLength(1000)]
        public string? Observations { get; set; }

        // Obsoletos: la entrega y la devolución viven en Departures / DepartureReturns.
        public int ExpectedQuantity { get; set; } = 1;
        public int DeliveredQuantity { get; set; } = 0;
        public int ReturnedQuantity { get; set; } = 0;
    }
}
