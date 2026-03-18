using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class Equipment : IAuditable
    {
        [Key]
        public int Id { get; set; }

        // ========================================
        // ENUMS SEGÚN FORMULARIOS (L-6)
        // ========================================

        // Reemplazamos el ID de la tabla externa por el Enum de Categoría
        [Required]
        [Display(Name = "Categoría (L-6)")]
        public EquipmentCategory Category { get; set; } = EquipmentCategory.Equipment;

        // Añadimos el Enum de Tipo de Utensilio (Material)
        [Display(Name = "Tipo de Material / Utensilio")]
        public UtensilType UtensilType { get; set; } = UtensilType.NoAplica;

        [Display(Name = "Clasificación de Tipo")]
        public EquipmentTypeClassification TypeClassification { get; set; } = EquipmentTypeClassification.Otro;

        // ========================================
        // IDENTIFICACIÓN Y ORIGEN
        // ========================================

        [Display(Name = "Imagen")]
        public string? ImageUrl { get; set; }

        [Display(Name = "País / Sede de Origen")]
        public int? CountryId { get; set; }

        [Display(Name = "Ciudad / Sede de Origen")]
        public int? CityId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200)]
        [Display(Name = "Nombre del Activo")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Marca")]
        public string? Brand { get; set; }

        [StringLength(100)]
        [Display(Name = "Modelo")]
        public string? Model { get; set; }

        // ========================================
        // ESPECIFICACIONES
        // ========================================

        [Display(Name = "Vida Útil Estimada (años)")]
        [Range(0, 100)]
        public int? UsefulLifeYears { get; set; }

        [StringLength(2000)]
        [Display(Name = "Descripción / Especificaciones")]
        public string? Description { get; set; }

        // ========================================
        // AUDITORÍA (Se mantiene original)
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
        // PROPIEDADES DE NAVEGACIÓN
        // ========================================

        // Quitamos EquipmentType porque ahora es un Enum
        public virtual Country? Country { get; set; }

        public virtual City? City { get; set; }

        public virtual User? CreatedBy { get; set; }

        public virtual User? ModifiedBy { get; set; }

        public virtual ICollection<EquipmentUnit>? Units { get; set; }
    }
}