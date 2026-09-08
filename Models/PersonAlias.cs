using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class PersonAlias
    {
        [Key]
        public long Id { get; set; }
        public int PersonId { get; set; }

        [Required, StringLength(300)]
        public string Alias { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string NormalizedAlias { get; set; } = string.Empty;

        public bool IsPreferred { get; set; }

        [StringLength(100)]
        public string? Source { get; set; }

        [ForeignKey(nameof(PersonId))]
        public virtual Person? Person { get; set; }
    }
}
