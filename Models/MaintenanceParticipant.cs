using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Participación de una persona en un mantenimiento. IsPrimary identifica al
    /// único técnico responsable; TechnicianId se conserva temporalmente como
    /// compatibilidad de lectura/escritura del flujo existente.
    /// </summary>
    public class MaintenanceParticipant
    {
        [Key]
        public long Id { get; set; }
        public int MaintenanceId { get; set; }
        public int PersonId { get; set; }
        public MaintenanceParticipantRole Role { get; set; } = MaintenanceParticipantRole.Technician;
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UnassignedAt { get; set; }

        [ForeignKey(nameof(MaintenanceId))]
        public virtual Maintenance? Maintenance { get; set; }

        [ForeignKey(nameof(PersonId))]
        public virtual Person? Person { get; set; }
    }
}
