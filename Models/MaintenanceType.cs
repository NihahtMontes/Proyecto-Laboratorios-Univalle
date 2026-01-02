using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class MaintenanceType
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del tipo de mantenimiento es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Tipo de Mantenimiento")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        public virtual ICollection<Maintenance>? Maintenances { get; set; }
    }
}
