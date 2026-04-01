using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum ManagementStatus
    {
        [Display(Name = "Active")]
        Active = 0,      // DASHBOARD: Only ONE can be active

        [Display(Name = "Inactive")]
        Inactive = 1,    // Ready but not current

        [Display(Name = "Completed")]
        Completed = 2,   // Historical (Dashboard Clone style)

        [Display(Name = "Deleted")]
        Deleted = 99     // Logical Delete
    }
}
