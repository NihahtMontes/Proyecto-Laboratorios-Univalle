using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class MaintenancePlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Laboratorio (Snapshot)")]
        public string LaboratorySnapshot { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Bloque (Snapshot)")]
        public string BlockSnapshot { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Equipamiento")]
        public int EquipmentId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Servicio")]
        public string Service { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipo de Servicio")]
        public ServiceType ServiceType { get; set; } = ServiceType.Internal;

        [Display(Name = "Tiempo Estimado (horas)")]
        [Range(0, 1000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? EstimatedTime { get; set; }

        [Display(Name = "Tiempo Real (horas)")]
        [Range(0, 1000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? ActualTime { get; set; }

        [Display(Name = "Técnico Asignado")]
        public int? AssignedTechnicianId { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("EquipmentId")]
        public virtual Equipment? Equipment { get; set; }

        [ForeignKey("AssignedTechnicianId")]
        public virtual User? Technician { get; set; }
    }
}
