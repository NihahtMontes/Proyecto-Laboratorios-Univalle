using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a physical asset in the laboratory with lifecycle tracking
    /// </summary>
    public class Equipment : IAuditable
    {
        // ========================================
        // PRIMARY KEY
        // ========================================
        [Key]
        public int Id { get; set; }

        // ========================================
        // FOREIGN KEYS
        // ========================================
        [Required(ErrorMessage = "El tipo de equipamiento es obligatorio")]
        [Display(Name = "Tipo de Equipamiento")]
        public int EquipmentTypeId { get; set; }

        [Required(ErrorMessage = "El laboratorio es obligatorio")]
        [Display(Name = "Laboratorio")]
        public int LaboratoryId { get; set; }

        // Assuming City/Country models will be renamed to City/Country later. 
        // For now, I'll keep the Property Names English but types referencing existing files if not yet renamed.
        // Wait, I should stick to English types if I plan to rename them soon. 
        // But Pais/Ciudad classes still exist as Spanish. 
        // I will use 'Pais' and 'Ciudad' types for now, rename later in Phase 1.1 continued.

        [Required(ErrorMessage = "La ciudad de procedencia es obligatoria")]
        [Display(Name = "Ciudad de Procedencia")]
        public int CityId { get; set; }

        [Required(ErrorMessage = "El país de procedencia es obligatorio")]
        [Display(Name = "País de Procedencia")]
        public int CountryId { get; set; }

        // ========================================
        // ASSET IDENTIFICATION
        // ========================================
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Nombre del Equipamiento")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El número de inventario es obligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El inventario debe tener al menos 3 caracteres")]
        [Display(Name = "N° Inventario")]
        public string InventoryNumber { get; set; } = string.Empty;

        // ========================================
        // TECHNICAL SPECS
        // ========================================
        [StringLength(100)]
        [Display(Name = "Marca")]
        public string? Brand { get; set; }

        [StringLength(100)]
        [Display(Name = "Modelo")]
        public string? Model { get; set; }

        [StringLength(100)]
        [Display(Name = "Número de Serie")]
        public string? SerialNumber { get; set; }

        // ========================================
        // FINANCIAL & LIFECYCLE
        // ========================================
        [Display(Name = "Vida Útil (años)")]
        [Range(0, 100, ErrorMessage = "La vida útil debe estar entre 0 y 100 años")]
        public int? UsefulLifeYears { get; set; }

        [Display(Name = "Fecha de Adquisición")]
        [DataType(DataType.Date)]
        public DateTime? AcquisitionDate { get; set; }

        [Display(Name = "Valor de Adquisición")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999, ErrorMessage = "El valor debe ser positivo")]
        public decimal? AcquisitionValue { get; set; }

        // ========================================
        // STATUS & OBSERVATIONS
        // ========================================
        [Display(Name = "Estado Actual")]
        public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Operational;

        [StringLength(2000)]
        [Display(Name = "Observaciones")]
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
        // NAVIGATION PROPERTIES
        // ========================================
        [ForeignKey("EquipmentTypeId")]
        public virtual EquipmentType? EquipmentType { get; set; }

        [ForeignKey("LaboratoryId")]
        public virtual Laboratory? Laboratory { get; set; }

        [ForeignKey("CountryId")]
        public virtual Country? Country { get; set; }

        [ForeignKey("CityId")]
        public virtual City? City { get; set; }

        // AUDIT NAVIGATION
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        // ========================================
        // INVERSE RELATIONSHIPS
        // ========================================
        public virtual ICollection<EquipmentStateHistory>? StateHistory { get; set; }
        public virtual ICollection<Maintenance>? Maintenances { get; set; }
        public virtual ICollection<MaintenancePlan>? MaintenancePlans { get; set; }
        public virtual ICollection<Verification>? Verifications { get; set; }

        // ========================================
        // CALCULATED PROPERTIES
        // ========================================

        [NotMapped]
        [Display(Name = "Edad (años)")]
        public int? AgeYears
        {
            get
            {
                if (!AcquisitionDate.HasValue) return null;
                var age = DateTime.Now.Year - AcquisitionDate.Value.Year;
                if (DateTime.Now < AcquisitionDate.Value.AddYears(age)) age--;
                return age;
            }
        }

        [NotMapped]
        [Display(Name = "Requiere Reemplazo")]
        public bool RequiresReplacement
        {
            get
            {
                if (!UsefulLifeYears.HasValue || !AgeYears.HasValue) return false;
                return AgeYears >= (UsefulLifeYears * 0.8);
            }
        }
    }
}
