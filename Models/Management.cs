using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Representa un Periodo de Gestión de Mantenimiento (ej. "Gestión L-48").
    /// Es la entidad raíz que agrupa todas las actividades de mantenimiento para un periodo dado.
    /// Solo puede existir UNA gestión ACTIVA a la vez.
    /// </summary>
    public class Management : IAuditable
    {
        [Key]
        public int Id { get; set; }

        // ========================================
        // IDENTIFICACIÓN (Clave de Negocio AAAA-S)
        // ========================================
        [Required(ErrorMessage = "El año es obligatorio")]
        [Range(2000, 2100, ErrorMessage = "Año fuera de rango permitido")]
        [Display(Name = "Año")]
        public int Year { get; set; }

        [Required(ErrorMessage = "El semestre es obligatorio")]
        [Range(0, 2, ErrorMessage = "El semestre debe ser 0, 1 o 2")]
        [Display(Name = "Semestre")]
        public int Semester { get; set; }

        [Required(ErrorMessage = "El código de gestión es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Código de Gestión")]
        public string Code { get; set; } = string.Empty; // e.g., "2026-1"

        [StringLength(1000)]
        [Display(Name = "Descripción Detallada")]
        public string? Description { get; set; }

        // ========================================
        // PERIODO
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
        // ESTADO Y RESPONSABILIDAD
        // ========================================
        [Required]
        [Display(Name = "Estado")]
        public ManagementStatus Status { get; set; } = ManagementStatus.Active;

        [StringLength(200)]
        [Display(Name = "Responsable")]
        public string? Responsible { get; set; }

        [Required]
        [Display(Name = "Tipo de Gestión")]
        public ManagementType Type { get; set; } = ManagementType.Preventive;

        [Display(Name = "Facultad Asociada")]
        public int? FacultyId { get; set; }

        // ========================================
        // AUDITORÍA (IAuditable)
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
        // NAVEGACIÓN
        // ========================================
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        [ForeignKey("FacultyId")]
        public virtual Faculty? Faculty { get; set; }

        // Todas las entradas del plan para este periodo
        public virtual ICollection<ManagementPlan> ManagementPlans { get; set; } = new List<ManagementPlan>();

        // ========================================
        // PROPIEDADES CALCULADAS
        // ========================================
        [NotMapped]
        public bool IsClosed => Status == ManagementStatus.Completed;

        [NotMapped]
        public int TotalEquipments => ManagementPlans?.Count(p => p.EquipmentUnitId.HasValue) ?? 0;

        [NotMapped]
        public int CompletedMaintenances => ManagementPlans?.Count(p =>
            p.EquipmentUnitId.HasValue && p.PlanStatus == ManagementPlanStatus.Completed) ?? 0;

        [NotMapped]
        public int CompletionPercentage => TotalEquipments > 0
            ? (int)Math.Round((double)CompletedMaintenances / TotalEquipments * 100)
            : 0;
    }
}
