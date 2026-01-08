using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents an academic unit of the university
    /// </summary>
    public class Faculty : IAuditable
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
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Relationships
        public virtual ICollection<Laboratory>? Laboratories { get; set; }

        [NotMapped]
        [Display(Name = "Cantidad de Laboratorios")]
        public int LaboratoriesCount => Laboratories?.Count(l => l.Status == GeneralStatus.Activo) ?? 0;

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
