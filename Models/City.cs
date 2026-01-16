using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a city within a country
    /// </summary>
    public class City : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El país es obligatorio")]
        [Display(Name = "País")]
        public int CountryId { get; set; }

        [Required(ErrorMessage = "El nombre de la ciudad es obligatorio")]
        [StringLength(100)]
        [Display(Name = "Ciudad")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Región/Departamento")]
        public string? Region { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("CountryId")]
        public virtual Country? Country { get; set; }

        public virtual ICollection<Equipment>? Equipments { get; set; }

        // ========================================
        // AUDIT
        // ========================================
        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

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
