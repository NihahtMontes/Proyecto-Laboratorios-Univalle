using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Historical record of equipment state changes
    /// </summary>
    public class EquipmentStateHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Equipamiento")]
        public int EquipmentId { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public EquipmentStatus Status { get; set; }

        [Required]
        [Display(Name = "Fecha de Inicio")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Fin")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Motivo del Cambio")]
        public string? Reason { get; set; }

        [Display(Name = "Registrado Por")]
        public int? RegisteredById { get; set; }

        [Display(Name = "Fecha de Registro")]
        public DateTime RegisteredDate { get; set; } = DateTime.Now;

        // ========================================
        // NAVIGATION
        // ========================================
        [ForeignKey("EquipmentId")]
        public virtual Equipment? Equipment { get; set; }

        [ForeignKey("RegisteredById")]
        public virtual User? RegisteredBy { get; set; }

        // ========================================
        // CALCULATED PROPERTY
        // ========================================

        [NotMapped]
        [Display(Name = "Duración (días)")]
        public int? DurationDays
        {
            get
            {
                var finalDate = EndDate ?? DateTime.Now;
                return (int)(finalDate - StartDate).TotalDays;
            }
        }
    }
}
