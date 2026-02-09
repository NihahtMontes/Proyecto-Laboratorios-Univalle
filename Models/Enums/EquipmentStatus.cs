using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum EquipmentStatus
    {
        [Display(Name = "Operativo")]
        Operational = 0,

        [Display(Name = "En Mantenimiento")]
        UnderMaintenance = 1,

        [Display(Name = "Fuera de Servicio")]
        OutOfService = 2,

        [Display(Name = "En Reparación")]
        InRepair = 5,

        [Display(Name = "De baja")]
        Decommissioned = 3,

        [Display(Name = "Desmantelado")]
        Dismantled = 4,

        [Display(Name = "Eliminado")]
        Deleted = 99
    }
}
