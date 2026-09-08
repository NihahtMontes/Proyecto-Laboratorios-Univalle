using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum PersonOperationalRole
    {
        [Display(Name = "Técnico")]
        Technician = 1,

        [Display(Name = "Prestatario")]
        Borrower = 2,

        [Display(Name = "Proveedor externo")]
        ExternalProvider = 3,

        [Display(Name = "Contacto institucional")]
        InstitutionalContact = 4,

        [Display(Name = "Solicitante")]
        Requester = 5,

        [Display(Name = "Responsable")]
        Responsible = 6,

        [Display(Name = "Verificador")]
        Verifier = 7,

        [Display(Name = "Otro")]
        Other = 99
    }
}
