using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Proyecto_Laboratorios_Univalle.Models.Enums;

using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    /// <summary>
    /// Represents a system user with credentials and permissions.
    /// Extends IdentityUser<int> for ASP.NET Core Identity integration.
    /// </summary>
    public class User : IdentityUser<int>, IAuditable
    {
        // IdentityUser includes: Id, UserName, Email, PasswordHash, PhoneNumber, etc.

        // ========================================
        // PERSONAL INFORMATION
        // ========================================
        [Required(ErrorMessage = "Los nombres son obligatorios")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Solo se permiten letras en los nombres")]
        [Display(Name = "Nombres")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El primer apellido es obligatorio")]
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Solo se permiten letras en el apellido")]
        [Display(Name = "Primer Apellido")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Solo se permiten letras en el apellido")]
        [Display(Name = "Segundo Apellido")]
        public string? SecondLastName { get; set; }

        [Required(ErrorMessage = "El CI es obligatorio")]
        [StringLength(8, ErrorMessage = "El CI no puede tener más de 8 números")]
        [RegularExpression(@"^[0-9A-Z-]*$", ErrorMessage = "Formato de cédula inválido")]

        [Display(Name = "Cédula de Identidad")]
        public string IdentityCard { get; set; } = string.Empty;

        // ========================================
        // ROLE AND STATUS
        // ========================================
        
        [Required]
        [Display(Name = "Rol")]
        public UserRole Role { get; set; } = UserRole.Supervisor;

        [Required]
        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        // ========================================
        // WORK INFORMATION
        // ========================================
        [StringLength(100)]
        [Display(Name = "Cargo")]
        public string? Position { get; set; }

        [StringLength(100)]
        [Display(Name = "Departamento")]
        public string? Department { get; set; }

        [Display(Name = "Fecha de Ingreso")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        // ========================================
        // AUDIT
        // ========================================
        [Display(Name = "Creado Por")]
        public int? CreatedById { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modificado Por")]
        public int? ModifiedById { get; set; }

        [Display(Name = "Última Modificación")]
        public DateTime? LastModifiedDate { get; set; }

        // ========================================
        // NAVIGATION PROPERTIES (Audit)
        // ========================================
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }

        // ========================================
        // INVERSE RELATIONSHIPS
        // ========================================

        // As Responsible Technician (renaming classes in relationships proactively, assuming Maintenance exists)
        // Note: Using 'Maintenance' class name although it might not exist yet (Refactoring dependency ring)
        // We will rename Mantenimiento to Maintenance shortly.
        
        // public virtual ICollection<Maintenance>? MaintenancesPerformed { get; set; }
        // public virtual ICollection<MaintenancePlan>? AssignedPlans { get; set; }
        // public virtual ICollection<Request>? CreatedRequests { get; set; }

        // Commenting out relationships temporarily to allow compilation of THIS file 
        // until other models are renamed. Or I can leave them if I rename everything fast.
        // Let's use the OLD class names for now to avoid breaking intellisense if I don't rename all at once?
        // NO, the plan is to rename everything. I should use the NEW names.

        // ========================================
        // CALCULATED PROPERTIES
        // ========================================

        /// <summary>
        /// Formatted full name
        /// </summary>
        [NotMapped]
        [Display(Name = "Nombre Completo")]
        public string FullName
        {
            get
            {
                var name = $"{FirstName} {LastName}";
                if (!string.IsNullOrEmpty(SecondLastName))
                {
                    name += $" {SecondLastName}";
                }
                return name;
            }
        }

        /// <summary>
        /// User initials
        /// </summary>
        [NotMapped]
        [Display(Name = "Iniciales")]
        public string Initials
        {
            get
            {
                var initials = "";
                if (!string.IsNullOrEmpty(FirstName)) initials += FirstName[0];
                if (!string.IsNullOrEmpty(LastName)) initials += LastName[0];
                if (!string.IsNullOrEmpty(SecondLastName)) initials += SecondLastName[0];
                return initials.ToUpper();
            }
        }
    }
}
