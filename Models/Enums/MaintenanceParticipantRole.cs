using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum MaintenanceParticipantRole
    {
        [Display(Name = "Técnico")]
        Technician = 1,

        [Display(Name = "Proveedor externo")]
        ExternalProvider = 2,

        [Display(Name = "Asistente")]
        Assistant = 3,

        [Display(Name = "Supervisor")]
        Supervisor = 4
    }
}
