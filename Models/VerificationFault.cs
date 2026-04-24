using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class VerificationFault : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VerificationId { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Descripción de la Falla")]
        public string Description { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        // ========================================
        // AUDIT
        // ========================================
        public int? CreatedById { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedById { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        // ========================================
        // NAVIGATION
        // ========================================
        [ForeignKey("VerificationId")]
        public virtual Verification? Verification { get; set; }
        
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }
    }
}
