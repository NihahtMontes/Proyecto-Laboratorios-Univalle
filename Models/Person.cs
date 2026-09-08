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

        [StringLength(30)]
        [Display(Name = "Código de Actor")]
        public string? ActorCode { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public GeneralStatus Status { get; set; } = GeneralStatus.Activo;

        [Required]
        [Display(Name = "Categoría")]
        public PersonCategory Category { get; set; } = PersonCategory.Otro;

        
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
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

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

        public int? ImportBatchId { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        [NotMapped]
        [Display(Name = "Nombre / Razón Social")]
        public virtual string FullName => "Ficha de Persona";

        public virtual ICollection<Departure>? Departures { get; set; }
        public virtual ICollection<PersonAlias> Aliases { get; set; } = [];
        public virtual ICollection<PersonRoleAssignment> RoleAssignments { get; set; } = [];
        public virtual ICollection<MaintenanceParticipant> MaintenanceParticipations { get; set; } = [];
        public virtual ICollection<Request> RequestedRequests { get; set; } = [];
        public virtual ICollection<Verification> ResponsibleVerifications { get; set; } = [];
        public virtual ICollection<CostDetail> ProvidedCostDetails { get; set; } = [];
        public virtual ICollection<ManagementPlan> ResponsibleManagementPlans { get; set; } = [];
        public virtual ICollection<MaintenancePlan> ResponsibleMaintenancePlans { get; set; } = [];

        [ForeignKey(nameof(ImportBatchId))]
        public virtual ImportBatch? ImportBatch { get; set; }
    }
}
