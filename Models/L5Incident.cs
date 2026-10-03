using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class L5Incident : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int? L3RequestId { get; set; }
        [ForeignKey("L3RequestId")]
        public L3Request? L3Request { get; set; }

        [Required]
        public int EquipmentUnitId { get; set; }
        [ForeignKey("EquipmentUnitId")]
        public EquipmentUnit EquipmentUnit { get; set; } = null!;

        /// <summary>
        /// Nombre completo del responsable (texto libre, según Excel L-5).
        /// Puede ser un estudiante o docente que no tenga usuario en el sistema.
        /// </summary>
        [Required(ErrorMessage = "Debe indicar el nombre del involucrado.")]
        [StringLength(200)]
        public string ResponsibleName { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de responsable: "DOCENTE" o "ESTUDIANTE" (según Excel L-5 Compendio).
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ResponsibleType { get; set; } = "ESTUDIANTE";

        [Required]
        [StringLength(100)]
        public string IncidentType { get; set; } = string.Empty;

        [Required]
        public string DamageDescription { get; set; } = string.Empty;

        public int AffectedQuantity { get; set; } = 1;

        [StringLength(50)]
        public string RepositionStatus { get; set; } = "Pendiente de Reposición";

        public DateTime RecordDate { get; set; } = DateTime.UtcNow;

        // IAuditable implementation
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedById { get; set; }
        public int? ModifiedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
