using System;

namespace Proyecto_Laboratorios_Univalle.Models.Interfaces
{
    public interface IAuditable
    {
        int? CreatedById { get; set; }
        DateTime CreatedDate { get; set; }
        int? ModifiedById { get; set; }
        DateTime? LastModifiedDate { get; set; }
    }
}
