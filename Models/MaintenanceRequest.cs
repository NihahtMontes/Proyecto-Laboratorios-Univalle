using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Relación N:M normalizada entre mantenimientos y solicitudes.
    /// Maintenance.RequestId legacy se conserva durante la transición.
    /// </summary>
    public class MaintenanceRequest : IAuditable
    {
        [Key]
        public long Id { get; set; }

        public int MaintenanceId { get; set; }
        public int RequestId { get; set; }
        public bool IsLegacyPrimary { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedDate { get; set; }

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        [ForeignKey(nameof(MaintenanceId))]
        public virtual Maintenance? Maintenance { get; set; }

        [ForeignKey(nameof(RequestId))]
        public virtual Request? Request { get; set; }
    }
}
