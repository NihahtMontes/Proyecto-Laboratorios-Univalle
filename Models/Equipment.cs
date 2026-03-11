using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents the concept/definition of an Equipment (e.g. "Microscope Model X").
    /// </summary>
    public class Equipment : IAuditable
    {
        // ========================================
        // PRIMARY KEY
        // ========================================
        [Key]
        public int Id { get; set; }

        // ========================================
        // FOREIGN KEYS
        // ========================================
        [Display(Name = "Tipo de Equipamiento")]
        public int? EquipmentTypeId { get; set; }

        // 0 = Equipment, 1 = Utensil
        [Required]
        [Display(Name = "Categoría")]
        public EquipmentCategory Category { get; set; } = EquipmentCategory.Equipment;



        [Display(Name = "Imagen")]
        public string? ImageUrl { get; set; }

        [Display(Name = "País / Sede de Origen")]
        public int? CountryId { get; set; }

        [Display(Name = "Ciudad / Sede de Origen")]
        public int? CityId { get; set; }

        // ========================================
        // ASSET IDENTIFICATION (Shared)
        // ========================================
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Nombre del Equipamiento")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Marca")]
        public string? Brand { get; set; }

        [StringLength(100)]
        [Display(Name = "Modelo")]
        public string? Model { get; set; }

        // ========================================
        // SPECS & LIFECYCLE (Shared)
        // ========================================
        [Display(Name = "Vida Útil Estimada (años)")]
        [Range(0, 100)]
        public int? UsefulLifeYears { get; set; }

        [StringLength(2000)]
        [Display(Name = "Descripción / Especificaciones")]
        public string? Description { get; set; }

        // ========================================
        // AUDIT
        // ========================================
        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        // ========================================
        // NAVIGATION PROPERTIES
        // ========================================
        public virtual EquipmentType? EquipmentType { get; set; }

        public virtual Country? Country { get; set; }

        public virtual City? City { get; set; }

        public virtual User? CreatedBy { get; set; }

        public virtual User? ModifiedBy { get; set; }

        // ========================================
        // INVERSE RELATIONSHIPS
        // ========================================
        public virtual ICollection<EquipmentUnit>? Units { get; set; }
    }
}
