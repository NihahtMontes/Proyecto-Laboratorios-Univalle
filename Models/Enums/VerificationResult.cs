using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    /// <summary>
    /// Simplicación de resultados de verificación a un estado binario (Completado o Pendiente).
    /// </summary>
    public enum VerificationResult
    {
        [Display(Name = "Pendiente")]
        NotChecked = 0,

        [Display(Name = "Realizado")]
        Completed = 1
    }
}
