using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Technical verification checklist for laboratory equipment.
    /// Los puntos de control se gestiona dinámicamente via VerificationCheckItem + VerificationCheckResult.
    /// </summary>
    public class Verification : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Unidad de Equipamiento")]
        public int EquipmentUnitId { get; set; }

        [Required]
        [Display(Name = "Gestión")]
        public int ManagementId { get; set; }

        // ========================================
        // METADATA
        // ========================================

        [Required]
        [Display(Name = "Fecha de Verificación")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Display(Name = "Observaciones (Fallas o problemas del equipo)")]
        public string? Observations { get; set; }

        [Required]
        [Display(Name = "Condición Física Detectada")]
        public PhysicalCondition PhysicalCondition { get; set; } = PhysicalCondition.Excellent;

        [Required]
        [Display(Name = "Estado de la Verificación")]
        public VerificationStatus Status { get; set; } = VerificationStatus.Draft;

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
        // NAVIGATION
        // ========================================
        [ForeignKey("EquipmentUnitId")]
        public virtual EquipmentUnit? EquipmentUnit { get; set; }

        [ForeignKey("ManagementId")]
        public virtual Management? Management { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        /// <summary>Resultados individuales de cada punto de control.</summary>
        public virtual ICollection<VerificationCheckResult> CheckResults { get; set; } = [];

        // ========================================
        // CALCULATED PROPERTIES
        // ========================================

        [NotMapped]
        [Display(Name = "Porcentaje Completado")]
        public int CompletionPercentage
        {
            get
            {
                if (CheckResults == null || CheckResults.Count == 0) return 0;
                int total = CheckResults.Count;
                int completed = CheckResults.Count(r => r.Result != VerificationResult.NotChecked);
                return (int)((completed / (double)total) * 100);
            }
        }

        [NotMapped]
        public bool HasFailures => !string.IsNullOrWhiteSpace(Observations);

        [NotMapped]
        public int FailuresCount => HasFailures ? 1 : 0;
    }
}
