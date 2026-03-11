using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a specific physical unit of an Equipment.
    /// </summary>
    public class EquipmentUnit : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        // ========================================
        // LOCATION & ORIGIN (Moved from Definition)
        // ========================================
        [Display(Name = "Laboratorio Asignado")]
        public int? LaboratoryId { get; set; }

        // ========================================
        // ASSET IDENTIFICATION (Unit Specific)
        // ========================================
        [Required(ErrorMessage = "El número de inventario es obligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El inventario debe tener al menos 3 caracteres")]
        [Display(Name = "N° Inventario")]
        public string InventoryNumber { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Número de Serie")]
        public string? SerialNumber { get; set; }

        [Display(Name = "Carrera / Programa")]
        public int? CareerId { get; set; }

        [StringLength(200)]
        [Display(Name = "Ubicación Interna")]
        public string? InternalLocation { get; set; }

        // ========================================
        // FINANCIAL & LIFECYCLE (Unit Specific)
        // ========================================
        [Display(Name = "Fecha de Adquisición")]
        [DataType(DataType.Date)]
        public DateTime? AcquisitionDate { get; set; }

        [Display(Name = "Fecha de Fabricación")]
        [DataType(DataType.Date)]
        public DateTime? ManufacturingDate { get; set; }

        // Each unit might have a slightly different cost or depreciation status
        [Display(Name = "Valor de Adquisición")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999, ErrorMessage = "El valor debe ser positivo")]
        public decimal? AcquisitionValue { get; set; }

        // ========================================
        // STATUS & OBSERVATIONS
        // ========================================
        [Display(Name = "Estado Actual")]
        public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Operational;

        [Display(Name = "Condición Física")]
        public PhysicalCondition? PhysicalCondition { get; set; } = Enums.PhysicalCondition.New;

        [StringLength(2000)]
        [Display(Name = "Observaciones (Unidad)")]
        public string? Notes { get; set; }

        // ========================================
        // AUDIT
        // ========================================
        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        // ========================================
        // NAVIGATION PROPERTIES
        // ========================================
        [ForeignKey("EquipmentId")]
        public virtual Equipment? Equipment { get; set; }

        [ForeignKey("LaboratoryId")]
        public virtual Laboratory? Laboratory { get; set; }

        [ForeignKey("CareerId")]
        public virtual Career? Career { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }
        
        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        // INVERSE RELATIONSHIPS
        public virtual ICollection<EquipmentStateHistory>? StateHistory { get; set; }
        public virtual ICollection<Maintenance>? Maintenances { get; set; }
        public virtual ICollection<Verification>? Verifications { get; set; }
        public virtual ICollection<MaintenancePlan>? MaintenancePlans { get; set; }
        public virtual ICollection<Loan>? Loans { get; set; }

        // Calculated Properties
        [NotMapped]
        public int? YearsInOperation 
        {
            get
            {
                if (!ManufacturingDate.HasValue) return null;
                var years = DateTime.UtcNow.Year - ManufacturingDate.Value.Year;
                if (DateTime.UtcNow < ManufacturingDate.Value.AddYears(years)) years--;
                return years;
            }
        }
    }
}
