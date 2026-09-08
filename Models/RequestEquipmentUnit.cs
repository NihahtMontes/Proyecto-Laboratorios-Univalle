using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Relación normalizada entre una solicitud y cada unidad física afectada.
    /// EquipmentUnitId legacy se conserva durante la transición.
    /// </summary>
    public class RequestEquipmentUnit : IAuditable
    {
        [Key]
        public long Id { get; set; }

        public int RequestId { get; set; }
        public int EquipmentUnitId { get; set; }
        public bool IsLegacyPrimary { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedDate { get; set; }

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        [ForeignKey(nameof(RequestId))]
        public virtual Request? Request { get; set; }

        [ForeignKey(nameof(EquipmentUnitId))]
        public virtual EquipmentUnit? EquipmentUnit { get; set; }
    }
}
