using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class Maintenance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Equipamiento")]
        public int EquipmentId { get; set; }

        [Required]
        [Display(Name = "Tipo de Mantenimiento")]
        public int MaintenanceTypeId { get; set; }

        [Display(Name = "Técnico Responsable")]
        public int? TechnicianId { get; set; }

        [Display(Name = "Solicitud Asociada")]
        public int? RequestId { get; set; }

        // ========================================
        // SCHEDULE & EXECUTION
        // ========================================
        [Display(Name = "Fecha Programada")]
        [DataType(DataType.Date)]
        public DateTime? ScheduledDate { get; set; }

        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.DateTime)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Fecha de Finalización")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        [StringLength(2000)]
        [Display(Name = "Descripción del Trabajo Realizado")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;

        // ========================================
        // COSTS
        // ========================================
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Costo Estimado")]
        public decimal? EstimatedCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Costo Real")]
        public decimal? ActualCost { get; set; }

        // ========================================
        // REPORT / POST-SERVICE
        // ========================================
        [StringLength(1000)]
        [Display(Name = "Recomendaciones Post-Servicio")]
        public string? Recommendations { get; set; }

        [Display(Name = "Fecha Sugerida Próximo Mantenimiento")]
        [DataType(DataType.Date)]
        public DateTime? SuggestedNextMaintenanceDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Observaciones del Técnico")]
        public string? Observations { get; set; }

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

        [ForeignKey("MaintenanceTypeId")]
        public virtual MaintenanceType? MaintenanceType { get; set; }

        [ForeignKey("TechnicianId")]
        public virtual User? Technician { get; set; }

        [ForeignKey("RequestId")]
        public virtual Request? Request { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        public virtual List<CostDetail> CostDetails { get; set; } = new List<CostDetail>();

        // ========================================
        // CALCULATED PROPERTIES
        // ========================================

        [NotMapped]
        [Display(Name = "Duración (horas)")]
        public double? DurationHours
        {
            get
            {
                if (StartDate.HasValue && EndDate.HasValue)
                {
                    return (EndDate.Value - StartDate.Value).TotalHours;
                }
                return null;
            }
        }

        [NotMapped]
        [Display(Name = "Completado a Tiempo")]
        public bool? CompletedOnTime
        {
            get
            {
                if (ScheduledDate.HasValue && EndDate.HasValue)
                {
                    return EndDate.Value.Date <= ScheduledDate.Value.Date;
                }
                return null;
            }
        }

        [NotMapped]
        [Display(Name = "Total Calculado")]
        public decimal CalculatedTotal => CostDetails?.Sum(d => d.Subtotal) ?? 0;
    }
}
