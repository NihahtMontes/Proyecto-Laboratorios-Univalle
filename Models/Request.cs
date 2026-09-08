using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a formal request for maintenance or service
    /// </summary>
    public class Request : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Laboratorio")]
        public int? LaboratoryId { get; set; }

        [Required]
        [Display(Name = "Estado de confirmación de ubicación")]
        public LocationResolutionStatus LocationResolutionStatus { get; set; } = LocationResolutionStatus.Confirmed;

        public int? ImportBatchId { get; set; }

        // For purchasing, this defines the Type of equipment we want.
        // For technical support, this defines the Type of the broken unit (redundant with EquipmentUnit but good for filtering).
        [Display(Name = "Modelo/Tipo de Equipamiento")]
        public int? EquipmentId { get; set; }

        [Display(Name = "Unidad Específica (Inventario)")]
        public int? EquipmentUnitId { get; set; }

        [Required]
        [Display(Name = "Gestión")]
        public int ManagementId { get; set; }

        [Display(Name = "Solicitado Por")]
        public int? RequestedById { get; set; }

        [Display(Name = "Actor Solicitante Histórico")]
        public int? RequestedByPersonId { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(1000)]
        [Display(Name = "Descripción del Problema")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Prioridad")]
        public RequestPriority Priority { get; set; } = RequestPriority.Medium;

        [StringLength(500)]
        [Display(Name = "Observaciones del Solicitante")]
        public string? Observations { get; set; }

        [StringLength(2000)]
        [Display(Name = "Sugerencia de solución")]
        public string? Suggestion { get; set; }

        [Display(Name = "Fecha de solicitud")]
        [DataType(DataType.Date)]
        public DateTime? RequestDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Tiempo Estimado de Reparación")]
        public string? EstimatedRepairTime { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [Display(Name = "Aprobado Por")]
        public int? ApprovedById { get; set; }

        [Display(Name = "Fecha de Aprobación")]
        [DataType(DataType.DateTime)]
        public DateTime? ApprovalDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Motivo de Rechazo")]
        public string? RejectionReason { get; set; }

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

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        // ========================================
        // NAVIGATION
        // ========================================
        [Display(Name = "Tipo de Solicitud")]
        public RequestType Type { get; set; } = RequestType.Technical;

        [StringLength(50)]
        [Display(Name = "Nro. Código de Inversión")]
        public string? InvestmentCode { get; set; }

        [StringLength(100)]
        [Display(Name = "Centro de Costos")]
        public string? CostCenter { get; set; }

        [StringLength(200)]
        public string? HistoricalSourceKey { get; set; }

        // ========================================
        // NAVIGATION
        // ========================================
        [ForeignKey("LaboratoryId")]
        public virtual Laboratory? Laboratory { get; set; }

        [ForeignKey("EquipmentId")]
        public virtual Equipment? Equipment { get; set; }

        [ForeignKey("EquipmentUnitId")]
        public virtual EquipmentUnit? EquipmentUnit { get; set; }

        [ForeignKey("RequestedById")]
        public virtual User? RequestedBy { get; set; }

        [ForeignKey(nameof(RequestedByPersonId))]
        public virtual Person? RequestedByPerson { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        [ForeignKey("ManagementId")]
        public virtual Management? Management { get; set; }

        [ForeignKey(nameof(ImportBatchId))]
        public virtual ImportBatch? ImportBatch { get; set; }

        // RELATIONSHIPS
        public virtual Maintenance? Maintenance { get; set; }
        public virtual ICollection<CostDetail> CostDetails { get; set; } = new List<CostDetail>();
        public virtual ICollection<RequestEquipmentUnit> EquipmentUnitLinks { get; set; } = [];
        public virtual ICollection<MaintenanceRequest> MaintenanceLinks { get; set; } = [];
    }
}
