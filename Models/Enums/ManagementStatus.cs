using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum ManagementStatus
    {
        [Display(Name = "Activa")]
        Active = 0,      // DASHBOARD: Only ONE can be active

        [Display(Name = "Inactiva")]
        Inactive = 1,    // Ready but not current

        [Display(Name = "Completada (Histórico)")]
        Completed = 2,   // Historical (Dashboard Clone style)

        [Display(Name = "Eliminada")]
        Deleted = 99     // Logical Delete
    }
}
