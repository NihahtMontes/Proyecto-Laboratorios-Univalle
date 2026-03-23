namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum ManagementStatus
    {
        Activo,      // DASHBOARD: Only ONE can be active
        Inactivo,    // Ready but not current
        Terminado,   // Historical (Dashboard Clone style)
        Eliminado    // Logical Delete
    }
}
