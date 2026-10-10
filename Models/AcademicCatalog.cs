using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Grupo (paralelo) de una materia en una gestión: docente, laboratorio y horario habituales.
    /// Equivale a GruposMateria de la base estructural del cliente.
    /// </summary>
    public class CourseGroup : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Código")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Gestión")]
        public int ManagementId { get; set; }
        [ForeignKey("ManagementId")]
        public virtual Management? Management { get; set; }

        [Display(Name = "Materia")]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Grupo")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Docente")]
        public int TeacherPersonId { get; set; }
        [ForeignKey("TeacherPersonId")]
        public virtual Person? TeacherPerson { get; set; }

        [Display(Name = "Laboratorio habitual")]
        public int? LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public virtual Laboratory? Laboratory { get; set; }

        /// <summary>Grupo de estudiantes (carrera + semestre + letra) al que pertenece esta materia. Null en grupos antiguos sin carrera/semestre.</summary>
        [Display(Name = "Grupo")]
        public int? StudentGroupId { get; set; }
        [ForeignKey("StudentGroupId")]
        public virtual StudentGroup? StudentGroup { get; set; }

        /// <summary>Día habitual: 1 = lunes ... 6 = sábado.</summary>
        [Range(1, 6)]
        [Display(Name = "Día")]
        public int? Weekday { get; set; }

        [Display(Name = "Hora de inicio")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "Hora de fin")]
        public TimeSpan? EndTime { get; set; }

        [Range(0, 1000)]
        [Display(Name = "Estudiantes")]
        public int? StudentCount { get; set; }

        [Display(Name = "Demostrativa")]
        public bool IsDemonstrative { get; set; }

        /// <summary>Factor de escalamiento de cantidades de la receta (varía por grupo).</summary>
        [Range(0, 1000)]
        [Display(Name = "Factor")]
        public int? ScalingFactor { get; set; }

        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>Horario semanal de clases del grupo (un registro por día y periodo).</summary>
        public virtual ICollection<CourseGroupScheduleSlot> ScheduleSlots { get; set; } = new List<CourseGroupScheduleSlot>();
    }

    /// <summary>
    /// Periodo de clase del horario semanal de un grupo: día (1 = lunes ... 6 = sábado) + periodo académico.
    /// La gestión se hereda del grupo. Es configuración: al quitar una celda del horario se elimina su registro.
    /// </summary>
    public class CourseGroupScheduleSlot : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int CourseGroupId { get; set; }
        [ForeignKey("CourseGroupId")]
        public virtual CourseGroup? CourseGroup { get; set; }

        /// <summary>1 = lunes ... 6 = sábado.</summary>
        [Range(1, 6)]
        public int Weekday { get; set; }

        public int ClassPeriodId { get; set; }
        [ForeignKey("ClassPeriodId")]
        public virtual ClassPeriod? ClassPeriod { get; set; }

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    /// <summary>Práctica del catálogo de una materia (P1, P4, E3P...). Equivale a Practicas del cliente.</summary>
    public class Practice : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Código")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Materia")]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Nombre")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Texto libre: P1, P4, E3P, PE3P... Se conserva por el Excel; no se captura en pantalla.</summary>
        [StringLength(20)]
        [Display(Name = "N.º de práctica")]
        public string? Number { get; set; }

        [StringLength(2000)]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [StringLength(40)]
        [Display(Name = "Estado de cobertura")]
        public string? CoverageStatus { get; set; }

        [StringLength(60)]
        [Display(Name = "Método de cobertura")]
        public string? CoverageMethod { get; set; }

        [StringLength(500)]
        [Display(Name = "Fuente de referencia")]
        public string? SourceReference { get; set; }

        [StringLength(2000)]
        [Display(Name = "Observaciones")]
        public string? Observations { get; set; }

        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        public virtual ICollection<PracticeRequirement> Requirements { get; set; } = [];

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    /// <summary>
    /// Kit de la materia: artículos (utensilios y equipos) y cantidades que la materia usa habitualmente.
    /// Es un atajo para cargar requerimientos L-3; los equipos van como cantidad del artículo vinculado al catálogo.
    /// </summary>
    public class SubjectKitItem : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [Display(Name = "Artículo")]
        public int ArticleId { get; set; }
        [ForeignKey("ArticleId")]
        public virtual Article? Article { get; set; }

        [Range(0.01, 999999)]
        [Display(Name = "Cantidad")]
        public decimal Quantity { get; set; }

        /// <summary>La cantidad se escala por estudiantes ÷ factor del grupo, igual que la receta.</summary>
        [Display(Name = "Escalable")]
        public bool IsScalable { get; set; }

        [StringLength(500)]
        [Display(Name = "Observaciones")]
        public string? Observations { get; set; }

        /// <summary>Retiro lógico de la línea (preserva historial).</summary>
        public bool IsRemoved { get; set; }

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    /// <summary>Valores esperados de Practice.CoverageStatus (cobertura de utensilios de la receta).</summary>
    public static class PracticeCoverageStatus
    {
        public const string Covered = "Cubierta";
        public const string Partial = "Parcial";
        public const string Pending = "Pendiente";

        public static readonly string[] All = [Covered, Partial, Pending];
    }

    /// <summary>"Receta" de una práctica: artículo y cantidad requerida. Equivale a RequerimientosPractica.</summary>
    public class PracticeRequirement : IAuditable
    {
        [Key]
        public int Id { get; set; }

        public int PracticeId { get; set; }
        [ForeignKey("PracticeId")]
        public virtual Practice? Practice { get; set; }

        /// <summary>RequerimientoCodigo de la fuente (trazabilidad e idempotencia de la importación).</summary>
        [StringLength(50)]
        public string? Code { get; set; }

        [Display(Name = "Artículo")]
        public int ArticleId { get; set; }
        [ForeignKey("ArticleId")]
        public virtual Article? Article { get; set; }

        /// <summary>Cantidad requerida; NULL si la fuente no la trae (se registra incidencia, no se inventa).</summary>
        [Range(0.01, 999999)]
        [Display(Name = "Cantidad")]
        public decimal? Quantity { get; set; } = 1;

        /// <summary>La cantidad depende del número de estudiantes del grupo.</summary>
        [Display(Name = "Escalable")]
        public bool IsScalable { get; set; }

        [StringLength(40)]
        [Display(Name = "Estado de validación")]
        public string? ValidationStatus { get; set; }

        [StringLength(1000)]
        [Display(Name = "Observaciones")]
        public string? Observations { get; set; }

        /// <summary>Retiro lógico de la línea de receta (preserva historial).</summary>
        public bool IsRemoved { get; set; }

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    /// <summary>Categoría de artículos (utensilios, vajilla, consumibles...).</summary>
    public class ArticleCategory
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Código")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Display(Name = "Categoría")]
        public string Name { get; set; } = string.Empty;

        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Unidad de medida de artículos (PZA, JUEGO, KG...). Equivale a UnidadesMedida.</summary>
    public class MeasurementUnit
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Código")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(60)]
        [Display(Name = "Unidad")]
        public string Name { get; set; } = string.Empty;

        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Existencia disponible de un artículo en un laboratorio (el Economato es un laboratorio de tipo almacén).</summary>
    public class ArticleStock : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Laboratorio")]
        public int LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public virtual Laboratory? Laboratory { get; set; }

        [Display(Name = "Artículo")]
        public int ArticleId { get; set; }
        [ForeignKey("ArticleId")]
        public virtual Article? Article { get; set; }

        [Range(0, 999999)]
        [Display(Name = "Cantidad disponible")]
        public decimal AvailableQuantity { get; set; }

        [Column(TypeName = "date")]
        [Display(Name = "Fecha de actualización")]
        public DateTime? UpdatedOn { get; set; }

        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];
    }
}
