using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Historical record of equipment state changes
    /// </summary>
    public class EquipmentStateHistory : IAuditable
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

        // ========================================
        // AUDIT (IAuditable Implementation)
        // ========================================
        [Display(Name = "Registrado Por (Creado Por)")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Registro")]
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

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

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
