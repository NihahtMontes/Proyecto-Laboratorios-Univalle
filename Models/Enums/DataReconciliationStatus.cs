namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum DataReconciliationStatus
    {
        Pending = 0,
        Match = 1,
        SourceOnly = 2,
        DatabaseOnly = 3,
        Conflict = 4,
        LegacyInferred = 5,
        PendingClient = 6
    }
}
