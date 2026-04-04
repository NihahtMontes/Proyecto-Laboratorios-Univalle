using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Resultado individual de un punto de control para una Verificación concreta.
    /// Reemplaza las columnas [Check] hardcodeadas en Verification.
    /// </summary>
    public class VerificationCheckResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VerificationId { get; set; }

        [Required]
        public int CheckItemId { get; set; }

        [Required]
        [Display(Name = "Resultado")]
        public VerificationResult Result { get; set; } = VerificationResult.NotChecked;

        // Navegación
        [ForeignKey("VerificationId")]
        public virtual Verification? Verification { get; set; }

        [ForeignKey("CheckItemId")]
        public virtual VerificationCheckItem? CheckItem { get; set; }
    }
}
