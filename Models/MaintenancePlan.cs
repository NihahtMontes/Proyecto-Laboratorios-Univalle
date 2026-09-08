using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class MaintenancePlan : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        [Display(Name = "Laboratorio (Snapshot)")]
        public string? LaboratorySnapshot { get; set; }

        [StringLength(200)]
        [Display(Name = "Bloque (Snapshot)")]
        public string? BlockSnapshot { get; set; }

        [Display(Name = "Unidad de Equipamiento")]
        public int? EquipmentUnitId { get; set; }

        [Display(Name = "Laboratorio")]
        public int? LaboratoryId { get; set; }

        [StringLength(200)]
        [Display(Name = "Servicio")]
        public string? Service { get; set; }

        [Display(Name = "Tipo de Servicio")]
        public ServiceType? ServiceType { get; set; }

        [StringLength(50)]
        [Display(Name = "Código del plan histórico")]
        public string? PlanCode { get; set; }

        [StringLength(200)]
        public string? HistoricalSourceKey { get; set; }

        [Display(Name = "Gestión")]
        public int? ManagementId { get; set; }

        [Display(Name = "Tipo de mantenimiento")]
        public MaintenanceType? MaintenanceType { get; set; }

        [Display(Name = "Fecha planificada")]
        [DataType(DataType.Date)]
        public DateTime? PlannedDate { get; set; }

        [Display(Name = "Estado histórico")]
        public MaintenanceStatus? Status { get; set; }

        [Display(Name = "Actor responsable")]
        public int? ResponsiblePersonId { get; set; }

        public int? ImportBatchId { get; set; }

        // ========================================
        // AUDIT
        // ========================================
        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        [Display(Name = "Tiempo Estimado (horas)")]
        [Range(0, 1000)]
        [Precision(10, 2)]
        public decimal? EstimatedTime { get; set; }

        [Display(Name = "Tiempo Real (horas)")]
        [Range(0, 1000)]
        [Precision(10, 2)]
        public decimal? ActualTime { get; set; }

        [Display(Name = "Técnico Asignado")]
        public int? AssignedTechnicianId { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("EquipmentUnitId")]
        public virtual EquipmentUnit? EquipmentUnit { get; set; }

        [ForeignKey(nameof(LaboratoryId))]
        public virtual Laboratory? Laboratory { get; set; }

        [ForeignKey(nameof(ManagementId))]
        public virtual Management? Management { get; set; }

        [ForeignKey(nameof(ResponsiblePersonId))]
        public virtual Person? ResponsiblePerson { get; set; }

        [ForeignKey(nameof(ImportBatchId))]
        public virtual ImportBatch? ImportBatch { get; set; }

        [ForeignKey("AssignedTechnicianId")]
        public virtual User? Technician { get; set; }

        [StringLength(200)]
        [Display(Name = "Proveedor / Ejecutor")]
        public string? ProviderSnapshot { get; set; }
    }
}
