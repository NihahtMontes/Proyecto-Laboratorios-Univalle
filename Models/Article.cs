using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Catálogo de consumibles y artículos no patrimoniales utilizados en L-3.
    /// </summary>
    public class Article : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Código de Artículo")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(200)]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Categoría")]
        public string? Category { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Unidad de Medida")]
        public string UnitOfMeasure { get; set; } = "UNIDAD";

        [Required]
        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        public int? ImportBatchId { get; set; }
        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        [ForeignKey(nameof(ImportBatchId))]
        public virtual ImportBatch? ImportBatch { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey(nameof(ModifiedById))]
        public virtual User? ModifiedBy { get; set; }

        public virtual ICollection<DepartureItem> DepartureItems { get; set; } = [];

        [Display(Name = "Categoría")]
        public int? ArticleCategoryId { get; set; }
        [ForeignKey("ArticleCategoryId")]
        public virtual ArticleCategory? ArticleCategory { get; set; }

        [Display(Name = "Unidad de medida")]
        public int? MeasurementUnitId { get; set; }
        [ForeignKey("MeasurementUnitId")]
        public virtual MeasurementUnit? MeasurementUnit { get; set; }

        [Display(Name = "Tipo")]
        public ArticleType? ArticleType { get; set; }

        /// <summary>Vínculo opcional con el catálogo de equipos (artículos de tipo Equipo).</summary>
        [Display(Name = "Equipo de catálogo")]
        public int? EquipmentId { get; set; }
        [ForeignKey("EquipmentId")]
        public virtual Equipment? Equipment { get; set; }
    }
}
