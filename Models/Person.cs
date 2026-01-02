using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a physical person (Technician, Employee) separate from system access (User).
    /// </summary>
    public class Person
    {
        [Key]
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
        // LINKED USER (Optional)
        // ========================================
        [Display(Name = "Usuario de Sistema")]
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        // ========================================
        // AUDIT
        // ========================================
        [Display(Name = "Fecha de Registro")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [NotMapped]
        [Display(Name = "Nombre Completo")]
        public string FullName => $"{FirstName} {LastName}";
    }
}
