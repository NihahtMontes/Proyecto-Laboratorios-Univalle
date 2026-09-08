using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class DepartureItem : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Salida (L-3)")]
        public int DepartureId { get; set; }

        [Display(Name = "Unidad Física")]
        public int? EquipmentUnitId { get; set; }

        [Display(Name = "Artículo / Consumible")]
        public int? ArticleId { get; set; }

        [StringLength(200)]
        [Display(Name = "Producto")]
        public string? ProductName { get; set; }

        [Display(Name = "Cantidad")]
        [Range(1, 9999, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int? Quantity { get; set; }

        [StringLength(50)]
        [Display(Name = "Unidad de Medida")]
        public string? UnitOfMeasure { get; set; } = "UNIDAD";

        [Display(Name = "Cantidad Devuelta")]
        public int? ReturnedQuantity { get; set; }

        [StringLength(500)]
        [Display(Name = "Observaciones")]
        public string? Observations { get; set; }

        [Display(Name = "Eliminado")]
        public bool IsRemoved { get; set; } = false;

        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        [ForeignKey("DepartureId")]
        public virtual Departure? Departure { get; set; }

        [ForeignKey("EquipmentUnitId")]
        public virtual EquipmentUnit? EquipmentUnit { get; set; }

        [ForeignKey(nameof(ArticleId))]
        public virtual Article? Article { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        [NotMapped]
        [Display(Name = "Saldo")]
        public int? Balance => Quantity.HasValue ? Quantity.Value - (ReturnedQuantity ?? 0) : null;
    }
}
