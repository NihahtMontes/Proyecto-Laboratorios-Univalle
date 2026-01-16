using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class EquipmentType : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The type name is required")]
        [StringLength(100)]
        [Display(Name = "Tipo de Equipamiento")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [Display(Name = "Requiere Calibración Periódica")]
        public bool RequiresCalibration { get; set; } = false;

        [Display(Name = "Frecuencia de Mantenimiento (meses)")]
        [Range(1, 120, ErrorMessage = "La frecuencia debe estar entre 1 y 120 meses")]
        public int? MaintenanceFrequencyMonths { get; set; }

        // Relationships
        public virtual ICollection<Equipment>? Equipments { get; set; }

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

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }
    }
}
