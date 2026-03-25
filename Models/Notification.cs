using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } 

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Message { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ActionUrl { get; set; } 

        [StringLength(50)]
        public string? IconClass { get; set; } 

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Propiedad de navegación
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}