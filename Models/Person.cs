using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a physical person (Technician, Employee) separate from system access (User).
    /// </summary>
    public class Person : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Correo Electrónico")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Phone(ErrorMessage = "Teléfono inválido")]
        [Display(Name = "Teléfono / Celular")]
        public string? PhoneNumber { get; set; }


        // ========================================
        // AUDIT
        // ========================================
        [Display(Name = "Fecha de Registro")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        // Navigation properties for audit (linked to User)
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        [NotMapped]
        [Display(Name = "Nombre / Razón Social")]
        public virtual string FullName => "Ficha de Persona";

        public virtual ICollection<Loan>? Loans { get; set; }
    }
}
