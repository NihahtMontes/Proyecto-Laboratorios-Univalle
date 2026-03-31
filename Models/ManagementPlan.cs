using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a single entry in the L-48 Maintenance Plan table.
    /// Links a Management period to a specific Maintenance record and Equipment Unit.
    /// If the parent Management is CLOSED, this record is read-only.
    /// </summary>
    public class ManagementPlan : IAuditable
    {
        // ========================================
        // PRIMARY KEY
        // ========================================
        [Key]
        public int Id { get; set; }

        // ========================================
        // FOREIGN KEYS (core relationship)
        // ========================================
        [Required(ErrorMessage = "La gestión es obligatoria")]
        [Display(Name = "Gestión")]
        public int ManagementId { get; set; }

        [Display(Name = "Mantenimiento Asociado (L-8)")]
        public int? MaintenanceId { get; set; }

        [Display(Name = "Unidad de Equipo")]
        public int? EquipmentUnitId { get; set; }

        // Nuevos Enlaces del Wizard
        [Display(Name = "Verificación (L-6)")]
        public int? VerificationId { get; set; }

        [Display(Name = "Solicitud Técnica (L-7)")]
        public int? RequestId { get; set; }

        [Display(Name = "Salida de Equipo (L-3)")]
        public int? DepartureId { get; set; }

        [Display(Name = "Solicitud Adquisición (Desembolso)")]
        public int? AcquisitionRequestId { get; set; }

        [Required]
        [Display(Name = "Fase Actual del Wizard")]
        public WizardPhase CurrentPhase { get; set; } = WizardPhase.Verification;

        [Required]
        [Display(Name = "Estado Actual del Equipo")]
        public WizardEquipmentState CurrentState { get; set; } = WizardEquipmentState.PendingVerification;

        // ========================================
        // PLAN DETAILS
        // ========================================
        [StringLength(200)]
        [Display(Name = "Responsable del Mantenimiento")]
        public string? Responsible { get; set; }

        [StringLength(500)]
        [Display(Name = "Referencia Documental / Adjunto")]
        public string? DocumentReference { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notas / Observaciones")]
        public string? Notes { get; set; }

        [Display(Name = "Fecha Planificada")]
        [DataType(DataType.Date)]
        public DateTime? PlannedDate { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public ManagementPlanStatus PlanStatus { get; set; } = ManagementPlanStatus.Pending;

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
        [ForeignKey("ManagementId")]
        public virtual Management? Management { get; set; }

        [ForeignKey("MaintenanceId")]
        public virtual Maintenance? Maintenance { get; set; }

        [ForeignKey("EquipmentUnitId")]
        public virtual EquipmentUnit? EquipmentUnit { get; set; }

        [ForeignKey("VerificationId")]
        public virtual Verification? Verification { get; set; }

        [ForeignKey("RequestId")]
        public virtual Request? TechnicalRequest { get; set; }

        [ForeignKey("DepartureId")]
        public virtual Departure? Departure { get; set; }

        [ForeignKey("AcquisitionRequestId")]
        public virtual Request? AcquisitionRequest { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        // ========================================
        // CALCULATED PROPERTIES
        // ========================================
        [NotMapped]
        public bool IsReadOnly => Management?.IsClosed ?? false;
    }
}
