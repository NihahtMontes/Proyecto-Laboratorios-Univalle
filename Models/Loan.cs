using Proyecto_Laboratorios_Univalle.Models.Enums;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class Loan : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Unidad de Equipamiento")]
        public int EquipmentUnitId { get; set; }

        [Required]
        [Display(Name = "Solicitante / Responsable")]
        public int BorrowerId { get; set; }

        [Required]
        [Display(Name = "Fecha de Préstamo")]
        [DataType(DataType.Date)]
        public DateTime LoanDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Fecha Estimada de Devolución")]
        [DataType(DataType.Date)]
        public DateTime EstimatedReturnDate { get; set; }

        [Display(Name = "Fecha Real de Devolución")]
        [DataType(DataType.DateTime)]
        public DateTime? ActualReturnDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Observaciones de Salida")]
        public string? DepartureObservations { get; set; }

        [StringLength(500)]
        [Display(Name = "Observaciones de Devolución")]
        public string? ReturnObservations { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public LoanStatus Status { get; set; } = LoanStatus.Active;

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
        // NAVIGATION PROPERTIES
        // ========================================
        [ForeignKey("EquipmentUnitId")]
        public virtual EquipmentUnit? EquipmentUnit { get; set; }

        [ForeignKey("BorrowerId")]
        public virtual Person? Borrower { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        [ForeignKey("ModifiedById")]
        public virtual User? ModifiedBy { get; set; }
    }
}
