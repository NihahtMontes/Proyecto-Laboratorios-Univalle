using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Requerimiento (línea de material) de una planificación (antes L3RequestDetail) (= SolicitudL3Detalles): artículo pedido y, opcionalmente, la unidad física.</summary>
    public class PlanningRequirement
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string? Code { get; set; }

        [Required]
        public int PlanningId { get; set; }
        [ForeignKey("PlanningId")]
        public Planning Planning { get; set; } = null!;

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
