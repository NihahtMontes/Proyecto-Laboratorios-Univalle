namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum EquipmentStatus
    {
        Operational = 0,
        UnderMaintenance = 1,
        OnLoan = 2,
        Decommissioned = 3, // Baja
        Dismantled = 4,     // Desmantelado
        Deleted = 99        // Soft Delete
    }
}
