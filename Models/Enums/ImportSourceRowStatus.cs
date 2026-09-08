namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum ImportSourceRowStatus
    {
        Pending = 0,
        Migrated = 1,
        DuplicatePendingConfirmation = 2,
        Conflict = 3,
        Rejected = 99
    }
}
