using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a physical space where equipment is stored and used.
    /// </summary>
    public class Laboratory : IAuditable
    {
        // ========================================
        // PRIMARY KEY
        // ========================================
        [Key]
        public int Id { get; set; }

        // ========================================
        // FOREIGN KEYS
        // ========================================
        [Required(ErrorMessage = "La facultad es obligatoria")]
        [Display(Name = "Facultad")]
        public int FacultyId { get; set; }

        // ========================================
        // IDENTIFICATION
        // ========================================
        [Required(ErrorMessage = "Las siglas son obligatorias")]
        [StringLength(20)]
        [Display(Name = "Siglas")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Nombre del Laboratorio")]
        public string Name { get; set; } = string.Empty;

        // ========================================
        // CLASSIFICATION & LOCATION
        // ========================================
        [StringLength(100)]
        [Display(Name = "Tipo de Laboratorio")]
        public string? Type { get; set; }

        [StringLength(100)]
        [Display(Name = "Edificio")]
        public string? Building { get; set; }

        [StringLength(50)]
        [Display(Name = "Piso/Nivel")]
        public string? Floor { get; set; }

        // ========================================
        // ADDITIONAL INFO
        // ========================================
        [StringLength(1000)]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        // ========================================
        // STATE & AUDIT
        // ========================================
        [Required]
        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        // ========================================
        // NAVIGATION PROPERTIES
        // ========================================
        [ForeignKey("FacultyId")]
        public virtual Faculty? Faculty { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        // ========================================
        // INVERSE RELATIONSHIPS
        // ========================================
        public virtual ICollection<Equipment>? Equipments { get; set; }
        public virtual ICollection<MaintenancePlan>? MaintenancePlans { get; set; }

        // ========================================
        // CALCULATED PROPERTIES
        // ========================================

        [NotMapped]
        [Display(Name = "Equipos Operativos")]
        public int OperationalEquipmentsCount =>
            Equipments?.Count(e => e.CurrentStatus == EquipmentStatus.Operational) ?? 0;

        [NotMapped]
        [Display(Name = "Valor Total Inventario")]
        public decimal TotalInventoryValue =>
            Equipments?.Where(e => e.AcquisitionValue.HasValue)
                       .Sum(e => e.AcquisitionValue.Value) ?? 0;
    }
}
