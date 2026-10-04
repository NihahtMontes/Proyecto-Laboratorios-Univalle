using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Devolución (parcial o total) de una salida L-3 (= DevolucionesL3).</summary>
    public class DepartureReturn : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public int DepartureId { get; set; }
        [ForeignKey(nameof(DepartureId))]
        public virtual Departure? Departure { get; set; }

        [Column(TypeName = "date")]
        public DateTime? ReturnDate { get; set; }

        public int ResponsiblePersonId { get; set; }
        [ForeignKey(nameof(ResponsiblePersonId))]
        public virtual Person? ResponsiblePerson { get; set; }

        public DepartureReturnStatus Status { get; set; } = DepartureReturnStatus.Partial;

        [StringLength(1000)]
        public string? Observations { get; set; }

        public virtual ICollection<DepartureReturnItem> Items { get; set; } = [];

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    /// <summary>Línea devuelta (= DevolucionL3Detalles).</summary>
    public class DepartureReturnItem
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public int DepartureReturnId { get; set; }
        [ForeignKey(nameof(DepartureReturnId))]
        public virtual DepartureReturn? DepartureReturn { get; set; }

        public int DepartureItemId { get; set; }
        [ForeignKey(nameof(DepartureItemId))]
        public virtual DepartureItem? DepartureItem { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal ReturnedQuantity { get; set; }

        [StringLength(500)]
        public string? Observations { get; set; }
    }
}
