using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        [Display(Name = "Gestión de Registro Técnico")]
        public int? ManagementId { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        // ========================================
        // LOCATION & ORIGIN (Verificación por Laboratorio)
        // ========================================
        [Display(Name = "Laboratorio Asignado")]
        public int? LaboratoryId { get; set; } 

        [Required]
        [Display(Name = "Estado de confirmación de ubicación")]
        public LocationResolutionStatus LocationResolutionStatus { get; set; } = LocationResolutionStatus.Confirmed;

        public int? ImportBatchId { get; set; }


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
        [Precision(18, 2)]
        [Range(0d, 999999999d, ErrorMessage = "El valor debe ser positivo")]
        public decimal? AcquisitionValue { get; set; }

        // ========================================
        // STATUS & OBSERVATIONS
        // ========================================
        [Display(Name = "Estado Actual")]
        public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Operational;

        [Display(Name = "Condición Física")]
        public PhysicalCondition? PhysicalCondition { get; set; } = Enums.PhysicalCondition.Excellent;

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

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

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

        [ForeignKey("ManagementId")]
        public virtual Management? Management { get; set; }

        [ForeignKey(nameof(ImportBatchId))]
        public virtual ImportBatch? ImportBatch { get; set; }

        // INVERSE RELATIONSHIPS
        public virtual ICollection<EquipmentStateHistory>? StateHistory { get; set; }
        public virtual ICollection<Maintenance>? Maintenances { get; set; }
        public virtual ICollection<Verification>? Verifications { get; set; }
        public virtual ICollection<MaintenancePlan>? MaintenancePlans { get; set; }
        public virtual ICollection<Departure>? Departures { get; set; }
        public virtual ICollection<RequestEquipmentUnit> RequestLinks { get; set; } = [];

        // Calculated Properties

        /// <summary>
        /// Inicio del ciclo de vida útil: la fecha de adquisición (puesta en servicio en la institución);
        /// si falta, la de fabricación. Null si la unidad no tiene ninguna de las dos.
        /// </summary>
        [NotMapped]
        public DateTime? ServiceStartDate => AcquisitionDate ?? ManufacturingDate;

        /// <summary>Años completos desde el inicio del ciclo de vida (<see cref="ServiceStartDate"/>); null sin fecha.</summary>
        [NotMapped]
        public int? YearsInOperation
        {
            get
            {
                if (ServiceStartDate is not DateTime start) return null;
                var today = DateTime.Today;
                var years = today.Year - start.Year;
                if (today < start.Date.AddYears(years)) years--;
                return Math.Max(years, 0);
            }
        }

        /// <summary>
        /// Vida útil restante en años (puede ser negativa si se excedió). Null si el equipo no define vida útil
        /// o la unidad no tiene fecha de adquisición ni de fabricación (requiere Equipment cargado).
        /// </summary>
        [NotMapped]
        public int? RemainingUsefulLifeYears =>
            Equipment?.UsefulLifeYears is int life && YearsInOperation is int used ? life - used : null;
    }
}
