using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class MaintenanceTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaintenanceId { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Descripción de la Tarea")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Completada")]
        public bool IsCompleted { get; set; } = false;

        [Display(Name = "Eliminada")]
        public bool IsDeleted { get; set; } = false;

        [ForeignKey("MaintenanceId")]
        public virtual Maintenance? Maintenance { get; set; }
    }
}
