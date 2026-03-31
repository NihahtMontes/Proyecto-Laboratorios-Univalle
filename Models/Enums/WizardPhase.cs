namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    /// <summary>
    /// Representa las 6 fases exactas de la Ronda de Mantenimiento.
    /// </summary>
    public enum WizardPhase
    {
        /// <summary>
        /// Paso 1: L6 Verificación.
        /// </summary>
        Verification = 1,

        /// <summary>
        /// Paso 2: L7 Solicitud Técnica.
        /// </summary>
        TechnicalRequest = 2,

        /// <summary>
        /// Paso 3: L8 Mantenimiento.
        /// </summary>
        Maintenance = 3,

        /// <summary>
        /// Paso 4: L3 Salida (Departure).
        /// </summary>
        Exit = 4,

        /// <summary>
        /// Paso 5: Kardex (Actualización de Historial/Inventario).
        /// </summary>
        Kardex = 5,

        /// <summary>
        /// Paso 6: Desembolso (Solicitud de Adquisición).
        /// </summary>
        Disbursement = 6
    }
}
