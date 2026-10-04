using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>Sede (campus) de la universidad: agrupa laboratorios por ciudad (LPZ, CBBA, SCZ).</summary>
    public class Campus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Código")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Display(Name = "Sede")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Ciudad")]
        public int? CityId { get; set; }
        [ForeignKey("CityId")]
        public virtual City? City { get; set; }

        [StringLength(300)]
        [Display(Name = "Dirección")]
        public string? Address { get; set; }

        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Laboratory> Laboratories { get; set; } = [];
    }
}
