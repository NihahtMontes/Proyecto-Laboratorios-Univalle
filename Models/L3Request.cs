using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Laboratorios_Univalle.Models.Interfaces;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class L3Request : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }
        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; } = null!;

        [Required]
        public int GroupId { get; set; }
        [ForeignKey("GroupId")]
        public Group Group { get; set; } = null!;

        // FK to AspNetUsers
        [Required]
        public int TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public User Teacher { get; set; } = null!;

        [Required]
        public int LaboratoryId { get; set; }
        [ForeignKey("LaboratoryId")]
        public Laboratory Laboratory { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string PracticeName { get; set; } = string.Empty;

        public int AcademicWeek { get; set; }

        public DateTime ScheduledDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pendiente";

        public ICollection<L3RequestDetail> Details { get; set; } = new List<L3RequestDetail>();
        public ICollection<L5Incident> Incidents { get; set; } = new List<L5Incident>();

        // IAuditable implementation
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public int? CreatedById { get; set; }
        public int? ModifiedById { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
