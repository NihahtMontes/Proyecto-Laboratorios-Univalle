using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a Maintenance Management Period (e.g., "Gestión L-48").
    /// It is the root entity that groups all maintenance activities for a given period.
    /// Once CLOSED, no new records can be added and existing ones cannot be modified.
    /// </summary>
    public class Management : IAuditable
    {
        // ========================================
        // PRIMARY KEY
        // ========================================
        [Key]
        public int Id { get; set; }

        // ========================================
        // IDENTIFICATION
        // ========================================
        [Required(ErrorMessage = "El código de gestión es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Código de Gestión")]
        public string Code { get; set; } = string.Empty; // e.g., "L-48"

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Nombre / Descripción")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Descripción Detallada")]
        public string? Description { get; set; }

        // ========================================
        // PERIOD
        // ========================================
        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Fecha de Cierre Planificada")]
        [DataType(DataType.Date)]
        public DateTime? PlannedEndDate { get; set; }

        [Display(Name = "Fecha de Cierre Real")]
        [DataType(DataType.Date)]
        public DateTime? ActualClosedDate { get; set; }

        // ========================================
        // STATUS & RESPONSIBILITY
        // ========================================
        [Required]
        [Display(Name = "Estado")]
        public ManagementStatus Status { get; set; } = ManagementStatus.Active;

        [StringLength(200)]
        [Display(Name = "Responsable de la Gestión")]
        public string? Responsible { get; set; }

        // ========================================
        // AUDIT
        // ========================================
        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        // ========================================
        // NAVIGATION PROPERTIES
        // ========================================
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        // Inverse: All plan entries for this Management period
        public virtual ICollection<ManagementPlan> ManagementPlans { get; set; } = new List<ManagementPlan>();

        // ========================================
        // CALCULATED PROPERTIES
        // ========================================
        [NotMapped]
        public bool IsClosed => Status == ManagementStatus.Closed;

        [NotMapped]
        public int TotalEquipments => ManagementPlans?.Count ?? 0;

        [NotMapped]
        public int CompletedMaintenances => ManagementPlans?.Count(p => p.PlanStatus == ManagementPlanStatus.Completed) ?? 0;

        [NotMapped]
        public int CompletionPercentage => TotalEquipments > 0
            ? (int)Math.Round((double)CompletedMaintenances / TotalEquipments * 100)
            : 0;
    }
}
