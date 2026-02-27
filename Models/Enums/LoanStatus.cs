using System.ComponentModel.DataAnnotations;

namespace Proyecto_Laboratorios_Univalle.Models.Enums
{
    public enum LoanStatus
    {
        [Display(Name = "Activo")]
        Active = 0,

        [Display(Name = "Devuelto")]
        Returned = 1,

        [Display(Name = "Vencido")]
        Overdue = 2,

        [Display(Name = "Cancelado")]
        Cancelled = 99
    }
}
