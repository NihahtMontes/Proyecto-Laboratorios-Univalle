using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    /// <summary>
    /// Clasificación de una Salida de Equipo (L3).
    /// </summary>
    public enum DepartureType
    {
        [Display(Name = "Préstamo Externo/Documentado")]
        ExternalLoan = 1,

        [Display(Name = "Préstamo Interno (Misma Universidad)")]
        InternalLoan = 2,

        [Display(Name = "Salida por Mantenimiento Externo")]
        ExternalMaintenance = 3,

        [Display(Name = "Salida por Mantenimiento Interno")]
        InternalMaintenance = 5,

        [Display(Name = "Baja Definitiva")]
        DefinitiveExit = 4
    }
}
