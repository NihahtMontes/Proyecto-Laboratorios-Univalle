using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Laboratorios_Univalle.Models
{
    public class L3RequestDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int L3RequestId { get; set; }
        [ForeignKey("L3RequestId")]
        public L3Request L3Request { get; set; } = null!;

        [Required]
        public int EquipmentUnitId { get; set; }
        [ForeignKey("EquipmentUnitId")]
        public EquipmentUnit EquipmentUnit { get; set; } = null!;

        public int ExpectedQuantity { get; set; } = 1;
        public int DeliveredQuantity { get; set; } = 0;
        public int ReturnedQuantity { get; set; } = 0;
    }
}
