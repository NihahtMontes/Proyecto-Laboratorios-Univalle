using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a physical person (Technician, Employee) separate from system access (User).
    /// </summary>
    public class Person : IAuditable
    {
        [Key]

        //Modificado. Estoy en mi nueva rama
        public int Id { get; set; }

        // ========================================
        // BASIC INFO
        // ========================================
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Solo se permiten letras en el nombre")]
        [Display(Name = "Nombres")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Solo se permiten letras en el apellido")]
        [Display(Name = "Apellidos")]
        public string LastName { get; set; } = string.Empty;

        // ========================================
        // CLASSIFICATION
        // ========================================
        [Required]
        [Display(Name = "Tipo de Personal")]
        public PersonType Type { get; set; } = PersonType.Internal;

        [StringLength(100)]
        [Display(Name = "Cargo / Título")]
        public string? JobTitle { get; set; }

        [StringLength(200)]
        [Display(Name = "Empresa (si es externo)")]
        public string? Company { get; set; }

        // ========================================
        // CONTACT INFO
        // ========================================
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


    }
}
