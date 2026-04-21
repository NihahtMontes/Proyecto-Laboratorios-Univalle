using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Catálogo de puntos de verificación técnica. Se gestiona desde la DB.
    /// </summary>
    public class VerificationCheckItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        [Display(Name = "Descripción del Punto de Control")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Categoría / Grupo")]
        public string? Category { get; set; }

        [Display(Name = "Orden de Visualización")]
        public int Order { get; set; }

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        // Navegación
        public virtual ICollection<VerificationCheckResult> Results { get; set; } = [];
    }
}
