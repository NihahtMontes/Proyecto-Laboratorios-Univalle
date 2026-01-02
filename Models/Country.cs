using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a country of origin for equipment or location
    /// </summary>
    public class Country
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del país es obligatorio")]
        [StringLength(100)]
        [Display(Name = "País")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Active;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Relationships
        public virtual ICollection<City>? Cities { get; set; }
        public virtual ICollection<Equipment>? Equipments { get; set; }
    }
}
