using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class EquipmentType
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
    }
}
