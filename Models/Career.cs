using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents an academic career/program.
    /// </summary>
    public class Career : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la carrera es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Nombre de la Carrera")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Facultad")]
        public int? FacultadId { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        // Audit Fields
        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        // Navigation Properties
        [ForeignKey("FacultadId")]
        public virtual Faculty? Facultad { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        public virtual ICollection<EquipmentUnit>? EquipmentUnits { get; set; }
    }
}
