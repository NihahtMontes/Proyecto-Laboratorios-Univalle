using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class EquipmentNote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [Required(ErrorMessage = "La nota no puede estar vacía")]
        [StringLength(500, ErrorMessage = "La nota no puede exceder los 500 caracteres")]
        [Display(Name = "Nota")]
        public string Note { get; set; } = string.Empty;

        [ForeignKey("EquipmentId")]
        public virtual Equipment? Equipment { get; set; }
    }
}
