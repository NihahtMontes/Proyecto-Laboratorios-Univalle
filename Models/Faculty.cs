using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents an academic unit of the university
    /// </summary>
    public class Faculty
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la facultad es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Nombre de la Facultad")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Código")]
        public string? Code { get; set; }

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Active;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Relationships
        public virtual ICollection<Laboratory>? Laboratories { get; set; }

        [NotMapped]
        [Display(Name = "Cantidad de Laboratorios")]
        public int LaboratoriesCount => Laboratories?.Count(l => l.Status == GeneralStatus.Active) ?? 0;
    }
}
