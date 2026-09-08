using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class PersonRoleAssignment
    {
        [Key]
        public long Id { get; set; }
        public int PersonId { get; set; }
        public PersonOperationalRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime? ValidTo { get; set; }

        [ForeignKey(nameof(PersonId))]
        public virtual Person? Person { get; set; }
    }
}
