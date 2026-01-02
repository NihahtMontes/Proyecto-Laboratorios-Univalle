using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a city within a country
    /// </summary>
    public class City
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
        public GeneralStatus Status { get; set; } = GeneralStatus.Active;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("CountryId")]
        public virtual Country? Country { get; set; }

        public virtual ICollection<Equipment>? Equipments { get; set; }
    }
}
