using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a formal request for maintenance or service
    /// </summary>
    public class Request : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El equipamiento es obligatorio")]
        [Display(Name = "Equipamiento")]
        public int EquipmentId { get; set; }

        [Display(Name = "Solicitado Por")]
        public int? RequestedById { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(1000)]
        [Display(Name = "Descripción del Problema")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Prioridad")]
        public RequestPriority Priority { get; set; } = RequestPriority.Medium;

        [StringLength(500)]
        [Display(Name = "Observaciones del Solicitante")]
        public string? Observations { get; set; }

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
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        // ========================================
        // NAVIGATION
        // ========================================
        [ForeignKey("EquipmentId")]
        public virtual Equipment? Equipment { get; set; }

        [ForeignKey("RequestedById")]
        public virtual User? RequestedBy { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        // RELATIONSHIPS
        public virtual Maintenance? Maintenance { get; set; }
    }
}
