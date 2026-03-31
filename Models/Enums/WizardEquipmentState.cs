namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    /// <summary>
    /// Representa el estado actual de un equipo dentro del Wizard.
    /// Ayuda a saber si está esperando acción o si ya terminó una fase.
    /// </summary>
    public enum WizardEquipmentState
    {
        PendingVerification = 1,
        VerifiedGood = 2,           // Aprobó L6 sin fallas
        AwaitingRequest = 3,        // Falló L6, requiere L7
        AwaitingMaintenance = 4,    // Tiene L7, espera L8
        InMaintenance = 5,          // L8 en curso
        AwaitingDeparture = 6,      // Terminado L8, requiere Salida L3
        AwaitingKardex = 7,         // Terminado L3, falta actualizar Kardex
        Completed = 8               // Llegó a Desembolso y cerró
    }
}
